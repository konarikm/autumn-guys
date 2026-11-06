# Autumn Guys
Simple 3D obstacle course game inspired by Fall Guys, built in Unity (6000.6.1, URP).

Tým **Gin & Tonic** – František Hromek (programování), Martin Koňařík (level design, grafika).

## První spuštění
1. Otevři projekt v Unity 6000.6.1f1.
2. V menu nahoře klikni **Autumn Guys → Build Everything (overwrites scenes)**.
   Vygeneruje materiály, prefaby (`Assets/_Game/Prefabs`), scény `Splash`, `MainMenu`, `Level01`
   (`Assets/_Game/Scenes`) a nastaví Build Settings.
3. Otevři scénu `Splash` a dej Play.

> Pozor: opětovné spuštění generátoru **přepíše** ruční úpravy ve scénách. Až začne Martin
> scénu `Level01` ručně upravovat, generátor už nespouštějte (nebo jen "Rebuild Level01 only" vědomě).

## Ovládání
WASD / šipky – pohyb · Mezerník – skok · Myš – kamera · R – návrat na checkpoint · Esc – pauza.
Gamepad: levá páčka, A, pravá páčka, Start.

## Struktura
| Složka | Obsah |
|---|---|
| `Assets/_Game/Scripts/Player` | `PlayerController` (Rigidbody pohyb, skok, knockback), `ThirdPersonCamera` |
| `Assets/_Game/Scripts/Obstacles` | `MovingPlatform`, `Rotator` (rotující klády), `FallingPlatform`, `PumpkinSpawner`, `KnockbackOnHit`, `Bouncer` |
| `Assets/_Game/Scripts/Game` | `GameManager` (odpočet, časomíra, respawn, pauza, cíl), `Checkpoint`, `FinishLine`, `SaveData` (nejlepší čas) |
| `Assets/_Game/Scripts/UI` | Splash screen, hlavní menu, HUD – UI se staví z kódu (`UIFactory`) |
| `Assets/_Game/Scripts/Editor` | `AutumnGuysBuilder` – generátor scén |
| `Assets/_Game/Resources` | Logo týmu (`TeamLogo.png`), titulek hry (`GameTitle.png`) |

## Úroveň Level01
Start → pohyblivé plošiny nad propastí → **CP1** → rotující klády (přeskočit) → otáčející se kláda-most
→ **CP2** → padající dlaždice → **CP3** → kopec s valícími se dýněmi → **CÍL**.

## Windows build
File → Build Profiles → Windows → Build (vyžaduje modul *Windows Build Support* v Unity Hub).
