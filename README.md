<p align="center">
  <img width="100%" src="https://capsule-render.vercel.app/api?type=waving&color=0:1D976C,50:56CCF2,100:93F9B9&height=260&section=header&text=MINI%20PLATFORMER&fontSize=64&fontColor=ffffff&fontAlignY=38&stroke=0b3d2e&strokeWidth=2&desc=%F0%9F%8D%8C%20Jump%20%E2%80%A2%20Dodge%20%E2%80%A2%20Collect%20%E2%80%A2%20Reach%20the%20Flag%20%F0%9F%8F%81&descSize=22&descAlignY=60&animation=twinkling" alt="Mini Platformer" />
</p>

<p align="center">
  <img src="https://readme-typing-svg.demolab.com?font=Press+Start+2P&size=16&duration=2500&pause=700&color=1D976C&center=true&vCenter=true&width=720&height=50&lines=PLAYER+1+READY;%3E+JUMP+THE+SPIKES;%3E+DODGE+THE+SAWS;%3E+GRAB+THE+BANANAS+%F0%9F%8D%8C;%3E+REACH+THE+FLAG+%F0%9F%8F%81" alt="tagline" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3%20LTS-000000?style=for-the-badge&logo=unity&logoColor=white" />
  <img src="https://img.shields.io/badge/C%23-Gameplay-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/Genre-2D%20Platformer-1D976C?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Art-Pixel-56CCF2?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Controls-Keyboard%20%2B%20Touch-93F9B9?style=for-the-badge&logoColor=black" />
</p>

---

<h2 align="center">🕹️ THE GAME</h2>

<p align="center">
A fast, bite-sized <b>2D pixel-art platformer</b>.<br/>
Run, jump and ride moving platforms through hand-built levels packed with <b>spikes</b> and <b>spinning saws</b>.<br/>
Collect every 🍌 banana, hit the checkpoints, and reach the 🏁 <b>finish flag</b>.
</p>

---

<h2 align="center">🗺️ GAME FLOW</h2>

```mermaid
flowchart LR
    A(["🌟 Start Screen"]):::s --> B["📋 Level Select"]:::m --> C["🏞️ Level 1"]:::l1 --> D["🌄 Level 2"]:::l2 --> E(["🏆 Victory!"]):::s
    C -. "💀 hit a hazard" .-> K["🚩 Checkpoint"]:::k -.-> C
    D -. "💀 hit a hazard" .-> K2["🚩 Checkpoint"]:::k -.-> D
    C <-. "⏸️" .-> P["⏸️ Pause Menu"]:::m
    classDef s fill:#1D976C,color:#fff,stroke:#fff
    classDef m fill:#56CCF2,color:#000,stroke:#fff
    classDef l1 fill:#93F9B9,color:#000,stroke:#fff
    classDef l2 fill:#F7B267,color:#000,stroke:#fff
    classDef k fill:#FF5E7E,color:#fff,stroke:#fff
```

---

<p align="center">
  <img width="100%" src="docs/readme/hazards.svg" alt="Level contents: spikes, saws, platforms, bananas, finish flag" />
</p>

---

<h2 align="center">🎮 CONTROLS</h2>

<table align="center">
<tr><th>Action</th><th>⌨️ Keyboard</th><th>📱 Touch</th></tr>
<tr><td>🏃 Move</td><td><kbd>A</kbd> <kbd>D</kbd> / <kbd>←</kbd> <kbd>→</kbd></td><td>On-screen joystick</td></tr>
<tr><td>🦘 Jump</td><td><kbd>Space</kbd></td><td>Jump button</td></tr>
<tr><td>⏸️ Pause · Resume · Restart</td><td colspan="2" align="center">On-screen pause button</td></tr>
</table>

---

<p align="center">
  <img width="100%" src="docs/readme/systems.svg" alt="Game systems: player controller, animation states, lives, checkpoints, parallax, menus" />
</p>

---

<h2 align="center">🚀 RUN IT</h2>

```bash
git clone https://github.com/vishwaMoger/VishwaMoger_MiniPlatformer.git
```

1. Open the folder in **Unity 2022.3 LTS**.
2. Open `Assets/Scenes/MainMenu.unity`.
3. Press ▶️ **Play**.

---

<p align="center">
  <img src="https://readme-typing-svg.demolab.com?font=Press+Start+2P&size=14&pause=1200&color=1D976C&center=true&vCenter=true&width=500&lines=GAME+OVER%3F;INSERT+COIN+TO+CONTINUE" alt="game over" /><br/>
  <b>Made with ❤️ and 🍌 by <a href="https://github.com/vishwaMoger">Vishwa Moger</a></b><br/>
  <sub>Game Developer · 3+ years · Bangalore, India</sub>
</p>

<img width="100%" src="https://capsule-render.vercel.app/api?type=waving&color=0:93F9B9,50:56CCF2,100:1D976C&height=120&section=footer" />
