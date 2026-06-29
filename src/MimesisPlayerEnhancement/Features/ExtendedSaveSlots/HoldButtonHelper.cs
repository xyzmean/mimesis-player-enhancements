using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class HoldButtonHelper : MonoBehaviour
    {
        private const float HoldDurationSeconds = 2f;

        private Button? _button;
        private Image? _progressImage;
        private Action? _onHoldComplete;
        private bool _isHolding;
        private Coroutine? _holdCoroutine;

        internal static HoldButtonHelper Attach(Button button, Action onHoldComplete)
        {
            HoldButtonHelper helper = button.gameObject.GetComponent<HoldButtonHelper>()
                ?? button.gameObject.AddComponent<HoldButtonHelper>();
            helper.Initialize(button, onHoldComplete);
            return helper;
        }

        internal void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        private void Initialize(Button button, Action onHoldComplete)
        {
            _button = button;
            _onHoldComplete = onHoldComplete;
            EnsureProgressImage(button);
            ResetProgress();
            SetupEventTrigger(button);
        }

        private void EnsureProgressImage(Button button)
        {
            Transform? existing = button.transform.Find("HoldProgress");
            if (existing != null)
            {
                _progressImage = existing.GetComponent<Image>();
                return;
            }

            GameObject progressGo = new("HoldProgress", typeof(RectTransform), typeof(Image));
            RectTransform progressRect = progressGo.GetComponent<RectTransform>();
            progressRect.SetParent(button.transform, false);
            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = Vector2.one;
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;

            _progressImage = progressGo.GetComponent<Image>();
            _progressImage.type = Image.Type.Filled;
            _progressImage.fillMethod = Image.FillMethod.Horizontal;
            _progressImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _progressImage.color = new Color(1f, 1f, 1f, 0.35f);
            _progressImage.raycastTarget = false;
            progressGo.SetActive(false);
        }

        private void SetupEventTrigger(Button button)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>()
                ?? button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            AddTrigger(trigger, EventTriggerType.PointerDown, OnPointerDown);
            AddTrigger(trigger, EventTriggerType.PointerUp, OnPointerUp);
            AddTrigger(trigger, EventTriggerType.PointerExit, OnPointerUp);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> handler)
        {
            EventTrigger.Entry entry = new()
            {
                eventID = type,
            };
            entry.callback.AddListener(handler.Invoke);
            trigger.triggers.Add(entry);
        }

        private void OnPointerDown(BaseEventData _)
        {
            if (_button == null || !_button.interactable || _isHolding)
            {
                return;
            }

            _isHolding = true;
            _holdCoroutine = StartCoroutine(HoldProgressCoroutine());
        }

        private void OnPointerUp(BaseEventData _)
        {
            if (!_isHolding)
            {
                return;
            }

            _isHolding = false;
            if (_holdCoroutine != null)
            {
                StopCoroutine(_holdCoroutine);
                _holdCoroutine = null;
            }

            EventSystem.current?.SetSelectedGameObject(null);
            ResetProgress();
        }

        private IEnumerator HoldProgressCoroutine()
        {
            float elapsed = 0f;
            if (_progressImage != null)
            {
                _progressImage.gameObject.SetActive(true);
                _progressImage.fillAmount = 0f;
            }

            while (elapsed < HoldDurationSeconds)
            {
                elapsed += Time.deltaTime;
                if (_progressImage != null)
                {
                    _progressImage.fillAmount = Mathf.Clamp01(elapsed / HoldDurationSeconds);
                }

                yield return null;
            }

            _isHolding = false;
            _holdCoroutine = null;
            ResetProgress();
            _onHoldComplete?.Invoke();
        }

        private void ResetProgress()
        {
            if (_progressImage == null)
            {
                return;
            }

            _progressImage.fillAmount = 0f;
            _progressImage.gameObject.SetActive(false);
        }
    }
}
