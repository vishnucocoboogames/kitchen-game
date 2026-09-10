using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KitchenGame.Core;
using KitchenGame.Player;

namespace KitchenGame.UI
{
    /// <summary>
    /// Master HUD + menu manager.
    /// Wires all UI panels to GameManager events and button callbacks.
    /// Uses TextMeshProUGUI for all text displays.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ── Main Menu ──────────────────────────────────────────────────────────
        [Header("Main Menu")]
        [SerializeField] public GameObject     mainMenuPanel;
        [SerializeField] public Button         startButton;
        [SerializeField] public TextMeshProUGUI controlsText;

        // ── HUD ────────────────────────────────────────────────────────────────
        [Header("HUD")]
        [SerializeField] public GameObject     hudPanel;
        [SerializeField] public TextMeshProUGUI scoreText;
        [SerializeField] public TextMeshProUGUI highScoreText;
        [SerializeField] public TextMeshProUGUI timerText;
        [SerializeField] public Button         pauseButton;
        [SerializeField] public TextMeshProUGUI pauseButtonText;
        [SerializeField] public Button         quitButton;
        [SerializeField] public TextMeshProUGUI interactHintText; // "Press E - Refrigerator"

        // ── Pause Overlay ──────────────────────────────────────────────────────
        [Header("Pause Overlay")]
        [SerializeField] public GameObject pausePanel;
        [SerializeField] public Button     resumeButton;

        // ── Game Over ──────────────────────────────────────────────────────────
        [Header("Game Over")]
        [SerializeField] public GameObject     gameOverPanel;
        [SerializeField] public TextMeshProUGUI finalScoreText;
        [SerializeField] public TextMeshProUGUI finalHighScoreText;
        [SerializeField] public TextMeshProUGUI newHighScoreText;   // "New High Score!"
        [SerializeField] public Button         restartButton;

        // ── Player ref ─────────────────────────────────────────────────────────
        [Header("Player")]
        [SerializeField] public PlayerController playerController;

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();
        }

        private void Start()
        {
            if (controlsText)
            {
                controlsText.text = "<b>HOW TO PLAY:</b>\n" +
                                    "- <b>WASD / Arrow Keys</b>: Move Chef\n" +
                                    "- <b>E / Space</b>: Interact with Stations\n" +
                                    "- <b>1 / 2 / 3</b>: Quick Pick at Refrigerator\n\n" +
                                    "<b>RECIPES & WORKFLOW:</b>\n" +
                                    "- <b>Vegetable (20 pts)</b>: Chop on Table (2s)\n" +
                                    "- <b>Cheese (10 pts)</b>: Ready immediately (no prep)\n" +
                                    "- <b>Meat (30 pts)</b>: Cook on Stove (6s)\n\n" +
                                    "Deliver to Customer Windows before time ticks down!\n" +
                                    "Score = Ingredients Sum - Elapsed Seconds.";
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnStateChanged      += HandleStateChange;
                gm.OnScoreChanged      += UpdateScore;
                gm.OnHighScoreChanged  += UpdateHighScore;
                gm.OnTimerTick         += UpdateTimer;

                UpdateHighScore(gm.HighScore);
            }

            if (playerController != null)
                playerController.OnInteractHintChanged += UpdateInteractHint;

            if (startButton)   startButton.onClick.AddListener(() => gm?.StartGame());
            if (pauseButton)   pauseButton.onClick.AddListener(() => gm?.TogglePause());
            if (resumeButton)  resumeButton.onClick.AddListener(() => gm?.ResumeGame());
            if (quitButton)    quitButton.onClick.AddListener(() => gm?.QuitGame());
            if (restartButton) restartButton.onClick.AddListener(() => gm?.RestartGame());

            ShowMainMenu();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnStateChanged     -= HandleStateChange;
                gm.OnScoreChanged     -= UpdateScore;
                gm.OnHighScoreChanged -= UpdateHighScore;
                gm.OnTimerTick        -= UpdateTimer;
            }

            if (playerController != null)
                playerController.OnInteractHintChanged -= UpdateInteractHint;
        }

        // ── State Handlers ─────────────────────────────────────────────────────

        private void HandleStateChange(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:  ShowMainMenu();  break;
                case GameState.Playing:   ShowHUD();       break;
                case GameState.Paused:    ShowPause();     break;
                case GameState.GameOver:  ShowGameOver();  break;
            }
        }

        private void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(hudPanel,      false);
            SetPanelActive(pausePanel,    false);
            SetPanelActive(gameOverPanel, false);
        }

        private void ShowHUD()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(hudPanel,      true);
            SetPanelActive(pausePanel,    false);
            SetPanelActive(gameOverPanel, false);

            if (GameManager.Instance != null)
                UpdateScore(GameManager.Instance.CurrentScore);
            if (pauseButtonText) pauseButtonText.text = "Pause";
        }

        private void ShowPause()
        {
            SetPanelActive(pausePanel, true);
            if (pauseButtonText) pauseButtonText.text = "Resume";
        }

        private void ShowGameOver()
        {
            SetPanelActive(hudPanel,      false);
            SetPanelActive(pausePanel,    false);
            SetPanelActive(gameOverPanel, true);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                bool isNewHigh = gm.CurrentScore >= gm.HighScore && gm.CurrentScore > 0;
                if (finalScoreText)     finalScoreText.text     = $"Final Score: {gm.CurrentScore}";
                if (finalHighScoreText) finalHighScoreText.text = $"High Score:  {gm.HighScore}";
                if (newHighScoreText)   newHighScoreText.gameObject.SetActive(isNewHigh);
            }
        }

        // ── Update Handlers ────────────────────────────────────────────────────

        private void UpdateScore(int score)
        {
            if (scoreText) scoreText.text = $"Score: {score}";
        }

        private void UpdateHighScore(int hs)
        {
            if (highScoreText) highScoreText.text = $"High Score: {hs}";
        }

        private void UpdateTimer(float remaining)
        {
            if (!timerText) return;
            int mins = Mathf.FloorToInt(remaining / 60f);
            int secs = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"{mins:00}:{secs:00}";
            timerText.color = remaining < 30f ? new Color(1f, 0.25f, 0.25f) : Color.white;
        }

        private void UpdateInteractHint(string hint)
        {
            if (interactHintText)
            {
                interactHintText.text = hint;
                interactHintText.gameObject.SetActive(!string.IsNullOrEmpty(hint));
            }
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel) panel.SetActive(active);
        }
    }
}
