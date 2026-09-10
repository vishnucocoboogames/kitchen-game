using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KitchenGame.Ingredients;
using KitchenGame.Orders;

namespace KitchenGame.UI
{
    /// <summary>
    /// Per-window UI panel showing:
    ///   - The list of required ingredients (greyed-out when delivered)
    ///   - A live elapsed timer
    /// Uses TextMeshProUGUI for crystal clear rendering.
    /// </summary>
    public class WindowUI : MonoBehaviour
    {
        [Header("Ingredient Rows")]
        [SerializeField] public Transform ingredientContainer; // parent for ingredient labels
        [SerializeField] public GameObject ingredientRowPrefab; // TextMeshPro prefab for each ingredient

        [Header("Elapsed Timer")]
        [SerializeField] public TextMeshProUGUI elapsedTimerText;

        [Header("Empty State")]
        [SerializeField] public GameObject emptyLabel; // "Waiting..." shown when no order

        // ── Internal ───────────────────────────────────────────────────────────
        private List<IngredientRow> _rows = new List<IngredientRow>();

        private class IngredientRow
        {
            public IngredientType   type;
            public TextMeshProUGUI label;
            public bool             delivered;
        }

        // ──────────────────────────────────────────────────────────────────────

        private void Start()
        {
            ClearDisplay();
        }

        /// <summary>Called by CustomerWindow.AssignOrder to populate the UI.</summary>
        public void DisplayOrder(Order order)
        {
            ClearDisplay();

            if (emptyLabel) emptyLabel.SetActive(false);
            if (elapsedTimerText) elapsedTimerText.gameObject.SetActive(true);

            Transform container = ingredientContainer != null ? ingredientContainer : transform;

            foreach (var type in order.RequiredIngredients)
            {
                GameObject go;
                if (ingredientRowPrefab != null)
                {
                    go = Instantiate(ingredientRowPrefab, container);
                }
                else
                {
                    go = new GameObject("Row_" + type, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    go.transform.SetParent(container, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(160, 26);
                }

                var text = go.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.fontSize = 20;
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.text  = IngredientLabel(type);
                    text.color = Color.white;
                }

                _rows.Add(new IngredientRow { type = type, label = text, delivered = false });
            }
        }

        /// <summary>Grey out the first undelivered row of the given type.</summary>
        public void MarkIngredientDelivered(IngredientType type)
        {
            foreach (var row in _rows)
            {
                if (row.type == type && !row.delivered)
                {
                    row.delivered = true;
                    if (row.label)
                    {
                        row.label.color = new Color(0.5f, 0.5f, 0.55f); // grey out
                        row.label.text  = $"<s>{IngredientLabel(type)}</s>";
                    }
                    break;
                }
            }
        }

        /// <summary>Update the elapsed timer text.</summary>
        public void UpdateElapsedTimer(float elapsed)
        {
            if (elapsedTimerText)
            {
                int s = Mathf.FloorToInt(elapsed);
                elapsedTimerText.text = $"{s}s";
                elapsedTimerText.color = s > 30 ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.9f, 0.3f);
            }
        }

        /// <summary>Reset the UI to the empty/waiting state.</summary>
        public void ClearDisplay()
        {
            foreach (var row in _rows)
                if (row.label != null) Destroy(row.label.gameObject);
            _rows.Clear();

            if (emptyLabel) emptyLabel.SetActive(true);
            if (elapsedTimerText)
            {
                elapsedTimerText.text = "";
                elapsedTimerText.gameObject.SetActive(false);
            }
        }

        private static string IngredientLabel(IngredientType type) => type switch
        {
            IngredientType.Vegetable => "Vegetable",
            IngredientType.Cheese    => "Cheese",
            IngredientType.Meat      => "Meat",
            _                        => type.ToString()
        };
    }
}
