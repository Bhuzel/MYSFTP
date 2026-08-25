<div align="center">

# ⚡ MYSFTP (v2.0.8 — SSH Termius Terminal Real-Time Engine Fix)

**Next-Generation Multi-Platform SFTP Client & SSH Termius Suite**  
*Engineered for Public Release with Luxury Dark Gold Aesthetics, Real-Time SSH Terminal & Self-Contained Native SSH.NET Core by **ZellRayy***

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

## 🌟 Fitur Unggulan (v2.0.8)

### 💻 1. Windows Desktop Suite (`MYSFTP-v2.0.8-Setup.exe`)
* **💻 SSH Termius Console Real-Time**: Terminal monospace interaktif ditenagai oleh `ShellStream` dengan thread pembaca stream non-blocking mandiri dan tombol **`🛑 Ctrl+C`** responsif.
* **⚡ Self-Contained Native SSH.NET Core**: Menggunakan library `Renci.SshNet` murni tanpa dependensi eksternal — 100% plug and play di seluruh edisi Windows.
* **🌐 Pure SFTP Directory Protocol (`SSH_FXP_READDIR`)**: Listing berkas/folder remote langsung dari kanal SFTP biner dengan atribut objek asli (`SftpFile`), bebas dari parsing regex `ls`.
* **🗜️ Solid In-Memory ZIP Archiving**: Mengunduh folder tunggal atau banyak berkas terpilih (multi-select batch) secara biner tanpa Base64 overhead, menghasilkan `.zip` valid standar PKWare.
* **📁 Clean Relative Folder Extraction**: Saat mengunduh folder remote, isi file ZIP hanya berisi folder target langsung tanpa rantai path parent yang panjang.
* **🎨 Luxury Glassmorphism Modals**: Bebas dari pop-up browser bawaan (`127.0.0.1 says...`). Semua pop-up konfirmasi dan input menggunakan tema dark gold transparan yang elegan.
* **📁 Upload Folder & Drag & Drop Rekursif**: Tombol **`📁 Upload Folder`** dan area drag & drop memungkinkan transfer folder lengkap secara rekursif.
* **🗂️ Multi-Select & Batch Actions**: Checkbox pilihan di setiap baris dan header tabel untuk menghapus atau mengunduh banyak berkas sekaligus dalam 1 kali klik.
* **✏️ Pro Code Editor**: Editor kode terintegrasi dengan shortcut keyboard **`Ctrl+S`** untuk menyimpan berkas langsung ke server remote via SFTP socket.
* **📦 Dedicated NSIS Installer**: Installer resmi tunggal lengkap dengan dependency `Renci.SshNet.dll`, Start Menu shortcut, dan terdaftar di Add/Remove Programs.

### 📱 2. Android Mobile App (`MYSFTP-v2.0.8.apk`)
* **Spacious Single-Pane Explorer**: Tampilan direktori remote yang lapang dan responsif.
* **Termius-Inspired Mobile Terminal**: Terminal monospace dengan auto-scroll dan drag-and-copy.
* **OTA Auto Updates**: Otomatis mendeteksi update terbaru dari GitHub repository.

---

## 🚀 Quick Start

### 🖥️ Windows PC / Laptop
1. Unduh **`MYSFTP-v2.0.8-Setup.exe`** dari [**GitHub Releases**](https://github.com/Bhuzel/MYSFTP/releases/latest).
2. Jalankan file installer dan selesaikan langkah instalasi.
3. Buka **MYSFTP** dari Start Menu atau Desktop.
4. Klik **`+ Tambah Server Baru`** → Masukkan Host, Port, Username, Password VPS.
5. Klik **`🚀 Buka`** untuk mulai mengelola berkas, mengunggah/mengunduh folder, dan membuka terminal SSH.

### 📱 Android Mobile
1. Unduh **`MYSFTP-v2.0.8.apk`** dari [**GitHub Releases**](https://github.com/Bhuzel/MYSFTP/releases/latest).
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
  <sub>MYSFTP v2.0.8 — Engineered with passion for speed, beauty, and developer productivity.</sub>
</div>
