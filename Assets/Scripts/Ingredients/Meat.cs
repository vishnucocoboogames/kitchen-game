using UnityEngine;

namespace KitchenGame.Ingredients
{
    /// <summary>
    /// Represents the cooking state of meat.
    /// </summary>
    public enum MeatState
    {
        Uncooked,
        Cooked
    }

    /// <summary>
    /// Meat ingredient component.
    /// Allows switching between a non-cooked (raw) GameObject and a cooked GameObject.
    /// Supports switching via code, Inspector dropdown, context menus, and kitchen stove.
    /// </summary>
    public class Meat : Ingredient
    {
        [Header("Meat Visual GameObjects")]
        [Tooltip("GameObject representing non-cooked / raw meat.")]
        [SerializeField] private GameObject uncookedGameObject;

        [Tooltip("GameObject representing cooked meat.")]
        [SerializeField] private GameObject cookedGameObject;

        [Header("Meat State")]
        [Tooltip("Current visual and cooking state of the meat.")]
        [SerializeField] private MeatState state = MeatState.Uncooked;

        public MeatState State => state;
        public bool IsCooked => state == MeatState.Cooked;
        public override IngredientType IngredientType => IngredientType.Meat;

        public GameObject UncookedGameObject => uncookedGameObject;
        public GameObject CookedGameObject => cookedGameObject;

        /// <summary>Set the meat state and switch active GameObjects.</summary>
        public void SetState(MeatState newState)
        {
            state = newState;
            IsPrepared = (state == MeatState.Cooked);
            UpdateVisuals();
        }

        /// <summary>Switch to the non-cooked (raw) meat visual GameObject.</summary>
        public void SwitchToUncooked() => SetState(MeatState.Uncooked);

        /// <summary>Switch to the cooked meat visual GameObject.</summary>
        public void SwitchToCooked() => SetState(MeatState.Cooked);

        /// <summary>
        /// Mark as prepared (cooked) or raw (uncooked).
        /// Called automatically by Stove upon completion.
        /// </summary>
        public override void SetPrepared(bool prepared = true)
        {
            SetState(prepared ? MeatState.Cooked : MeatState.Uncooked);
            base.SetPrepared(prepared);
        }

        public override void Initialize(IngredientData ingredientData)
        {
            data = ingredientData;
            SetState(MeatState.Uncooked);
            base.Initialize(ingredientData);
        }

        protected override void UpdateVisuals()
        {
            bool isCooked = (state == MeatState.Cooked);

            if (uncookedGameObject != null)
                uncookedGameObject.SetActive(!isCooked);

            if (cookedGameObject != null)
                cookedGameObject.SetActive(isCooked);
        }

        // ── Context Menus for testing in Unity Editor ─────────────────────────

        [ContextMenu("Switch to Uncooked (Raw)")]
        private void ContextSwitchUncooked() => SwitchToUncooked();

        [ContextMenu("Switch to Cooked")]
        private void ContextSwitchCooked() => SwitchToCooked();
    }
}
