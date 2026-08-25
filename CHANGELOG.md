# MYSFTP v2.1.0 — Universal Tar.Gz Archive Streaming & Non-Blocking Terminal Core

## ✨ Apa yang Baru & Diperbaiki di Versi 2.1.0

### 1. 🗜️ Download Folder Universal Tar.Gz (100% Cepat & Bebas Korupsi)
- **Standard Linux Native Tar.Gz Archiving:** Menggunakan engine `tar -czf` yang tersedia bawaan di 100% seluruh distro Linux (Ubuntu, Debian, CentOS, AlmaLinux, Alpine, dll.).
- **Zero Overhead & Zero Delay:** Kompresi langsung mengeksekusi direktori target secara instan (<100ms) tanpa dependensi package zip tambahan.
- **Direct SFTP Stream:** Berkas arsip `.tar.gz` dialirkan langsung via socket SFTP stream 64KB ke peramban tanpa menghabiskan RAM klien dan tanpa risiko 0-Byte.

### 2. 💻 SSH Termius Console Anti-Timeout & Responsif Seketika
- **Non-Blocking Dynamic Stream Drainer:** Eksekusi perintah membaca stdout dan stderr secara real-time. Begitu respon selesai atau hening selama 600ms, output langsung dikembalikan seketika tanpa menunggu timeout 30 detik.
- **Auto-Tuned Continuous Logs:** Perintah `pm2 logs` otomatis diarahkan ke mode non-blocking (`--lines 50 --nostream`) sehingga menampilkan 50 log terkini secara instan tanpa freeze.
- **Instant Monospace Prompt:** Prompt interaktif `root@host:~# [command]` langsung tampil di layar terminal dengan penataan warna ANSI yang rapi.

---

# MYSFTP v2.0.9 — Direct Server-Side Archive Streaming & Instant Terminal Core
# MYSFTP v2.0.8 — SSH Termius Terminal Real-Time Engine Fix
# MYSFTP v2.0.7 — Hotfix Self-Contained SSH.NET Engine
# MYSFTP v2.0.6 — Native SSH.NET & Persistent Session Core

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.1.0-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.1.0.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
