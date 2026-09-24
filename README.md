# Haunted Hospital

A 3D first-person survival-horror shooter set in an abandoned hospital, built with Unity.

You are alone in a decaying hospital. Something hunts by sound, something else steals your supplies, and the building itself seems to be working against you. Scavenge, shoot, and stay quiet long enough to get out.

---

## Features

- **First-person shooting** in a fully explorable 3D hospital
- **Physics-based interaction:** open doors, push barricades, and throw objects to make noise or block paths
- **Sound-driven enemy:** a hunter that investigates gunshots, footsteps, slammed doors, and thrown objects
- **Resource thief:** an enemy that steals supplies and runs them back to its stash
- **Adaptive horror:** a director system that times lighting, sound, and scripted scares to the player's tension
- **Dynamic item placement:** supplies are placed differently each run by a constraint-solving spawn director
- **Custom characters and props** modelled in Blender

---

## Enemies and systems

| System | What it does | Techniques |
|---|---|---|
| **Sound Hunter** | Hears sounds, ranks them by importance, investigates, and searches the area | State machine / behaviour tree, sound-priority scoring, A* |
| **Resource Thief** | Picks the most valuable reachable supply, steals it, and flees to its stash | Utility AI, risk-aware A* |
| **Horror Director** | Chooses the next environmental scare (flicker, blackout, door slam, whisper) based on pacing | Fuzzy logic, heuristic look-ahead search |
| **Item Spawn Director** | Places supplies in valid, balanced locations | Constraint satisfaction, backtracking with MRV |

---

## Getting started

### Prerequisites

- **Unity 6.6 (6000.6.2f1)**. 
- **Git** and **Git LFS** (install LFS before cloning)
- **Blender** (only needed to edit source models)

### Setup

```bash
git lfs install
git clone https://github.com/it24103457/Haunted-Hospital.git
cd Haunted-Hospital
git lfs pull
```

Open the folder in **Unity Hub** (Add > Add project from disk).

---

## Project structure

```
Assets/
├── Agents/                  ← one folder per agent, owned by that member
│   ├── Thief/
│   ├── Hunter/
│   ├── EnvironmentDirector/
│   └── ItemSpawnDirector/
├── Art/                     ← shared, game-ready assets
│   ├── Models/
│   ├── Textures/
│   ├── Materials/
│   └── Animations/
├── Audio/
├── Prefabs/
├── Scenes/
│   ├── Main/                ← the hospital level (World Builder)
│   └── Sandbox/             ← per-member test scenes
├── Scripts/                 ← shared code (interfaces, registries, events)
└── Settings/                ← URP render settings
ArtSource/                   ← raw .blend / .psd files, not imported by Unity
```

- Commit `.meta` files together with their assets.
- Move and rename assets inside the Unity editor, not in Explorer.
- Only edit scenes and prefabs you own; changes to `Scripts/` should be agreed with the team.
