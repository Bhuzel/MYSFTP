# MYSFTP v2.0.6 — Native SSH.NET & Persistent Session Core

## ✨ Apa yang Baru & Diperbaiki di Versi 2.0.6

### 1. 🚀 Native SSH.NET Persistent Architecture (Renci.SshNet)
- **Single Persistent SSH & SFTP Channel:** Menghubungkan satu sesi `SshClient` (eksekusi perintah & terminal) dan `SftpClient` (transfer file) secara persisten.
- **Zero Subprocess Overhead:** Tidak lagi men-spawn proses `ssh.exe` baru di setiap klik navigasi, buka file, atau aksi remote.
- **Auto Keep-Alive:** Interval keep-alive 15 detik aktif secara otomatis untuk mencegah pemutusan koneksi oleh firewall/NAT saat idle.

### 2. ⚡ Native SFTP Directory Listing (`SSH_FXP_READDIR`)
- Listing folder kini menggunakan protokol biner SFTP murni (`sftpClient.ListDirectory()`).
- Atribut ukuran, tipe direktori, dan tanggal modifikasi langsung berupa tipe data bawaan (`ISftpFile`), 100% akurat tanpa parsing teks atau dependensi `ls` di server remote.

### 3. 📄 Direct Binary Stream Archiving & File I/O
- `ReadRemoteBytes` dan `WriteRemoteBytes` mentransfer byte data murni langsung lewat socket SFTP.
- Unduhan arsip `.tar.gz` di-stream langsung secara biner tanpa overhead konversi Base64.

### 4. 💻 Seamless ShellStream Interactive Terminal
- Terminal SSH Termius ditenagai oleh `ShellStream` terintegrasi dengan dukungan xterm-256color dan pembatalan Ctrl+C instan.

---

# MYSFTP v2.0.5 — Universal Linux Suite
- Optimalisasi askpass caching dan universal directory parser.

---

# MYSFTP v2.0.1 - v2.0.4 — Hotfix & Archiving Updates
- Rilis bertahap Glassmorphism Modals, in-memory ZipPacker, dan UI enhancements.

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.0.6-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.0.6.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
