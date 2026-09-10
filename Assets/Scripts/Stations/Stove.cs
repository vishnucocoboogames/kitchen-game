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
    /// Stove station with 2 independent cooking slots.
    /// Each slot cooks meat in 6 seconds. Player doesn't need to stay.
    /// Shows per-slot countdown. Player picks up finished meat.
    /// </summary>
    public class Stove : MonoBehaviour, IInteractable
    {
        // ── Public slot data ───────────────────────────────────────────────────
        [System.Serializable]
        public class StoveSlot
        {
            public Transform       displayPoint;   // where meat sits visually
            public Image           progressBar;    // fill 0–1
            public TextMeshProUGUI timerText;      // "5.2s"
            public GameObject      timerUI;        // panel to show/hide

            [HideInInspector] public IngredientObject ingredient = null;
            [HideInInspector] public StationTimer     timer      = null;
            [HideInInspector] public bool             isCooking  = false;
            [HideInInspector] public bool             readyToPickUp = false;
        }

        // ── Config ─────────────────────────────────────────────────────────────
        [Header("Slots")]
        [SerializeField] public StoveSlot[] slots = new StoveSlot[2];
        [SerializeField] private float cookDuration = 6f;

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (slots == null) slots = new StoveSlot[2];

            // Create a StationTimer per slot and wire callbacks
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) slots[i] = new StoveSlot();
                int capturedIndex = i; // closure capture
                slots[i].timer = gameObject.AddComponent<StationTimer>();
                slots[i].timer.OnComplete += () => OnSlotCookComplete(capturedIndex);
            }
        }

        private void Start()
        {
            foreach (var slot in slots)
            {
                if (slot != null && slot.timerUI != null)
                    slot.timerUI.SetActive(false);
            }
        }

        private void Update()
        {
            foreach (var slot in slots)
            {
                if (slot == null || !slot.isCooking || slot.timer == null) continue;
                if (slot.progressBar) slot.progressBar.fillAmount = slot.timer.Progress;
                if (slot.timerText)   slot.timerText.text = $"{slot.timer.TimeRemaining:F1}s";
            }
        }

        // ── IInteractable ──────────────────────────────────────────────────────

        public bool CanInteract(PlayerController player)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return false;

            // Can pick up: player has empty hands and a slot is ready
            if (!player.HasItem && GetReadySlot() != null) return true;

            // Can place: player holds raw meat and there is a free slot
            if (player.HasItem
                && player.HeldIngredient.Data.ingredientType == IngredientType.Meat
                && !player.HeldIngredient.IsPrepared
                && GetFreeSlot() != null)
                return true;

            return false;
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract(player)) return;

            if (!player.HasItem)
            {
                // Pick up from the first ready slot
                var slot = GetReadySlot();
                if (slot != null) PickUpFromSlot(player, slot);
            }
            else
            {
                // Place in first free slot
                var slot = GetFreeSlot();
                if (slot != null) PlaceInSlot(player, slot);
            }
        }

        public string GetInteractHint(PlayerController player)
        {
            if (!player.HasItem)
            {
                if (GetReadySlot() != null) return "Press E - Pick up cooked meat";
                return "Stove (empty hands)";
            }

            if (player.HeldIngredient.Data.ingredientType == IngredientType.Meat)
            {
                if (player.HeldIngredient.IsPrepared) return "Meat is already cooked!";
                if (GetFreeSlot() != null) return "Press E - Cook Meat on Stove";
                return "Stove is full!";
            }

            return "Can only cook raw meat on stove";
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void PlaceInSlot(PlayerController player, StoveSlot slot)
        {
            slot.ingredient = player.TakeItem();
            Transform parent = slot.displayPoint != null ? slot.displayPoint : transform;
            slot.ingredient.transform.SetParent(parent);
            slot.ingredient.transform.localPosition = Vector3.zero;
            slot.ingredient.SetVisible(true);

            slot.isCooking     = true;
            slot.readyToPickUp = false;
            if (slot.timerUI) slot.timerUI.SetActive(true);
            slot.timer.Begin(cookDuration);
        }

        private void OnSlotCookComplete(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            var slot = slots[index];
            slot.isCooking     = false;
            slot.readyToPickUp = true;
            if (slot.ingredient != null)
                slot.ingredient.SetPrepared();
            if (slot.timerUI) slot.timerUI.SetActive(false);
        }

        private void PickUpFromSlot(PlayerController player, StoveSlot slot)
        {
            if (slot.ingredient == null) return;
            slot.ingredient.transform.SetParent(null);
            player.TryPickUp(slot.ingredient);
            slot.ingredient    = null;
            slot.readyToPickUp = false;
        }

        private StoveSlot GetFreeSlot()
        {
            foreach (var s in slots)
                if (s != null && s.ingredient == null && !s.isCooking) return s;
            return null;
        }

        private StoveSlot GetReadySlot()
        {
            foreach (var s in slots)
                if (s != null && s.readyToPickUp) return s;
            return null;
        }
    }
}
