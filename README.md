# 🏗️ Surveyor Simulator V.1

> Simulasi pengukuran geodesi menggunakan Total Station dalam lingkungan kota 3D.

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue?logo=unity)
![License](https://img.shields.io/badge/License-Academic-green)

## 📖 Tentang Project

**Surveyor Simulator** adalah game simulasi edukasi yang memungkinkan pengguna untuk belajar dan berlatih penggunaan alat ukur **Total Station** dalam lingkungan kota 3D yang realistis.

### Fitur Utama
- 🏙️ **3D City Environment** — Menggunakan asset Demo City By Versatile Studio
- 📐 **Total Station Simulation** — Setup, leveling, dan pengukuran
- 🎯 **Prism Target System** — Target pengukuran yang tersebar di kota
- 🚶 **First-Person Controller** — Navigasi kota secara bebas (WASD + Mouse)
- 📋 **Tutorial System** — Panduan langkah-demi-langkah setup Total Station
- 🛡️ **Auto Ground Detection** — Sistem raycast untuk spawn position yang akurat

## 🚀 Cara Menjalankan

### Prerequisites
- Unity **2021.3.9f1** atau lebih tinggi (URP)
- Git

### Langkah Setup

1. **Clone repository**
   ```bash
   git clone https://github.com/jadilebihbaikcaripanggilanmu-bot/3DCITY.git
   ```

2. **Buka di Unity Hub**
   - Add → Browse → pilih folder `My project`
   - Tunggu Unity import semua asset

3. **Setup Scene**
   - Buka `Assets/Scenes/SampleScene.unity`
   - Klik menu **Surveyor ▸ Setup City Scene**
   - Tekan **Ctrl+S** untuk save
   - Tekan **▶ Play**

### Kontrol
| Input | Aksi |
|-------|------|
| WASD | Bergerak |
| Mouse | Melihat sekeliling |
| E | Interaksi dengan Total Station |
| ESC | Menu/Pause |

## 📁 Struktur Project

```
Assets/
├── _Game/
│   └── Scripts/          # Script game utama
│       ├── Bootstrap.cs
│       ├── GameManager.cs
│       ├── PlayerController.cs
│       ├── CameraController.cs
│       ├── TotalStation.cs
│       ├── CityBuilder.cs         # Procedural city (fallback)
│       ├── CityGroundFixer.cs     # Auto-collider & spawn detection
│       └── ...
├── Editor/
│   └── CitySceneSetup.cs  # Editor tool untuk setup scene
├── Scenes/
│   └── SampleScene.unity
└── Versatile Studio Assets/
    └── Demo City By Versatile Studio/
        ├── Prefabs/
        ├── Models/
        ├── Materials/
        └── Textures/
```

## 🏙️ Asset Credits

- **Demo City By Versatile Studio (Mobile Friendly)** — [Asset Store](https://assetstore.unity.com/)
  - Versatile Studio (versatilestudioproject@gmail.com)

## 📝 Catatan

Project ini dibuat sebagai tugas UAS simulasi survei geodesi.

---

*Surveyor Simulator V.1 © 2026*
