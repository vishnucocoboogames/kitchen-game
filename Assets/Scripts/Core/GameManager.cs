using System;
using UnityEngine;
using KitchenGame.Core;

namespace KitchenGame.Core
{
    /// <summary>
    /// Central singleton that owns: game state, score, 3-minute countdown, and high score persistence.
    /// All other systems subscribe to events rather than polling this class.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Config ─────────────────────────────────────────────────────────────
        [Header("Game Settings")]
        [SerializeField] private float gameDuration = 180f; // 3 minutes

        // ── State ──────────────────────────────────────────────────────────────
        public GameState State    { get; private set; } = GameState.MainMenu;
        public float TimeRemaining { get; private set; }
        public int   CurrentScore  { get; private set; }
        public int   HighScore     { get; private set; }
        public bool  IsPlaying     => State == GameState.Playing;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action<GameState> OnStateChanged;
        public event Action<float>     OnTimerTick;      // seconds remaining
        public event Action<int>       OnScoreChanged;   // new current score
        public event Action<int>       OnHighScoreChanged;

        // ── Constants ──────────────────────────────────────────────────────────
        private const string HighScoreKey = "KitchenGame_HighScore";

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            TimeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(TimeRemaining);

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                EndGame();
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void StartGame()
        {
            CurrentScore  = 0;
            TimeRemaining = gameDuration;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
            OnScoreChanged?.Invoke(CurrentScore);
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) PauseGame();
            else if (State == GameState.Paused) ResumeGame();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            StartGame();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Add (or subtract) points. Called by OrderManager when an order completes.</summary>
        public void AddScore(int points)
        {
            CurrentScore += points;
            OnScoreChanged?.Invoke(CurrentScore);
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void EndGame()
        {
            Time.timeScale = 0f; // freeze everything
            bool newHigh = CurrentScore > HighScore;
            if (newHigh)
            {
                HighScore = CurrentScore;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
                OnHighScoreChanged?.Invoke(HighScore);
            }
            SetState(GameState.GameOver);
        }

        private void SetState(GameState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
