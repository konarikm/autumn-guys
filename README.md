# Autumn Guys
Jednoduchá 3D hra inspirovaná Fall Guys – tři minihry, hlavní menu a lokální rekordy.
Unity **6000.6.1f1**, URP, Input System.

Tým **Gin & Tonic** – František Hromek (programování), Martin Koňařík (level design, grafika).

## První spuštění
1. Otevři projekt v Unity 6000.6.1f1.
2. Otevři scénu `Assets/_Game/Scenes/Splash` a dej Play.

Scény jsou v repozitáři už vygenerované. Pokud je potřeba je postavit znovu, slouží k tomu menu
**Autumn Guys** v horní liště:

| Položka | Co dělá |
|---|---|
| **Build Everything (overwrites scenes)** | Vygeneruje materiály, meshe, prefaby, všech pět scén a nastaví Build Settings |
| **Rebuild AutumnRush / LeafPile / HarvestMemory / MainMenu only** | Přestaví jen jednu scénu |
| **Convert Standard materials to URP** | Převede materiály z Asset Store balíčků na URP |
| **Reset best time** | Smaže uložené rekordy a nastavení (PlayerPrefs) |

> Pozor: generátor **přepíše** ruční úpravy v přestavované scéně. Ručně upravené scény už
> nepřestavuj (nebo jen vědomě konkrétní scénu přes „Rebuild … only“).

## Průchod hrou
`Splash` (logo týmu) → `MainMenu` (Play / Controls / Exit) → výběr minihry s nejlepším výsledkem →
minihra → výsledková obrazovka (Play Again / Menu). Přechody mezi scénami mají prolínání.

## Minihry
### Autumn Rush (`AutumnRush`) – na čas
Start → pohyblivé plošiny nad propastí → **CP1** → rotující klády → otáčející se kláda-most
→ **CP2** → padající dlaždice s mezerami → **CP3** → kopec s valícími se dýněmi → **CÍL**.
Ukládá se nejlepší čas.

### Leaf Pile (`LeafPile`) – přežití
Tři patra šestiúhelníkových hromádek listí (91 dlaždic na patro). Listí pod hráčem se zatřese a propadne,
z nebe padají žaludy (ničí dlaždice), vítr hráče odfukuje do strany. Zlaté žaludy přidávají +5 s.
Obtížnost roste ~2 minuty. Skóre = doba přežití + bonusy.

### Harvest Memory (`HarvestMemory`) – paměť
Mřížka 5×5 dlaždic. Na dlaždicích se ukážou ikony (dýně, muchomůrka, žalud, list), pak se skryjí
a na ceduli se objeví hledaná ikona – když hráč nestojí na správné dlaždici, spadne. S každým kolem je méně
času i bezpečných dlaždic. Skóre = počet přežitých kol.

## Ovládání
WASD / šipky – pohyb · Mezerník – skok · Myš – kamera · R – návrat na checkpoint (Autumn Rush) · Esc – pauza.
Gamepad: levá páčka, A, pravá páčka, Start (B = zpět v menu).

## Struktura
| Složka | Obsah |
|---|---|
| `Assets/_Game/Scripts/Player` | `PlayerController` (Rigidbody pohyb, skok, knockback), `ThirdPersonCamera` |
| `Assets/_Game/Scripts/Obstacles` | `MovingPlatform`, `Rotator`, `FallingPlatform`, `PumpkinSpawner`, `KnockbackOnHit`, `Bouncer` |
| `Assets/_Game/Scripts/Game` | `GameManager` (odpočet, časomíra, respawn, pauza, režim na čas / přežití), `Checkpoint`, `FinishLine`, `SaveData` (rekordy), `SceneLoader` (prolínání scén), `MinigameInfo` (názvy a popisy miniher) |
| `Assets/_Game/Scripts/LeafPile` | `LeafPileArena` (obtížnost, patra), `LeafTile`, `AcornDropper`, `FallingAcorn`, `WindGust`, `GoldenAcorn(Spawner)` |
| `Assets/_Game/Scripts/HarvestMemory` | `MemoryArena` (průběh kol), `MemoryTile` |
| `Assets/_Game/Scripts/UI` | Splash, hlavní menu, HUD (`LevelHUD`, `HudWidgets`), animované logo (`TitleLogo`) – UI se staví z kódu (`UIFactory`) |
| `Assets/_Game/Scripts/Audio` | `AudioManager` – hudba podle scény (crossfade) a zvukové efekty |
| `Assets/_Game/Scripts/Editor` | `AutumnGuysBuilder` (generátor scén), `AudioImportSettings` (import zvuků) |
| `Assets/_Game/Resources` | Logo týmu, font Lilita One (OFL), `Audio/Music/<Scéna>`, `Audio/Sfx/<Efekt>/` (varianty se střídají náhodně) |
| `Assets/_Game/Scenes` | `Splash`, `MainMenu`, `AutumnRush`, `LeafPile`, `HarvestMemory` |
| `Assets/Asset Store` | ForestPack, Halloween, Mushrooms, PartyGameCharacters, SnowyAutumn |

## Zvuk
Hudba a efekty jsou CC0 (Kenney, OpenGameArt) – seznam autorů v `Assets/_Game/Resources/Audio/CREDITS.txt`.

## Windows build
File → Build Profiles → Windows → Build (vyžaduje modul *Windows Build Support* v Unity Hub).
Build Settings obsahují všech pět scén, první je `Splash`.
