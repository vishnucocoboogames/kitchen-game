using System;
using UnityEngine;

namespace KitchenGame.Core
{
    /// <summary>
    /// A reusable countdown timer component.
    /// Start it with Begin(), query Progress (0–1) or TimeRemaining.
    /// Fires OnComplete when it reaches zero. Respects game pause.
    /// </summary>
    public class StationTimer : MonoBehaviour
    {
        // ── State ──────────────────────────────────────────────────────────────
        public bool IsRunning { get; private set; } = false;
        public float Duration  { get; private set; }
        public float TimeRemaining { get; private set; }

        /// <summary>0 = not started, 1 = complete.</summary>
        public float Progress => Duration > 0f ? 1f - (TimeRemaining / Duration) : 0f;

        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Fired once when the timer finishes.</summary>
        public event Action OnComplete;

        // ──────────────────────────────────────────────────────────────────────

        /// <summary>Start or restart the timer with the given duration in seconds.</summary>
        public void Begin(float duration)
        {
            Duration      = duration;
            TimeRemaining = duration;
            IsRunning     = true;
        }

        /// <summary>Stop and reset the timer without firing OnComplete.</summary>
        public void Cancel()
        {
            IsRunning     = false;
            TimeRemaining = 0f;
        }

        private void Update()
        {
            if (!IsRunning) return;

            // Respect game-level pause (Time.timeScale is set to 0 on pause)
            TimeRemaining -= Time.deltaTime;

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsRunning     = false;
                OnComplete?.Invoke();
            }
        }
    }
}
