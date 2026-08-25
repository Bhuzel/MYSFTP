# MYSFTP v2.0.8 — SSH Termius Terminal Real-Time Engine Fix

## ✨ Apa yang Baru & Diperbaiki di Versi 2.0.8

### 1. 💻 Terminal SSH Termius Berjalan 100% Real-Time
- **Dedicated Background Reader Thread:** Terminal SSH kini dilengkapi dengan thread pembaca stream non-blocking mandiri yang terus-menerus membaca dan mengalirkan setiap byte output dari server secara instan ke layar terminal.
- **Auto-Flush & LF Formatting:** Setiap perintah yang diketik langsung di-encode UTF-8, diakhiri baris baru standar Linux (`\n`), dan di-flush seketika lewat socket stream PTY.
- **Auto-Recovery & Fallback:** Apabila stream interaktif terputus, backend otomatis memulihkan sesi atau menjalankan perintah via mode `RunCommand` tanpa kehilangan output.
- **Visual Feedback Instan:** Setiap perintah yang diketik atau dipilih via chip (seperti `pm2 status`, `ls -la`, dll.) langsung ditampilkan di layar bersama respon server.

---

# MYSFTP v2.0.7 — Hotfix Self-Contained SSH.NET Engine
- Eliminasi error `Microsoft.Bcl.AsyncInterfaces` dengan paket self-contained .NET 4.0 murni.

---

# MYSFTP v2.0.6 — Native SSH.NET & Persistent Session Core
- Migrasi awal ke arsitektur SSH.NET.

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.0.8-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.0.8.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
