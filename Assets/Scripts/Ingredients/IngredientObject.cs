using UnityEngine;

namespace KitchenGame.Ingredients
{
    /// <summary>
    /// Represents a physical ingredient in the 3D world (fallback/primitive renderer).
    /// Handles visual state (raw vs prepared) via color and scale changes on a MeshRenderer.
    /// Inherits from Ingredient base class for full backward-compatibility with existing prefabs.
    /// Compatible with both URP and Built-in Render Pipeline.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class IngredientObject : Ingredient
    {
        public override IngredientType IngredientType => data != null ? data.ingredientType : IngredientType.Vegetable;

        // ── Internal refs ──────────────────────────────────────────────────────
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _mpb;

        // ──────────────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
            base.Awake();
        }

        public override void Initialize(IngredientData ingredientData)
        {
            data = ingredientData;
            IsPrepared = false;
            ApplyVisual();
        }

        /// <summary>Overload for backward compatibility.</summary>
        public new void SetPrepared()
        {
            SetPrepared(true);
        }

        public override void SetPrepared(bool prepared = true)
        {
            base.SetPrepared(prepared);
        }

        protected override void UpdateVisuals()
        {
            ApplyVisual();
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void ApplyVisual()
        {
            if (data == null) return;
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            if (_renderer == null) return;

            Color targetColor = IsPrepared ? data.preparedColor : data.rawColor;

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", targetColor);
            _mpb.SetColor("_Color", targetColor);
            _renderer.SetPropertyBlock(_mpb);

            if (_renderer.material != null)
            {
                if (_renderer.material.HasProperty("_BaseColor"))
                    _renderer.material.SetColor("_BaseColor", targetColor);
                if (_renderer.material.HasProperty("_Color"))
                    _renderer.material.SetColor("_Color", targetColor);
            }

            transform.localScale = IsPrepared
                ? new Vector3(0.42f, 0.42f, 0.42f)
                : new Vector3(0.35f, 0.35f, 0.35f);
        }
    }
}
