using System;
using UnityEngine;

namespace KitchenGame.Ingredients
{
    /// <summary>
    /// Abstract base class for all ingredients in the kitchen game.
    /// Manages common properties (static data, prepared state, visibility) and defines hooks for visual updates.
    /// Subclasses (Vegetable, Meat, Cheese) provide specialized states and GameObject switching.
    /// </summary>
    public abstract class Ingredient : MonoBehaviour
    {
        [Header("Ingredient Base Settings")]
        [Tooltip("The static configuration data asset for this ingredient.")]
        [SerializeField] protected IngredientData data;

        public IngredientData Data => data;
        public abstract IngredientType IngredientType { get; }
        public bool IsPrepared { get; protected set; } = false;

        public event Action<Ingredient> OnStateChanged;

        protected virtual void Awake()
        {
            UpdateVisuals();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            UpdateVisuals();
        }
#endif

        /// <summary>Initialize this ingredient with its data asset.</summary>
        public virtual void Initialize(IngredientData ingredientData)
        {
            data = ingredientData;
            IsPrepared = false;
            UpdateVisuals();
            OnStateChanged?.Invoke(this);
        }

        /// <summary>Mark this ingredient as prepared (e.g. chopped, cooked) and update visuals.</summary>
        public virtual void SetPrepared(bool prepared = true)
        {
            IsPrepared = prepared;
            UpdateVisuals();
            OnStateChanged?.Invoke(this);
        }

        /// <summary>Show or hide this ingredient object in the world.</summary>
        public virtual void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Called when the ingredient's state or readiness changes to activate/deactivate corresponding GameObjects.
        /// </summary>
        protected abstract void UpdateVisuals();
    }
}
