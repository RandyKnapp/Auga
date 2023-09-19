using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaTopLeftMessage : MonoBehaviour
    {
        public const float WaitTime = 2.0f;
        public const float FadeTime = 1.0f;
        public const float MoveSpeed = 4.0f;
        public const float Spacing = -4;

        public TMP_Text MessageText;
        public Image Icon;

        [NonSerialized]
        public string Message;
        [NonSerialized]
        public int Amount;

        protected RectTransform _rt;
        protected bool _isFadingOut;
        protected Coroutine _fadeCoroutine;

        public virtual void Awake()
        {
            _rt = (RectTransform)transform;
        }

        public virtual void SetValue(string text, Sprite icon, int amount)
        {
            Message = text;
            Icon.enabled = icon != null;
            if (icon != null)
            {
                Icon.sprite = icon;
            }
            Amount = amount;
            SetAtTargetPosition();
            RefreshText();
        }

        public virtual void AddAmount(int amount)
        {
            Amount += amount;
            RefreshText();
            if (_isFadingOut)
            {
                StartFade();
            }
        }

        protected virtual void RefreshText()
        {
            MessageText.text = $"{Message}{(Amount > 0 ? $" x{Amount}" : "")}";
        }

        protected virtual void StartFade()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _isFadingOut = true;
            _fadeCoroutine = StartCoroutine(FadeCoroutine());
        }

        protected virtual IEnumerator FadeCoroutine()
        {
            yield return new WaitForSeconds(WaitTime);

            MessageText.canvasRenderer.SetAlpha(1);
            MessageText.CrossFadeAlpha(0, FadeTime, true);
            Icon.canvasRenderer.SetAlpha(1);
            Icon.CrossFadeAlpha(0, FadeTime, true);

            yield return new WaitForSeconds(FadeTime);

            Destroy(gameObject);
        }

        public virtual void Update()
        {
            var targetPosition = GetTargetPosition();
            _rt.anchoredPosition = Vector2.Lerp(_rt.anchoredPosition, targetPosition, Time.deltaTime * MoveSpeed);

            if (transform.GetSiblingIndex() == 0 && !_isFadingOut)
            {
                StartFade();
            }
        }

        public virtual Vector2 GetTargetPosition()
        {
            var index = _rt.GetSiblingIndex();
            var targetHeight = index * -1 * (_rt.rect.height + Spacing);
            return new Vector2(0, targetHeight);
        }

        public virtual void SetAtTargetPosition()
        {
            _rt.anchoredPosition = GetTargetPosition();
        }
    }

    public class AugaTopLeftMessageController : MonoBehaviour
    {
        public const int MaxMessageCount = 10;

        public RectTransform LogContainer;
        public AugaTopLeftMessage LogPrefab;

        public void AddMessage(string text, Sprite icon, int amount)
        {
            if (LogContainer.childCount >= MaxMessageCount)
            {
                Destroy(LogContainer.GetChild(0).gameObject);
            }

            TryAddExistingMessage(text, icon, amount);
        }

        private void TryAddExistingMessage(string text, Sprite icon, int amount)
        {
            foreach (Transform child in LogContainer)
            {
                var message = child.GetComponent<AugaTopLeftMessage>();
                if (message.Message == text)
                {
                    message.AddAmount(amount);
                    return;
                }
            }

            AddNewMessage(text, icon, amount);
        }

        private void AddNewMessage(string text, Sprite icon, int amount)
        {
            var newMessage = Instantiate(LogPrefab, LogContainer, false);
            newMessage.SetValue(text, icon, amount);
        }
    }
}
