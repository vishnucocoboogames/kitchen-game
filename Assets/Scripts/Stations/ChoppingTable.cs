using UnityEngine;
using UnityEngine.UI;
using KitchenGame.Core;
using KitchenGame.Ingredients;
using KitchenGame.Player;
using KitchenGame.Stations;

using TMPro;

namespace KitchenGame.Stations
{
    /// <summary>
    /// Chopping Table station. Accepts only raw Vegetables.
    /// Only 1 vegetable at a time. Chop takes 2 seconds.
    /// Shows a progress bar while chopping. Player picks up when done.
    /// </summary>
    public class ChoppingTable : MonoBehaviour, IInteractable
    {
        // ── Config ─────────────────────────────────────────────────────────────
        [Header("Timer")]
        [SerializeField] public float chopDuration = 2f;

        [Header("UI")]
        [SerializeField] public GameObject     timerUI;       // world-space canvas panel
        [SerializeField] public Image          progressBar;   // filled image (fill amount 0–1)
        [SerializeField] public TextMeshProUGUI timerText;     // "1.4s" countdown

        [Header("Visual")]
        [SerializeField] public Transform  ingredientDisplayPoint; // where ingredient sits on table

        // ── State ──────────────────────────────────────────────────────────────
        private IngredientObject _currentVeg  = null;
        private StationTimer     _timer;
        private bool             _isChopping  = false;
        private bool             _readyToPickUp = false;

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _timer = gameObject.AddComponent<StationTimer>();
            _timer.OnComplete += OnChopComplete;
        }

        private void Start()
        {
            if (timerUI) timerUI.SetActive(false);
        }

        private void Update()
        {
            if (!_isChopping) return;

            // Update progress bar and text each frame
            if (progressBar) progressBar.fillAmount = _timer.Progress;
            if (timerText)   timerText.text = $"{_timer.TimeRemaining:F1}s";
        }

        // ── IInteractable ──────────────────────────────────────────────────────

        public bool CanInteract(PlayerController player)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return false;

            // Pick up: table has a ready vegetable and player has empty hands
            if (_readyToPickUp && !player.HasItem) return true;

            // Place: player holds a raw vegetable, table is empty
            if (player.HasItem
                && player.HeldIngredient.Data.ingredientType == IngredientType.Vegetable
                && !player.HeldIngredient.IsPrepared
                && _currentVeg == null)
                return true;

            return false;
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract(player)) return;

            if (_readyToPickUp && !player.HasItem)
            {
                // Hand chopped vegetable to player
                PickUpIngredient(player);
            }
            else if (player.HasItem)
            {
                // Place vegetable on table and start chopping
                PlaceIngredient(player);
            }
        }

        public string GetInteractHint(PlayerController player)
        {
            if (_readyToPickUp && !player.HasItem) return "Press E - Pick up chopped vegetable";
            if (_isChopping) return "Chopping vegetable...";
            if (_currentVeg != null) return "Chopping table in use";

            if (player.HasItem)
            {
                if (player.HeldIngredient.Data.ingredientType == IngredientType.Vegetable)
                {
                    if (player.HeldIngredient.IsPrepared) return "Vegetable is already chopped!";
                    return "Press E - Place Vegetable to chop";
                }
                return "Can only chop raw vegetables here";
            }

            return "Chopping Table (empty hands)";
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void PlaceIngredient(PlayerController player)
        {
            _currentVeg = player.TakeItem();
            Transform parent = ingredientDisplayPoint != null ? ingredientDisplayPoint : transform;
            _currentVeg.transform.SetParent(parent);
            _currentVeg.transform.localPosition = Vector3.zero;
            _currentVeg.SetVisible(true);

            _isChopping    = true;
            _readyToPickUp = false;

            if (timerUI) timerUI.SetActive(true);
            _timer.Begin(chopDuration);
        }

        private void OnChopComplete()
        {
            _isChopping    = false;
            _readyToPickUp = true;
            if (_currentVeg != null)
                _currentVeg.SetPrepared();

            if (timerUI) timerUI.SetActive(false);
        }

        private void PickUpIngredient(PlayerController player)
        {
            if (_currentVeg == null) return;
            _currentVeg.transform.SetParent(null);
            player.TryPickUp(_currentVeg);
            _currentVeg    = null;
            _readyToPickUp = false;
        }
    }
}
