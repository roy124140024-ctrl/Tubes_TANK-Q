## i. Penjelasan Singkat Algoritma Greedy untuk Setiap Bot

### 1. TANK_Q (Main Bot)
Bot utama menggunakan **Greedy Target Scoring & Movement**. Fungsi seleksi memilih target berdasarkan kalkulasi jarak terdekat, kelemahan energi musuh, dan bonus target yang sama. Pergerakan dievaluasi dari beberapa kandidat arah (himpunan solusi) dan bot akan mengambil manuver dengan skor kelayakan tertinggi secara lokal.

### 2. BotKill (Alternative Bot 1)
Bot alternatif ini mengimplementasikan strategi **Greedy Proximity**. Heuristik yang digunakan adalah mencari jarak terdekat; musuh dengan jarak paling dekat akan langsung dikunci sebagai target utama untuk memaksimalkan efisiensi tembakan (*hit rate*).

### 3. BotViper (Alternative Bot 2)
Bot alternatif dengan strategi **Reactive Dodging & Linear Predictive Aiming**. Sifat *greedy* diterapkan pada respons instan (evasi peluru) yang langsung aktif ketika mendeteksi adanya penurunan energi musuh, dipadukan dengan prediksi arah gerak lawan untuk menembak.

### 4. VELORA (Alternative Bot 3)
Bot alternatif dengan strategi **Opportunistic Firepower & Randomized Evasion**. Bot bergerak secara acak untuk bertahan hidup, namun sangat *greedy* dalam penyerangan dengan menembakkan peluru berdaya hancur maksimal (Level 3) secara oportunis saat jarak musuh sangat dekat atau energi musuh sedang kritis.

---

## ii. Requirement Program dan Instalasi
* **.NET SDK** (Runtime pendukung C#)
* [cite_start]**Robocode Tank Royale GUI & Server** 
* **Robocode.TankRoyale.BotApi** (Library API)
* **Java Runtime Environment (JRE)** (Untuk mengeksekusi server)

---

## iii. Cara Menjalankan

**Melalui GUI Server:**
1. Buka server Robocode Tank Royale.
2. Masuk ke menu konfigurasi *Bot Directories*.
3. Tambahkan *path* folder bot yang ingin dijalankan.
4. Lakukan *Boot* pada bot yang dipilih.
5. Tambahkan bot ke dalam *Battle*.
6. Jalankan pertempuran.

**Melalui Terminal (Direkomendasikan):**
Arahkan terminal ke dalam folder bot masing-masing, lalu jalankan perintah:
```bash
dotnet run

## iv. Author (Identitas Pembuat)
* **Nama Kelompok:** TANK Q
* **Program Studi:** Teknik Informatika
* **Institusi:** Institut Teknologi Sumatera (ITERA)