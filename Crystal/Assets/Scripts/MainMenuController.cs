using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Crystal
{
    /// <summary>
    /// Scene-placeable main menu controller that owns its generated UI and routes into gameplay.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Scene Flow")]
        [SerializeField] private string gameplaySceneName = "SampleScene";
        [SerializeField] private bool quitStopsEditorPlayMode = true;

        [Header("Text")]
        [SerializeField] private Font menuFont;
        [SerializeField] private string titleText = "Protect The Crystal";
        [SerializeField] private string playButtonText = "PLAY";
        [SerializeField] private string quitButtonText = "QUIT";
        [SerializeField, TextArea(5, 8)] private string instructionsText =
            "WASD - Move\n" +
            "LMB - Attack\n" +
            "Space - Jump\n" +
            "E - Place Health Pickup\n\n" +
            "Violence may not always be the answer...Sometimes something more glittering is the key.";

        [Header("Timing")]
        [SerializeField] private float fadeOutDuration = 0.75f;
        [SerializeField] private float instructionsFadeInDuration = 0.45f;
        [SerializeField] private float instructionsHoldDuration = 3.5f;
        [SerializeField] private float hoverAnimationSharpness = 18f;

        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.035f, 0.045f, 0.075f, 1f);
        [SerializeField] private Color titleColor = new Color(0.88f, 0.98f, 1f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.12f, 0.13f, 0.18f, 1f);
        [SerializeField] private Color buttonHoverColor = new Color(0.62f, 0.92f, 1f, 1f);
        [SerializeField] private Color buttonPressedColor = new Color(1f, 0.86f, 0.38f, 1f);
        [SerializeField] private Color buttonTextColor = Color.white;
        [SerializeField] private Color buttonActiveTextColor = new Color(0.06f, 0.07f, 0.1f, 1f);
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private Color instructionsColor = new Color(0.92f, 0.98f, 1f, 1f);

        [Header("Button Feedback")]
        [SerializeField] private Vector3 buttonNormalScale = Vector3.one;
        [SerializeField] private Vector3 buttonHoverScale = new Vector3(1.06f, 1.06f, 1f);
        [SerializeField] private Vector3 buttonPressedScale = new Vector3(0.98f, 0.98f, 1f);

        private readonly List<MenuButtonView> buttonViews = new List<MenuButtonView>();

        private CanvasGroup menuCanvasGroup;
        private CanvasGroup instructionsCanvasGroup;
        private Image fadeOverlayImage;
        private MenuButtonView playButtonView;
        private MenuButtonView quitButtonView;
        private Coroutine playSequenceRoutine;
        private bool isTransitioning;

        private void Awake()
        {
            Time.timeScale = 1f;
            CreateMenuUi();
            SetMenuInteractable(true);
            SetFadeOverlayAlpha(0f);
            if (instructionsCanvasGroup != null)
                instructionsCanvasGroup.alpha = 0f;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;

            foreach (MenuButtonView buttonView in buttonViews)
                buttonView.Tick(hoverAnimationSharpness, deltaTime);

            if (isTransitioning)
                return;

            UpdatePointerInput();

            if (WasSubmitPressed())
                BeginPlaySequence();
            else if (WasCancelPressed())
                QuitGame();
        }

        private void OnDisable()
        {
            if (playSequenceRoutine != null)
            {
                StopCoroutine(playSequenceRoutine);
                playSequenceRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (playButtonView != null)
                playButtonView.Button.onClick.RemoveListener(BeginPlaySequence);

            if (quitButtonView != null)
                quitButtonView.Button.onClick.RemoveListener(QuitGame);
        }

        private void OnValidate()
        {
            fadeOutDuration = Mathf.Max(0.01f, fadeOutDuration);
            instructionsFadeInDuration = Mathf.Max(0f, instructionsFadeInDuration);
            instructionsHoldDuration = Mathf.Max(0f, instructionsHoldDuration);
            hoverAnimationSharpness = Mathf.Max(0.01f, hoverAnimationSharpness);
        }

        private void BeginPlaySequence()
        {
            if (isTransitioning)
                return;

            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogError("[MainMenuController] Cannot start gameplay because gameplaySceneName is empty.", this);
                return;
            }

            playSequenceRoutine = StartCoroutine(PlayIntroThenLoadGameplay());
        }

        private IEnumerator PlayIntroThenLoadGameplay()
        {
            isTransitioning = true;
            SetMenuInteractable(false);

            yield return FadeMenuOut();
            yield return FadeCanvasGroup(instructionsCanvasGroup, 1f, instructionsFadeInDuration);
            yield return WaitForUnscaledSeconds(instructionsHoldDuration);

            SceneManager.LoadScene(gameplaySceneName);
        }

        private IEnumerator FadeMenuOut()
        {
            float elapsed = 0f;
            float menuStartAlpha = menuCanvasGroup != null ? menuCanvasGroup.alpha : 1f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float percent = Mathf.Clamp01(elapsed / fadeOutDuration);

                if (menuCanvasGroup != null)
                    menuCanvasGroup.alpha = Mathf.Lerp(menuStartAlpha, 0f, percent);

                SetFadeOverlayAlpha(percent);
                yield return null;
            }

            if (menuCanvasGroup != null)
                menuCanvasGroup.alpha = 0f;

            SetFadeOverlayAlpha(1f);
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            if (canvasGroup == null)
                yield break;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float percent = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, percent);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void QuitGame()
        {
            if (isTransitioning)
                return;

#if UNITY_EDITOR
            if (quitStopsEditorPlayMode && Application.isEditor)
            {
                EditorApplication.isPlaying = false;
                return;
            }
#endif

            Application.Quit();
        }

        private void CreateMenuUi()
        {
            Font resolvedFont = GetMenuFont();

            GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRectTransform = canvasObject.GetComponent<RectTransform>();
            StretchToFill(canvasRectTransform);

            Image backgroundImage = CreateFullscreenImage("Background", canvasRectTransform, backgroundColor);
            backgroundImage.raycastTarget = false;

            RectTransform menuRoot = CreateRectTransform("MenuRoot", canvasRectTransform);
            StretchToFill(menuRoot);
            menuCanvasGroup = menuRoot.gameObject.AddComponent<CanvasGroup>();

            Text titleLabel = CreateText("Title", menuRoot, titleText, resolvedFont, 86, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter);
            RectTransform titleRectTransform = titleLabel.rectTransform;
            titleRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            titleRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            titleRectTransform.anchoredPosition = new Vector2(0f, 190f);
            titleRectTransform.sizeDelta = new Vector2(1280f, 140f);

            playButtonView = CreateButton("PlayButton", menuRoot, playButtonText, new Vector2(0f, -10f), resolvedFont);
            playButtonView.Button.onClick.AddListener(BeginPlaySequence);

            quitButtonView = CreateButton("QuitButton", menuRoot, quitButtonText, new Vector2(0f, -110f), resolvedFont);
            quitButtonView.Button.onClick.AddListener(QuitGame);

            fadeOverlayImage = CreateFullscreenImage("FadeOverlay", canvasRectTransform, new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f));
            fadeOverlayImage.raycastTarget = false;

            RectTransform instructionsRoot = CreateRectTransform("IntroInstructions", canvasRectTransform);
            StretchToFill(instructionsRoot);
            instructionsCanvasGroup = instructionsRoot.gameObject.AddComponent<CanvasGroup>();
            instructionsCanvasGroup.alpha = 0f;
            instructionsCanvasGroup.interactable = false;
            instructionsCanvasGroup.blocksRaycasts = false;

            Text instructionsLabel = CreateText("InstructionsText", instructionsRoot, instructionsText, resolvedFont, 44, FontStyle.Bold, instructionsColor, TextAnchor.MiddleCenter);
            instructionsLabel.resizeTextForBestFit = true;
            instructionsLabel.resizeTextMinSize = 30;
            instructionsLabel.resizeTextMaxSize = 48;
            instructionsLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            instructionsLabel.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform instructionsRectTransform = instructionsLabel.rectTransform;
            instructionsRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            instructionsRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            instructionsRectTransform.anchoredPosition = Vector2.zero;
            instructionsRectTransform.sizeDelta = new Vector2(1320f, 620f);
        }

        private MenuButtonView CreateButton(string objectName, RectTransform parent, string labelText, Vector2 anchoredPosition, Font resolvedFont)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.layer = parent.gameObject.layer;

            RectTransform buttonRectTransform = buttonObject.GetComponent<RectTransform>();
            buttonRectTransform.SetParent(parent, false);
            buttonRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRectTransform.anchoredPosition = anchoredPosition;
            buttonRectTransform.sizeDelta = new Vector2(390f, 76f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = buttonColor;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.None;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            Text label = CreateText("Label", buttonRectTransform, labelText, resolvedFont, 34, FontStyle.Bold, buttonTextColor, TextAnchor.MiddleCenter);
            RectTransform labelRectTransform = label.rectTransform;
            StretchToFill(labelRectTransform);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 22;
            label.resizeTextMaxSize = 36;

            MenuButtonView buttonView = new MenuButtonView(
                buttonRectTransform,
                button,
                buttonImage,
                label,
                buttonColor,
                buttonHoverColor,
                buttonPressedColor,
                buttonTextColor,
                buttonActiveTextColor,
                buttonNormalScale,
                buttonHoverScale,
                buttonPressedScale);

            buttonViews.Add(buttonView);
            return buttonView;
        }

        private Text CreateText(
            string objectName,
            RectTransform parent,
            string text,
            Font resolvedFont,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.layer = parent.gameObject.layer;

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            Text label = textObject.GetComponent<Text>();
            label.text = text;
            label.font = resolvedFont;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            return label;
        }

        private Image CreateFullscreenImage(string objectName, RectTransform parent, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = parent.gameObject.layer;

            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            StretchToFill(rectTransform);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private RectTransform CreateRectTransform(string objectName, RectTransform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static void StretchToFill(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private void SetMenuInteractable(bool isInteractable)
        {
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.interactable = isInteractable;
                menuCanvasGroup.blocksRaycasts = isInteractable;
            }

            foreach (MenuButtonView buttonView in buttonViews)
                buttonView.SetInteractable(isInteractable);
        }

        private void SetFadeOverlayAlpha(float alpha)
        {
            if (fadeOverlayImage == null)
                return;

            fadeOverlayImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Clamp01(alpha));
        }

        private void UpdatePointerInput()
        {
            if (Mouse.current == null)
            {
                playButtonView.SetPointerState(false, false);
                quitButtonView.SetPointerState(false, false);
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            bool mousePressed = Mouse.current.leftButton.isPressed;
            bool mouseClicked = Mouse.current.leftButton.wasPressedThisFrame;

            UpdateButtonPointer(playButtonView, mousePosition, mousePressed, mouseClicked, BeginPlaySequence);
            if (!isTransitioning)
                UpdateButtonPointer(quitButtonView, mousePosition, mousePressed, mouseClicked, QuitGame);
        }

        private static void UpdateButtonPointer(
            MenuButtonView buttonView,
            Vector2 mousePosition,
            bool mousePressed,
            bool mouseClicked,
            System.Action clickAction)
        {
            bool isHovered = RectTransformUtility.RectangleContainsScreenPoint(buttonView.RectTransform, mousePosition, null);
            buttonView.SetPointerState(isHovered, isHovered && mousePressed);

            if (isHovered && mouseClicked)
                clickAction.Invoke();
        }

        private bool WasSubmitPressed()
        {
            return Keyboard.current != null
                && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        }

        private bool WasCancelPressed()
        {
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        private Font GetMenuFont()
        {
            if (menuFont != null)
                return menuFont;

            Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return legacyFont != null ? legacyFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static IEnumerator WaitForUnscaledSeconds(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private sealed class MenuButtonView
        {
            private readonly Image backgroundImage;
            private readonly Text label;
            private readonly Color normalColor;
            private readonly Color hoverColor;
            private readonly Color pressedColor;
            private readonly Color normalTextColor;
            private readonly Color activeTextColor;
            private readonly Vector3 normalScale;
            private readonly Vector3 hoverScale;
            private readonly Vector3 pressedScale;
            private Vector3 targetScale;

            public MenuButtonView(
                RectTransform rectTransform,
                Button button,
                Image backgroundImage,
                Text label,
                Color normalColor,
                Color hoverColor,
                Color pressedColor,
                Color normalTextColor,
                Color activeTextColor,
                Vector3 normalScale,
                Vector3 hoverScale,
                Vector3 pressedScale)
            {
                RectTransform = rectTransform;
                Button = button;
                this.backgroundImage = backgroundImage;
                this.label = label;
                this.normalColor = normalColor;
                this.hoverColor = hoverColor;
                this.pressedColor = pressedColor;
                this.normalTextColor = normalTextColor;
                this.activeTextColor = activeTextColor;
                this.normalScale = normalScale;
                this.hoverScale = hoverScale;
                this.pressedScale = pressedScale;
                targetScale = normalScale;

                SetPointerState(false, false);
                RectTransform.localScale = normalScale;
            }

            public RectTransform RectTransform { get; }
            public Button Button { get; }

            public void SetInteractable(bool isInteractable)
            {
                Button.interactable = isInteractable;
                if (!isInteractable)
                    SetPointerState(false, false);
            }

            public void SetPointerState(bool isHovered, bool isPressed)
            {
                if (!Button.interactable)
                {
                    ApplyVisuals(normalColor, normalTextColor, normalScale);
                    return;
                }

                if (isPressed)
                    ApplyVisuals(pressedColor, activeTextColor, pressedScale);
                else if (isHovered)
                    ApplyVisuals(hoverColor, activeTextColor, hoverScale);
                else
                    ApplyVisuals(normalColor, normalTextColor, normalScale);
            }

            public void Tick(float sharpness, float deltaTime)
            {
                float lerp = 1f - Mathf.Exp(-sharpness * deltaTime);
                RectTransform.localScale = Vector3.Lerp(RectTransform.localScale, targetScale, lerp);
            }

            private void ApplyVisuals(Color backgroundColor, Color textColor, Vector3 scale)
            {
                backgroundImage.color = backgroundColor;
                label.color = textColor;
                targetScale = scale;
            }
        }
    }
}
