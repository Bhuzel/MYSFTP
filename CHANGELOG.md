# MYSFTP v2.0.2 — Hotfix JSON Parsing & Robust ZIP Archiving Engine

## ✨ Apa yang Baru & Diperbaiki di Versi 2.0.2

### 1. 🛡️ Perbaikan Error JSON `Unexpected token 'i', ..."size":tidak...`
- **Penyebab:** Pada server VPS Linux dengan locale non-Inggris atau pesan peringatan (warning/error/banner) bahasa Indonesia (seperti `ls: tidak dapat mengakses...`), teks tersebut ikut ter-split sehingga string `"tidak"` masuk tanpa tanda kutip ke field integer `size`. Akibatnya `JSON.parse()` di browser gagal.
- **Perbaikan:** 
  - Memaksa `LC_ALL=C` saat memanggil `ls -la` agar format listing selalu standar POSIX.
  - Memfilter dan hanya memproses baris dengan permission Unix valid (`-`, `d`, `l`, `c`, `b`, `s`, `p`).
  - Menggunakan `long.TryParse()` yang menjamin field `size` selalu berupa angka valid (misal `0` atau `102400`), dan menangani string error baik bahasa Indonesia maupun Inggris.

### 2. 🗜️ Robust ZIP Archiving & Download Fix
- **Delimiter Isolasi SSH:** Menambahkan marker unik `___MYSFTP_B64_START___` dan `___MYSFTP_B64_END___` saat stream `tar | base64` agar banner MOTD/login SSH tidak mengotori payload arsip dan tidak menimbulkan file 0-byte corrupt.
- **Dukungan Nama Berkas Berspasi:** Semua path dan nama file pada command `tar` kini di-quote secara aman `'...'`.
- **Penyelarasan Header ZipPacker:** Memperbaiki ketidakcocokan compression method antara Local Header dan Central Directory pada file 0-byte, sehingga arsip ZIP 100% valid dan langsung bisa dibuka di Windows Explorer.
- **Dukungan ZIP untuk Local Storage:** Mode folder download dan batch download kini juga mendukung mode Local Drive.

---

# MYSFTP v2.0.1 — Hotfix & Solid ZIP Archiving Engine

## ✨ Fitur & Perbaikan di Versi 2.0.1
- **Engine ZipPacker C# Internal:** Kompresi remote folder secara in-memory menjadi `.zip` murni standar.
- **Struktur Direktori Bersih:** Unduhan folder remote tidak lagi membawa rantai parent path yang panjang.
- **Custom Luxury Glassmorphism Modals:** Modal elegan dark gold menggantikan pop-up browser bawaan.
- **Upload Folder & Batch Actions:** Dukungan upload folder rekursif dan multi-select batch delete/download.
- **SSH Termius Console:** Menghilangkan duplikasi echo dan responsif tombol `🛑 Ctrl+C`.

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.0.2-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.0.2.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
