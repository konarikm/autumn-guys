using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AutumnGuys
{
    /// <summary>
    /// Level flow: countdown → running (timer) → finished. Handles checkpoints, respawn after falling,
    /// pause and best time.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameState { Countdown, Running, Paused, Finished }

        /// <summary>Race = reach the finish fast (checkpoints, respawn). Survival = last as long as possible, falling ends the run.</summary>
        public enum LevelMode { Race, Survival }

        public static GameManager Instance { get; private set; }

        /// <summary>Survival scoring: Time = seconds survived + bonus, Rounds = rounds survived.</summary>
        public enum ScoreType { Time, Rounds }

        public LevelMode mode = LevelMode.Race;
        public ScoreType scoreType = ScoreType.Time;
        public PlayerController player;
        public ThirdPersonCamera playerCamera;
        public Transform startPoint;
        [Tooltip("Player respawns when falling below this height.")]
        public float killHeight = -12f;
        public float countdownSeconds = 3f;

        public GameState State { get; private set; } = GameState.Countdown;
        public float ElapsedTime { get; private set; }
        public int FallCount { get; private set; }
        public string LevelId => SceneManager.GetActiveScene().name;

        public bool IsSurvival => mode == LevelMode.Survival;
        /// <summary>Survival: bonus seconds collected (golden acorns).</summary>
        public float BonusTime { get; private set; }
        public int BonusCount { get; private set; }
        /// <summary>Survival (Rounds scoring): rounds survived.</summary>
        public int Rounds { get; private set; }
        public bool ScoresRounds => scoreType == ScoreType.Rounds;
        /// <summary>Survival score = rounds survived, or time survived + bonus.</summary>
        public float Score => ScoresRounds ? Rounds : ElapsedTime + BonusTime;
        /// <summary>Formats a score value of this level for display.</summary>
        public string FormatScore(float score) => ScoresRounds
            ? $"{score:0} {(Mathf.RoundToInt(score) == 1 ? "round" : "rounds")}"
            : SaveData.FormatTime(score);
        public LevelHUD Hud => hud;

        LevelHUD hud;
        Checkpoint currentCheckpoint;
        GameState stateBeforePause;
        bool respawning;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            hud = gameObject.AddComponent<LevelHUD>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        IEnumerator Start()
        {
            SetCursorLocked(true);
            if (startPoint != null) player.Teleport(startPoint.position, startPoint.rotation);
            playerCamera.SnapBehindTarget();
            player.ControlsEnabled = false;

            for (int i = Mathf.CeilToInt(countdownSeconds); i > 0; i--)
            {
                hud.ShowCountdown(i.ToString());
                yield return new WaitForSeconds(1f);
            }
            hud.ShowCountdown("START!");
            player.ControlsEnabled = true;
            State = GameState.Running;
            yield return new WaitForSeconds(0.8f);
            hud.ShowCountdown(null);
        }

        void Update()
        {
            var kb = Keyboard.current;
            bool pausePressed = (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                                (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

            if (pausePressed && State != GameState.Finished)
            {
                if (State == GameState.Paused) Resume(); else Pause();
            }

            if (State == GameState.Paused || State == GameState.Finished) return;

            if (State == GameState.Running) ElapsedTime += Time.deltaTime;

            // Manual respawn if stuck.
            if (!IsSurvival && kb != null && kb.rKey.wasPressedThisFrame && State == GameState.Running) RespawnPlayer(false);

            if (player.transform.position.y < killHeight && !respawning)
            {
                if (IsSurvival) GameOver();
                else RespawnPlayer(true);
            }

            // Re-lock the cursor after clicking back into the window.
            if (Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);
        }

        public bool TrySetCheckpoint(Checkpoint cp)
        {
            if (currentCheckpoint != null && cp.index <= currentCheckpoint.index) return false;
            currentCheckpoint = cp;
            hud.Flash("CHECKPOINT!");
            return true;
        }

        public void RespawnPlayer(bool fell)
        {
            if (fell) FallCount++;
            StartCoroutine(RespawnRoutine(fell));
        }

        IEnumerator RespawnRoutine(bool fell)
        {
            respawning = true;
            if (fell) hud.Flash("OOPS!");
            yield return hud.FadeRoutine(0f, 1f, 0.15f);

            Vector3 pos = currentCheckpoint != null ? currentCheckpoint.SpawnPosition : startPoint.position;
            Quaternion rot = currentCheckpoint != null ? currentCheckpoint.SpawnRotation : startPoint.rotation;
            player.Teleport(pos, rot);
            playerCamera.SnapBehindTarget();

            yield return hud.FadeRoutine(1f, 0f, 0.25f);
            respawning = false;
        }

        public void Finish()
        {
            if (State != GameState.Running) return;
            State = GameState.Finished;
            player.Celebrate();
            bool record = SaveData.SubmitTime(LevelId, ElapsedTime);
            StartCoroutine(ShowWinDelayed(record));
        }

        IEnumerator ShowWinDelayed(bool record)
        {
            yield return new WaitForSeconds(1.5f);
            SetCursorLocked(false);
            hud.ShowWin(ElapsedTime, SaveData.GetBestTime(LevelId), record, FallCount);
        }

        /// <summary>Survival: adds bonus seconds to the score.</summary>
        public void AddBonus(float seconds)
        {
            if (State != GameState.Running) return;
            BonusTime += seconds;
            BonusCount++;
            hud.Flash($"+{seconds:0} s");
        }

        /// <summary>Survival (Rounds scoring): the player survived a round.</summary>
        public void AddRound()
        {
            if (State != GameState.Running) return;
            Rounds++;
        }

        /// <summary>Survival: the player fell out of the arena.</summary>
        public void GameOver()
        {
            if (State != GameState.Running && State != GameState.Countdown) return;
            State = GameState.Finished;
            player.ControlsEnabled = false;
            player.ExternalVelocity = Vector3.zero;
            hud.Flash("WHOOPS!");
            bool record = SaveData.SubmitScore(LevelId, Score);
            StartCoroutine(ShowGameOverDelayed(record));
        }

        IEnumerator ShowGameOverDelayed(bool record)
        {
            yield return new WaitForSeconds(1.2f);
            SetCursorLocked(false);
            hud.ShowGameOver(ElapsedTime, BonusTime, BonusCount, Score, SaveData.GetBestScore(LevelId), record);
        }

        public void Pause()
        {
            if (State == GameState.Paused) return;
            stateBeforePause = State;
            State = GameState.Paused;
            Time.timeScale = 0f;
            SetCursorLocked(false);
            hud.ShowPause(true);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = stateBeforePause;
            Time.timeScale = 1f;
            SetCursorLocked(true);
            hud.ShowPause(false);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void GoToMenu()
        {
            Time.timeScale = 1f;
            SetCursorLocked(false);
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
