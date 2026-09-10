using System.Collections.Generic;
using UnityEngine;
using KitchenGame.Ingredients;

namespace KitchenGame.Orders
{
    /// <summary>
    /// Represents a single customer order: a list of required ingredients,
    /// elapsed time tracker, and scoring logic.
    /// </summary>
    public class Order
    {
        // ── Data ───────────────────────────────────────────────────────────────
        /// <summary>All ingredients originally required (including duplicates).</summary>
        public List<IngredientType> RequiredIngredients { get; private set; }

        /// <summary>Remaining unfulfilled ingredients (decremented on delivery).</summary>
        public List<IngredientType> RemainingIngredients { get; private set; }

        /// <summary>Seconds this order has been active. Updated externally.</summary>
        public float ElapsedSeconds { get; private set; } = 0f;

        public bool IsComplete => RemainingIngredients.Count == 0;

        // ── Constructor ────────────────────────────────────────────────────────

        public Order(List<IngredientType> ingredients)
        {
            RequiredIngredients  = new List<IngredientType>(ingredients);
            RemainingIngredients = new List<IngredientType>(ingredients);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Advance the elapsed timer. Call each frame while order is active.</summary>
        public void Tick(float deltaTime)
        {
            ElapsedSeconds += deltaTime;
        }

        /// <summary>
        /// Try to fulfill one unit of the given ingredient type.
        /// Returns true if the ingredient was needed and accepted.
        /// </summary>
        public bool TryFulfill(IngredientType type)
        {
            int idx = RemainingIngredients.IndexOf(type);
            if (idx < 0) return false;

            RemainingIngredients.RemoveAt(idx);
            return true;
        }

        /// <summary>
        /// Calculate final score: sum of ingredient values minus floor(elapsed seconds).
        /// Can be negative.
        /// </summary>
        public int CalculateScore(Dictionary<IngredientType, int> scoreValues)
        {
            int ingredientSum = 0;
            foreach (var t in RequiredIngredients)
            {
                if (scoreValues.TryGetValue(t, out int val))
                    ingredientSum += val;
            }
            int timePenalty = Mathf.FloorToInt(ElapsedSeconds);
            return ingredientSum - timePenalty;
        }
    }
}
