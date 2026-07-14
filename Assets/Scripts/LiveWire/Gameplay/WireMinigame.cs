using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LiveWire
{
    public class WireMinigame : MonoBehaviour
    {
        public static WireMinigame Instance { get; private set; }

        public Transform bombRoot;
        public Transform interiorRoot;
        public Canvas minigameCanvas;
        public RectTransform overlayRoot;
        public RectTransform wireListRoot;
        public RectTransform challengeRoot;
        public Image vignetteImage;
        public Text panelTitle;
        public Text panelSubtitle;
        public Text controlsText;
        public Text subsystemStatusText;
        public Text utilityText;
        public Text feedText;
        public Image instabilityFill;

        public enum WireType { Red, Blue, Green, Yellow, White }
        static readonly string[] SubsystemNames = { "TRAVA", "REGULADOR", "BARRAMENTO", "FUSOR", "NUCLEO" };

        static readonly Color RedCol = new(1.0f, 0.18f, 0.18f);
        static readonly Color BlueCol = new(0.25f, 0.55f, 1.0f);
        static readonly Color GreenCol = new(0.2f, 1.0f, 0.35f);
        static readonly Color YellowCol = new(1.0f, 0.85f, 0.15f);
        static readonly Color WhiteCol = new(0.92f, 0.95f, 1.0f);
        static readonly Color BaseOverlayColor = new(0f, 0.02f, 0.01f, 0.92f);
        const float InstabilityMax = 100f;

        class WireEntry
        {
            public WireType type;
            public Color color;
            public RectTransform entry;
            public Image wireGraphic;
            public Image led;
            public Text label;
            public bool connected;
        }

        readonly List<WireEntry> wires = new();
        readonly List<Renderer> hiddenRenderers = new();
        int currentIndex;
        int phase;
        bool open;
        Coroutine challengeRoutine;
        Coroutine openSequenceRoutine;
        Coroutine overloadRoutine;
        bool tensionCuePlaying;
        bool almostTriggered;
        bool finalOverrideActive;
        bool finalOverridePending;
        RectTransform activeChallenge;
        float instability;
        float feedMessageClock;
        string transientFeedMessage = string.Empty;
        bool combinedChallengeStarted;
        Coroutine combinedRoutine;

        enum CombinedMode { Reaction, Diagnostics }
        enum ReactionRule
        {
            WithRed,
            NeverAlone,
            AfterGreenOff,
            WithBlue,
            Fastest,
            Slowest
        }

        class ReactionWire
        {
            public WireType type;
            public string name;
            public Color color;
            public float interval;
            public bool lit;
            public bool wasLit;
            public float lastOffTime = -999f;
            public Image wireImage;
            public Image glowImage;
            public Text stateText;
            public Button cutButton;
        }

        class DiagnosticWire
        {
            public WireType type;
            public string name;
            public Color color;
            public bool energized;
            public bool marked;
            public Button selectButton;
            public Image plateImage;
            public Text labelText;
        }

        public bool IsOpen => open;

        void Update()
        {
            if (!open) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                BombManager.Instance?.CancelMinigame();
            }

            UpdateInstability();
            HandleEmergencyAction();
            UpdateSupportUi();
        }

        void Awake()
        {
            Instance = this;
            if (interiorRoot != null) interiorRoot.gameObject.SetActive(false);
            if (minigameCanvas != null) minigameCanvas.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(int currentPhase)
        {
            if (open) return;
            phase = currentPhase;
            openSequenceRoutine = StartCoroutine(OpenSequence());
        }

        IEnumerator OpenSequence()
        {
            open = true;
            currentIndex = 0;
            almostTriggered = false;
            tensionCuePlaying = false;
            finalOverrideActive = false;
            finalOverridePending = PhaseUsesFinalOverride();
            combinedChallengeStarted = false;
            instability = phase >= 3 ? 10f + (phase - 3) * 4f : 0f;
            transientFeedMessage = string.Empty;
            feedMessageClock = 0f;

            PositionInterior();
            HideBombExterior();

            if (minigameCanvas != null) minigameCanvas.gameObject.SetActive(true);
            BuildWireList();
            ClearChallenge();
            HighlightActiveWire();
            UpdateSupportUi();

            if (panelTitle != null) panelTitle.text = "PAINEL ABERTO";
            if (panelSubtitle != null) panelSubtitle.text = "...";
            if (controlsText != null) controlsText.text = "PREPARE-SE   |   ESC FECHA O PAINEL";

            AudioManager.Instance?.PauseHiss();
            AudioManager.Instance?.StopTicking();

            yield return new WaitForSecondsRealtime(1.0f);

            AudioManager.Instance?.StartElectricHiss();
            AudioManager.Instance?.StartTicking();

            if (panelSubtitle != null) panelSubtitle.text = "CORTE OS FIOS EM ORDEM";

            yield return new WaitForSecondsRealtime(0.4f);

            StartCurrentWireChallenge();
            openSequenceRoutine = null;
        }

        void PositionInterior()
        {
            if (interiorRoot == null || bombRoot == null) return;
            Camera cam = PlayerController.Instance != null ? PlayerController.Instance.playerCamera : Camera.main;
            if (cam == null) return;

            Vector3 bombCenter = BombManager.Instance != null && BombManager.Instance.cameraZoomTarget != null
                ? BombManager.Instance.cameraZoomTarget.position
                : bombRoot.position + Vector3.up * 0.62f;
            Vector3 dir = bombCenter - cam.transform.position;
            if (dir.sqrMagnitude < 0.0001f) dir = cam.transform.forward;
            dir.Normalize();

            interiorRoot.position = bombCenter;
            interiorRoot.rotation = Quaternion.LookRotation(dir, Vector3.up);
            interiorRoot.gameObject.SetActive(true);
        }

        void HideBombExterior()
        {
            hiddenRenderers.Clear();
            if (bombRoot == null) return;
            Renderer[] all = bombRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in all)
            {
                if (r == null || !r.enabled) continue;
                if (IsDescendantOf(r.transform, interiorRoot)) continue;
                r.enabled = false;
                hiddenRenderers.Add(r);
            }
        }

        void RestoreBombExterior()
        {
            foreach (var r in hiddenRenderers)
            {
                if (r != null) r.enabled = true;
            }
            hiddenRenderers.Clear();
        }

        static bool IsDescendantOf(Transform child, Transform root)
        {
            if (root == null) return false;
            Transform t = child;
            while (t != null)
            {
                if (t == root) return true;
                t = t.parent;
            }
            return false;
        }

        public void Close(bool restoreControl = true, bool resumeBombLoop = false)
        {
            if (challengeRoutine != null) { StopCoroutine(challengeRoutine); challengeRoutine = null; }
            if (combinedRoutine != null) { StopCoroutine(combinedRoutine); combinedRoutine = null; }
            if (openSequenceRoutine != null) { StopCoroutine(openSequenceRoutine); openSequenceRoutine = null; }
            if (overloadRoutine != null) { StopCoroutine(overloadRoutine); overloadRoutine = null; }
            open = false;
            ClearChallenge();
            ClearWireList();
            if (panelTitle != null) panelTitle.text = string.Empty;
            if (panelSubtitle != null) panelSubtitle.text = string.Empty;
            if (controlsText != null) controlsText.text = string.Empty;
            if (subsystemStatusText != null) subsystemStatusText.text = string.Empty;
            if (utilityText != null) utilityText.text = string.Empty;
            if (feedText != null) feedText.text = string.Empty;
            if (instabilityFill != null) instabilityFill.fillAmount = 0f;
            if (vignetteImage != null) vignetteImage.color = BaseOverlayColor;
            if (PlayerController.Instance != null) PlayerController.Instance.SetCursorUnlocked(false);
            if (minigameCanvas != null) minigameCanvas.gameObject.SetActive(false);
            if (interiorRoot != null) interiorRoot.gameObject.SetActive(false);
            RestoreBombExterior();
            AudioManager.Instance?.StopElectricHiss();
            AudioManager.Instance?.StopTensionRising();
            AudioManager.Instance?.ResetRunState();
            if (resumeBombLoop && TimerController.Instance != null && TimerController.Instance.Running)
            {
                AudioManager.Instance?.ResumeHiss();
                AudioManager.Instance?.StartTicking();
            }
            tensionCuePlaying = false;
            instability = 0f;
            finalOverrideActive = false;
            finalOverridePending = false;
            if (restoreControl && PlayerController.Instance != null) PlayerController.Instance.SetInputLocked(false);
        }

        void ClearChallenge()
        {
            if (activeChallenge != null)
            {
                Destroy(activeChallenge.gameObject);
                activeChallenge = null;
            }
            AudioManager.Instance?.StopTensionRising();
            tensionCuePlaying = false;
        }

        void ClearWireList()
        {
            foreach (var w in wires)
            {
                if (w.entry != null) Destroy(w.entry.gameObject);
            }
            wires.Clear();
        }

        void BuildWireList()
        {
            ClearWireList();
            if (wireListRoot == null) return;

            int count = GameManager.Instance != null ? GameManager.Instance.GetWireCount(phase) : 2;
            count = Mathf.Clamp(count, 2, 5);

            WireType[] order = { WireType.Red, WireType.Blue, WireType.Green, WireType.Yellow, WireType.White };

            float rowHeight = 86f;
            float spacing = 14f;

            for (int i = 0; i < count; i++)
            {
                WireType type = order[i % order.Length];
                if (type == WireType.White && phase < 6) type = order[(i - 1 + order.Length) % order.Length];
                if (phase >= 6 && i == count - 1) type = WireType.White;

                Color c = GetWireColor(type);
                WireEntry w = new WireEntry
                {
                    type = type,
                    color = c,
                };

                GameObject entryGO = new GameObject($"Wire_{type}", typeof(RectTransform), typeof(Image));
                RectTransform rect = (RectTransform)entryGO.transform;
                rect.SetParent(wireListRoot, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -i * (rowHeight + spacing) - 10f);
                rect.sizeDelta = new Vector2(0f, rowHeight);
                Image bg = entryGO.GetComponent<Image>();
                bg.sprite = SceneBuildHelpers.GetWhiteSprite();
                bg.color = new Color(0.02f, 0.05f, 0.03f, 0.65f);
                w.entry = rect;

                GameObject wireGO = new GameObject("WireGraphic", typeof(RectTransform), typeof(Image));
                RectTransform wireRect = (RectTransform)wireGO.transform;
                wireRect.SetParent(rect, false);
                wireRect.anchorMin = new Vector2(0f, 0.5f);
                wireRect.anchorMax = new Vector2(1f, 0.5f);
                wireRect.pivot = new Vector2(0.5f, 0.5f);
                wireRect.offsetMin = new Vector2(70f, -7f);
                wireRect.offsetMax = new Vector2(-54f, 7f);
                Image wireImg = wireGO.GetComponent<Image>();
                wireImg.sprite = SceneBuildHelpers.GetWhiteSprite();
                wireImg.color = new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 0.9f);
                w.wireGraphic = wireImg;

                GameObject ledGO = new GameObject("LED", typeof(RectTransform), typeof(Image));
                RectTransform ledRect = (RectTransform)ledGO.transform;
                ledRect.SetParent(rect, false);
                ledRect.anchorMin = ledRect.anchorMax = new Vector2(1f, 0.5f);
                ledRect.pivot = new Vector2(1f, 0.5f);
                ledRect.anchoredPosition = new Vector2(-18f, 0f);
                ledRect.sizeDelta = new Vector2(28f, 28f);
                Image ledImg = ledGO.GetComponent<Image>();
                ledImg.sprite = SceneBuildHelpers.GetWhiteSprite();
                ledImg.color = new Color(0.08f, 0.08f, 0.08f, 1f);
                w.led = ledImg;

                GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                RectTransform labelRect = (RectTransform)labelGO.transform;
                labelRect.SetParent(rect, false);
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(0f, 1f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = new Vector2(18f, 0f);
                labelRect.sizeDelta = new Vector2(60f, 0f);
                Text label = labelGO.GetComponent<Text>();
                label.text = GetWireShortName(type);
                label.font = HudController.GetMonoFont();
                label.fontSize = 26;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = c;
                w.label = label;

                wires.Add(w);
            }
        }

        static Color GetWireColor(WireType t) => t switch
        {
            WireType.Red => RedCol,
            WireType.Blue => BlueCol,
            WireType.Green => GreenCol,
            WireType.Yellow => YellowCol,
            WireType.White => WhiteCol,
            _ => Color.gray,
        };

        static string GetWireShortName(WireType t) => t switch
        {
            WireType.Red => "VER",
            WireType.Blue => "AZU",
            WireType.Green => "VRD",
            WireType.Yellow => "AMA",
            WireType.White => "BRC",
            _ => "---",
        };

        void StartCurrentWireChallenge()
        {
            if (!open) return;
            if (!combinedChallengeStarted)
            {
                combinedChallengeStarted = true;
                HighlightActiveWire();
                if (combinedRoutine != null) StopCoroutine(combinedRoutine);
                combinedRoutine = StartCoroutine(RunCombinedBombChallenge());
                return;
            }

            if (currentIndex >= wires.Count)
            {
                StartCoroutine(finalOverridePending ? FinalOverrideSequence() : CompleteAll());
                return;
            }

            WireEntry wire = wires[currentIndex];
            HighlightActiveWire();
            UpdatePanelHeader(wire);

            if (currentIndex == wires.Count - 1 && !almostTriggered)
            {
                almostTriggered = true;
                AudioManager.Instance?.SwitchToFinalWireAmbience();
            }

            if (wire.type == WireType.Yellow) AudioManager.Instance?.PlayAlert();

            if (challengeRoutine != null) StopCoroutine(challengeRoutine);
            challengeRoutine = StartCoroutine(RunChallenge(wire));
        }

        void UpdatePanelHeader(WireEntry wire)
        {
            if (panelTitle != null) panelTitle.text = $"FIO {GetWireShortName(wire.type)}";
            if (panelSubtitle != null) panelSubtitle.text = GetWireInstruction(wire.type);
            if (controlsText != null) controlsText.text = $"{GetWireControlHint(wire.type)}   |   ESC FECHA O PAINEL";
            if (panelTitle != null) panelTitle.color = wire.color;
            ApplyCursorMode(wire.type);
        }

        bool PhaseUsesLayeredReveal() => phase >= 3;
        bool PhaseUsesDiagnosticsNoise() => phase >= 5;
        bool PhaseUsesInstability() => phase >= 3;
        bool PhaseUsesFinalOverride() => phase >= 4;

        bool IsWireRevealed(int index)
        {
            if (index <= currentIndex) return true;
            if (!PhaseUsesLayeredReveal()) return true;
            int lookAhead = phase >= 6 ? 0 : 1;
            return index <= currentIndex + lookAhead;
        }

        static string GetWireInstruction(WireType t) => t switch
        {
            WireType.Red => "ESPAÇO NA ZONA VERDE",
            WireType.Blue => "MEMORIZE E REPITA",
            WireType.Green => "MÃO FIRME NO CAMINHO",
            WireType.Yellow => "CORTE RÁPIDO",
            WireType.White => "CAOS TOTAL",
            _ => "",
        };

        static string GetWireControlHint(WireType t) => t switch
        {
            WireType.Red => "APERTE ESPAÇO",
            WireType.Blue => "SETAS DO TECLADO",
            WireType.Green => "ARRASTE COM O MOUSE",
            WireType.Yellow => "CLIQUE NA OPÇÃO",
            WireType.White => "CLIQUE PARA SELECIONAR QUADROS",
            _ => "",
        };

        static bool WireUsesMouse(WireType t) => t == WireType.Green || t == WireType.Yellow || t == WireType.White;

        void ApplyCursorMode(WireType wireType)
        {
            if (PlayerController.Instance == null) return;
            PlayerController.Instance.SetCursorUnlocked(WireUsesMouse(wireType));
        }

        string GetSubsystemName(int index)
        {
            if (index < 0 || index >= SubsystemNames.Length) return "MODULO";
            return SubsystemNames[index];
        }

        void PushFeedMessage(string message, float duration = 1.4f)
        {
            transientFeedMessage = message;
            feedMessageClock = duration;
        }

        void HandleEmergencyAction()
        {
            if (!PhaseUsesInstability() || Keyboard.current == null) return;
            if (!Keyboard.current.qKey.wasPressedThisFrame) return;

            float purgeCost = Mathf.Lerp(2f, 4.5f, Mathf.Clamp01((phase - 3f) / 5f));
            float purgeRelief = Mathf.Lerp(42f, 62f, Mathf.Clamp01((phase - 3f) / 5f));
            TimerController.Instance?.ApplyPenalty(purgeCost);
            instability = Mathf.Max(0f, instability - purgeRelief);
            AudioManager.Instance?.PlayClick();
            CameraShake.Instance?.Impulse(0.12f);
            PushFeedMessage($"PURGA EXECUTADA  -{purgeCost:0.0}s");
        }

        void UpdateInstability()
        {
            if (combinedChallengeStarted)
            {
                instability = 0f;
                return;
            }

            if (!PhaseUsesInstability())
            {
                instability = 0f;
                return;
            }

            if (openSequenceRoutine != null) return;

            float phaseT = Mathf.Clamp01((phase - 3f) / 5f);
            float growth = Mathf.Lerp(6f, 15f, phaseT);
            growth *= 1f + currentIndex * 0.16f;
            if (almostTriggered) growth *= 1.18f;
            if (finalOverrideActive) growth *= 1.45f;

            instability = Mathf.Clamp(instability + growth * Time.unscaledDeltaTime, 0f, InstabilityMax);
            if (instability >= InstabilityMax && overloadRoutine == null)
            {
                overloadRoutine = StartCoroutine(TriggerInstabilityDischarge());
            }
        }

        IEnumerator TriggerInstabilityDischarge()
        {
            TimerController.Instance?.ApplyPenalty(Mathf.Lerp(5f, 8f, Mathf.Clamp01((phase - 3f) / 5f)));
            PushFeedMessage("DESCARGA NO CIRCUITO");

            RectTransform root = activeChallenge != null ? activeChallenge : challengeRoot;
            yield return FailShock(root);

            instability = Mathf.Min(58f, InstabilityMax * 0.58f);
            overloadRoutine = null;

            if (!open || currentIndex >= wires.Count || finalOverrideActive) yield break;

            if (challengeRoutine != null) StopCoroutine(challengeRoutine);
            challengeRoutine = StartCoroutine(RunChallenge(wires[currentIndex]));
        }

        void UpdateSupportUi()
        {
            if (instabilityFill != null)
            {
                instabilityFill.fillAmount = PhaseUsesInstability() ? instability / InstabilityMax : 0f;
                instabilityFill.color = Color.Lerp(new Color(0.2f, 0.95f, 0.35f), new Color(1f, 0.2f, 0.1f), instabilityFill.fillAmount);
            }

            if (utilityText != null)
            {
                if (PhaseUsesInstability())
                {
                    float purgeCost = Mathf.Lerp(2f, 4.5f, Mathf.Clamp01((phase - 3f) / 5f));
                    utilityText.text = $"INSTABILIDADE {(instability / InstabilityMax):P0}\nQ PURGAR  (-{purgeCost:0.0}s)";
                }
                else
                {
                    utilityText.text = "INSTABILIDADE SOB CONTROLE";
                }
            }

            if (feedMessageClock > 0f) feedMessageClock -= Time.unscaledDeltaTime;
            string feed = feedMessageClock > 0f
                ? transientFeedMessage
                : (finalOverrideActive ? "TRAVA FINAL ARMADA" : GetAmbientFeedText());
            if (feedText != null) feedText.text = feed;

            if (subsystemStatusText != null) subsystemStatusText.text = BuildSubsystemStatusText();

            if (vignetteImage != null)
            {
                float danger = PhaseUsesInstability() ? instability / InstabilityMax : 0f;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(3f, 8f, danger));
                vignetteImage.color = Color.Lerp(
                    BaseOverlayColor,
                    new Color(0.1f + 0.2f * pulse, 0.02f, 0.02f, Mathf.Lerp(0.92f, 0.98f, danger)),
                    Mathf.Lerp(0f, 0.55f, danger));
            }
        }

        string GetAmbientFeedText()
        {
            if (PhaseUsesInstability() && instability >= 78f) return "LEITURA CRITICA";
            if (PhaseUsesInstability() && instability >= 48f) return "OSCILACAO NO NUCLEO";
            if (PhaseUsesDiagnosticsNoise()) return "DIAGNOSTICO INTERMITENTE";
            return "CASCAS REMOVIDAS  /  MODULOS EXPOSTOS";
        }

        string BuildSubsystemStatusText()
        {
            System.Text.StringBuilder sb = new();
            int total = Mathf.Min(Mathf.Max(wires.Count, 1), SubsystemNames.Length);
            for (int i = 0; i < total; i++)
            {
                bool solved = i < currentIndex;
                bool revealed = IsWireRevealed(i);
                string label = revealed || solved ? GetSubsystemName(i) : "????";
                string state;
                if (solved) state = "ISOLADO";
                else if (i == currentIndex) state = "EXPOSTO";
                else if (revealed) state = "BLOQUEADO";
                else state = "OCULTO";

                if (PhaseUsesDiagnosticsNoise() && !solved && i > currentIndex && Mathf.Sin(Time.unscaledTime * 8f + i * 1.37f) > 0.72f)
                {
                    state = "OSCILANDO";
                    label = "?SINAL?";
                }

                sb.Append('[').Append(i + 1).Append("] ").Append(label).Append("  ").Append(state);
                if (i < total - 1) sb.Append('\n');
            }

            if (finalOverridePending || finalOverrideActive)
            {
                sb.Append('\n').Append("[F] NUCLEO FINAL  ").Append(finalOverrideActive ? "ARMADO" : "PRESSURIZANDO");
            }
            return sb.ToString();
        }

        void HighlightActiveWire()
        {
            for (int i = 0; i < wires.Count; i++)
            {
                var w = wires[i];
                bool revealed = IsWireRevealed(i);
                if (w.label != null)
                {
                    w.label.text = revealed || w.connected ? GetWireShortName(w.type) : "???";
                    w.label.color = revealed || w.connected ? w.color : new Color(0.16f, 0.24f, 0.2f, 0.8f);
                }

                if (!revealed && !w.connected)
                {
                    if (w.wireGraphic != null) w.wireGraphic.color = new Color(0.04f, 0.08f, 0.06f, 0.45f);
                    if (w.led != null) w.led.color = new Color(0.02f, 0.03f, 0.03f, 1f);
                    if (w.entry != null)
                    {
                        var bg = w.entry.GetComponent<Image>();
                        if (bg != null) bg.color = new Color(0.015f, 0.03f, 0.025f, 0.45f);
                    }
                    continue;
                }

                if (w.connected)
                {
                    if (w.wireGraphic != null) w.wireGraphic.color = new Color(w.color.r, w.color.g, w.color.b, 1f);
                    if (w.led != null) w.led.color = w.color;
                    if (w.entry != null)
                    {
                        var bg = w.entry.GetComponent<Image>();
                        if (bg != null) bg.color = new Color(w.color.r * 0.1f, w.color.g * 0.1f, w.color.b * 0.1f, 0.75f);
                    }
                }
                else if (i == currentIndex)
                {
                    if (w.wireGraphic != null) w.wireGraphic.color = new Color(w.color.r * 0.6f, w.color.g * 0.6f, w.color.b * 0.6f, 1f);
                    if (w.led != null) w.led.color = new Color(w.color.r * 0.3f, w.color.g * 0.3f, w.color.b * 0.3f, 1f);
                    if (w.entry != null)
                    {
                        var bg = w.entry.GetComponent<Image>();
                        if (bg != null) bg.color = new Color(0.08f, 0.12f, 0.1f, 0.85f);
                    }
                }
                else
                {
                    if (w.wireGraphic != null) w.wireGraphic.color = new Color(w.color.r * 0.22f, w.color.g * 0.22f, w.color.b * 0.22f, 0.7f);
                    if (w.led != null) w.led.color = new Color(0.06f, 0.06f, 0.06f, 1f);
                    if (w.entry != null)
                    {
                        var bg = w.entry.GetComponent<Image>();
                        if (bg != null) bg.color = new Color(0.02f, 0.05f, 0.03f, 0.6f);
                    }
                }
            }
        }

        IEnumerator RunChallenge(WireEntry wire)
        {
            switch (wire.type)
            {
                case WireType.Red: yield return RedTimingChallenge(wire); break;
                case WireType.Blue: yield return BlueMemoryChallenge(wire); break;
                case WireType.Green: yield return GreenSteadyHandChallenge(wire); break;
                case WireType.Yellow: yield return YellowDecisionChallenge(wire); break;
                case WireType.White: yield return WhiteChaosChallenge(wire); break;
            }
        }

        IEnumerator RunCombinedBombChallenge()
        {
            finalOverridePending = false;
            finalOverrideActive = false;
            ApplyCursorMode(WireType.Yellow);
            if (panelTitle != null) panelTitle.text = "PAINEL DE DESARME";
            if (panelSubtitle != null) panelSubtitle.text = "SISTEMA SORTEANDO MODULO";
            if (controlsText != null) controlsText.text = "USE O MOUSE   |   ESC FECHA O PAINEL";
            if (subsystemStatusText != null) subsystemStatusText.text = "MODULOS: REACAO / DIAGNOSTICO";
            PushFeedMessage("CIRCUITO ABERTO", 1f);

            yield return new WaitForSecondsRealtime(0.35f);

            CombinedMode mode = Random.value < 0.5f ? CombinedMode.Reaction : CombinedMode.Diagnostics;
            if (mode == CombinedMode.Reaction)
                yield return ReactionModeChallenge();
            else
                yield return DiagnosticsModeChallenge();

            combinedRoutine = null;
        }

        IEnumerator ReactionModeChallenge()
        {
            RectTransform root = CreateChallengePanel("MODO REACAO");
            activeChallenge = root;
            if (panelTitle != null) panelTitle.text = "MODO REACAO";
            if (panelSubtitle != null) panelSubtitle.text = "FIOS COM PULSO";
            if (controlsText != null) controlsText.text = "CLIQUE NO BOTAO NO MOMENTO CERTO";

            ReactionWire[] reactionWires =
            {
                new() { type = WireType.Red, name = "VERMELHO", color = RedCol, interval = 2f },
                new() { type = WireType.Blue, name = "AZUL", color = BlueCol, interval = 1f },
                new() { type = WireType.Green, name = "VERDE", color = GreenCol, interval = 3f },
                new() { type = WireType.Yellow, name = "AMARELO", color = YellowCol, interval = 0.5f }
            };

            ReactionRule rule = (ReactionRule)Random.Range(0, 6);
            Text tip = AddHintText(root, ReactionTip(rule), new Vector2(0f, 250f));
            tip.fontSize = 30;
            tip.color = new Color(1f, 0.92f, 0.45f);

            Image timingFill = CreateBar(root, "TimingPerfect", new Vector2(0f, 196f), new Vector2(760f, 18f), new Color(0.05f, 0.1f, 0.08f), out RectTransform timingFillRT);
            timingFill.type = Image.Type.Filled;
            timingFill.fillMethod = Image.FillMethod.Horizontal;
            timingFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            timingFill.fillAmount = 0f;
            timingFill.color = new Color(0.2f, 1f, 0.45f, 0.95f);
            AddHintText(root, "TIMING PERFEITO", new Vector2(0f, 170f)).fontSize = 18;

            bool solved = false;
            bool clicked = false;
            ReactionWire clickedWire = null;
            float startTime = Time.unscaledTime + Random.Range(0f, 0.35f);

            float startX = -390f;
            for (int i = 0; i < reactionWires.Length; i++)
            {
                ReactionWire rw = reactionWires[i];
                RectTransform card = CreatePanel(root, $"Reaction_{rw.name}", new Vector2(startX + i * 260f, -10f), new Vector2(220f, 300f), new Color(0.025f, 0.035f, 0.032f, 0.94f));
                AddText(card, rw.name, new Vector2(0f, 112f), new Vector2(200f, 34f), 24, rw.color, TextAnchor.MiddleCenter);

                Image glow = CreateImage(card, "Glow", new Vector2(0f, 32f), new Vector2(122f, 122f), rw.color * 0.28f);
                glow.color = new Color(rw.color.r, rw.color.g, rw.color.b, 0.12f);
                rw.glowImage = glow;

                Image wire = CreateImage(card, "Wire", new Vector2(0f, 32f), new Vector2(172f, 28f), rw.color * 0.35f);
                rw.wireImage = wire;

                rw.stateText = AddText(card, "apagado", new Vector2(0f, -30f), new Vector2(190f, 26f), 18, new Color(0.7f, 0.84f, 0.76f), TextAnchor.MiddleCenter);
                Button btn = CreateUiButton(card, "CORTAR ESTE FIO", new Vector2(0f, -104f), new Vector2(184f, 54f), new Color(0.08f, 0.12f, 0.1f), Color.white, 18);
                rw.cutButton = btn;
                btn.onClick.AddListener(() =>
                {
                    clicked = true;
                    clickedWire = rw;
                });
            }

            float challengeTimer = 30f;
            Text timerText = AddText(root, "30.0s", new Vector2(392f, 250f), new Vector2(240f, 36f), 28, new Color(0.75f, 1f, 0.85f), TextAnchor.MiddleRight);

            while (open && !solved && challengeTimer > 0f)
            {
                challengeTimer -= Time.unscaledDeltaTime;
                timerText.text = $"{Mathf.Max(0f, challengeTimer):0.0}s";

                int litCount = 0;
                for (int i = 0; i < reactionWires.Length; i++)
                {
                    ReactionWire rw = reactionWires[i];
                    rw.wasLit = rw.lit;
                    float phaseTime = Mathf.Repeat(Time.unscaledTime - startTime, rw.interval);
                    rw.lit = phaseTime < rw.interval * 0.5f;
                    if (rw.wasLit && !rw.lit) rw.lastOffTime = Time.unscaledTime;
                    if (rw.lit) litCount++;

                    float brightness = rw.lit ? 1f : 0.22f;
                    rw.wireImage.color = new Color(rw.color.r * brightness, rw.color.g * brightness, rw.color.b * brightness, 1f);
                    rw.glowImage.color = new Color(rw.color.r, rw.color.g, rw.color.b, rw.lit ? 0.42f : 0.08f);
                    rw.stateText.text = rw.lit ? "ACESO" : "apagado";
                }

                float quality = ReactionTimingQuality(rule, reactionWires, litCount);
                timingFill.fillAmount = quality;
                timingFill.color = Color.Lerp(new Color(1f, 0.24f, 0.12f), new Color(0.25f, 1f, 0.45f), quality);

                if (clicked)
                {
                    clicked = false;
                    bool correct = clickedWire != null && IsReactionCorrect(rule, clickedWire, reactionWires, litCount);
                    if (correct)
                    {
                        solved = true;
                        TimerController.Instance?.AddTime(5f);
                        PushFeedMessage("DESARMADO! +5 segundos", 1.4f);
                        AudioManager.Instance?.PlaySuccess();
                        yield return ShowVictoryAndNext(root, "DESARMADO! +5s", new Color(0.25f, 1f, 0.45f));
                        yield return CompleteAll();
                        yield break;
                    }

                    TimerController.Instance?.ApplyPenalty(3f);
                    PushFeedMessage("CHOQUE! -3 segundos", 1.2f);
                    yield return FailShock(root);
                }

                yield return null;
            }

            if (open)
                TriggerImmediateGameOver();
        }

        IEnumerator DiagnosticsModeChallenge()
        {
            RectTransform root = CreateChallengePanel("MODO DIAGNOSTICO");
            activeChallenge = root;
            if (panelTitle != null) panelTitle.text = "MODO DIAGNOSTICO";
            if (panelSubtitle != null) panelSubtitle.text = "MULTIMETRO DIGITAL";
            if (controlsText != null) controlsText.text = "SELECIONE 2 FIOS, TESTE, DEPOIS MARQUE OS NEUTROS";
            if (subsystemStatusText != null) subsystemStatusText.text = "DICA: use a logica; 220V significa potenciais diferentes.";

            AddHintText(root, "Use a logica! Teste combinacoes. Corte somente fios neutros.", new Vector2(0f, 254f)).fontSize = 24;

            DiagnosticWire[] diagnosticWires =
            {
                new() { type = WireType.Red, name = "VERMELHO", color = RedCol },
                new() { type = WireType.Blue, name = "AZUL", color = BlueCol },
                new() { type = WireType.Green, name = "VERDE", color = GreenCol },
                new() { type = WireType.Yellow, name = "AMARELO", color = YellowCol },
                new() { type = WireType.White, name = "LARANJA", color = new Color(1f, 0.48f, 0.12f) }
            };

            List<int> pool = new() { 0, 1, 2, 3, 4 };
            int energizedCount = Random.Range(2, 4);
            for (int i = 0; i < energizedCount; i++)
            {
                int pick = Random.Range(0, pool.Count);
                diagnosticWires[pool[pick]].energized = true;
                pool.RemoveAt(pick);
            }

            List<DiagnosticWire> selected = new();
            bool testRequested = false;
            bool cutRequested = false;
            int testsUsed = 0;
            float voltage = 0f;

            RectTransform meter = CreatePanel(root, "Multimeter", new Vector2(0f, 70f), new Vector2(500f, 190f), new Color(0.015f, 0.018f, 0.02f, 0.98f));
            Text display = AddText(meter, "000V", new Vector2(0f, 36f), new Vector2(420f, 74f), 54, new Color(0.36f, 1f, 0.56f), TextAnchor.MiddleCenter);
            Text testsText = AddText(meter, "TESTES 0/3", new Vector2(0f, -46f), new Vector2(420f, 34f), 24, new Color(0.8f, 0.92f, 0.84f), TextAnchor.MiddleCenter);
            Image needle = CreateImage(meter, "AnalogNeedle", new Vector2(0f, -78f), new Vector2(190f, 6f), new Color(1f, 0.25f, 0.12f));
            needle.rectTransform.pivot = new Vector2(0f, 0.5f);
            needle.rectTransform.anchoredPosition = new Vector2(-95f, -78f);

            Button testButton = CreateUiButton(root, "TESTAR PAR", new Vector2(-150f, -40f), new Vector2(210f, 54f), new Color(0.08f, 0.14f, 0.12f), Color.white, 22);
            testButton.onClick.AddListener(() => testRequested = true);
            Button cutButton = CreateUiButton(root, "CORTAR MARCADOS", new Vector2(150f, -40f), new Vector2(230f, 54f), new Color(0.18f, 0.07f, 0.06f), Color.white, 22);
            cutButton.onClick.AddListener(() => cutRequested = true);

            float startX = -420f;
            for (int i = 0; i < diagnosticWires.Length; i++)
            {
                DiagnosticWire dw = diagnosticWires[i];
                RectTransform plate = CreatePanel(root, $"Diag_{dw.name}", new Vector2(startX + i * 210f, -202f), new Vector2(178f, 156f), new Color(0.025f, 0.035f, 0.034f, 0.95f));
                dw.plateImage = plate.GetComponent<Image>();
                CreateImage(plate, "Wire", new Vector2(0f, 30f), new Vector2(138f, 22f), dw.color);
                dw.labelText = AddText(plate, dw.name, new Vector2(0f, 66f), new Vector2(160f, 28f), 18, dw.color, TextAnchor.MiddleCenter);

                Button selectButton = CreateUiButton(plate, "SELEC", new Vector2(0f, -16f), new Vector2(136f, 38f), new Color(0.06f, 0.1f, 0.09f), Color.white, 16);
                dw.selectButton = selectButton;
                selectButton.onClick.AddListener(() =>
                {
                    if (selected.Contains(dw))
                        selected.Remove(dw);
                    else if (selected.Count < 2)
                        selected.Add(dw);
                });

                Button markButton = CreateUiButton(plate, "CORTAR?", new Vector2(0f, -60f), new Vector2(136f, 34f), new Color(0.11f, 0.07f, 0.06f), Color.white, 15);
                markButton.onClick.AddListener(() => dw.marked = !dw.marked);
            }

            Text log = AddText(root, "Selecione dois fios e use TESTAR PAR.", new Vector2(0f, -106f), new Vector2(900f, 40f), 20, new Color(0.8f, 0.94f, 0.84f), TextAnchor.MiddleCenter);
            Text timerText = AddText(root, "30.0s", new Vector2(392f, 254f), new Vector2(240f, 36f), 28, new Color(0.75f, 1f, 0.85f), TextAnchor.MiddleRight);

            float challengeTimer = 30f;
            while (open && challengeTimer > 0f)
            {
                challengeTimer -= Time.unscaledDeltaTime;
                timerText.text = $"{Mathf.Max(0f, challengeTimer):0.0}s";

                for (int i = 0; i < diagnosticWires.Length; i++)
                {
                    DiagnosticWire dw = diagnosticWires[i];
                    bool isSelected = selected.Contains(dw);
                    Color baseColor = isSelected ? new Color(0.08f, 0.18f, 0.14f, 1f) : new Color(0.025f, 0.035f, 0.034f, 0.95f);
                    if (dw.marked) baseColor = new Color(0.18f, 0.08f, 0.05f, 0.96f);
                    dw.plateImage.color = baseColor;
                    dw.labelText.text = dw.marked ? $"{dw.name}\nMARCADO" : dw.name;
                }

                if (testRequested)
                {
                    testRequested = false;
                    if (selected.Count != 2)
                    {
                        log.text = "Escolha exatamente DOIS fios.";
                        AudioManager.Instance?.PlayAlert();
                    }
                    else if (testsUsed >= 3)
                    {
                        log.text = "Sem testes restantes. Decida os cortes.";
                        AudioManager.Instance?.PlayAlert();
                    }
                    else
                    {
                        testsUsed++;
                        voltage = selected[0].energized == selected[1].energized ? 0f : 220f;
                        display.text = $"{voltage:000}V";
                        testsText.text = $"TESTES {testsUsed}/3";
                        log.text = $"{selected[0].name} <-> {selected[1].name}: {voltage:0}V";
                        AudioManager.Instance?.PlayClick();
                        selected.Clear();
                    }
                }

                float targetAngle = Mathf.Lerp(12f, -92f, Mathf.InverseLerp(0f, 300f, voltage));
                needle.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpAngle(needle.rectTransform.localEulerAngles.z, targetAngle, Time.unscaledDeltaTime * 8f));

                if (cutRequested)
                {
                    cutRequested = false;
                    bool cutAnyEnergized = false;
                    bool allNeutralMarked = true;
                    for (int i = 0; i < diagnosticWires.Length; i++)
                    {
                        if (diagnosticWires[i].marked && diagnosticWires[i].energized) cutAnyEnergized = true;
                        if (!diagnosticWires[i].energized && !diagnosticWires[i].marked) allNeutralMarked = false;
                    }

                    if (cutAnyEnergized)
                    {
                        log.text = "FIO ENERGIZADO CORTADO. FALHA CRITICA.";
                        AudioManager.Instance?.PlayShock();
                        CameraShake.Instance?.Impulse(0.75f);
                        yield return new WaitForSecondsRealtime(0.25f);
                        TriggerImmediateGameOver();
                        yield break;
                    }

                    if (allNeutralMarked)
                    {
                        PushFeedMessage("DIAGNOSTICO CORRETO", 1.4f);
                        AudioManager.Instance?.PlaySuccess();
                        yield return ShowVictoryAndNext(root, "NEUTROS ISOLADOS", new Color(0.25f, 1f, 0.45f));
                        yield return CompleteAll();
                        yield break;
                    }

                    log.text = "Ainda ha neutros sem marcar.";
                    yield return FailShock(root);
                }

                yield return null;
            }

            if (open)
                TriggerImmediateGameOver();
        }

        static string ReactionTip(ReactionRule rule) => rule switch
        {
            ReactionRule.WithRed => "Corte o fio quando ele ACENDER junto com o VERMELHO",
            ReactionRule.NeverAlone => "Corte um fio quando ele NAO estiver piscando sozinho",
            ReactionRule.AfterGreenOff => "Corte o fio 0.5 segundos depois que o VERDE apagar",
            ReactionRule.WithBlue => "Corte o fio que pisca na mesma hora que o AZUL",
            ReactionRule.Fastest => "Corte o fio que pisca MAIS RAPIDO entre todos",
            ReactionRule.Slowest => "Corte o fio que pisca MAIS DEVAGAR entre todos",
            _ => "Corte no momento correto",
        };

        static bool IsReactionCorrect(ReactionRule rule, ReactionWire clicked, ReactionWire[] wires, int litCount)
        {
            ReactionWire red = FindReactionWire(wires, WireType.Red);
            ReactionWire blue = FindReactionWire(wires, WireType.Blue);
            ReactionWire green = FindReactionWire(wires, WireType.Green);
            ReactionWire yellow = FindReactionWire(wires, WireType.Yellow);

            return rule switch
            {
                ReactionRule.WithRed => clicked != red && clicked.lit && red != null && red.lit,
                ReactionRule.NeverAlone => clicked.lit && litCount > 1,
                ReactionRule.AfterGreenOff => green != null && !green.lit && Time.unscaledTime - green.lastOffTime >= 0.42f && Time.unscaledTime - green.lastOffTime <= 0.68f,
                ReactionRule.WithBlue => clicked != blue && clicked.lit && blue != null && blue.lit,
                ReactionRule.Fastest => clicked == yellow && clicked.lit,
                ReactionRule.Slowest => clicked == green && clicked.lit,
                _ => false,
            };
        }

        static float ReactionTimingQuality(ReactionRule rule, ReactionWire[] wires, int litCount)
        {
            float best = 0f;
            for (int i = 0; i < wires.Length; i++)
            {
                if (IsReactionCorrect(rule, wires[i], wires, litCount))
                    best = 1f;
            }

            if (rule == ReactionRule.AfterGreenOff)
            {
                ReactionWire green = FindReactionWire(wires, WireType.Green);
                if (green != null && !green.lit)
                {
                    float delta = Mathf.Abs((Time.unscaledTime - green.lastOffTime) - 0.5f);
                    best = Mathf.Max(best, 1f - Mathf.Clamp01(delta / 0.5f));
                }
            }

            return best;
        }

        static ReactionWire FindReactionWire(ReactionWire[] wires, WireType type)
        {
            for (int i = 0; i < wires.Length; i++)
                if (wires[i].type == type) return wires[i];
            return null;
        }

        IEnumerator ShowVictoryAndNext(RectTransform root, string message, Color color)
        {
            RectTransform panel = CreatePanel(root, "VictoryOverlay", Vector2.zero, new Vector2(560f, 210f), new Color(0.02f, 0.08f, 0.045f, 0.98f));
            AddText(panel, message, new Vector2(0f, 44f), new Vector2(520f, 52f), 34, color, TextAnchor.MiddleCenter);
            AddText(panel, "Sistema estabilizado. Proximo desafio liberado.", new Vector2(0f, 0f), new Vector2(520f, 34f), 18, new Color(0.8f, 1f, 0.88f), TextAnchor.MiddleCenter);

            bool next = false;
            Button button = CreateUiButton(panel, "PROXIMO DESAFIO", new Vector2(0f, -66f), new Vector2(260f, 48f), new Color(0.08f, 0.16f, 0.11f), Color.white, 18);
            button.onClick.AddListener(() => next = true);

            while (open && !next)
                yield return null;
        }

        RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = SceneBuildHelpers.GetWhiteSprite();
            image.color = color;
            return rt;
        }

        Image CreateImage(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = SceneBuildHelpers.GetWhiteSprite();
            image.color = color;
            return image;
        }

        Text AddText(RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            Text t = go.GetComponent<Text>();
            t.text = text;
            t.font = HudController.GetMonoFont();
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        Button CreateUiButton(RectTransform parent, string label, Vector2 anchoredPosition, Vector2 size, Color bgColor, Color textColor, int fontSize)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = SceneBuildHelpers.GetWhiteSprite();
            image.color = bgColor;

            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform textRT = (RectTransform)textGO.transform;
            textRT.SetParent(rt, false);
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            Text t = textGO.GetComponent<Text>();
            t.text = label;
            t.font = HudController.GetMonoFont();
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = textColor;

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 1f, 0.88f, 1f);
            colors.pressedColor = new Color(0.55f, 1f, 0.68f, 1f);
            button.colors = colors;
            return button;
        }

        Image CreateBar(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color bgColor, out RectTransform fillRect)
        {
            RectTransform bg = CreatePanel(parent, name, anchoredPosition, size, bgColor);
            Image fill = CreateImage(bg, "Fill", Vector2.zero, size, Color.white);
            fillRect = fill.rectTransform;
            return fill;
        }

        void TriggerImmediateGameOver()
        {
            Camera cam = PlayerController.Instance != null ? PlayerController.Instance.playerCamera : Camera.main;
            Vector3 bombPos = BombManager.Instance != null ? BombManager.Instance.GetBombPosition() : Vector3.zero;
            Close(restoreControl: false);
            GameManager.Instance?.TriggerGameOver(bombPos, cam);
        }

        IEnumerator RedTimingChallenge(WireEntry wire)
        {
            RectTransform root = CreateChallengePanel("ENCAIXE NA ZONA VERDE");
            activeChallenge = root;

            float trackWidth = 60f;
            float trackHeight = 420f;

            GameObject trackGO = new GameObject("Track", typeof(RectTransform), typeof(Image));
            RectTransform trackRT = (RectTransform)trackGO.transform;
            trackRT.SetParent(root, false);
            trackRT.anchorMin = trackRT.anchorMax = new Vector2(0.5f, 0.5f);
            trackRT.pivot = new Vector2(0.5f, 0.5f);
            trackRT.anchoredPosition = Vector2.zero;
            trackRT.sizeDelta = new Vector2(trackWidth, trackHeight);
            Image trackImg = trackGO.GetComponent<Image>();
            trackImg.sprite = SceneBuildHelpers.GetWhiteSprite();
            trackImg.color = new Color(0.04f, 0.06f, 0.08f, 0.95f);

            float zoneHeight = Mathf.Lerp(120f, 40f, Mathf.Clamp01((phase - 1) / 7f));
            GameObject zoneGO = new GameObject("GreenZone", typeof(RectTransform), typeof(Image));
            RectTransform zoneRT = (RectTransform)zoneGO.transform;
            zoneRT.SetParent(trackRT, false);
            zoneRT.anchorMin = zoneRT.anchorMax = new Vector2(0.5f, 0.5f);
            zoneRT.pivot = new Vector2(0.5f, 0.5f);
            zoneRT.sizeDelta = new Vector2(trackWidth, zoneHeight);
            Image zoneImg = zoneGO.GetComponent<Image>();
            zoneImg.sprite = SceneBuildHelpers.GetWhiteSprite();
            zoneImg.color = new Color(0.15f, 0.9f, 0.35f, 0.7f);

            GameObject indicatorGO = new GameObject("Indicator", typeof(RectTransform), typeof(Image));
            RectTransform indRT = (RectTransform)indicatorGO.transform;
            indRT.SetParent(trackRT, false);
            indRT.anchorMin = indRT.anchorMax = new Vector2(0.5f, 0.5f);
            indRT.pivot = new Vector2(0.5f, 0.5f);
            indRT.sizeDelta = new Vector2(trackWidth + 18f, 14f);
            Image indImg = indicatorGO.GetComponent<Image>();
            indImg.sprite = SceneBuildHelpers.GetWhiteSprite();
            indImg.color = new Color(1f, 0.95f, 0.6f, 1f);

            Text hint = AddHintText(root, "APERTE ESPAÇO", new Vector2(0f, -240f));

            float t = 0f;
            float speed = Mathf.Lerp(1.4f, 3.2f, Mathf.Clamp01((phase - 1) / 8f));
            bool moveZone = phase >= 5;
            float zonePhase = 0f;

            while (open && challengeRoutine != null)
            {
                t += Time.unscaledDeltaTime * speed;
                float y = Mathf.Sin(t) * (trackHeight * 0.5f - 10f);
                indRT.anchoredPosition = new Vector2(0f, y);

                if (moveZone)
                {
                    zonePhase += Time.unscaledDeltaTime * 0.8f;
                    float zy = Mathf.Sin(zonePhase) * (trackHeight * 0.5f - zoneHeight * 0.5f - 10f);
                    zoneRT.anchoredPosition = new Vector2(0f, zy);
                }

                bool triggerPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

                if (triggerPressed)
                {
                    float dist = Mathf.Abs(y - zoneRT.anchoredPosition.y);
                    if (dist < zoneHeight * 0.5f)
                    {
                        yield return FlashSuccess(zoneImg, new Color(0.4f, 1.4f, 0.5f));
                        OnWireConnected();
                        yield break;
                    }

                    yield return FailShock(root);
                    t = 0f;
                }
                yield return null;
            }
        }

        IEnumerator BlueMemoryChallenge(WireEntry wire)
        {
            RectTransform root = CreateChallengePanel("MEMORIZE A SEQUÊNCIA");
            activeChallenge = root;

            int length = phase >= 5 ? 5 : 4;
            string[] arrowGlyphs = { "↑", "↓", "←", "→" };
            Key[] arrowKeys = { Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow };

            while (open && challengeRoutine != null)
            {
                int[] sequence = new int[length];
                for (int i = 0; i < length; i++) sequence[i] = Random.Range(0, 4);

                List<Text> glyphTexts = new();
                for (int i = 0; i < length; i++)
                {
                    GameObject glyphGO = new GameObject($"Glyph_{i}", typeof(RectTransform), typeof(Text));
                    RectTransform gRT = (RectTransform)glyphGO.transform;
                    gRT.SetParent(root, false);
                    gRT.anchorMin = gRT.anchorMax = new Vector2(0.5f, 0.5f);
                    gRT.pivot = new Vector2(0.5f, 0.5f);
                    float spacing = 120f;
                    float startX = -(length - 1) * spacing * 0.5f;
                    gRT.anchoredPosition = new Vector2(startX + i * spacing, 40f);
                    gRT.sizeDelta = new Vector2(100f, 120f);
                    Text g = glyphGO.GetComponent<Text>();
                    g.text = arrowGlyphs[sequence[i]];
                    g.font = HudController.GetMonoFont();
                    g.fontSize = 96;
                    g.alignment = TextAnchor.MiddleCenter;
                    g.color = new Color(0.3f, 0.6f, 1.2f);
                    glyphTexts.Add(g);
                }

                Text hint = AddHintText(root, "MEMORIZE...", new Vector2(0f, -220f));
                yield return new WaitForSecondsRealtime(2f);

                foreach (var g in glyphTexts) g.text = "?";
                hint.text = "REPITA COM AS SETAS";

                int pos = 0;
                bool fail = false;
                while (pos < length && open && challengeRoutine != null)
                {
                    if (Keyboard.current == null) { yield return null; continue; }

                    int detected = -1;
                    for (int k = 0; k < 4; k++)
                    {
                        if (Keyboard.current[arrowKeys[k]].wasPressedThisFrame) { detected = k; break; }
                    }

                    if (detected >= 0)
                    {
                        if (detected == sequence[pos])
                        {
                            glyphTexts[pos].text = arrowGlyphs[detected];
                            glyphTexts[pos].color = new Color(0.4f, 1.2f, 0.6f);
                            AudioManager.Instance?.PlayConnect();
                            pos++;
                        }
                        else
                        {
                            fail = true;
                            break;
                        }
                    }
                    yield return null;
                }

                if (!open || challengeRoutine == null) yield break;

                if (!fail)
                {
                    yield return new WaitForSecondsRealtime(0.2f);
                    OnWireConnected();
                    yield break;
                }

                yield return FailShock(root);
                foreach (var g in glyphTexts) if (g != null) Destroy(g.gameObject);
                if (hint != null) Destroy(hint.gameObject);
            }
        }

        IEnumerator GreenSteadyHandChallenge(WireEntry wire)
        {
            RectTransform root = CreateChallengePanel("ATÉ O PONTO B SEM TOCAR AS BORDAS");
            activeChallenge = root;

            List<Vector2> path = BuildPath(phase);
            float halfWidth = Mathf.Lerp(28f, 12f, Mathf.Clamp01((phase - 1) / 8f));

            GameObject pathGO = new GameObject("PathContainer", typeof(RectTransform));
            RectTransform pathRT = (RectTransform)pathGO.transform;
            pathRT.SetParent(root, false);
            pathRT.anchorMin = pathRT.anchorMax = new Vector2(0.5f, 0.5f);
            pathRT.pivot = new Vector2(0.5f, 0.5f);
            pathRT.sizeDelta = new Vector2(800f, 420f);
            pathRT.anchoredPosition = Vector2.zero;

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 a = path[i];
                Vector2 b = path[i + 1];
                Vector2 mid = (a + b) * 0.5f;
                Vector2 diff = b - a;
                float len = diff.magnitude;
                float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                GameObject seg = new GameObject($"Seg_{i}", typeof(RectTransform), typeof(Image));
                RectTransform segRT = (RectTransform)seg.transform;
                segRT.SetParent(pathRT, false);
                segRT.anchorMin = segRT.anchorMax = new Vector2(0.5f, 0.5f);
                segRT.pivot = new Vector2(0.5f, 0.5f);
                segRT.anchoredPosition = mid;
                segRT.sizeDelta = new Vector2(len + halfWidth * 2f, halfWidth * 2f);
                segRT.localEulerAngles = new Vector3(0f, 0f, angle);
                Image im = seg.GetComponent<Image>();
                im.sprite = SceneBuildHelpers.GetWhiteSprite();
                im.color = new Color(0.1f, 0.25f, 0.15f, 0.9f);
            }

            Vector2 startLocal = path[0];
            Vector2 endLocal = path[^1];
            CreateDot(pathRT, startLocal, "StartDot", new Color(1f, 1f, 0.6f));
            CreateDot(pathRT, endLocal, "EndDot", new Color(0.4f, 1.3f, 0.5f));

            AddHintText(root, "ARRASTE DO A AO B", new Vector2(0f, -240f));

            bool dragging = false;
            bool nearEndTensionPlayed = false;

            Vector2 lastLocal = Vector2.zero;
            bool hasLast = false;

            while (open && challengeRoutine != null)
            {
                if (Mouse.current == null) { yield return null; continue; }
                Vector2 mouseScreen = Mouse.current.position.ReadValue();

                bool ok = ScreenToLocal(pathRT, mouseScreen, out Vector2 local);
                if (!ok) { yield return null; continue; }

                if (!dragging)
                {
                    if (Mouse.current.leftButton.wasPressedThisFrame && Vector2.Distance(local, startLocal) < 26f)
                    {
                        dragging = true;
                        lastLocal = local;
                        hasLast = true;
                    }
                    yield return null;
                    continue;
                }

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    dragging = false;
                    hasLast = false;
                    AudioManager.Instance?.StopTensionRising();
                    tensionCuePlaying = false;
                    yield return FailShock(root);
                    continue;
                }

                Vector2 sample = hasLast ? lastLocal : local;
                int steps = 6;
                bool offPath = false;
                for (int k = 1; k <= steps; k++)
                {
                    Vector2 s = Vector2.Lerp(sample, local, k / (float)steps);
                    if (!PointOnPath(path, s, halfWidth)) { offPath = true; break; }
                }
                lastLocal = local;
                hasLast = true;

                if (offPath)
                {
                    dragging = false;
                    hasLast = false;
                    AudioManager.Instance?.StopTensionRising();
                    tensionCuePlaying = false;
                    yield return FailShock(root);
                    continue;
                }

                float distToEnd = Vector2.Distance(local, endLocal);
                if (distToEnd < 80f && !nearEndTensionPlayed && !tensionCuePlaying)
                {
                    AudioManager.Instance?.StartTensionRising();
                    tensionCuePlaying = true;
                    nearEndTensionPlayed = true;
                }
                if (distToEnd < 18f)
                {
                    AudioManager.Instance?.StopTensionRising();
                    tensionCuePlaying = false;
                    OnWireConnected();
                    yield break;
                }

                yield return null;
            }
        }

        List<Vector2> BuildPath(int p)
        {
            List<Vector2> pts = new();
            float width = 700f;
            float height = 330f;

            if (p < 5)
            {
                pts.Add(new Vector2(-width * 0.48f, -height * 0.3f));
                pts.Add(new Vector2(-width * 0.15f, height * 0.3f));
                pts.Add(new Vector2(width * 0.15f, -height * 0.3f));
                pts.Add(new Vector2(width * 0.48f, height * 0.3f));
            }
            else
            {
                pts.Add(new Vector2(-width * 0.48f, -height * 0.35f));
                pts.Add(new Vector2(-width * 0.3f, height * 0.4f));
                pts.Add(new Vector2(-width * 0.1f, -height * 0.3f));
                pts.Add(new Vector2(width * 0.05f, height * 0.3f));
                pts.Add(new Vector2(width * 0.25f, -height * 0.35f));
                pts.Add(new Vector2(width * 0.48f, height * 0.35f));
            }
            return pts;
        }

        void CreateDot(RectTransform parent, Vector2 localPos, string name, Color c)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = localPos;
            rt.sizeDelta = new Vector2(28f, 28f);
            Image img = go.GetComponent<Image>();
            img.sprite = SceneBuildHelpers.GetWhiteSprite();
            img.color = c;
        }

        static bool ScreenToLocal(RectTransform rect, Vector2 screenPos, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, null, out local);
        }

        static bool PointOnPath(List<Vector2> path, Vector2 point, float halfWidth)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (DistanceToSegment(path[i], path[i + 1], point) < halfWidth) return true;
            }
            return false;
        }

        static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float denom = Vector2.Dot(ab, ab);
            if (denom < 0.0001f) return Vector2.Distance(a, p);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / denom);
            Vector2 closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }

        IEnumerator YellowDecisionChallenge(WireEntry wire)
        {
            RectTransform root = CreateChallengePanel("CORTE O FIO:");
            activeChallenge = root;

            bool three = phase >= 5;
            int buttonCount = three ? 3 : 2;
            float window = three ? 2f : 3f;

            Color[] optionColors;
            string[] optionLabels;
            if (three)
            {
                optionColors = new[] { new Color(1f, 0.25f, 0.25f), new Color(0.3f, 0.6f, 1f), new Color(0.3f, 1f, 0.45f) };
                optionLabels = new[] { "VER", "AZU", "VRD" };
            }
            else
            {
                optionColors = new[] { new Color(1f, 0.25f, 0.25f), new Color(0.3f, 0.6f, 1f) };
                optionLabels = new[] { "VER", "AZU" };
            }
            int correctIdx = Random.Range(0, buttonCount);

            GameObject qGO = new GameObject("Question", typeof(RectTransform), typeof(Text));
            RectTransform qRT = (RectTransform)qGO.transform;
            qRT.SetParent(root, false);
            qRT.anchorMin = qRT.anchorMax = new Vector2(0.5f, 0.5f);
            qRT.pivot = new Vector2(0.5f, 0.5f);
            qRT.anchoredPosition = new Vector2(0f, 100f);
            qRT.sizeDelta = new Vector2(900f, 80f);
            Text qT = qGO.GetComponent<Text>();
            qT.text = $"CORTE O FIO {optionLabels[correctIdx]}";
            qT.font = HudController.GetMonoFont();
            qT.fontSize = 56;
            qT.alignment = TextAnchor.MiddleCenter;
            qT.color = new Color(1f, 0.9f, 0.4f);

            RectTransform[] buttons = new RectTransform[buttonCount];
            Text[] buttonLabels = new Text[buttonCount];
            float spacing = 220f;
            float startX = -(buttonCount - 1) * spacing * 0.5f;

            for (int i = 0; i < buttonCount; i++)
            {
                GameObject bGO = new GameObject($"Btn_{i}", typeof(RectTransform), typeof(Image));
                RectTransform bRT = (RectTransform)bGO.transform;
                bRT.SetParent(root, false);
                bRT.anchorMin = bRT.anchorMax = new Vector2(0.5f, 0.5f);
                bRT.pivot = new Vector2(0.5f, 0.5f);
                bRT.anchoredPosition = new Vector2(startX + i * spacing, -40f);
                bRT.sizeDelta = new Vector2(180f, 90f);
                Image bImg = bGO.GetComponent<Image>();
                bImg.sprite = SceneBuildHelpers.GetWhiteSprite();
                bImg.color = optionColors[i] * 0.5f;
                buttons[i] = bRT;

                GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                RectTransform lRT = (RectTransform)labelGO.transform;
                lRT.SetParent(bRT, false);
                lRT.anchorMin = Vector2.zero;
                lRT.anchorMax = Vector2.one;
                lRT.offsetMin = lRT.offsetMax = Vector2.zero;
                Text lT = labelGO.GetComponent<Text>();
                lT.text = optionLabels[i];
                lT.font = HudController.GetMonoFont();
                lT.fontSize = 48;
                lT.alignment = TextAnchor.MiddleCenter;
                lT.color = Color.white;
                buttonLabels[i] = lT;
            }

            GameObject timerGO = new GameObject("ChallengeTimer", typeof(RectTransform), typeof(Image));
            RectTransform tRT = (RectTransform)timerGO.transform;
            tRT.SetParent(root, false);
            tRT.anchorMin = tRT.anchorMax = new Vector2(0.5f, 0.5f);
            tRT.pivot = new Vector2(0.5f, 0.5f);
            tRT.anchoredPosition = new Vector2(0f, -140f);
            tRT.sizeDelta = new Vector2(500f, 14f);
            Image tBar = timerGO.GetComponent<Image>();
            tBar.sprite = SceneBuildHelpers.GetWhiteSprite();
            tBar.color = new Color(1f, 0.85f, 0.25f);

            float elapsed = 0f;
            int chosen = -1;

            while (open && challengeRoutine != null && elapsed < window && chosen < 0)
            {
                elapsed += Time.unscaledDeltaTime;
                float left = 1f - elapsed / window;
                tRT.sizeDelta = new Vector2(500f * left, 14f);
                tBar.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(1f, 0.85f, 0.25f), left);

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector2 mp = Mouse.current.position.ReadValue();
                    for (int i = 0; i < buttonCount; i++)
                    {
                        if (RectTransformUtility.RectangleContainsScreenPoint(buttons[i], mp))
                        {
                            chosen = i;
                            break;
                        }
                    }
                }

                yield return null;
            }

            if (!open || challengeRoutine == null) yield break;

            if (chosen == correctIdx)
            {
                OnWireConnected();
            }
            else
            {
                TimerController.Instance?.ApplyPenalty(8f);
                UIFlasher flasher = FindAnyObjectByType<UIFlasher>();
                if (flasher != null) flasher.FlashRed(0.45f);
                AudioManager.Instance?.PlayShock();
                CameraShake.Instance?.Impulse(0.5f);
                yield return new WaitForSecondsRealtime(0.4f);
                if (!open || challengeRoutine == null) yield break;
                yield return RunChallenge(wire);
            }
        }

        IEnumerator WhiteChaosChallenge(WireEntry wire)
        {
            RectTransform root = CreateChallengePanel("CAOS TOTAL — RESOLVA TODOS");
            activeChallenge = root;

            RectTransform[] quads = new RectTransform[4];
            Text[] quadLabels = new Text[4];
            bool[] quadSolved = new bool[4];
            float[] quadTimers = new float[4];
            string[] quadNames = { "VER", "AZU", "VRD", "AMA" };

            Vector2[] positions =
            {
                new(-340f, 170f),
                new(340f, 170f),
                new(-340f, -170f),
                new(340f, -170f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject qGO = new GameObject($"Quad_{i}", typeof(RectTransform), typeof(Image));
                RectTransform qRT = (RectTransform)qGO.transform;
                qRT.SetParent(root, false);
                qRT.anchorMin = qRT.anchorMax = new Vector2(0.5f, 0.5f);
                qRT.pivot = new Vector2(0.5f, 0.5f);
                qRT.anchoredPosition = positions[i];
                qRT.sizeDelta = new Vector2(540f, 260f);
                Image qImg = qGO.GetComponent<Image>();
                qImg.sprite = SceneBuildHelpers.GetWhiteSprite();
                qImg.color = new Color(0.03f, 0.05f, 0.08f, 0.9f);
                quads[i] = qRT;

                GameObject lGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                RectTransform lRT = (RectTransform)lGO.transform;
                lRT.SetParent(qRT, false);
                lRT.anchorMin = lRT.anchorMax = new Vector2(0.5f, 1f);
                lRT.pivot = new Vector2(0.5f, 1f);
                lRT.anchoredPosition = new Vector2(0f, -10f);
                lRT.sizeDelta = new Vector2(400f, 40f);
                Text l = lGO.GetComponent<Text>();
                l.text = $"FIO {quadNames[i]}";
                l.font = HudController.GetMonoFont();
                l.fontSize = 28;
                l.alignment = TextAnchor.MiddleCenter;
                l.color = i == 0 ? RedCol : i == 1 ? BlueCol : i == 2 ? GreenCol : YellowCol;
                quadLabels[i] = l;

                quadTimers[i] = 8f;
            }

            int active = 0;
            HighlightQuad(quads, active);

            float redBarT = 0f;
            float redSpeed = 3f;
            float redZoneY = 0f;
            float redZoneH = 60f;

            int[] blueSeq = { Random.Range(0, 4), Random.Range(0, 4), Random.Range(0, 4), Random.Range(0, 4) };
            int bluePos = 0;
            bool blueShowing = true;
            float blueShowT = 0f;

            List<Vector2> greenPath = new()
            {
                new Vector2(-200f, -40f), new Vector2(-60f, 40f), new Vector2(80f, -40f), new Vector2(200f, 40f)
            };
            bool greenDragging = false;
            float greenHalfWidth = 20f;

            int yellowCorrect = Random.Range(0, 2);
            Text yellowQ = null;

            float topLeftX = positions[0].x - 270f;
            float topLeftY = positions[0].y + 130f;

            while (open && challengeRoutine != null)
            {
                bool allSolved = true;
                for (int i = 0; i < 4; i++) if (!quadSolved[i]) allSolved = false;
                if (allSolved) break;

                for (int i = 0; i < 4; i++)
                {
                    if (quadSolved[i]) continue;
                    quadTimers[i] -= Time.unscaledDeltaTime;
                    if (quadTimers[i] <= 0f)
                    {
                        TimerController.Instance?.ApplyPenalty(10f);
                        UIFlasher flasher = FindAnyObjectByType<UIFlasher>();
                        if (flasher != null) flasher.FlashRed(0.35f);
                        AudioManager.Instance?.PlayShock();
                        CameraShake.Instance?.Impulse(0.4f);
                        quadTimers[i] = 8f;
                    }
                }

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector2 mp = Mouse.current.position.ReadValue();
                    for (int i = 0; i < 4; i++)
                    {
                        if (!quadSolved[i] && RectTransformUtility.RectangleContainsScreenPoint(quads[i], mp))
                        {
                            if (i != active)
                            {
                                active = i;
                                HighlightQuad(quads, active);
                            }
                        }
                    }
                }

                if (active == 0 && !quadSolved[0])
                {
                    redBarT += Time.unscaledDeltaTime * redSpeed;
                    float y = Mathf.Sin(redBarT) * 90f;
                    if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                    {
                        if (Mathf.Abs(y - redZoneY) < redZoneH * 0.5f)
                        {
                            quadSolved[0] = true;
                            quadLabels[0].color = new Color(0.4f, 1.3f, 0.5f);
                            AudioManager.Instance?.PlayConnect();
                        }
                        else
                        {
                            AudioManager.Instance?.PlayShock();
                        }
                    }
                }

                if (active == 1 && !quadSolved[1])
                {
                    blueShowT += Time.unscaledDeltaTime;
                    if (blueShowing && blueShowT > 2f)
                    {
                        blueShowing = false;
                    }
                    if (!blueShowing && Keyboard.current != null)
                    {
                        Key[] arrowKeys = { Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow };
                        for (int k = 0; k < 4; k++)
                        {
                            if (Keyboard.current[arrowKeys[k]].wasPressedThisFrame)
                            {
                                if (k == blueSeq[bluePos])
                                {
                                    bluePos++;
                                    if (bluePos >= blueSeq.Length)
                                    {
                                        quadSolved[1] = true;
                                        quadLabels[1].color = new Color(0.4f, 1.3f, 0.5f);
                                        AudioManager.Instance?.PlayConnect();
                                    }
                                }
                                else
                                {
                                    bluePos = 0;
                                    blueShowing = true;
                                    blueShowT = 0f;
                                    blueSeq = new[] { Random.Range(0, 4), Random.Range(0, 4), Random.Range(0, 4), Random.Range(0, 4) };
                                    AudioManager.Instance?.PlayShock();
                                }
                            }
                        }
                    }
                }

                if (active == 2 && !quadSolved[2])
                {
                    if (Mouse.current != null)
                    {
                        Vector2 mp = Mouse.current.position.ReadValue();
                        bool ok = ScreenToLocal(quads[2], mp, out Vector2 local);
                        Vector2 local2D = local;
                        Vector2 startPt = greenPath[0];
                        Vector2 endPt = greenPath[^1];

                        if (Mouse.current.leftButton.wasPressedThisFrame && ok && Vector2.Distance(local2D, startPt) < 26f)
                            greenDragging = true;

                        if (greenDragging && ok)
                        {
                            if (Mouse.current.leftButton.wasReleasedThisFrame)
                            {
                                greenDragging = false;
                                AudioManager.Instance?.PlayShock();
                            }
                            else if (!PointOnPath(greenPath, local2D, greenHalfWidth))
                            {
                                greenDragging = false;
                                AudioManager.Instance?.PlayShock();
                            }
                            else if (Vector2.Distance(local2D, endPt) < 20f)
                            {
                                quadSolved[2] = true;
                                quadLabels[2].color = new Color(0.4f, 1.3f, 0.5f);
                                AudioManager.Instance?.PlayConnect();
                            }
                        }
                    }
                }

                if (active == 3 && !quadSolved[3])
                {
                    if (yellowQ == null)
                    {
                        GameObject yqGO = new GameObject("YellowQ", typeof(RectTransform), typeof(Text));
                        RectTransform yqRT = (RectTransform)yqGO.transform;
                        yqRT.SetParent(quads[3], false);
                        yqRT.anchorMin = yqRT.anchorMax = new Vector2(0.5f, 0.5f);
                        yqRT.pivot = new Vector2(0.5f, 0.5f);
                        yqRT.sizeDelta = new Vector2(500f, 180f);
                        yellowQ = yqGO.GetComponent<Text>();
                        yellowQ.font = HudController.GetMonoFont();
                        yellowQ.fontSize = 30;
                        yellowQ.alignment = TextAnchor.MiddleCenter;
                        yellowQ.color = YellowCol;
                        yellowQ.text = $"CORTE\n{(yellowCorrect == 0 ? "VERMELHO" : "AZUL")}\n(V/A)";
                    }
                    if (Keyboard.current != null)
                    {
                        bool chose = false;
                        int choice = -1;
                        if (Keyboard.current.vKey.wasPressedThisFrame) { chose = true; choice = 0; }
                        if (Keyboard.current.aKey.wasPressedThisFrame) { chose = true; choice = 1; }
                        if (chose)
                        {
                            if (choice == yellowCorrect)
                            {
                                quadSolved[3] = true;
                                quadLabels[3].color = new Color(0.4f, 1.3f, 0.5f);
                                AudioManager.Instance?.PlayConnect();
                            }
                            else
                            {
                                AudioManager.Instance?.PlayShock();
                                yellowCorrect = Random.Range(0, 2);
                                yellowQ.text = $"CORTE\n{(yellowCorrect == 0 ? "VERMELHO" : "AZUL")}\n(V/A)";
                            }
                        }
                    }
                }

                yield return null;
            }

            OnWireConnected();
        }

        static void HighlightQuad(RectTransform[] quads, int active)
        {
            for (int i = 0; i < quads.Length; i++)
            {
                var img = quads[i].GetComponent<Image>();
                if (img == null) continue;
                img.color = i == active ? new Color(0.08f, 0.18f, 0.12f, 0.95f) : new Color(0.03f, 0.05f, 0.08f, 0.9f);
            }
        }

        RectTransform CreateChallengePanel(string title)
        {
            ClearChallenge();
            if (challengeRoot == null) return null;
            GameObject go = new GameObject("Challenge", typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(challengeRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(Text));
            RectTransform tRT = (RectTransform)titleGO.transform;
            tRT.SetParent(rt, false);
            tRT.anchorMin = new Vector2(0f, 1f);
            tRT.anchorMax = new Vector2(1f, 1f);
            tRT.pivot = new Vector2(0.5f, 1f);
            tRT.anchoredPosition = new Vector2(0f, -16f);
            tRT.sizeDelta = new Vector2(0f, 50f);
            Text t = titleGO.GetComponent<Text>();
            t.text = title;
            t.font = HudController.GetMonoFont();
            t.fontSize = 32;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.7f, 1.1f, 0.85f);

            return rt;
        }

        Text AddHintText(RectTransform parent, string text, Vector2 anchored)
        {
            GameObject go = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(700f, 60f);
            Text t = go.GetComponent<Text>();
            t.text = text;
            t.font = HudController.GetMonoFont();
            t.fontSize = 28;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.85f, 1f, 0.95f);
            return t;
        }

        IEnumerator FlashSuccess(Image target, Color bright)
        {
            if (target == null) yield break;
            Color orig = target.color;
            target.color = bright;
            yield return new WaitForSecondsRealtime(0.1f);
            target.color = orig;
        }

        IEnumerator FailShock(RectTransform root)
        {
            AudioManager.Instance?.PlayShock();
            UIFlasher flasher = FindAnyObjectByType<UIFlasher>();
            if (flasher != null) flasher.FlashRed(0.35f);
            CameraShake.Instance?.Impulse(0.35f);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        void OnWireConnected()
        {
            if (currentIndex >= wires.Count) return;
            WireEntry w = wires[currentIndex];
            string solvedSubsystem = GetSubsystemName(Mathf.Min(currentIndex, SubsystemNames.Length - 1));
            w.connected = true;
            AudioManager.Instance?.PlayConnect();
            AudioManager.Instance?.StopTensionRising();
            tensionCuePlaying = false;
            instability = Mathf.Max(0f, instability - Mathf.Lerp(16f, 28f, Mathf.Clamp01((phase - 1f) / 7f)));
            TimerController.Instance?.PauseCountdown(0.55f);
            PushFeedMessage($"{solvedSubsystem} ISOLADO");

            currentIndex++;
            HighlightActiveWire();
            ClearChallenge();
            UpdateSupportUi();

            if (currentIndex >= wires.Count)
            {
                challengeRoutine = StartCoroutine(finalOverridePending ? FinalOverrideSequence() : CompleteAll());
            }
            else
            {
                StartCurrentWireChallenge();
            }
        }

        IEnumerator FinalOverrideSequence()
        {
            finalOverridePending = false;
            finalOverrideActive = true;
            ApplyCursorMode(WireType.Red);

            if (panelTitle != null) panelTitle.text = "TRAVA FINAL";
            if (panelSubtitle != null) panelSubtitle.text = "ALINHE O TAMBOR DO NUCLEO";
            if (controlsText != null) controlsText.text = "A/D OU SETAS   |   ESC FECHA O PAINEL";
            PushFeedMessage("FALSO ALIVIO  /  NUCLEO REARMADO", 1.8f);
            AudioManager.Instance?.PlayAlert();

            yield return new WaitForSecondsRealtime(0.75f);
            yield return MasterOverrideChallenge();

            if (!open) yield break;

            finalOverrideActive = false;
            PushFeedMessage("OVERRIDE ACEITO", 1.2f);
            yield return new WaitForSecondsRealtime(0.45f);
            yield return CompleteAll();
        }

        IEnumerator MasterOverrideChallenge()
        {
            RectTransform root = CreateChallengePanel("MASTER OVERRIDE");
            activeChallenge = root;

            GameObject trackGO = new GameObject("OverrideTrack", typeof(RectTransform), typeof(Image));
            RectTransform trackRT = (RectTransform)trackGO.transform;
            trackRT.SetParent(root, false);
            trackRT.anchorMin = trackRT.anchorMax = new Vector2(0.5f, 0.5f);
            trackRT.pivot = new Vector2(0.5f, 0.5f);
            trackRT.anchoredPosition = new Vector2(0f, 16f);
            trackRT.sizeDelta = new Vector2(560f, 48f);
            Image trackImg = trackGO.GetComponent<Image>();
            trackImg.sprite = SceneBuildHelpers.GetWhiteSprite();
            trackImg.color = new Color(0.08f, 0.1f, 0.12f, 0.95f);

            float zoneWidth = Mathf.Lerp(180f, 90f, Mathf.Clamp01((phase - 4f) / 4f));
            GameObject zoneGO = new GameObject("Zone", typeof(RectTransform), typeof(Image));
            RectTransform zoneRT = (RectTransform)zoneGO.transform;
            zoneRT.SetParent(trackRT, false);
            zoneRT.anchorMin = zoneRT.anchorMax = new Vector2(0.5f, 0.5f);
            zoneRT.pivot = new Vector2(0.5f, 0.5f);
            zoneRT.sizeDelta = new Vector2(zoneWidth, 48f);
            Image zoneImg = zoneGO.GetComponent<Image>();
            zoneImg.sprite = SceneBuildHelpers.GetWhiteSprite();
            zoneImg.color = new Color(0.18f, 0.85f, 0.35f, 0.7f);

            GameObject indicatorGO = new GameObject("Indicator", typeof(RectTransform), typeof(Image));
            RectTransform indRT = (RectTransform)indicatorGO.transform;
            indRT.SetParent(trackRT, false);
            indRT.anchorMin = indRT.anchorMax = new Vector2(0.5f, 0.5f);
            indRT.pivot = new Vector2(0.5f, 0.5f);
            indRT.sizeDelta = new Vector2(34f, 70f);
            Image indImg = indicatorGO.GetComponent<Image>();
            indImg.sprite = SceneBuildHelpers.GetWhiteSprite();
            indImg.color = new Color(1f, 0.95f, 0.7f, 1f);

            GameObject progBgGO = new GameObject("ProgressBG", typeof(RectTransform), typeof(Image));
            RectTransform progBgRT = (RectTransform)progBgGO.transform;
            progBgRT.SetParent(root, false);
            progBgRT.anchorMin = progBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            progBgRT.pivot = new Vector2(0.5f, 0.5f);
            progBgRT.anchoredPosition = new Vector2(0f, -92f);
            progBgRT.sizeDelta = new Vector2(420f, 14f);
            Image progBg = progBgGO.GetComponent<Image>();
            progBg.sprite = SceneBuildHelpers.GetWhiteSprite();
            progBg.color = new Color(0.06f, 0.08f, 0.1f, 0.9f);

            GameObject progFillGO = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            RectTransform progFillRT = (RectTransform)progFillGO.transform;
            progFillRT.SetParent(progBgRT, false);
            progFillRT.anchorMin = new Vector2(0f, 0f);
            progFillRT.anchorMax = new Vector2(1f, 1f);
            progFillRT.offsetMin = progFillRT.offsetMax = Vector2.zero;
            Image progFill = progFillGO.GetComponent<Image>();
            progFill.sprite = SceneBuildHelpers.GetWhiteSprite();
            progFill.type = Image.Type.Filled;
            progFill.fillMethod = Image.FillMethod.Horizontal;
            progFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progFill.fillAmount = 0f;
            progFill.color = new Color(0.95f, 0.25f, 0.12f, 1f);

            AddHintText(root, "SEGURE O INDICADOR NA JANELA VERDE", new Vector2(0f, -164f));

            float travel = trackRT.sizeDelta.x * 0.5f - 28f;
            float indicatorX = Random.Range(-travel * 0.6f, travel * 0.6f);
            float velocity = 0f;
            float holdProgress = 0f;
            float holdTarget = Mathf.Lerp(1.4f, 2.8f, Mathf.Clamp01((phase - 4f) / 4f));

            while (open && challengeRoutine != null)
            {
                float input = 0f;
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input -= 1f;
                    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input += 1f;
                }

                float zoneX = 0f;
                if (phase >= 7)
                {
                    zoneX = Mathf.Sin(Time.unscaledTime * 1.1f) * (travel - zoneWidth * 0.5f - 8f);
                }

                velocity += input * 980f * Time.unscaledDeltaTime;
                velocity += Mathf.Sin(Time.unscaledTime * (0.9f + phase * 0.14f)) * 420f * Time.unscaledDeltaTime;
                velocity *= 0.92f;
                indicatorX = Mathf.Clamp(indicatorX + velocity * Time.unscaledDeltaTime, -travel, travel);

                zoneRT.anchoredPosition = new Vector2(zoneX, 0f);
                indRT.anchoredPosition = new Vector2(indicatorX, 0f);

                bool inside = Mathf.Abs(indicatorX - zoneX) <= zoneWidth * 0.5f - 12f;
                holdProgress = inside
                    ? Mathf.Min(holdTarget, holdProgress + Time.unscaledDeltaTime)
                    : Mathf.Max(0f, holdProgress - Time.unscaledDeltaTime * 1.35f);

                progFill.fillAmount = holdProgress / holdTarget;
                zoneImg.color = inside ? new Color(0.26f, 1f, 0.45f, 0.82f) : new Color(0.18f, 0.85f, 0.35f, 0.7f);

                if (holdProgress >= holdTarget)
                {
                    yield return FlashSuccess(zoneImg, new Color(0.5f, 1.4f, 0.55f));
                    yield break;
                }

                yield return null;
            }
        }

        IEnumerator CompleteAll()
        {
            TimerController.Instance?.Stop();
            AudioManager.Instance?.StopTicking();
            AudioManager.Instance?.StopHiss();
            AudioManager.Instance?.StopElectricHiss();

            yield return new WaitForSecondsRealtime(1.5f);

            AudioManager.Instance?.PlaySuccess();
            AudioManager.Instance?.PlayRelief();

            BombManager bomb = BombManager.Instance;
            float remaining = TimerController.Instance?.Remaining ?? 0f;

            if (bomb != null)
            {
                Close(restoreControl: false);
                bomb.OnDefused();
            }
            else
            {
                Close();
            }

            HudController hud = FindAnyObjectByType<HudController>();
            if (hud != null) hud.ShowPhaseComplete(remaining);

            yield return new WaitForSecondsRealtime(2f);

            GameManager.Instance?.AdvancePhase(remaining);
        }
    }
}
