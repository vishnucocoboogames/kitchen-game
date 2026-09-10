using UnityEngine;
using KitchenGame.Ingredients;

namespace KitchenGame.Ingredients
{
    /// <summary>
    /// Represents a physical ingredient in the 3D world.
    /// Handles visual state (raw vs prepared) via color and scale changes.
    /// Compatible with both URP and Built-in Render Pipeline.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class IngredientObject : MonoBehaviour
    {
        // ── Data ───────────────────────────────────────────────────────────────
        public IngredientData Data       { get; private set; }
        public bool           IsPrepared { get; private set; } = false;

        // ── Internal refs ──────────────────────────────────────────────────────
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _mpb;

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        /// <summary>Initialize this ingredient with its data asset.</summary>
        public void Initialize(IngredientData data)
        {
            Data       = data;
            IsPrepared = false;
            ApplyVisual();
        }

        /// <summary>Mark this ingredient as prepared and update visuals.</summary>
        public void SetPrepared()
        {
            IsPrepared = true;
            ApplyVisual();
        }

        /// <summary>Show or hide the world object.</summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void ApplyVisual()
        {
            if (Data == null || _renderer == null) return;

            Color targetColor = IsPrepared ? Data.preparedColor : Data.rawColor;

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
