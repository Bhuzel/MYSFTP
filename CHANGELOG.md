# MYSFTP v2.0.3 — High-Performance & Universal Linux Suite

## ✨ Apa yang Baru & Diperbaiki di Versi 2.0.3

### 1. ⚡ Ultra-Fast Directory Navigation & Zero-Disk-Churn
- **AskPass Caching:** Menghilangkan pembuatan/penghapusan file temporer berulang di disk pada setiap command SSH, menghapus latency scan Windows Defender sehingga membuka server dan folder terasa sangat cepat.
- **Optimistic In-Memory Directory Cache:** Folder yang sudah pernah dibuka akan ditampilkan seketika (**0ms**) saat navigasi bolak-balik, lalu secara cerdas di-refresh di latar belakang.
- **Low-Latency SSH Options:** Menambahkan parameter `-o Compression=no -o TCPKeepAlive=yes -o ConnectTimeout=5` untuk respon SSH instan.

### 2. 🌐 Universal Multi-Format Linux Directory Parser
- Menggunakan parser cerdas multi-format yang kompatibel 100% dengan semua distro Linux (Debian, Ubuntu, CentOS, RHEL, Alpine, BusyBox, OpenWrt, Arch, BSD, macOS, dll.).
- Menjamin seluruh direktori (termasuk `/root`) terbaca lengkap tanpa masalah parsing kolom atau locale bahasa.

### 3. 🛡️ Perbaikan Error JSON `size:tidak` & Delimiter Base64
- Field `size` dijamin selalu berupa angka valid (`long`), bebas dari token error tak valid.
- Marker unik `___MYSFTP_B64_START___` dan `___MYSFTP_B64_END___` mengisolasi transmisi data dari gangguan banner login/MOTD.

### 4. 🗜️ Robust In-Memory ZIP Archiving Engine
- Header dan Central Directory ZIP diselaraskan secara konsisten sesuai standar PKWare, mencegah korupsi file 0-byte.
- Semua nama berkas/folder berspasi di-quote secara aman `'...'` saat pengompresan batch atau folder tunggal.

---

# MYSFTP v2.0.2 — Hotfix JSON Parsing & Robust ZIP Archiving Engine
- Perbaikan parsing `size` integer JSON dan delimiter base64 untuk isolasi banner SSH.

---

# MYSFTP v2.0.1 — Hotfix & Solid ZIP Archiving Engine
- Engine ZipPacker C# Internal, Luxury Glassmorphism Modals, Batch actions, dan terminal responsif.

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.0.3-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.0.3.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
