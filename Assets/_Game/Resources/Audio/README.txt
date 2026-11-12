Autumn Guys audio (loaded at runtime by AudioManager.cs)

Music/<SceneName>.*    - looping background track per scene (Splash uses MainMenu; per-track loudness
                         trim in MusicTrim, AudioManager.cs):
    MainMenu, AutumnRush, LeafPile, HarvestMemory

Sfx/<Name>/            - one or more clips per effect; a random one plays each time:
    Click        UI button press
    Countdown    3-2-1 beep              Go          "START!"
    Land         landing after a jump/fall
    Bounce       mushroom trampoline     Hit         knocked back (logs, pumpkins, acorns)
    Fall         fell off the course     Checkpoint  checkpoint reached
    Finish       crossed the finish      NewRecord   new best result
    GameOver     survival run ended
    TileShake    tile starts shaking     TileFall    tile(s) drop away
    AcornImpact  falling acorn lands     Wind        wind gust
    Pickup       golden acorn            Reveal      Harvest Memory icons appear
    Tick         Harvest Memory countdown            Safe        round survived

Missing clips are silent. Import settings are applied automatically (Editor/AudioImportSettings.cs).

Sources and licenses: see CREDITS.txt in this folder.
