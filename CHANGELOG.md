# MYSFTP v2.0.5 — Streamlined High-Performance & Universal Linux Suite

## ✨ Apa yang Baru & Diperbaiki di Versi 2.0.5

### 1. ⚡ Streamlined Direct SSH Execution & AskPass In-Memory Caching
- **Direct Robust Execution:** Menjaga keandalan maksimal eksekusi SSH mandiri yang kompatibel penuh dengan seluruh versi Windows OpenSSH bawaan tanpa dependensi socket Unix domain (`AF_UNIX`).
- **Zero-Disk-Churn AskPass Caching:** Autentikasi SSH di-cache selama sesi aktif tanpa disk churn berulang, menghilangkan intervensi antivirus/Windows Defender.
- **Optimistic In-Memory Directory Cache:** Navigasi folder bolak-balik tampil seketika (**0ms**) dan di-refresh di latar belakang.

### 2. 🌐 Universal Multi-Format Linux Directory Parser
- Mendukung 100% seluruh format `ls` Linux (POSIX standard, BusyBox/Alpine tanpa group, GNU ISO `--time-style=long-iso`, file berspasi, symlink, dan hidden files).
- Direktori `/root` dan subfolder terbaca secara konsisten dan akurat.

### 3. 🗜️ Solid In-Memory ZIP Archiving Engine
- Kompresi ZIP PKWare murni in-memory tanpa dependensi paket `zip` Linux.
- Header dan Central Directory diselaraskan untuk mencegah berkas 0-byte corrupt.

---

# MYSFTP v2.0.4 — High-Performance Suite
- Eksperimen Connection multiplexing dan pembaruan versi.

---

# MYSFTP v2.0.3 — Universal Directory Parser & Fast Navigation
- Optimalisasi askpass caching, universal directory parser, dan instant optimistic cache.

---

# MYSFTP v2.0.2 — Hotfix JSON Parsing & Robust ZIP Archiving Engine
- Perbaikan parsing `size` integer JSON dan delimiter base64 untuk isolasi banner SSH.

---

# MYSFTP v2.0.1 — Hotfix & Solid ZIP Archiving Engine
- Engine ZipPacker C# Internal, Luxury Glassmorphism Modals, Batch actions, dan terminal responsif.

---

## 📦 File Rilis Resmi (Clean Single Assets):
* 💻 **Windows PC:** `MYSFTP-v2.0.5-Setup.exe` (Installer tunggal resmi)
* 📱 **Android:** `MYSFTP-v2.0.5.apk` (Aplikasi Android resmi)

## Cara pakai
1. Jalankan `MYSFTP-Setup.exe` di Windows, pilih folder instalasi (misalnya `D:\Apps\MYSFTP`).
2. Selesai install, buka MYSFTP dari Start Menu / Desktop.
