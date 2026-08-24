# 📘 Panduan Lengkap Penggunaan, Upload GitHub, & Auto-Build APK MYSFTP v1.0.0

Selamat datang di panduan resmi **MYSFTP v1.0.0 (Luxury Dark Edition)** — Aplikasi Android Native tingkat lanjut berbasis **Kotlin + Jetpack Compose & Material 3** untuk mengelola server remote dengan kecepatan tinggi, aman, dan tanpa lag.

---

## 📑 Daftar Isi
1. [Cara Upload & Push Proyek ke GitHub (`Bhuzel/MYSFTP`)](#1-cara-upload--push-proyek-ke-github-bhuzelmysftp)
2. [Cara Otomatis Build APK & Rilis OTA di GitHub Actions](#2-cara-otomatis-build-apk--rilis-ota-di-github-actions)
3. [Cara Memasang & Menjalankan APK di HP](#3-cara-memasang--menjalankan-apk-di-hp)
4. [Cara Build Manual APK di Android Studio (Offline)](#4-cara-build-manual-apk-di-android-studio-offline)
5. [Fitur-Fitur Utama MYSFTP](#5-fitur-fitur-utama-mysftp)
6. [Kontak Bantuan Pengembang](#6-kontak-bantuan-pengembang)

---

## 1. Cara Upload & Push Proyek ke GitHub (`Bhuzel/MYSFTP`)

Ikuti langkah-langkah berikut di terminal laptop Anda (PowerShell / Command Prompt / Git Bash) di folder proyek:

### Langkah 1: Inisialisasi Git
```bash
git init
```

### Langkah 2: Tambahkan Semua Berkas ke Git
```bash
git add .
```

### Langkah 3: Buat Commit Awal
```bash
git commit -m "feat: initial release MYSFTP v1.0.0 Luxury Dark Edition"
```

### Langkah 4: Pastikan Nama Branch Utama adalah `main`
```bash
git branch -M main
```

### Langkah 5: Hubungkan ke Repositori GitHub Anda
```bash
git remote add origin https://github.com/Bhuzel/MYSFTP.git
```
*(Jika remote origin sudah pernah ada sebelumnya, jalankan: `git remote set-url origin https://github.com/Bhuzel/MYSFTP.git`)*

### Langkah 6: Push ke GitHub
```bash
git push -u origin main
```

> **💡 Tips Login GitHub:**
> Jika diminta memasukkan password saat `git push`, gunakan **GitHub Personal Access Token (PAT)** sebagai password Anda:
> 1. Buka akun GitHub di browser > **Settings** > **Developer settings** > **Personal access tokens (tokens classic)**.
> 2. Klik **Generate new token (classic)**, beri nama (misal: `MYSFTP`), dan centang izin `repo` serta `workflow`.
> 3. Salin token tersebut dan gunakan sebagai password di terminal.

---

## 2. Cara Otomatis Build APK & Rilis OTA di GitHub Actions

Proyek ini telah dilengkapi sistem **CI/CD Automation** pada berkas [`.github/workflows/android.yml`](file:///.github/workflows/android.yml).

### Bagaimana Alur Kerjanya?
1. **Build Otomatis:** Setiap kali Anda melakukan `git push` ke branch `main`, GitHub Actions di server cloud GitHub akan secara otomatis:
   * Mengatur Java JDK 17 & Android SDK.
   * Mengompilasi aplikasi menjadi file APK (`MYSFTP.apk` & `MYSFTP-v1.0.0.apk`).
   * Membuat rilis resmi otomatis pada menu **Releases** di GitHub (`https://github.com/Bhuzel/MYSFTP/releases`).
2. **Sistem OTA Update (Over-The-Air) di HP:**
   * Di dalam aplikasi Android, sistem OTA Updater di [`OtaUpdater.kt`](file:///app/src/main/java/com/yoursftp/app/ota/OtaUpdater.kt) sudah dikonfigurasi untuk membaca rilis dari repo **`Bhuzel/MYSFTP`**.
   * Ketika Anda merilis versi baru di GitHub, pengguna yang membuka aplikasi di HP akan langsung menerima notifikasi pembaruan dan bisa mengunduh APK terbaru secara otomatis dengan 1 klik!

### Cara Membuat Rilis Versi Tag Baru (Contoh v1.0.0):
Jalankan perintah ini di terminal:
```bash
git tag v1.0.0
git push origin v1.0.0
```
GitHub Actions akan langsung mengompilasi dan menerbitkan rilis **MYSFTP Release v1.0.0** secara otomatis.

---

## 3. Cara Memasang & Menjalankan APK di HP

1. Buka halaman rilis repositori Anda di browser HP: `https://github.com/Bhuzel/MYSFTP/releases`
2. Unduh berkas **`MYSFTP.apk`**.
3. Buka berkas yang diunduh dan pasang (install) di perangkat Android Anda.
4. Nikmati antarmuka bertema gelap mewah *Luxury Dark Gold*, kecepatan transfer super kencang, in-app editor, SSH terminal, dan database viewer!

---

## 4. Cara Build Manual APK di Android Studio (Offline)

Jika ingin mengompilasi APK langsung di laptop tanpa melalui GitHub:

1. Buka software **Android Studio**.
2. Pilih **File > Open**, lalu pilih folder proyek ini (`SFTP`).
3. Biarkan Android Studio menyelesaikan sinkronisasi Gradle (*Gradle Sync*).
4. Pilih menu **Build > Build Bundle(s) / APK(s) > Build APK(s)**.
5. Atau jalankan perintah via terminal di Android Studio:
   ```bash
   ./gradlew assembleDebug
   ```
6. File APK yang telah selesai dibangun akan berada di:
   `app/build/outputs/apk/debug/app-debug.apk`

---

## 5. Fitur-Fitur Utama MYSFTP

* **🔀 Dual-Pane File Manager**: Sinkronisasi dan transfer berkas cepat antara penyimpanan lokal dan remote dengan buffer 64KB, TCP_NODELAY, dan kompresi zlib (jauh lebih cepat dan responsif dibanding WinSCP).
* **📝 In-App Code Editor & Large Text Editor**: Buka, edit, dan simpan berkas kode dengan *syntax highlighting* 15+ bahasa pemrograman, penomoran baris, dan status bar VS Code.
* **💻 Terminal Emulator SSH (VT100 / xterm)**: Konsol interaktif dengan 256 warna dan tombol pintasan perintah kilat Linux (`ls -la`, `df -h`, `free -m`, `top`, `docker ps`, `pm2 status`, dll.).
* **🗄️ Multi-Format Database Viewer**: Membaca file SQLite (`.sqlite`, `.db`), JSON / NoSQL, CSV, dan SQL Dump langsung dengan pencarian data instan.
* **🔒 Keamanan & Anti-Overflow**: Normalisasi path traversal, penyimpanan kredensial aman di Room DB, dan layout responsif anti-clipping di semua ukuran layar.

---

## 6. Kontak Bantuan Pengembang

Jika Anda membutuhkan bantuan teknis atau konsultasi:
* **Pengembang:** ZellRayy
* **WhatsApp:** [082352052566](https://wa.me/6282352052566)
* **Telegram:** [@BhuzelRayhan](https://t.me/BhuzelRayhan)
* **Repositori GitHub:** [https://github.com/Bhuzel/MYSFTP](https://github.com/Bhuzel/MYSFTP)

---
*MYSFTP v1.0.0 — Dibuat dan dioptimalkan secara khusus untuk performa tinggi & kenyamanan pengembang.*
