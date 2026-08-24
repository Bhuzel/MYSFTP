# MYSFTP v2.0.0 — Major Release: Luxury Modals, Multi-Select & Drag-Drop Folder Suite

## ✨ Apa yang Baru & Diperbaiki di Versi 2.0.0

### 1. 🎨 Pop-up Bawaan Browser Diganti Modal Mewah (Luxury Glassmorphism Dialogs)
- Tidak ada lagi pop-up bawaan browser (`127.0.0.1 says...`).
- Semua dialog konfirmasi hapus berkas/folder, hapus profil koneksi, dan input nama berkas/folder baru kini menggunakan **Custom Luxury Glassmorphism Modal** dengan tema dark-gold yang senada dan elegan.

### 2. 📁 Upload Folder & Drag-and-Drop Rekursif
- **Tombol Dedicated "📁 Upload Folder":** Sekarang kamu bisa mengunggah seluruh folder beserta subfolder dan isinya sekaligus langsung dari PC ke server.
- **Drag & Drop Folder:** Cukup seret (drag & drop) berkas atau folder langsung ke layar File Explorer, sistem akan otomatis membaca struktur direktori secara rekursif dan membuat direktori serta mengunggah semua file ke server VPS.
- Tampilan visual **Dropzone Overlay** aktif secara otomatis saat file/folder diseret ke atas aplikasi.

### 3. 🗂️ Sistem Multi-Select & Batch Delete (Hapus Sekaligus)
- Setiap baris berkas dan folder kini dilengkapi checkbox pilihan.
- Terdapat checkbox **"Select All"** di header tabel.
- Dilengkapi **Batch Actions Toolbar** di bagian atas (`N item terpilih`) dengan tombol **"🗑 Hapus Terpilih"** sehingga tidak perlu lagi menghapus berkas satu per satu.

### 4. 💻 SSH Termius Console Bebas Duplikasi & Responsif Ctrl+C
- **Fix Double Echo:** Output perintah seperti `ls` atau `pm2 ls` tidak lagi terduplikasi ganda pada layar terminal.
- **Responsive Ctrl+C (SIGINT):** Menekan tombol `🛑 Ctrl+C` atau shortcut keyboard kini langsung mengirimkan sinyal interupsi ke proses remote yang sedang berjalan (seperti `pm2 logs`, `tail -f`, atau monitoring), menghentikan streaming secara seketika dan mencetak `^C` dengan jelas.
- Navigasi riwayat perintah (↑ / ↓) dan eksekusi instan tombol Enter tetap berjalan lancar.

### 5. 📥 Unduh Berkas & Folder (Single & Multi-Select Batch Archive)
- **Tombol Download Per Item:** Setiap baris berkas dan folder memiliki tombol `📥` untuk mengunduh berkas langsung atau mengunduh folder utuh sebagai arsip `.tar.gz`.
- **Download Terpilih (Batch Archive):** Kamu bisa mencentang beberapa berkas/folder sekaligus, lalu klik **`📥 Download Terpilih`** pada toolbar seleksi untuk mengunduh semua item terpilih dalam 1 arsip terkompresi.

### 6. ⚠️ Catatan Penting Setelah Instalasi Pertama
> **Catatan Setelah Install:** Pada awal-awal peluncuran pertama kali setelah install di PC Windows, sistem SmartScreen / antivirus mungkin memerlukan waktu beberapa detik untuk memvalidasi port dan service lokal. Jika aplikasi belum langsung terhubung, cukup tutup dan buka ulang (relog) 1–2 kali agar semua service & port berjalan normal dan lancar.

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.0.0-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.0.0.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
3. `MYSFTP.exe` dan `MYSFTP.cs` mentah juga disertakan kalau kamu ingin build ulang sendiri lewat
   GitHub Actions / Visual Studio (source sudah kompatibel, sudah dites compile dengan Mono C#
   compiler agar sintaksnya valid).

## Catatan jujur soal keterbatasan
- Perbaikan ini saya kompilasi dan verifikasi sintaksnya dengan Mono C# compiler (mcs), dan
  installer-nya sudah benar-benar dibuild dan diverifikasi sebagai file PE Windows (NSIS
  installer). Tapi karena saya bekerja di lingkungan Linux, saya **tidak bisa menjalankan/klik
  langsung di Windows sungguhan** untuk uji coba interaktif penuh (misalnya memastikan tampilan
  window app-mode persis seperti yang kamu lihat). Kalau setelah dicoba masih ada perilaku aneh di
  PC kamu, kirim detailnya (pesan error, screenshot) dan saya lanjutkan perbaikannya.
