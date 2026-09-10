using System;
using UnityEngine;
using KitchenGame.Core;
using KitchenGame.Ingredients;
using KitchenGame.Stations;

namespace KitchenGame.Player
{
    /// <summary>
    /// Handles player movement (WASD / Arrow Keys) and context-sensitive interaction (E / Space).
    /// Supports both New Input System and Legacy Input Manager seamlessly.
    /// Manages the single held ingredient slot.
    /// Detects nearby interactable stations via OverlapSphere each frame.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // ── Config ─────────────────────────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float moveSpeed       = 5.5f;
        [SerializeField] private float interactRadius  = 1.8f;
        [SerializeField] private LayerMask interactLayer = ~0; // default to Everything

        [Header("Held Item")]
        [SerializeField] private Transform holdPoint;          // child transform above player head

        [Header("Ingredient Prefab")]
        [SerializeField] private GameObject ingredientPrefab;  // simple cube prefab with IngredientObject

        // ── State ──────────────────────────────────────────────────────────────
        public IngredientObject HeldIngredient { get; private set; } = null;
        public bool             HasItem        => HeldIngredient != null;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action<IngredientObject> OnPickedUp;
        public event Action                   OnDropped;
        public event Action<string>           OnInteractHintChanged; // UI hint text

        // ── Internal ───────────────────────────────────────────────────────────
        private CharacterController _cc;
        private IInteractable       _nearestInteractable;
        private string              _currentHint = "";

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += state =>
                {
                    if (state == GameState.Playing && HasItem)
                        DiscardItem();
                };
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            HandleMovement();
            DetectInteractable();
            HandleInteractInput();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Give the player an ingredient. Fails silently if hands are full.</summary>
        public bool TryPickUp(IngredientObject ingredient)
        {
            if (HasItem || ingredient == null) return false;

            HeldIngredient = ingredient;
            ingredient.transform.SetParent(holdPoint != null ? holdPoint : transform);
            ingredient.transform.localPosition = Vector3.zero;
            ingredient.transform.localRotation = Quaternion.identity;
            ingredient.SetVisible(true);

            OnPickedUp?.Invoke(ingredient);
            return true;
        }

        /// <summary>Remove and return the held ingredient (does not destroy it).</summary>
        public IngredientObject TakeItem()
        {
            if (!HasItem) return null;

            var item = HeldIngredient;
            HeldIngredient = null;
            item.transform.SetParent(null);
            OnDropped?.Invoke();
            return item;
        }

        /// <summary>Destroy the held ingredient (used by trash).</summary>
        public void DiscardItem()
        {
            if (!HasItem) return;
            Destroy(HeldIngredient.gameObject);
            HeldIngredient = null;
            OnDropped?.Invoke();
        }

        /// <summary>
        /// Spawn a new ingredient from data and place it in the player's hand.
        /// Called by Refrigerator.
        /// </summary>
        public bool TrySpawnAndPickUp(IngredientData data)
        {
            if (HasItem) return false;
            if (ingredientPrefab == null)
            {
                Debug.LogError("[PlayerController] ingredientPrefab is not assigned!");
                return false;
            }

            Transform parent = holdPoint != null ? holdPoint : transform;
            GameObject go    = Instantiate(ingredientPrefab, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            var obj = go.GetComponent<IngredientObject>();
            if (obj == null) obj = go.AddComponent<IngredientObject>();
            obj.Initialize(data);

            HeldIngredient = obj;
            OnPickedUp?.Invoke(obj);
            return true;
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void HandleMovement()
        {
            float h = 0f;
            float v = 0f;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Mathf.Approximately(h, 0f)) h = Input.GetAxisRaw("Horizontal");
                if (Mathf.Approximately(v, 0f)) v = Input.GetAxisRaw("Vertical");
            }
            catch {}
#endif

            Vector3 dir = new Vector3(h, 0f, v).normalized;
            if (dir.sqrMagnitude > 0.01f)
            {
                _cc.Move(dir * moveSpeed * Time.deltaTime);
                // Rotate smoothly towards movement direction
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
            }

            // Grounding check
            if (!_cc.isGrounded)
                _cc.Move(Vector3.down * 4f * Time.deltaTime);
        }

        private void DetectInteractable()
        {
            LayerMask mask = interactLayer.value == 0 ? ~0 : interactLayer;
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius, mask);

            IInteractable best  = null;
            float         bestD = float.MaxValue;

            foreach (var col in hits)
            {
                var interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;

                float d = Vector3.Distance(transform.position, col.transform.position);
                if (d < bestD)
                {
                    bestD = d;
                    best  = interactable;
                }
            }

            _nearestInteractable = best;

            // Update hint text
            string hint = best != null ? best.GetInteractHint(this) : "";
            if (hint != _currentHint)
            {
                _currentHint = hint;
                OnInteractHintChanged?.Invoke(hint);
            }
        }

        private void HandleInteractInput()
        {
            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                    interactPressed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                    interactPressed = true;
            }
            catch {}
#endif

            if (!interactPressed) return;
            if (_nearestInteractable == null) return;
            if (!_nearestInteractable.CanInteract(this)) return;

            _nearestInteractable.Interact(this);
        }

        // ── Gizmos ─────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
