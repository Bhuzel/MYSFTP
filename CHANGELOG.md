# MYSFTP v2.0.4 — SSH Connection Multiplexing & Instant Socket Engine

## ✨ Apa yang Baru & Diperbaiki di Versi 2.0.4

### 1. ⚡ SSH Connection Multiplexing (`ControlMaster` & `ControlPersist`)
- **Shared Multiplexed Socket:** Seluruh panggilan `ssh.exe` selama satu sesi koneksi kini berbagi satu socket kontrol terpusat (`-o ControlMaster=auto -o ControlPath=... -o ControlPersist=10m`).
- **Zero Handshake Cost:** Panggilan pertama melakukan autentikasi awal dan membuka socket kontrol; seluruh aksi navigasi, listing folder, read/write berkas, dan eksekusi command setelahnya langsung menumpang pada socket yang sama tanpa perlu mengulang TCP 3-way handshake dan negosiasi key SSH dari nol.
- **Graceful Socket Teardown:** Saat koneksi diputuskan atau aplikasi ditutup, socket multiplexing dimatikan secara bersih melalui `-O exit`.

### 2. ⚡ Ultra-Fast Directory Navigation & Zero-Disk-Churn
- **AskPass Caching:** Menghilangkan pembuatan/penghapusan file temporer berulang di disk pada setiap command SSH, menghapus latency scan Windows Defender sehingga membuka server dan folder terasa sangat cepat.
- **Optimistic In-Memory Directory Cache:** Folder yang sudah pernah dibuka akan ditampilkan seketika (**0ms**) saat navigasi bolak-balik, lalu secara cerdas di-refresh di latar belakang.

### 3. 🌐 Universal Multi-Format Linux Directory Parser
- Menggunakan parser cerdas multi-format yang kompatibel 100% dengan semua distro Linux (Debian, Ubuntu, CentOS, RHEL, Alpine, BusyBox, OpenWrt, Arch, BSD, macOS, dll.).
- Menjamin seluruh direktori (termasuk `/root`) terbaca lengkap tanpa masalah parsing kolom atau locale bahasa.

### 4. 🗜️ Robust In-Memory ZIP Archiving Engine
- Header dan Central Directory ZIP diselaraskan secara konsisten sesuai standar PKWare, mencegah korupsi file 0-byte.
- Semua nama berkas/folder berspasi di-quote secara aman `'...'` saat pengompresan batch atau folder tunggal.

---

# MYSFTP v2.0.3 — High-Performance & Universal Linux Suite
- Optimalisasi askpass caching, universal directory parser, dan instant optimistic cache.

---

# MYSFTP v2.0.2 — Hotfix JSON Parsing & Robust ZIP Archiving Engine
- Perbaikan parsing `size` integer JSON dan delimiter base64 untuk isolasi banner SSH.

---

# MYSFTP v2.0.1 — Hotfix & Solid ZIP Archiving Engine
- Engine ZipPacker C# Internal, Luxury Glassmorphism Modals, Batch actions, dan terminal responsif.

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.0.4-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.0.4.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
