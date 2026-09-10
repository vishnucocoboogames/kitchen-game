using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KitchenGame.Core;
using KitchenGame.Ingredients;
using KitchenGame.Orders;
using KitchenGame.Stations;

namespace KitchenGame.Orders
{
    /// <summary>
    /// Singleton that manages all 4 customer windows:
    /// spawns initial orders, handles respawn timers, exposes score value table.
    /// </summary>
    public class OrderManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static OrderManager Instance { get; private set; }

        // ── Config ─────────────────────────────────────────────────────────────
        [Header("Windows")]
        [SerializeField] public CustomerWindow[] customerWindows; // assign all 4 in Inspector

        [Header("Respawn")]
        [SerializeField] public float respawnDelay = 5f;

        [Header("Ingredient Score Values")]
        [SerializeField] public int vegetableScore = 20;
        [SerializeField] public int cheeseScore    = 10;
        [SerializeField] public int meatScore      = 30;

        // ── Public ─────────────────────────────────────────────────────────────
        public Dictionary<IngredientType, int> ScoreValues { get; private set; }

        // ── Internal ───────────────────────────────────────────────────────────
        private readonly IngredientType[] _allTypes =
        {
            IngredientType.Vegetable,
            IngredientType.Cheese,
            IngredientType.Meat
        };

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            ScoreValues = new Dictionary<IngredientType, int>
            {
                { IngredientType.Vegetable, vegetableScore },
                { IngredientType.Cheese,    cheeseScore    },
                { IngredientType.Meat,      meatScore      }
            };

            if (customerWindows == null || customerWindows.Length == 0)
            {
                customerWindows = FindObjectsByType<CustomerWindow>(FindObjectsSortMode.None);
                System.Array.Sort(customerWindows, (a, b) => a.windowIndex.CompareTo(b.windowIndex));
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void OnGameStateChanged(Core.GameState state)
        {
            if (state == Core.GameState.Playing)
                InitializeAllWindows();
        }

        /// <summary>Called at game start — spawns orders on all 4 windows immediately.</summary>
        public void InitializeAllWindows()
        {
            StopAllCoroutines();
            if (customerWindows == null) return;

            foreach (var w in customerWindows)
            {
                if (w == null) continue;
                w.ClearOrder();
                w.OnOrderCompleted -= OnWindowOrderCompleted;
                w.OnOrderCompleted += OnWindowOrderCompleted;
                w.AssignOrder(GenerateOrder());
            }
        }

        private void OnWindowOrderCompleted(CustomerWindow window)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            StartCoroutine(RespawnAfterDelay(window));
        }

        private IEnumerator RespawnAfterDelay(CustomerWindow window)
        {
            yield return new WaitForSeconds(respawnDelay);
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) yield break;
            if (window != null)
                window.AssignOrder(GenerateOrder());
        }

        /// <summary>
        /// Generate a random order:
        /// 50% chance of 2 ingredients, 50% chance of 3.
        /// Each ingredient slot is uniformly random (duplicates allowed).
        /// </summary>
        public Order GenerateOrder()
        {
            int count = Random.value < 0.5f ? 2 : 3;
            var ingredients = new List<IngredientType>(count);
            for (int i = 0; i < count; i++)
                ingredients.Add(_allTypes[Random.Range(0, _allTypes.Length)]);
            return new Order(ingredients);
        }
    }
}
