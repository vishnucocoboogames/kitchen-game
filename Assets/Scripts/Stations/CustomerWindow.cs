using System;
using System.Collections.Generic;
using UnityEngine;
using KitchenGame.Core;
using KitchenGame.Ingredients;
using KitchenGame.Orders;
using KitchenGame.Player;
using KitchenGame.UI;

namespace KitchenGame.Stations
{
    /// <summary>
    /// One customer window (order slot). Manages its current Order, delivers ingredients,
    /// shows completion popup, and notifies OrderManager to respawn after 5 seconds.
    /// </summary>
    public class CustomerWindow : MonoBehaviour, IInteractable
    {
        // ── Config ─────────────────────────────────────────────────────────────
        [Header("Window Index (0-3)")]
        [SerializeField] public int windowIndex;

        [Header("Score Popup")]
        [SerializeField] public ScorePopup scorePopup;

        [Header("UI")]
        [SerializeField] public WindowUI windowUI;

        // ── State ──────────────────────────────────────────────────────────────
        public Order CurrentOrder { get; private set; } = null;
        public bool  HasOrder     => CurrentOrder != null;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action<CustomerWindow> OnOrderCompleted;  // notifies OrderManager

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (scorePopup == null) scorePopup = GetComponentInChildren<ScorePopup>();
            if (windowUI == null) windowUI = GetComponentInChildren<WindowUI>();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            if (!HasOrder) return;

            CurrentOrder.Tick(Time.deltaTime);
            windowUI?.UpdateElapsedTimer(CurrentOrder.ElapsedSeconds);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Assign a new order to this window.</summary>
        public void AssignOrder(Order order)
        {
            CurrentOrder = order;
            windowUI?.DisplayOrder(order);
        }

        /// <summary>Clear the order (window is empty/waiting for respawn).</summary>
        public void ClearOrder()
        {
            CurrentOrder = null;
            windowUI?.ClearDisplay();
        }

        // ── IInteractable ──────────────────────────────────────────────────────

        public bool CanInteract(PlayerController player)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return false;
            if (!HasOrder) return false;
            if (!player.HasItem) return false;
            if (!player.HeldIngredient.IsPrepared
                && player.HeldIngredient.Data.ingredientType != IngredientType.Cheese)
                return false; // unprepared non-cheese cannot be delivered

            // Check if the ingredient is actually required
            return CurrentOrder.RemainingIngredients.Contains(player.HeldIngredient.Data.ingredientType);
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract(player)) return;

            var ingredient = player.TakeItem();
            bool accepted  = CurrentOrder.TryFulfill(ingredient.Data.ingredientType);

            if (!accepted)
            {
                player.TryPickUp(ingredient);
                return;
            }

            // Ingredient accepted — destroy its world object
            Destroy(ingredient.gameObject);
            windowUI?.MarkIngredientDelivered(ingredient.Data.ingredientType);

            if (CurrentOrder.IsComplete)
                CompleteOrder();
        }

        public string GetInteractHint(PlayerController player)
        {
            if (!HasOrder) return "Window (waiting for order)";
            if (!player.HasItem) return "Window (needs food delivery)";

            if (player.HeldIngredient.Data.ingredientType != IngredientType.Cheese && !player.HeldIngredient.IsPrepared)
                return "Must prepare ingredient before serving!";

            if (!CurrentOrder.RemainingIngredients.Contains(player.HeldIngredient.Data.ingredientType))
                return "Not needed for this order!";

            return "Press E - Deliver ingredient";
        }

        // ── Private ────────────────────────────────────────────────────

        private void CompleteOrder()
        {
            int score = CurrentOrder.CalculateScore(OrderManager.Instance.ScoreValues);
            GameManager.Instance.AddScore(score);

            scorePopup?.Show(score);
            OnOrderCompleted?.Invoke(this);
            ClearOrder();
        }
    }
}
