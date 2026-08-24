package com.yoursftp.app.transfer

import android.content.Context
import com.yoursftp.app.data.Connection
import java.util.concurrent.ConcurrentHashMap

/**
 * Termius-Style Intelligent Session Pool & Cache.
 * Mempertahankan sesi SFTP/SSH yang aktif selama 30 menit tanpa perlu login ulang.
 */
object SessionManager {

    private data class CachedSession(
        val client: FileClient,
        var lastAccessTime: Long
    )

    private val sessionCache = ConcurrentHashMap<Long, CachedSession>()
    private const val SESSION_TIMEOUT_MS = 30 * 60 * 1000L // 30 Menit Keep-Alive

    @Volatile
    var client: FileClient? = null
        private set

    @Volatile
    var editingClient: FileClient? = null

    fun set(client: FileClient) {
        this.client = client
    }

    fun require(): FileClient =
        editingClient ?: client ?: sessionCache.values.firstOrNull()?.client ?: throw IllegalStateException("Tidak ada sesi aktif")

    /**
     * Mengambil sesi aktif dari cache jika ada dan masih valid (< 30 menit),
     * atau membuat koneksi baru jika belum ada.
     */
    fun getOrCreate(context: Context, conn: Connection): FileClient {
        cleanupExpired()
        val cached = sessionCache[conn.id]
        if (cached != null && cached.client.isConnected && (System.currentTimeMillis() - cached.lastAccessTime < SESSION_TIMEOUT_MS)) {
            cached.lastAccessTime = System.currentTimeMillis()
            this.client = cached.client
            return cached.client
        }

        // Buat sesi baru
        val newClient = FileClientFactory.create(context, conn)
        newClient.connect()
        sessionCache[conn.id] = CachedSession(newClient, System.currentTimeMillis())
        this.client = newClient
        return newClient
    }

    fun getActive(connectionId: Long): FileClient? {
        cleanupExpired()
        val cached = sessionCache[connectionId]
        if (cached != null && cached.client.isConnected && (System.currentTimeMillis() - cached.lastAccessTime < SESSION_TIMEOUT_MS)) {
            cached.lastAccessTime = System.currentTimeMillis()
            return cached.client
        }
        return null
    }

    fun touch(connectionId: Long) {
        sessionCache[connectionId]?.lastAccessTime = System.currentTimeMillis()
    }

    fun disconnect(connectionId: Long) {
        val session = sessionCache.remove(connectionId)
        if (client == session?.client) client = null
        runCatching { session?.client?.disconnect() }
    }

    fun disconnectAll() {
        for ((_, session) in sessionCache) {
            runCatching { session.client.disconnect() }
        }
        sessionCache.clear()
        client = null
        editingClient = null
    }

    private fun cleanupExpired() {
        val now = System.currentTimeMillis()
        val iterator = sessionCache.entries.iterator()
        while (iterator.hasNext()) {
            val entry = iterator.next()
            if (now - entry.value.lastAccessTime > SESSION_TIMEOUT_MS || !entry.value.client.isConnected) {
                if (client == entry.value.client) client = null
                runCatching { entry.value.client.disconnect() }
                iterator.remove()
            }
        }
    }
}
