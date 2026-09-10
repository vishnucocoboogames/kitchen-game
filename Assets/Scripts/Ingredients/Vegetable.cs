using UnityEngine;

namespace KitchenGame.Ingredients
{
    /// <summary>
    /// Represents the visual and preparation state of a vegetable.
    /// </summary>
    public enum VegetableState
    {
        Normal,
        Chopped
    }

    /// <summary>
    /// Vegetable ingredient component.
    /// Allows switching between a whole/normal GameObject and a chopped ("chopper") GameObject.
    /// Supports switching via code, Inspector dropdown, context menus, and kitchen chopping table.
    /// </summary>
    public class Vegetable : Ingredient
    {
        [Header("Vegetable Visual GameObjects")]
        [Tooltip("GameObject representing the whole / normal vegetable.")]
        [SerializeField] private GameObject normalGameObject;

        [Tooltip("GameObject representing the chopped / sliced vegetable.")]
        [SerializeField] private GameObject choppedGameObject;

        [Header("Vegetable State")]
        [Tooltip("Current visual and preparation state of the vegetable.")]
        [SerializeField] private VegetableState state = VegetableState.Normal;

        public VegetableState State => state;
        public bool IsChopped => state == VegetableState.Chopped;
        public override IngredientType IngredientType => IngredientType.Vegetable;

        public GameObject NormalGameObject => normalGameObject;
        public GameObject ChoppedGameObject => choppedGameObject;

        /// <summary>Set the vegetable state and switch active GameObjects.</summary>
        public void SetState(VegetableState newState)
        {
            state = newState;
            IsPrepared = (state == VegetableState.Chopped);
            UpdateVisuals();
        }

        /// <summary>Switch to the normal (whole) vegetable visual GameObject.</summary>
        public void SwitchToNormal() => SetState(VegetableState.Normal);

        /// <summary>Switch to the chopped vegetable visual GameObject.</summary>
        public void SwitchToChopped() => SetState(VegetableState.Chopped);

        /// <summary>
        /// Mark as prepared (chopped) or raw (normal).
        /// Called automatically by ChoppingTable upon completion.
        /// </summary>
        public override void SetPrepared(bool prepared = true)
        {
            SetState(prepared ? VegetableState.Chopped : VegetableState.Normal);
            base.SetPrepared(prepared);
        }

        public override void Initialize(IngredientData ingredientData)
        {
            data = ingredientData;
            SetState(VegetableState.Normal);
            base.Initialize(ingredientData);
        }

        protected override void UpdateVisuals()
        {
            bool isChopped = (state == VegetableState.Chopped);

            if (normalGameObject != null)
                normalGameObject.SetActive(!isChopped);

            if (choppedGameObject != null)
                choppedGameObject.SetActive(isChopped);
        }

        // ── Context Menus for testing in Unity Editor ─────────────────────────

        [ContextMenu("Switch to Normal (Whole)")]
        private void ContextSwitchNormal() => SwitchToNormal();

        [ContextMenu("Switch to Chopped")]
        private void ContextSwitchChopped() => SwitchToChopped();
    }
}
