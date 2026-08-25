<div align="center">

# ⚡ MYSFTP (v2.0.3 — High-Performance & Luxury Edition)

**Next-Generation Multi-Platform SFTP Client & SSH Termius Suite**  
*Engineered for Public Release with Luxury Dark Gold Aesthetics & Ultra-Fast Core by **ZellRayy***

---

[![Platform](https://img.shields.io/badge/Platform-Windows%20PC%20%7C%20Android-121213?style=for-the-badge&logo=android&logoColor=CDBD94)](https://github.com/Bhuzel/MYSFTP/releases)
[![Build CI/CD](https://img.shields.io/badge/GitHub-Actions%20CI%2FCD-19191B?style=for-the-badge&logo=githubactions&logoColor=7FBF8F)](https://github.com/Bhuzel/MYSFTP/actions)
[![Language](https://img.shields.io/badge/Kotlin%20%7C%20C%23-222224?style=for-the-badge&logo=kotlin&logoColor=CDBD94)](https://github.com/Bhuzel/MYSFTP)
[![License](https://img.shields.io/badge/License-MIT-101011?style=for-the-badge&logoColor=D9D4C7)](https://github.com/Bhuzel/MYSFTP)
[![Author](https://img.shields.io/badge/Author-ZellRayy-17150F?style=for-the-badge&logoColor=DED0AA)](https://t.me/BhuzelRayhan)

</div>

---

> ⚠️ **Catatan Penting Setelah Instalasi Pertama (Windows PC):**  
> Pada peluncuran pertama kali setelah selesai menginstal di Windows, sistem Windows SmartScreen / Antivirus mungkin membutuhkan waktu beberapa detik untuk memvalidasi port dan service lokal. Jika aplikasi belum langsung merespons atau terkoneksi, cukup tutup aplikasi dan **buka ulang (relog) 1–2 kali** agar semua port lokal dan service SSH aktif secara normal dan stabil.

---

## 🎨 Theme Palette (Luxury Dark Gold)

```css
:root {
  --bg-base: #060709;
  --bg-surface: #0c0d12;
  --bg-card: #12141a;
  --bg-card-hover: #181b22;
  --border: rgba(255, 255, 255, 0.07);
  --border-gold: rgba(205, 189, 148, 0.35);
  --gold: #cdbd94;
  --gold-light: #f0e6cf;
  --gold-glow: rgba(205, 189, 148, 0.22);
  --text: #eae6db;
  --text-dim: #7f7c72;
  --green: #73d285;
  --red: #e06c75;
  --blue: #6ab0f3;
  --yellow: #e5c07b;
  --cyan: #56c8d8;
  --purple: #c678dd;
}
```

---

## 🌟 Fitur Unggulan (v2.0.3)

### 💻 1. Windows Desktop Suite (`MYSFTP-v2.0.3-Setup.exe`)
* **⚡ High-Performance Remote Engine**: Navigasi direktori remote super cepat dengan *Zero-Disk-Churn AskPass caching* dan *Optimistic In-Memory Directory Cache* (buka folder terasa instan 0ms).
* **🌐 Universal Linux Compatibility**: Kompatibel 100% dengan semua distro Linux (Ubuntu, Debian, CentOS, RHEL, Alpine, Busybox, OpenWrt, Arch, BSD, macOS) dengan parser cerdas multi-format.
* **🗜️ Solid ZIP Archiving Engine**: Mengunduh folder tunggal atau banyak berkas terpilih (multi-select batch) 100% menggunakan engine ZIP internal C# standar PKWare, bebas dari error 0-byte atau file corrupt.
* **📁 Clean Relative Folder Extraction**: Saat mengunduh folder (misal `/root/ProjectBot/snippet/script`), isi file `.zip` hanya berisi folder `script/` langsung tanpa rantai parent direktori yang panjang.
* **🎨 Luxury Glassmorphism Modals**: Bebas dari pop-up browser bawaan (`127.0.0.1 says...`). Semua pop-up konfirmasi dan input menggunakan tema dark gold transparan yang elegan.
* **📁 Upload Folder & Drag & Drop Rekursif**: Tombol **`📁 Upload Folder`** dan area drag & drop memungkinkan kamu menarik seluruh folder proyek dari Windows ke server VPS secara otomatis.
* **🗂️ Multi-Select & Batch Actions**: Checkbox pilihan di setiap baris dan header tabel untuk menghapus atau mengunduh banyak berkas sekaligus dalam 1 kali klik.
* **💻 SSH Termius Console Bebas Duplikasi**: Terminal interaktif tanpa duplikasi echo, riwayat perintah (↑ / ↓), dan tombol **`🛑 Ctrl+C`** responsif untuk menghentikan proses log (`pm2 logs`, `tail -f`).
* **✏️ Pro Code Editor**: Editor kode terintegrasi dengan shortcut keyboard **`Ctrl+S`** untuk menyimpan file langsung ke server remote.
* **📦 Dedicated NSIS Installer**: Instalasi bersih ke drive manapun (misal `D:\`), Start Menu shortcut, dan terdaftar rapi di Windows Add/Remove Programs.

### 📱 2. Android Mobile App (`MYSFTP-v2.0.3.apk`)
* **Spacious Single-Pane Explorer**: Tampilan direktori remote yang lapang dan responsif.
* **Termius-Inspired Mobile Terminal**: Terminal monospace dengan auto-scroll dan drag-and-copy.
* **OTA Auto Updates**: Otomatis mendeteksi update terbaru dari GitHub repository.

---

## 🚀 Quick Start

### 🖥️ Windows PC / Laptop
1. Unduh **`MYSFTP-v2.0.3-Setup.exe`** dari [**GitHub Releases**](https://github.com/Bhuzel/MYSFTP/releases/latest).
2. Jalankan file installer dan selesaikan langkah instalasi.
3. Buka **MYSFTP** dari Start Menu atau Desktop.
4. Klik **`+ Tambah Server Baru`** → Masukkan Host, Port, Username, Password VPS.
5. Klik **`🚀 Buka`** untuk mulai mengelola berkas, mengunggah/mengunduh folder, dan membuka terminal SSH.

### 📱 Android Mobile
1. Unduh **`MYSFTP-v2.0.3.apk`** dari [**GitHub Releases**](https://github.com/Bhuzel/MYSFTP/releases/latest).
2. Pasang APK pada perangkat Android Anda.

---

## 🛠️ Automated CI/CD & Build System

Repository ini dilengkapi dengan **GitHub Actions CI/CD** (`.github/workflows/android.yml`):
* Setiap tag `v*` otomatis mengompilasi APK Android dan Windows Setup Installer.
* File unduhan bersih langsung tersedia di [**GitHub Releases**](https://github.com/Bhuzel/MYSFTP/releases).

---

## 💬 Developer & Support

* **Developer**: ZellRayy
* **Telegram**: [@BhuzelRayhan](https://t.me/BhuzelRayhan)
* **WhatsApp**: [+62 823-5205-2566](https://wa.me/6282352052566)
* **Repository**: [https://github.com/Bhuzel/MYSFTP](https://github.com/Bhuzel/MYSFTP)

---
<div align="center">
  <sub>MYSFTP v2.0.3 — Engineered with passion for speed, beauty, and developer productivity.</sub>
</div>
