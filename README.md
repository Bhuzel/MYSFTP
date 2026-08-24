# MYSFTP 🌐⚡ (v1.0.0 — Luxury Dark Edition)

[![Platform](https://img.shields.io/badge/Platform-Android-3DDC84?style=for-the-badge&logo=android&logoColor=white)](https://android.com)
[![Kotlin](https://img.shields.io/badge/Language-Kotlin-7F52FF?style=for-the-badge&logo=kotlin&logoColor=white)](https://kotlinlang.org)
[![Jetpack Compose](https://img.shields.io/badge/UI-Jetpack%20Compose-4285F4?style=for-the-badge&logo=jetpackcompose&logoColor=white)](https://developer.android.com/compose)
[![Version](https://img.shields.io/badge/Version-1.0.0-CDBD94?style=for-the-badge&logo=appveyor&logoColor=17150F)](https://github.com/Bhuzel/MYSFTP)
[![Build Status](https://img.shields.io/badge/CI%2FCD-GitHub%20Actions-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)](https://github.com/Bhuzel/MYSFTP/actions)
[![Author](https://img.shields.io/badge/Author-ZellRayy-7FBF8F?style=for-the-badge)](https://t.me/BhuzelRayhan)

**MYSFTP v1.0.0** adalah aplikasi Android Native tingkat lanjut berbasis **Kotlin + Jetpack Compose** & **Material 3** untuk mengelola server remote Anda dengan kecepatan tinggi, mulus, aman, dan tanpa lag. Dirancang dengan tema visual eksklusif **Luxury Dark Gold** (`#0a0a0b` & `#cdbd94`), MYSFTP menggabungkan kekuatan **Dual-Pane File Manager**, **In-App Code & Large Text Editor**, **Terminal Emulator SSH (VT100/xterm)**, serta **Multi-Format Database Viewer (SQLite, JSON, CSV, SQL)**.

> 📖 **Panduan Lengkap Upload GitHub & Auto-Build APK**:  
> Silakan baca panduan step-by-step di [**`Penggunaan.md`**](file:///Penggunaan.md).

---

## 🎨 Palet Tema Visual (Luxury Dark Gold)

```css
:root,
[data-theme="dark"]{
  --bg: #0a0a0b;
  --bg-soft: #121213;
  --surface: #19191b;
  --surface-2: #222224;
  --border: #2b2b2e;
  --border-soft: #1f1f21;
  --text: #d9d4c7;
  --text-muted: #8b877c;
  --text-dim: #5c5952;
  --accent: #cdbd94;
  --accent-strong: #ded0aa;
  --accent-ink: #17150f;
  --success: #7fbf8f;
  --shadow: 0 8px 30px rgba(0,0,0,.35);
  --shadow-sm: 0 2px 10px rgba(0,0,0,.25);
  --grain-opacity: .05;
  --code-bg: #101011;
  color-scheme: dark;
}
```

---

## 🚀 Fitur-Fitur Unggulan MYSFTP

### 1. 🔀 Dual-Pane File Manager Berkecepatan Tinggi
* **Performa Ekstra Cepat & Ringan**: Streaming data teroptimasi dengan buffer 64KB, TCP_NODELAY, dan kompresi zlib (jauh lebih responsif dan ringan dibanding WinSCP).
* **Dual-Pane Sync**: Panel kiri dan panel kanan berdampingan untuk transfer file dan folder langsung dengan sekali ketuk.
* **Layout Anti-Overflow**: Tipografi dan nama berkas ditangani dengan pemotongan teks (*ellipsis*) yang rapi tanpa terpotong atau keluar batas di layar split.

### 2. 📝 In-App Code & Large File Editor
* Buka, edit, dan simpan berkas teks langsung di server remote.
* **Syntax Highlighting Otomatis**: Kotlin, Java, Python, C/C++, Go, JavaScript, TypeScript, HTML, CSS, XML, JSON, CSV, Shell Script (`.sh`), YAML, dan konfigurasi.
* **VS Code-Style Status Bar**: Baris, Karakter, Skala Zoom, dan Ekstensi Dokumen.

### 3. 💻 Terminal Emulator SSH Interaktif (VT100/xterm)
* Emulator terminal interaktif dengan gamut warna penuh 256-warna ANSI.
* **Pintasan Perintah Cepat (Quick Snippets)**: `ls -la`, `df -h`, `free -m`, `top`, `pm2 status`, `docker ps`, `git status`, dll.
* Tombol aksesori cepat: `ESC`, `TAB`, `CTRL+C`, `CTRL+D`, `CTRL+Z`, `CTRL+L`, `CTRL+R`, `|`, `~`, `/`, `-`.

### 4. 🗄️ Multi-Format Database Viewer (SQL & NoSQL)
* **SQLite (`.sqlite`, `.db`, `.sqlite3`, `.db3`)**: Jelajahi baris data dan eksekusi query SQL kustom.
* **JSON / NoSQL (`.json`, `.jsonl`)**, **CSV**, **TSV**, **SQL Dump**: Pembaca tabel presisi dengan fitur pencarian dan filter instan.

### 5. 🌐 Multi-Protokol & Keamanan
* Mendukung protokol: **SFTP (SSH)**, **FTP**, **FTPS (FTP over SSL/TLS)**, **Amazon S3 / MinIO**, dan **Local Storage Android**.
* **Uji Koneksi Instan (Test Connection)**: Cek latensi dan validasi kredensial sebelum menyimpan profil.
* **OTA Updater Otomatis**: Deteksi versi baru langsung dari repositori GitHub `Bhuzel/MYSFTP`.

---

## 🛠️ Cara Membangun (Build) Proyek

### Opsi A: Otomatis via GitHub Actions (Rilis OTA)
Setiap kali Anda melakukan `git push` ke repositori `Bhuzel/MYSFTP`, GitHub Actions akan otomatis mengompilasi dan mengunggah APK ke menu **Releases** di GitHub.

### Opsi B: Build Manual di Android Studio / Terminal
```bash
./gradlew assembleDebug
```
File APK siap dipasang akan berada di:
`app/build/outputs/apk/debug/app-debug.apk`

---

## 💬 Hubungi & Kontak Pengembang

Jika Anda membutuhkan bantuan, kustomisasi, atau ingin berkonsultasi seputar pengembangan aplikasi:
* **Pengembang:** ZellRayy
* **WhatsApp:** [082352052566](https://wa.me/6282352052566)
* **Telegram:** [@BhuzelRayhan](https://t.me/BhuzelRayhan)
* **Repositori:** [https://github.com/Bhuzel/MYSFTP](https://github.com/Bhuzel/MYSFTP)

---
*MYSFTP v1.0.0 — Dibuat dan dioptimalkan secara khusus untuk performa tinggi & kenyamanan pengembang.*
