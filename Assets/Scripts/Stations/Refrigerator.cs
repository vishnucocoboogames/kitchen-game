using UnityEngine;
using KitchenGame.Core;
using KitchenGame.Ingredients;
using KitchenGame.Player;
using KitchenGame.Stations;

namespace KitchenGame.Stations
{
    /// <summary>
    /// Refrigerator station. Shows a simple 3-option UI (Vegetable / Cheese / Meat)
    /// so the player can choose which ingredient to pick up. Never runs out.
    /// Supports both clicking UI buttons and pressing 1/2/3 or V/C/M keys.
    /// Player must have empty hands to interact.
    /// </summary>
    public class Refrigerator : MonoBehaviour, IInteractable
    {
        // ── Config ─────────────────────────────────────────────────────────────
        [Header("Ingredient Data Assets")]
        [SerializeField] public IngredientData vegetableData;
        [SerializeField] public IngredientData cheeseData;
        [SerializeField] public IngredientData meatData;

        [Header("Selection UI (world-space canvas)")]
        [SerializeField] public GameObject selectionPanel;   // shown when player is in range
        [SerializeField] public UnityEngine.UI.Button btnVegetable;
        [SerializeField] public UnityEngine.UI.Button btnCheese;
        [SerializeField] public UnityEngine.UI.Button btnMeat;

        // ── Internal ───────────────────────────────────────────────────────────
        private PlayerController _playerInRange;
        private bool             _panelOpen = false;

        // ──────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (selectionPanel != null) selectionPanel.SetActive(false);

            // Wire buttons
            if (btnVegetable) btnVegetable.onClick.AddListener(() => GiveIngredient(vegetableData));
            if (btnCheese)    btnCheese.onClick.AddListener(()    => GiveIngredient(cheeseData));
            if (btnMeat)      btnMeat.onClick.AddListener(()      => GiveIngredient(meatData));
        }

        private void Update()
        {
            if (!_panelOpen || _playerInRange == null) return;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.digit1Key.wasPressedThisFrame || kb.vKey.wasPressedThisFrame) GiveIngredient(vegetableData);
                else if (kb.digit2Key.wasPressedThisFrame || kb.cKey.wasPressedThisFrame) GiveIngredient(cheeseData);
                else if (kb.digit3Key.wasPressedThisFrame || kb.mKey.wasPressedThisFrame) GiveIngredient(meatData);
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.V)) GiveIngredient(vegetableData);
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.C)) GiveIngredient(cheeseData);
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.M)) GiveIngredient(meatData);
            }
            catch {}
#endif
        }

        // ── IInteractable ──────────────────────────────────────────────────────

        public bool CanInteract(PlayerController player)
        {
            return GameManager.Instance != null && GameManager.Instance.IsPlaying && !player.HasItem;
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract(player)) return;
            _playerInRange = player;
            TogglePanel(true);
        }

        public string GetInteractHint(PlayerController player)
        {
            if (player.HasItem) return "Hands full (can't take ingredient)";
            return _panelOpen ? "Press 1: Veg | 2: Cheese | 3: Meat" : "Press E - Refrigerator";
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void GiveIngredient(IngredientData data)
        {
            if (_playerInRange == null || data == null) return;
            _playerInRange.TrySpawnAndPickUp(data);
            TogglePanel(false);
            _playerInRange = null;
        }

        public void TogglePanel(bool open)
        {
            _panelOpen = open;
            if (selectionPanel != null) selectionPanel.SetActive(open);
        }

        // Close panel if player walks away
        private void OnTriggerExit(Collider other)
        {
            if (_panelOpen && other.GetComponent<PlayerController>() != null)
                TogglePanel(false);
        }
    }
}
