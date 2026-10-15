using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Tylerland.Core
{
    /// <summary>
    /// Screen fader for smooth visual state transitions (fade to black / fade from black).
    /// Can automatically generate a fullscreen canvas if one is not wired up in the inspector.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _fadeImage;

        private Coroutine _currentFadeRoutine;

        private void Awake()
        {
            EnsureCanvasSetup();
        }

        private void EnsureCanvasSetup()
        {
            if (_canvasGroup != null) return;

            // Check if we have one attached or in children
            _canvasGroup = GetComponentInChildren<CanvasGroup>();
            if (_canvasGroup != null) return;

            // Auto-generate UI overlay canvas
            var canvasObj = new GameObject("ScreenFaderCanvas");
            canvasObj.transform.SetParent(transform, false);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Always on top

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;

            var imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);

            _fadeImage = imageObj.AddComponent<Image>();
            _fadeImage.color = Color.black;

            var rect = _fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }

        /// <summary>
        /// Fades the screen to black (opaque).
        /// </summary>
        public Coroutine FadeToBlack(float duration, Action onComplete = null)
        {
            EnsureCanvasSetup();
            if (_currentFadeRoutine != null) StopCoroutine(_currentFadeRoutine);
            _currentFadeRoutine = StartCoroutine(FadeRoutine(1f, duration, onComplete));
            return _currentFadeRoutine;
        }

        /// <summary>
        /// Fades the screen from black to clear (transparent).
        /// </summary>
        public Coroutine FadeFromBlack(float duration, Action onComplete = null)
        {
            EnsureCanvasSetup();
            if (_currentFadeRoutine != null) StopCoroutine(_currentFadeRoutine);
            _currentFadeRoutine = StartCoroutine(FadeRoutine(0f, duration, onComplete));
            return _currentFadeRoutine;
        }

        public IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
        {
            EnsureCanvasSetup();
            _canvasGroup.blocksRaycasts = targetAlpha > 0.01f;

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            if (duration <= 0.001f)
            {
                _canvasGroup.alpha = targetAlpha;
                onComplete?.Invoke();
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.blocksRaycasts = targetAlpha > 0.01f;
            onComplete?.Invoke();
            _currentFadeRoutine = null;
        }

        public void SetAlphaImmediate(float alpha)
        {
            EnsureCanvasSetup();
            _canvasGroup.alpha = alpha;
            _canvasGroup.blocksRaycasts = alpha > 0.01f;
        }
    }
}
