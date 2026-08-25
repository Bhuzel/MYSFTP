# MYSFTP v2.0.9 — Direct Server-Side Archive Streaming & Instant Terminal Core

## ✨ Apa yang Baru & Diperbaiki di Versi 2.0.9

### 1. 🗜️ Solusi Tuntas Download ZIP Berkas Besar (0-Byte & Korupsi Teratasi)
- **Direct Server-Side Archive Generation:** Arsip ZIP dibuat langsung di server remote menggunakan engine native Linux (`zip`, `python3 make_archive`, atau `tar`) tanpa membebani memori RAM PC klien.
- **Zero Memory Allocation Streaming:** Mengunduh arsip dan berkas berukuran besar (dari MB hingga puluhan GB) secara streaming langsung via socket SFTP (`sftpClient.DownloadFile`) dengan alokasi chunk buffer 64KB.
- **Valid Header & Content-Length:** Ukuran berkas terdeteksi 100% akurat oleh browser, proses unduh stabil, dan berkas ZIP tidak lagi 0 Byte maupun korup.
- **Auto Clean-Up:** Berkas arsip sementara di `/tmp/` server remote langsung dibersihkan secara otomatis setelah unduhan selesai dialirkan.

### 2. 💻 SSH Termius Console Eksekusi Instan & Dijamin Tampil
- **Direct Synchronous Command Pipeline:** Setiap perintah (seperti `ls`, `pm2 status`, `df -h`, `pm2 logs`, dll.) dieksekusi secara instan lewat kanal persistent SSH `SshClient.CreateCommand` dan langsung mengembalikan output lengkap (stdout + stderr) tanpa jeda polling.
- **Preset Quick-Chips Teroptimasi:** Chip `pm2 logs` telah disesuaikan menjadi mode non-stream (`--lines 40 --nostream`) agar responsif seketika.
- **Instant Prompt Visual Feedback:** Menampilkan prompt server (`root@host:~# [command]`) langsung saat tombol Kirim atau tombol Enter ditekan.

---

# MYSFTP v2.0.8 — SSH Termius Terminal Real-Time Engine Fix
# MYSFTP v2.0.7 — Hotfix Self-Contained SSH.NET Engine
# MYSFTP v2.0.6 — Native SSH.NET & Persistent Session Core

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.0.9-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.0.9.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
