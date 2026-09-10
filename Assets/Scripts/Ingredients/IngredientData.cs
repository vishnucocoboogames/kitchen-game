using UnityEngine;

namespace KitchenGame.Ingredients
{
    /// <summary>
    /// ScriptableObject that defines the static data for one ingredient type.
    /// Create one asset per ingredient via Assets > Create > KitchenGame > IngredientData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewIngredientData", menuName = "KitchenGame/IngredientData")]
    public class IngredientData : ScriptableObject
    {
        [Header("Identity")]
        public IngredientType ingredientType;
        public string displayName;

        [Header("Scoring")]
        public int scoreValue = 10;

        [Header("Preparation")]
        public bool requiresPreparation = false;
        // e.g., "ChoppingTable" or "Stove" — matched by tag on interactable
        public string prepStationTag = "";
        public float prepDuration = 0f;

        [Header("Visuals")]
        public Color rawColor = Color.white;
        public Color preparedColor = Color.green;
        // Optional mesh — leave null to use default primitive
        public Mesh rawMesh;
        public Mesh preparedMesh;
    }
}
