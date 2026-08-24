# 📘 Panduan Lengkap Penggunaan MYSFTP v1.0.0 (Windows PC & Android)

Selamat datang di panduan resmi **MYSFTP v1.0.0 (Luxury Dark Edition)** — Aplikasi Hybrid SFTP + Termius modern untuk **Laptop / PC (Windows .exe)** dan **HP Android (.apk)**.

---

## 📑 Daftar Isi
1. [Cara Menggunakan di Laptop / Windows PC (`MYSFTP.exe`)](#1-cara-menggunakan-di-laptop--windows-pc-mysftpexe)
2. [Cara Memasang & Menjalankan APK di HP Android](#2-cara-memasang--menjalankan-apk-di-hp-android)
3. [Cara Upload & Push Proyek ke GitHub (`Bhuzel/MYSFTP`)](#3-cara-upload--push-proyek-ke-github-bhuzelmysftp)
4. [Cara Otomatis Build & Rilis di GitHub Actions](#4-cara-otomatis-build--rilis-di-github-actions)
5. [Cara Compile Manual `MYSFTP.exe` di Laptop (Opsional)](#5-cara-compile-manual-mysftpexe-di-laptop-opsional)
6. [Fitur-Fitur Utama MYSFTP](#6-fitur-fitur-utama-mysftp)
7. [Kontak Bantuan Pengembang](#7-kontak-bantuan-pengembang)

---

## 1. Cara Menggunakan di Laptop / Windows PC (`MYSFTP.exe`)

Untuk pengguna Laptop / PC Windows, Anda **tidak perlu** menginstall atau membuka Android Studio!

### Langkah Menjalankan:
1. Di dalam folder proyek, cukup **klik ganda pada file [`MYSFTP.exe`](file:///MYSFTP.exe)**.
2. Aplikasi akan langsung terbuka dalam **Jendela Aplikasi Desktop (App Window Mode)** mandiri dengan tema gelap mewah *Luxury Dark Gold*.
3. Anda bisa langsung menggunakan:
   * **🌐 Koneksi Server:** Tambah profil SFTP, FTP, FTPS, atau Local PC.
   * **📁 File Explorer:** Jelajahi dan kelola berkas dengan tampilan grid/list.
   * **🔀 Dual-Pane Transfer:** Transfer berkas berdampingan lokal dan remote.
   * **📝 Pro Code Editor:** Buka dan edit file dengan nomor baris & simpan langsung (`Ctrl + S`).
   * **💻 SSH Terminal (Termius Style):** Konsol perintah dengan tombol pintasan kilat Linux (`ls -la`, `df -h`, `free -m`, `uptime`, `docker ps`, `pm2 status`).

---

## 2. Cara Memasang & Menjalankan APK di HP Android

1. Buka halaman rilis repositori Anda di browser HP: `https://github.com/Bhuzel/MYSFTP/releases`
2. Unduh berkas **`MYSFTP.apk`**.
3. Buka berkas yang diunduh dan install di HP Android Anda.
4. Nikmati antarmuka bertema *Luxury Dark Gold*, kecepatan transfer tinggi, in-app editor, SSH terminal, dan database viewer di HP Anda!

---

## 3. Cara Upload & Push Proyek ke GitHub (`Bhuzel/MYSFTP`)

Ikuti langkah-langkah berikut di terminal laptop Anda (PowerShell / Command Prompt / Git Bash) di folder proyek:

### Langkah 1: Tambahkan Semua Berkas ke Git
```bash
git add .
```

### Langkah 2: Buat Commit
```bash
git commit -m "feat: add standalone Windows MYSFTP.exe and multiplatform release"
```

### Langkah 3: Push ke GitHub
```bash
git push origin main
```

### Langkah 4: Rilis Versi Tag Baru (Opsional)
```bash
git tag -f v1.0.0
git push -f origin v1.0.0
```

---

## 4. Cara Otomatis Build & Rilis di GitHub Actions

Proyek ini telah dilengkapi sistem **CI/CD Multi-Platform** pada berkas [`.github/workflows/android.yml`](file:///.github/workflows/android.yml).

### Bagaimana Alur Kerjanya?
1. Setiap kali Anda melakukan `git push` ke branch `main` atau tag `v1.0.0`, GitHub Actions di server cloud GitHub akan otomatis:
   * Mengompilasi kode Android menjadi **`MYSFTP.apk`**.
   * Mengemas aplikasi Windows **`MYSFTP.exe`**.
   * Membuat rilis resmi otomatis di menu **Releases** GitHub Anda (`https://github.com/Bhuzel/MYSFTP/releases`).
2. Pengguna bisa langsung mengunduh `MYSFTP.exe` untuk PC dan `MYSFTP.apk` untuk HP!

---

## 5. Cara Compile Manual `MYSFTP.exe` di Laptop (Opsional)

Jika Anda ingin mengompilasi ulang file C# `MYSFTP.cs` menjadi `MYSFTP.exe` secara manual di PowerShell:
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /optimize+ /out:MYSFTP.exe MYSFTP.cs
```
File executable `MYSFTP.exe` akan langsung tercipta (hanya berukuran ~134 KB, sangat ringan & portabel tanpa perlu installer).

---

## 6. Fitur-Fitur Utama MYSFTP

* **🔀 Dual-Pane File Manager**: Sinkronisasi dan transfer berkas super cepat antara lokal dan remote (buffer 64KB, TCP_NODELAY, kompresi zlib).
* **📝 In-App Code Editor**: Syntax highlighting, penomoran baris, dan status bar VS Code.
* **💻 Terminal Emulator SSH (Termius Hybrid)**: Konsol interaktif VT100/xterm 256-warna dengan tombol pintasan perintah Linux.
* **🗄️ Multi-Format Database Viewer**: Membaca file SQLite (`.db`), JSON, CSV, dan SQL Dump.
* **🔒 Keamanan & Anti-Overflow**: Normalisasi path traversal, penyimpanan kredensial terenkripsi, dan layout responsif anti-clipping.

---

## 7. Kontak Bantuan Pengembang

Jika Anda membutuhkan bantuan teknis atau kustomisasi:
* **Pengembang:** ZellRayy
* **WhatsApp:** [082352052566](https://wa.me/6282352052566)
* **Telegram:** [@BhuzelRayhan](https://t.me/BhuzelRayhan)
* **Repositori GitHub:** [https://github.com/Bhuzel/MYSFTP](https://github.com/Bhuzel/MYSFTP)

---
*MYSFTP v1.0.0 — Dibuat dan dioptimalkan secara khusus untuk performa tinggi & kenyamanan pengembang.*
