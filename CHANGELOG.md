# MYSFTP v2.0.7 — Hotfix Self-Contained SSH.NET Engine

## ✨ Apa yang Baru & Diperbaiki di Versi 2.0.7

### 1. 🛡️ Hotfix: Eliminasi Error `Microsoft.Bcl.AsyncInterfaces`
- **Penyebab:** Pada paket SSH.NET modern tertentu (net462), terdapat dependensi eksternal terhadap DLL `Microsoft.Bcl.AsyncInterfaces`, `System.Threading.Tasks.Extensions`, dan `System.Runtime.CompilerServices.Unsafe` yang menyebabkan crash `Could not load file or assembly` saat aplikasi dijalankan di PC Windows.
- **Solusi:** Menggunakan library `Renci.SshNet.dll` (net40) murni yang **100% self-contained tanpa dependensi eksternal apapun**, langsung berjalan mulus di seluruh PC Windows (Windows 10, 11, 8.1, 7).

### 2. ⚡ Native SSH.NET & Persistent SFTP Session
- Satu koneksi persisten untuk seluruh aktivitas browser berkas, editor, dan terminal SSH.
- Navigasi folder instan dengan protokol biner `SSH_FXP_READDIR` tanpa proses `ssh.exe` berulang.

---

# MYSFTP v2.0.6 — Native SSH.NET & Persistent Session Core
- Migrasi awal ke arsitektur SSH.NET.

---

# MYSFTP v2.0.5 — Universal Linux Suite
- Optimalisasi askpass caching dan universal directory parser.

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.0.7-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.0.7.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
