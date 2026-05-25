using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Crystal
{
    /// <summary>
    /// Runs the boss/player victory presentation after PlayerVictoryEventChannel is raised.
    /// Place this on a scene UI canvas, assign the fade image, and wire the victory event channel.
    /// </summary>
    public sealed class PlayerVictoryScreenController : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField] private PlayerVictoryEventChannel playerVictoryEventChannel;

        [Header("UI References")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private CanvasGroup victoryScreenCanvasGroup;
        [SerializeField] private CanvasGroup victoryTextCanvasGroup;
        [SerializeField] private CanvasGroup backToMainMenuButtonCanvasGroup;
        [SerializeField] private Text victoryTextLabel;
        [SerializeField] private Button backToMainMenuButton;
        [SerializeField] private Text backToMainMenuButtonLabel;

        [Header("Text")]
        [SerializeField] private Font menuFont;
        [SerializeField] private string victoryText = "Victory!";
        [SerializeField] private string backToMainMenuButtonText = "Back to Main Menu";

        [Header("Timing")]
        [SerializeField] private float revealDelay = 0.25f;
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private float whiteHoldDuration = 2f;
        [SerializeField] private float victoryTextFadeDuration = 0.45f;
        [SerializeField] private float buttonRevealDelay = 0.3f;
        [SerializeField] private float buttonFadeDuration = 0.45f;

        [Header("Scene Flow")]
        [SerializeField] private bool freezeTimeDuringVictory = true;
        [SerializeField] private string mainMenuSceneName = "Main Manu";

        [Header("Visuals")]
        [SerializeField] private Color victoryTextColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color buttonTextColor = Color.white;
        [SerializeField] private Vector3 buttonNormalScale = Vector3.one;
        [SerializeField] private Vector3 buttonHoverScale = new Vector3(1.08f, 1.08f, 1f);

        private Coroutine sequenceRoutine;
        private float previousTimeScale = 1f;
        private bool timeScaleFrozen;
        private bool backButtonReady;
        private bool loadingMainMenu;
        private Image backToMainMenuButtonImage;
        private RectTransform backToMainMenuButtonRectTransform;
        private bool backToMainMenuButtonHovered;

        private void Awake()
        {
            EnsureVictoryScreen();
            ConfigureBackToMainMenuButton();
            ApplyVictoryVisuals();
            HideVictoryScreenImmediate();
        }

        private void OnEnable()
        {
            if (playerVictoryEventChannel != null)
                playerVictoryEventChannel.Subscribe(StartVictorySequence);
            else
                Debug.LogWarning("[PlayerVictoryScreenController] No PlayerVictoryEventChannel assigned.", this);
        }

        private void OnDisable()
        {
            if (playerVictoryEventChannel != null)
                playerVictoryEventChannel.Unsubscribe(StartVictorySequence);

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            backButtonReady = false;
            RestoreTimeScale();
        }

        private void OnDestroy()
        {
            if (backToMainMenuButton != null)
                backToMainMenuButton.onClick.RemoveListener(LoadMainMenu);
        }

        private void Update()
        {
            if (!backButtonReady || loadingMainMenu)
            {
                SetBackToMainMenuButtonHovered(false);
                return;
            }

            UpdateBackToMainMenuButtonHover();
            if (WasSubmitPressed() || WasBackToMainMenuClicked())
                LoadMainMenu();
        }

        private void OnValidate()
        {
            revealDelay = Mathf.Max(0f, revealDelay);
            fadeDuration = Mathf.Max(0.01f, fadeDuration);
            whiteHoldDuration = Mathf.Max(0f, whiteHoldDuration);
            victoryTextFadeDuration = Mathf.Max(0f, victoryTextFadeDuration);
            buttonRevealDelay = Mathf.Max(0f, buttonRevealDelay);
            buttonFadeDuration = Mathf.Max(0f, buttonFadeDuration);
            ApplyVictoryVisuals();
        }

        public void StartVictorySequence()
        {
            if (sequenceRoutine != null)
                return;

            if (fadeImage == null)
            {
                Debug.LogError("[PlayerVictoryScreenController] Missing required reference: fadeImage.", this);
                return;
            }

            sequenceRoutine = StartCoroutine(PlayVictorySequenceRoutine());
        }

        private IEnumerator PlayVictorySequenceRoutine()
        {
            HideVictoryScreenImmediate();

            if (freezeTimeDuringVictory)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                timeScaleFrozen = true;
            }

            yield return WaitForUnscaledSeconds(revealDelay);
            SfxPlayer.Play("final");
            yield return FadeToWhite();
            yield return WaitForUnscaledSeconds(whiteHoldDuration);

            ShowVictoryScreenImmediate();
            yield return FadeCanvasGroup(victoryTextCanvasGroup, 1f, victoryTextFadeDuration);
            yield return WaitForUnscaledSeconds(buttonRevealDelay);
            yield return FadeCanvasGroup(backToMainMenuButtonCanvasGroup, 1f, buttonFadeDuration);
            SetBackToMainMenuButtonInteractable(true);
        }

        private void EnsureVictoryScreen()
        {
            if (victoryScreenCanvasGroup == null)
                CreateVictoryScreen();

            if (victoryScreenCanvasGroup != null)
                victoryScreenCanvasGroup.transform.SetAsLastSibling();

            RectTransform victoryRoot = victoryScreenCanvasGroup != null
                ? victoryScreenCanvasGroup.transform as RectTransform
                : null;

            if (victoryTextLabel == null && victoryRoot != null)
                CreateVictoryTitle(victoryRoot);

            if (backToMainMenuButton == null && victoryRoot != null)
                CreateBackToMainMenuButton(victoryRoot);

            if (victoryTextCanvasGroup == null && victoryTextLabel != null)
                victoryTextCanvasGroup = EnsureCanvasGroup(victoryTextLabel.gameObject);

            if (backToMainMenuButtonCanvasGroup == null && backToMainMenuButton != null)
                backToMainMenuButtonCanvasGroup = EnsureCanvasGroup(backToMainMenuButton.gameObject);
        }

        private void CreateVictoryScreen()
        {
            RectTransform parent = fadeImage != null
                ? fadeImage.rectTransform.parent as RectTransform
                : transform as RectTransform;

            if (parent == null)
            {
                Debug.LogError("[PlayerVictoryScreenController] Cannot create victory UI without a RectTransform parent.", this);
                return;
            }

            GameObject root = new GameObject("VictoryScreen", typeof(RectTransform), typeof(CanvasGroup));
            root.layer = parent.gameObject.layer;

            RectTransform rootRectTransform = root.GetComponent<RectTransform>();
            rootRectTransform.SetParent(parent, false);
            rootRectTransform.anchorMin = Vector2.zero;
            rootRectTransform.anchorMax = Vector2.one;
            rootRectTransform.offsetMin = Vector2.zero;
            rootRectTransform.offsetMax = Vector2.zero;
            rootRectTransform.SetAsLastSibling();

            victoryScreenCanvasGroup = root.GetComponent<CanvasGroup>();
        }

        private void CreateVictoryTitle(RectTransform parent)
        {
            GameObject titleObject = new GameObject("VictoryTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(CanvasGroup));
            titleObject.layer = parent.gameObject.layer;

            RectTransform titleRectTransform = titleObject.GetComponent<RectTransform>();
            titleRectTransform.SetParent(parent, false);
            titleRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            titleRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            titleRectTransform.anchoredPosition = new Vector2(0f, 90f);
            titleRectTransform.sizeDelta = new Vector2(720f, 140f);

            victoryTextLabel = titleObject.GetComponent<Text>();
            victoryTextLabel.alignment = TextAnchor.MiddleCenter;
            victoryTextLabel.fontSize = 76;
            victoryTextLabel.fontStyle = FontStyle.Bold;
            victoryTextLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            victoryTextLabel.verticalOverflow = VerticalWrapMode.Overflow;
            victoryTextLabel.raycastTarget = false;

            victoryTextCanvasGroup = titleObject.GetComponent<CanvasGroup>();
        }

        private void CreateBackToMainMenuButton(RectTransform parent)
        {
            GameObject buttonObject = new GameObject("BackToMainMenuButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
            buttonObject.layer = parent.gameObject.layer;

            RectTransform buttonRectTransform = buttonObject.GetComponent<RectTransform>();
            buttonRectTransform.SetParent(parent, false);
            buttonRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRectTransform.anchoredPosition = new Vector2(0f, -80f);
            buttonRectTransform.sizeDelta = new Vector2(380f, 64f);
            backToMainMenuButtonRectTransform = buttonRectTransform;

            backToMainMenuButtonImage = buttonObject.GetComponent<Image>();
            backToMainMenuButton = buttonObject.GetComponent<Button>();
            backToMainMenuButton.targetGraphic = backToMainMenuButtonImage;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.layer = parent.gameObject.layer;

            RectTransform labelRectTransform = labelObject.GetComponent<RectTransform>();
            labelRectTransform.SetParent(buttonRectTransform, false);
            labelRectTransform.anchorMin = Vector2.zero;
            labelRectTransform.anchorMax = Vector2.one;
            labelRectTransform.offsetMin = Vector2.zero;
            labelRectTransform.offsetMax = Vector2.zero;

            backToMainMenuButtonLabel = labelObject.GetComponent<Text>();
            backToMainMenuButtonLabel.alignment = TextAnchor.MiddleCenter;
            backToMainMenuButtonLabel.fontSize = 28;
            backToMainMenuButtonLabel.fontStyle = FontStyle.Bold;
            backToMainMenuButtonLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            backToMainMenuButtonLabel.verticalOverflow = VerticalWrapMode.Overflow;
            backToMainMenuButtonLabel.raycastTarget = false;

            backToMainMenuButtonCanvasGroup = buttonObject.GetComponent<CanvasGroup>();
        }

        private void ConfigureBackToMainMenuButton()
        {
            if (backToMainMenuButton == null)
                return;

            backToMainMenuButton.onClick.RemoveListener(LoadMainMenu);
            backToMainMenuButton.onClick.AddListener(LoadMainMenu);
        }

        private void ApplyVictoryVisuals()
        {
            Font font = GetMenuFont();

            if (victoryTextLabel != null)
            {
                victoryTextLabel.text = victoryText;
                victoryTextLabel.color = victoryTextColor;
                if (font != null)
                    victoryTextLabel.font = font;
            }

            if (backToMainMenuButton != null && backToMainMenuButtonImage == null)
                backToMainMenuButton.TryGetComponent(out backToMainMenuButtonImage);

            if (backToMainMenuButton != null && backToMainMenuButtonRectTransform == null)
                backToMainMenuButtonRectTransform = backToMainMenuButton.transform as RectTransform;

            if (backToMainMenuButtonLabel != null)
            {
                backToMainMenuButtonLabel.text = backToMainMenuButtonText;
                if (font != null)
                    backToMainMenuButtonLabel.font = font;
            }

            ApplyBackToMainMenuButtonVisuals(backToMainMenuButtonHovered && backButtonReady);
        }

        private Font GetMenuFont()
        {
            if (menuFont != null)
                return menuFont;

            Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return legacyFont != null ? legacyFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            if (target == null)
                return null;

            if (!target.TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup = target.AddComponent<CanvasGroup>();

            return canvasGroup;
        }

        private IEnumerator FadeToWhite()
        {
            float elapsed = 0f;
            SetFadeImage(Color.white, 0f);

            while (elapsed < fadeDuration)
            {
                float percent = Mathf.Clamp01(elapsed / fadeDuration);
                SetFadeImage(Color.white, percent);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetFadeImage(Color.white, 1f);
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            if (canvasGroup == null)
                yield break;

            float startAlpha = canvasGroup.alpha;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, percent);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void HideVictoryScreenImmediate()
        {
            backButtonReady = false;
            SetBackToMainMenuButtonHovered(false);
            SetBackToMainMenuButtonInteractable(false);

            if (victoryScreenCanvasGroup != null)
            {
                victoryScreenCanvasGroup.alpha = 0f;
                victoryScreenCanvasGroup.interactable = false;
                victoryScreenCanvasGroup.blocksRaycasts = false;
            }

            if (victoryTextCanvasGroup != null)
                victoryTextCanvasGroup.alpha = 0f;

            if (backToMainMenuButtonCanvasGroup != null)
                backToMainMenuButtonCanvasGroup.alpha = 0f;
        }

        private void ShowVictoryScreenImmediate()
        {
            if (victoryScreenCanvasGroup != null)
            {
                victoryScreenCanvasGroup.alpha = 1f;
                victoryScreenCanvasGroup.interactable = true;
                victoryScreenCanvasGroup.blocksRaycasts = true;
            }

            if (victoryTextCanvasGroup != null)
                victoryTextCanvasGroup.alpha = 0f;

            if (backToMainMenuButtonCanvasGroup != null)
                backToMainMenuButtonCanvasGroup.alpha = 0f;

            SetBackToMainMenuButtonInteractable(false);
            SetBackToMainMenuButtonHovered(false);
        }

        private void SetBackToMainMenuButtonInteractable(bool isInteractable)
        {
            backButtonReady = isInteractable;

            if (backToMainMenuButton != null)
                backToMainMenuButton.interactable = isInteractable;

            if (backToMainMenuButtonCanvasGroup != null)
            {
                backToMainMenuButtonCanvasGroup.interactable = isInteractable;
                backToMainMenuButtonCanvasGroup.blocksRaycasts = isInteractable;
            }

            if (!isInteractable)
                SetBackToMainMenuButtonHovered(false);
        }

        private void SetFadeImage(Color color, float alpha)
        {
            if (fadeImage == null)
                return;

            fadeImage.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        private bool WasSubmitPressed()
        {
            return (Keyboard.current != null
                    && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        }

        private bool WasBackToMainMenuClicked()
        {
            return Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame
                && IsPointerOverBackToMainMenuButton();
        }

        private void UpdateBackToMainMenuButtonHover()
        {
            SetBackToMainMenuButtonHovered(IsPointerOverBackToMainMenuButton());
        }

        private void SetBackToMainMenuButtonHovered(bool isHovered)
        {
            if (backToMainMenuButtonHovered == isHovered)
                return;

            backToMainMenuButtonHovered = isHovered;
            ApplyBackToMainMenuButtonVisuals(isHovered);
        }

        private void ApplyBackToMainMenuButtonVisuals(bool isHovered)
        {
            if (backToMainMenuButton != null && backToMainMenuButtonImage == null)
                backToMainMenuButton.TryGetComponent(out backToMainMenuButtonImage);

            RectTransform buttonRectTransform = GetBackToMainMenuButtonRectTransform();

            Color backgroundColor = isHovered ? buttonTextColor : buttonColor;
            Color textColor = isHovered ? buttonColor : buttonTextColor;

            if (backToMainMenuButtonImage != null)
                backToMainMenuButtonImage.color = backgroundColor;

            if (backToMainMenuButtonLabel != null)
                backToMainMenuButtonLabel.color = textColor;

            if (buttonRectTransform != null)
                buttonRectTransform.localScale = isHovered ? buttonHoverScale : buttonNormalScale;
        }

        private bool IsPointerOverBackToMainMenuButton()
        {
            if (Mouse.current == null)
                return false;

            RectTransform buttonRectTransform = GetBackToMainMenuButtonRectTransform();
            if (buttonRectTransform == null)
                return false;

            Canvas canvas = buttonRectTransform.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            return RectTransformUtility.RectangleContainsScreenPoint(
                buttonRectTransform,
                Mouse.current.position.ReadValue(),
                camera);
        }

        private RectTransform GetBackToMainMenuButtonRectTransform()
        {
            if (backToMainMenuButtonRectTransform == null && backToMainMenuButton != null)
                backToMainMenuButtonRectTransform = backToMainMenuButton.transform as RectTransform;

            return backToMainMenuButtonRectTransform;
        }

        private void LoadMainMenu()
        {
            if (loadingMainMenu)
                return;

            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                Debug.LogError("[PlayerVictoryScreenController] Cannot load main menu because mainMenuSceneName is empty.", this);
                return;
            }

            loadingMainMenu = true;
            MusicManager.PlayMainMenuTrackFromActiveManager();
            RestoreTimeScale();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void RestoreTimeScale()
        {
            if (!timeScaleFrozen)
                return;

            Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;
            timeScaleFrozen = false;
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
    }
}
