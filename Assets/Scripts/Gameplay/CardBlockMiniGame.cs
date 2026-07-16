using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum CardBlockColor
    {
        Red,
        Green,
        Blue
    }

    public class CardBlockMiniGame : MonoBehaviour
    {
        private const int SequenceLength = 3;

        [Header("Player Freeze")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Rigidbody playerBody;
        [SerializeField] private Behaviour[] movementComponents;

        [Header("UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Font uiFont;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.68f);
        [SerializeField] private Color redColor = new Color(0.92f, 0.22f, 0.18f);
        [SerializeField] private Color greenColor = new Color(0.16f, 0.72f, 0.36f);
        [SerializeField] private Color blueColor = new Color(0.22f, 0.48f, 0.95f);

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float flashSeconds = 0.32f;
        [SerializeField, Min(0.05f)] private float flashGapSeconds = 0.16f;
        [SerializeField, Min(0f)] private float retryDelaySeconds = 0.75f;
        [SerializeField, Min(0f)] private float failTimePenaltySeconds;

        [Header("Reward")]
        [SerializeField, Min(0f)] private float successTimeReward = 5f;

        public UnityEvent OnMiniGameStarted = new UnityEvent();
        public UnityEvent OnMiniGameCompleted = new UnityEvent();
        public UnityEvent OnMiniGameFailed = new UnityEvent();

        private readonly CardBlockColor[] sequence = new CardBlockColor[SequenceLength];
        private readonly Button[] colorButtons = new Button[3];
        private readonly Image[] buttonImages = new Image[3];

        private GameObject overlayRoot;
        private Text statusText;
        private Coroutine gameRoutine;
        private bool[] movementWasEnabled = System.Array.Empty<bool>();
        private int inputIndex;
        private bool acceptingInput;
        private bool isRunning;

        public bool IsRunning => isRunning;

        private void Awake()
        {
            if (playerRoot == null)
            {
                var playerObject = GameObject.FindWithTag("Player");
                if (playerObject != null)
                {
                    playerRoot = playerObject.transform;
                }
            }

            if (playerBody == null && playerRoot != null)
            {
                playerBody = playerRoot.GetComponent<Rigidbody>();
            }

            if (movementComponents == null || movementComponents.Length == 0)
            {
                AutoBindMovementComponents();
            }

            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        public void Action()
        {
            StartMiniGame();
        }

        public void StartCardBlockTask()
        {
            StartMiniGame();
        }

        public void StartMiniGame()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;
            FreezePlayer(true);
            EnsureOverlay();
            overlayRoot.SetActive(true);
            OnMiniGameStarted.Invoke();
            gameRoutine = StartCoroutine(RunRound());
        }

        public void CancelMiniGame()
        {
            if (!isRunning)
            {
                return;
            }

            if (gameRoutine != null)
            {
                StopCoroutine(gameRoutine);
                gameRoutine = null;
            }

            CompleteCleanup(true);
        }

        private IEnumerator RunRound()
        {
            GenerateSequence();
            inputIndex = 0;
            acceptingInput = false;
            SetButtonsInteractable(false);
            SetStatus("WATCH");

            yield return FlashSequence();

            SetStatus("ENTER CODE");
            acceptingInput = true;
            SetButtonsInteractable(true);
        }

        private void GenerateSequence()
        {
            for (var i = 0; i < sequence.Length; i++)
            {
                sequence[i] = (CardBlockColor)Random.Range(0, 3);
            }
        }

        private IEnumerator FlashSequence()
        {
            for (var i = 0; i < sequence.Length; i++)
            {
                var index = (int)sequence[i];
                SetButtonHighlighted(index, true);
                yield return new WaitForSeconds(flashSeconds);
                SetButtonHighlighted(index, false);
                yield return new WaitForSeconds(flashGapSeconds);
            }
        }

        private void HandleButtonPressed(CardBlockColor color)
        {
            if (!acceptingInput || !isRunning)
            {
                return;
            }

            if (sequence[inputIndex] != color)
            {
                gameRoutine = StartCoroutine(HandleFailedInput());
                return;
            }

            inputIndex++;
            SetStatus($"{inputIndex}/{SequenceLength}");

            if (inputIndex >= SequenceLength)
            {
                HandleSuccess();
            }
        }

        private IEnumerator HandleFailedInput()
        {
            acceptingInput = false;
            SetButtonsInteractable(false);
            SetStatus("TRY AGAIN");
            OnMiniGameFailed.Invoke();

            if (failTimePenaltySeconds > 0f && TimeManager.Instance != null)
            {
                TimeManager.Instance.SubtractTime(failTimePenaltySeconds);
            }

            yield return new WaitForSeconds(retryDelaySeconds);
            gameRoutine = StartCoroutine(RunRound());
        }

        private void HandleSuccess()
        {
            acceptingInput = false;
            SetButtonsInteractable(false);
            SetStatus("UNBLOCKED");

            if (successTimeReward > 0f && TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(successTimeReward);
            }

            OnMiniGameCompleted.Invoke();
            CompleteCleanup(true);
        }

        private void CompleteCleanup(bool hideOverlay)
        {
            isRunning = false;
            acceptingInput = false;
            gameRoutine = null;
            FreezePlayer(false);

            if (hideOverlay && overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        private void FreezePlayer(bool freeze)
        {
            if (freeze)
            {
                if (movementWasEnabled.Length != movementComponents.Length)
                {
                    movementWasEnabled = new bool[movementComponents.Length];
                }

                for (var i = 0; i < movementComponents.Length; i++)
                {
                    var component = movementComponents[i];
                    movementWasEnabled[i] = component != null && component.enabled;
                    if (component != null)
                    {
                        component.enabled = false;
                    }
                }

                if (playerBody != null)
                {
                    playerBody.linearVelocity = Vector3.zero;
                    playerBody.angularVelocity = Vector3.zero;
                }

                return;
            }

            for (var i = 0; i < movementComponents.Length && i < movementWasEnabled.Length; i++)
            {
                var component = movementComponents[i];
                if (component != null)
                {
                    component.enabled = movementWasEnabled[i];
                }
            }
        }

        private void AutoBindMovementComponents()
        {
            if (playerRoot == null)
            {
                return;
            }

            movementComponents = new Behaviour[]
            {
                playerRoot.GetComponent<ChubbyTopDownInputController>(),
                playerRoot.GetComponent<MobilePlayerController>(),
                playerRoot.GetComponent<ChubbyRigidbodyCharacterController>()
            };
        }

        private void EnsureOverlay()
        {
            if (overlayRoot != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasObject = new GameObject("Card Block MiniGame Canvas");
                targetCanvas = canvasObject.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            overlayRoot = new GameObject("Card Block MiniGame Overlay");
            overlayRoot.transform.SetParent(targetCanvas.transform, false);

            var panel = overlayRoot.AddComponent<Image>();
            panel.color = overlayColor;
            var panelRect = overlayRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            statusText = CreateText("Status", overlayRoot.transform, "WATCH", 42, new Vector2(0f, 210f), new Vector2(420f, 80f));
            CreateButton(CardBlockColor.Red, 0, redColor, new Vector2(-180f, -20f));
            CreateButton(CardBlockColor.Green, 1, greenColor, new Vector2(0f, -20f));
            CreateButton(CardBlockColor.Blue, 2, blueColor, new Vector2(180f, -20f));
        }

        private void CreateButton(CardBlockColor color, int index, Color baseColor, Vector2 anchoredPosition)
        {
            var buttonObject = new GameObject($"{color} Button");
            buttonObject.transform.SetParent(overlayRoot.transform, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = baseColor;
            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => HandleButtonPressed(color));

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(140f, 140f);

            CreateText("Label", buttonObject.transform, color.ToString().ToUpperInvariant(), 24, Vector2.zero, new Vector2(130f, 60f));

            colorButtons[index] = button;
            buttonImages[index] = image;
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, Vector2 anchoredPosition, Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            for (var i = 0; i < colorButtons.Length; i++)
            {
                if (colorButtons[i] != null)
                {
                    colorButtons[i].interactable = interactable;
                }
            }
        }

        private void SetButtonHighlighted(int index, bool highlighted)
        {
            if (index < 0 || index >= buttonImages.Length || buttonImages[index] == null)
            {
                return;
            }

            var baseColor = GetColor((CardBlockColor)index);
            buttonImages[index].color = highlighted ? Color.Lerp(baseColor, Color.white, 0.65f) : baseColor;
        }

        private Color GetColor(CardBlockColor color)
        {
            return color switch
            {
                CardBlockColor.Red => redColor,
                CardBlockColor.Green => greenColor,
                CardBlockColor.Blue => blueColor,
                _ => Color.white
            };
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }
        }
    }
}
