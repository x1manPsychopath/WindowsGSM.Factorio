<p align="center">
  <img src="icon.png" width="128" alt="Factorio Plugin Icon">
</p>

<h1 align="center">WindowsGSM.Factorio</h1>

<p align="center">
  A fully updated and modernized WindowsGSM plugin for hosting Factorio Dedicated Servers.
</p>

<p align="center">
  <a href="https://github.com/YOURNAME/WindowsGSM.Factorio-main/releases">
    <img src="https://img.shields.io/github/v/release/YOURNAME/WindowsGSM.Factorio-main?color=gold&label=version" alt="Version">
  </a>
  <img src="https://img.shields.io/badge/Factorio-Dedicated%20Server-orange" alt="Factorio">
  <img src="https://img.shields.io/badge/WindowsGSM-Plugin-blue" alt="WindowsGSM">
</p>

---
# WindowsGSM.Factorio  
A fully updated and corrected WindowsGSM plugin for hosting **Factorio Dedicated Servers** using SteamCMD.

This plugin fixes the issues found in older community versions, including:
- Correct Steam App ID (`894490`)
- Correct executable path (`bin/Factorio.exe`)
- Anonymous SteamCMD login
- Automatic save creation
- Proper server-settings.json generation
- Clean start parameters
- Correct working directory
- Accurate install/import validation

---

## 📦 Installation

1. Download or clone this repository.
2. Place the folder **WindowsGSM.Factorio** into: WindowsGSM/plugins/
3. Restart WindowsGSM.
4. Add a new server and select:
**Factorio Dedicated Server [Factorio.cs]**

---

## 🛠 Features

- ✔ Fully working SteamCMD installation  
- ✔ Automatic save creation on first launch  
- ✔ Auto‑generated `server-settings.json`  
- ✔ Supports embedded console  
- ✔ Correct query method (A2S)  
- ✔ Clean, modernized codebase  

---

## 🔧 Configuration

After installation, the plugin automatically creates: serverfiles/data/server-settings.jso

You may edit this file to customize:
- Server name  
- Description  
- Tags  
- Max players  
- Visibility  

Save files are stored as: <mapname>_save.zip

---

## 🚀 Start Parameters

The plugin launches Factorio using: --start-server "<savefile>" --server-settings "<settingsfile>" --port <port>

Additional parameters can be added in WindowsGSM under **Server Params**.

---

## 🧪 Tested With

- WindowsGSM 1.22+
- Factorio Dedicated Server (Steam App ID 894490)
- Windows 10 / Windows 11

---

## 📜 License

This project is licensed under the MIT License.  
See the `LICENSE` file for details.

---

## 🤝 Credits

- Plugin rewritten and modernized by **Joshua + Copilot**
- Original concept by Andy

- ![Version](https://img.shields.io/github/v/release/x1manPsychopath/WindowsGSM.Factorio-main?style=flat-square&color=gold)
