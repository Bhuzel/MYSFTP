# MYSFTP v2.1.2 — Real-Time Live Streaming Terminal Core & Full Control Character Encoder

## ✨ Apa yang Baru & Diperbaiki di Versi 2.1.2

### 1. ⚡ Real-Time Live Streaming Terminal (Live Execution Display)
- **Direct HTTP Chunked ReadableStream Engine:** Seluruh eksekusi perintah di terminal (`wget ... | bash`, `bench.sh`, `speedtest`, `pm2 logs`, `apt update`, `git clone`, dll.) dialirkan byte-per-byte secara **real-time** ke layar terminal saat perintah sedang berlangsung.
- **Zero Freeze & Live Output:** Tampilan output tidak lagi menunggu script selesai, melainkan langsung mengetikkan baris demi baris secara interaktif seperti terminal Linux asli.

### 2. 🛡️ RFC 8259 Compliant Control Character Encoder
- **Fixed Bad Control Character JSON Error:** Mengatasi tuntas error `Bad control character in string literal in JSON` yang sebelumnya terjadi saat script Linux menghasilkan karakter ANSI escape, backspace (`\x08`), bell (`\x07`), atau karakter kontrol biner `< 0x20`.
- Semua karakter kontrol sekarang di-encode secara sempurna dengan standar `\u00XX` sehingga aman diproses oleh JSON parser peramban.

---

# MYSFTP v2.1.1 — Persistent Terminal Working Directory & Seamless CD Navigation
# MYSFTP v2.1.0 — Universal Tar.Gz Archive Streaming & Non-Blocking Terminal Core
# MYSFTP v2.0.9 — Direct Server-Side Archive Streaming & Instant Terminal Core
# MYSFTP v2.0.8 — SSH Termius Terminal Real-Time Engine Fix
# MYSFTP v2.0.7 — Hotfix Self-Contained SSH.NET Engine
# MYSFTP v2.0.6 — Native SSH.NET & Persistent Session Core

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.1.2-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.1.2.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
