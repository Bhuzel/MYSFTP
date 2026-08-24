# MYSFTP v1.9.5 — Perbaikan & Instalasi Dedicated Suite

## Apa yang diperbaiki

### 1. Terminal sekarang benar-benar bisa dipakai
Sebelumnya setiap perintah di terminal membuka **koneksi SSH baru** dari nol — jadi `cd` tidak
tersimpan, dan program interaktif (nano, htop) tidak jalan dengan benar. Sekarang terminal
membuka **satu sesi SSH persisten** (pakai `ssh -tt`, PTY asli) yang tetap hidup selama kamu
terhubung — persis seperti Termius. Prompt yang muncul adalah prompt asli dari server, riwayat
perintah bisa diakses dengan ↑ / ↓, dan tombol Ctrl+C sekarang mengirim sinyal Ctrl+C asli
(bukan memutus seluruh sesi).

### 2. Bukan Microsoft Edge lagi yang "kebuka"
Ikon aplikasi sebelumnya disajikan sebagai file JPEG dengan tipe MIME yang salah, jadi Windows
tidak mengenali ikon custom-nya dan menampilkan ikon Edge/Chrome generik di taskbar — itu sebabnya
terasa seperti "Edge yang kebuka". Sekarang `favicon.ico` disajikan dengan tipe yang benar, dan
ikon aplikasi (`app.ico`) juga di-embed langsung ke dalam `MYSFTP.exe` itu sendiri.

### 3. Tidak perlu buka dua kali lagi
Sebelumnya aplikasi hanya menunggu ±1 detik sebelum membuka jendela, dan sisa file lock dari sesi
sebelumnya bisa membuat percobaan pertama gagal diam-diam. Sekarang:
- Menunggu server lokal benar-benar siap (sampai 15 detik, bukan ~1 detik) sebelum membuka jendela.
- Membersihkan file lock lama sebelum membuka.
- Kalau jendela tetap gagal terbuka di percobaan pertama, aplikasi **otomatis mencoba ulang sendiri**
  dengan profil baru — kamu tidak perlu klik dua kali lagi.

### 4. Tombol "◀ Kembali" tidak glitch lagi
Sebelumnya ada efek "flash" ke folder lama sebelum lompat ke folder baru, terasa seperti delay/
nge-glitch. Sekarang: indikator loading jauh lebih terlihat (badge "⏳ Memuat..." + tombol
navigasi otomatis nonaktif selagi memuat), dan tampilan tidak lagi menampilkan folder lama kalau
memang belum ada datanya.

### 5. Upload dari lokal — sekarang bisa banyak file + drag & drop
Tombol **📤 Upload dari Lokal** sekarang bisa memilih beberapa file sekaligus (progress per file
ditampilkan), dan kamu juga bisa langsung **drag & drop** file dari File Explorer Windows ke area
File Explorer di MYSFTP.

### 6. Instalasi seperti aplikasi besar
`MYSFTP-Setup.exe` sekarang adalah installer sungguhan (dibuat dengan NSIS):
- Halaman lisensi, pilih folder instalasi (bebas pilih drive mana pun, termasuk `D:\`), pilih
  komponen (shortcut Desktop opsional).
- Shortcut Start Menu otomatis + opsi shortcut Desktop.
- Terdaftar rapi di **Add/Remove Programs** Windows, lengkap dengan ikon, versi, dan uninstaller.
- Data (`connections.json`, dll) disimpan di folder instalasi itu sendiri — jadi kalau kamu pasang
  di `D:\Apps\MYSFTP`, semua datanya juga di situ, tidak nyampur dengan `C:\`.

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
