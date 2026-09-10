using UnityEngine;

namespace KitchenGame.Ingredients
{
    /// <summary>
    /// Cheese ingredient component.
    /// Manages cheese visual GameObjects. By default, cheese is ready to serve without requiring cooking or chopping,
    /// but optionally supports switching between a whole block and prepared/sliced cheese GameObjects.
    /// </summary>
    public class Cheese : Ingredient
    {
        [Header("Cheese Visual GameObjects")]
        [Tooltip("GameObject representing the cheese (e.g. block or slice).")]
        [SerializeField] private GameObject normalGameObject;

        [Tooltip("Optional GameObject for prepared/melted/grated cheese.")]
        [SerializeField] private GameObject preparedGameObject;

        public override IngredientType IngredientType => IngredientType.Cheese;

        public GameObject NormalGameObject => normalGameObject;
        public GameObject PreparedGameObject => preparedGameObject;

        public override void Initialize(IngredientData ingredientData)
        {
            data = ingredientData;
            // In kitchen game rules, cheese does not require preparation and is ready by default
            IsPrepared = true;
            base.Initialize(ingredientData);
        }

        public override void SetPrepared(bool prepared = true)
        {
            base.SetPrepared(prepared);
        }

        protected override void UpdateVisuals()
        {
            if (preparedGameObject != null)
            {
                if (normalGameObject != null)
                    normalGameObject.SetActive(!IsPrepared);

                preparedGameObject.SetActive(IsPrepared);
            }
            else if (normalGameObject != null)
            {
                normalGameObject.SetActive(true);
            }
        }

        // ── Context Menus for testing in Unity Editor ─────────────────────────

        [ContextMenu("Switch to Normal")]
        private void ContextSwitchNormal() => SetPrepared(false);

        [ContextMenu("Switch to Prepared")]
        private void ContextSwitchPrepared() => SetPrepared(true);
    }
}
