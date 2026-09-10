using System.Collections;
using UnityEngine;
using TMPro;

namespace KitchenGame.UI
{
    /// <summary>
    /// Animated score popup that appears near a customer window and fades out.
    /// Attach to a world-space Canvas child of the CustomerWindow GameObject.
    /// </summary>
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI popupText;
        [SerializeField] private float floatSpeed  = 1.5f;  // units per second upward
        [SerializeField] private float fadeDuration = 2f;

        private Coroutine _activeCoroutine;

        private void Awake()
        {
            if (popupText == null) popupText = GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>Display a score value and animate it floating up then fading out.</summary>
        public void Show(int score)
        {
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            _activeCoroutine = StartCoroutine(AnimatePopup(score));
        }

        private IEnumerator AnimatePopup(int score)
        {
            if (popupText == null) yield break;

            popupText.text  = score >= 0 ? $"+{score}" : $"{score}";
            Color baseColor = score >= 0 ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.25f, 0.25f);
            popupText.color = baseColor;
            popupText.gameObject.SetActive(true);

            Vector3 startPos = transform.localPosition;
            float   elapsed  = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;

                // Float towards +Z in top-down view (or +Y)
                transform.localPosition = startPos + new Vector3(0f, 0.2f, floatSpeed * elapsed);

                popupText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
                yield return null;
            }

            popupText.gameObject.SetActive(false);
            transform.localPosition = startPos;
        }
    }
}
