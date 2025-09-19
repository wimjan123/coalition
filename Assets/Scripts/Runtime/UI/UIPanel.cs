using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Base class for all UI panels in the COALITION demo.
    /// Provides common functionality for panel management, animations, and theming.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        [Header("Panel Configuration")]
        [SerializeField] protected string panelId = "";
        [SerializeField] protected bool startVisible = false;
        [SerializeField] protected bool hideOnStart = true;

        [Header("Animation Settings")]
        [SerializeField] protected bool enableAnimations = true;
        [SerializeField] protected float fadeInDuration = 0.3f;
        [SerializeField] protected float fadeOutDuration = 0.2f;
        [SerializeField] protected AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Panel Behavior")]
        [SerializeField] protected bool blockInteractionWhenHidden = true;
        [SerializeField] protected bool deactivateWhenHidden = true;

        // Panel state
        protected UIManager uiManager;
        protected CanvasGroup canvasGroup;
        protected bool isOpen = false;
        protected bool isAnimating = false;
        protected Coroutine currentAnimation;

        // Panel components
        protected RectTransform rectTransform;
        protected Canvas panelCanvas;

        // Events
        public System.Action<UIPanel> OnPanelOpened;
        public System.Action<UIPanel> OnPanelClosed;
        public System.Action<UIPanel> OnPanelAnimationComplete;

        #region Properties

        /// <summary>
        /// Unique identifier for this panel.
        /// </summary>
        public string PanelId => panelId;

        /// <summary>
        /// Whether the panel is currently open and visible.
        /// </summary>
        public bool IsOpen => isOpen;

        /// <summary>
        /// Whether the panel is currently animating.
        /// </summary>
        public bool IsAnimating => isAnimating;

        /// <summary>
        /// The CanvasGroup component for this panel.
        /// </summary>
        public CanvasGroup CanvasGroup => canvasGroup;

        /// <summary>
        /// The RectTransform component for this panel.
        /// </summary>
        public RectTransform RectTransform => rectTransform;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializeComponents();
        }

        protected virtual void Start()
        {
            if (hideOnStart && !startVisible)
            {
                SetVisibility(false, false);
            }
            else if (startVisible)
            {
                SetVisibility(true, false);
                isOpen = true;
            }
        }

        protected virtual void OnDestroy()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the panel with UIManager reference.
        /// </summary>
        /// <param name="manager">UI manager instance</param>
        /// <param name="id">Panel identifier</param>
        public virtual void Initialize(UIManager manager, string id)
        {
            uiManager = manager;
            panelId = id;

            OnInitialized();
        }

        /// <summary>
        /// Initialize required components.
        /// </summary>
        protected virtual void InitializeComponents()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            rectTransform = GetComponent<RectTransform>();
            panelCanvas = GetComponent<Canvas>();

            // Generate panel ID if not set
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = $"{GetType().Name}_{GetInstanceID()}";
            }
        }

        /// <summary>
        /// Called after panel initialization. Override for custom setup.
        /// </summary>
        protected virtual void OnInitialized()
        {
            // Override in derived classes for custom initialization
        }

        #endregion

        #region Panel Control

        /// <summary>
        /// Open the panel with optional animation.
        /// </summary>
        /// <param name="animate">Whether to animate the opening</param>
        public virtual void Open(bool animate = true)
        {
            if (isOpen && !isAnimating)
            {
                return; // Already open
            }

            StopCurrentAnimation();

            gameObject.SetActive(true);
            isOpen = true;

            if (animate && enableAnimations && uiManager?.CanStartAnimation() == true)
            {
                currentAnimation = StartCoroutine(AnimateOpen());
            }
            else
            {
                SetVisibility(true, false);
                OnOpenComplete();
            }

            OnPanelOpened?.Invoke(this);
        }

        /// <summary>
        /// Close the panel with optional animation.
        /// </summary>
        /// <param name="animate">Whether to animate the closing</param>
        public virtual void Close(bool animate = true)
        {
            if (!isOpen && !isAnimating)
            {
                return; // Already closed
            }

            StopCurrentAnimation();

            isOpen = false;

            if (animate && enableAnimations && uiManager?.CanStartAnimation() == true)
            {
                currentAnimation = StartCoroutine(AnimateClose());
            }
            else
            {
                SetVisibility(false, false);
                OnCloseComplete();
            }

            OnPanelClosed?.Invoke(this);
        }

        /// <summary>
        /// Toggle the panel open/closed state.
        /// </summary>
        /// <param name="animate">Whether to animate the transition</param>
        public virtual void Toggle(bool animate = true)
        {
            if (isOpen)
            {
                Close(animate);
            }
            else
            {
                Open(animate);
            }
        }

        /// <summary>
        /// Set panel visibility immediately without animation.
        /// </summary>
        /// <param name="visible">Target visibility state</param>
        /// <param name="updateOpenState">Whether to update isOpen state</param>
        protected virtual void SetVisibility(bool visible, bool updateOpenState = true)
        {
            if (canvasGroup == null) return;

            canvasGroup.alpha = visible ? 1.0f : 0.0f;
            canvasGroup.interactable = visible && !blockInteractionWhenHidden;
            canvasGroup.blocksRaycasts = visible;

            if (updateOpenState)
            {
                isOpen = visible;
            }

            if (!visible && deactivateWhenHidden)
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Animation

        /// <summary>
        /// Animate the panel opening.
        /// </summary>
        protected virtual IEnumerator AnimateOpen()
        {
            isAnimating = true;
            uiManager?.RegisterAnimationStart();

            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeInDuration;
                float curveValue = fadeCurve.Evaluate(progress);

                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1.0f, curveValue);

                yield return null;
            }

            canvasGroup.alpha = 1.0f;
            canvasGroup.interactable = true;

            isAnimating = false;
            uiManager?.RegisterAnimationEnd();

            OnOpenComplete();
            OnPanelAnimationComplete?.Invoke(this);
        }

        /// <summary>
        /// Animate the panel closing.
        /// </summary>
        protected virtual IEnumerator AnimateClose()
        {
            isAnimating = true;
            uiManager?.RegisterAnimationStart();

            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            canvasGroup.interactable = false;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeOutDuration;
                float curveValue = fadeCurve.Evaluate(progress);

                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0.0f, curveValue);

                yield return null;
            }

            canvasGroup.alpha = 0.0f;
            canvasGroup.blocksRaycasts = false;

            if (deactivateWhenHidden)
            {
                gameObject.SetActive(false);
            }

            isAnimating = false;
            uiManager?.RegisterAnimationEnd();

            OnCloseComplete();
            OnPanelAnimationComplete?.Invoke(this);
        }

        /// <summary>
        /// Stop any currently running animation.
        /// </summary>
        protected virtual void StopCurrentAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
                isAnimating = false;
                uiManager?.RegisterAnimationEnd();
            }
        }

        #endregion

        #region Theme Support

        /// <summary>
        /// Apply a UI theme to this panel. Override for custom theming.
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        public virtual void ApplyTheme(UITheme theme)
        {
            if (theme == null) return;

            // Apply background color if panel has an Image component
            var backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = theme.PanelBackgroundColor;
            }

            // Apply theme to child UI elements
            ApplyThemeToChildren(theme);
        }

        /// <summary>
        /// Apply theme to child UI elements. Override for custom child theming.
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        protected virtual void ApplyThemeToChildren(UITheme theme)
        {
            // Apply theme to Text components
            var textComponents = GetComponentsInChildren<Text>();
            foreach (var text in textComponents)
            {
                text.color = theme.TextColor;
                if (theme.Font != null)
                {
                    text.font = theme.Font;
                }
            }

            // Apply theme to Button components
            var buttonComponents = GetComponentsInChildren<Button>();
            foreach (var button in buttonComponents)
            {
                var colors = button.colors;
                colors.normalColor = theme.ButtonNormalColor;
                colors.highlightedColor = theme.ButtonHighlightColor;
                colors.pressedColor = theme.ButtonPressedColor;
                colors.disabledColor = theme.ButtonDisabledColor;
                button.colors = colors;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when panel opening is complete.
        /// </summary>
        protected virtual void OnOpenComplete()
        {
            // Override in derived classes for custom behavior
        }

        /// <summary>
        /// Called when panel closing is complete.
        /// </summary>
        protected virtual void OnCloseComplete()
        {
            // Override in derived classes for custom behavior
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Bring this panel to the front of the UI hierarchy.
        /// </summary>
        public virtual void BringToFront()
        {
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Send this panel to the back of the UI hierarchy.
        /// </summary>
        public virtual void SendToBack()
        {
            transform.SetAsFirstSibling();
        }

        /// <summary>
        /// Set the panel's position in the UI hierarchy.
        /// </summary>
        /// <param name="index">Target sibling index</param>
        public virtual void SetSiblingIndex(int index)
        {
            transform.SetSiblingIndex(index);
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Validate the panel configuration in the editor.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (string.IsNullOrEmpty(panelId))
            {
                panelId = GetType().Name;
            }

            // Ensure valid animation durations
            fadeInDuration = Mathf.Max(0.1f, fadeInDuration);
            fadeOutDuration = Mathf.Max(0.1f, fadeOutDuration);
        }

        [ContextMenu("Test Open")]
        private void TestOpen()
        {
            Open();
        }

        [ContextMenu("Test Close")]
        private void TestClose()
        {
            Close();
        }
#endif
    }
}