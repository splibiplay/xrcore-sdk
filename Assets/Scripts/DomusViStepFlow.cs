using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DomusViStepFlow : MonoBehaviour
{
    [System.Serializable]
    private struct InstructionAudioEntry
    {
        public string key;
        public AudioClip clip;
    }

    [Header("Refs")]
    [SerializeField] private RoutineEngine routineEngine;
    [SerializeField] private DomusViStepUI stepUI;

    [Header("Manual fallback")]
    [SerializeField] private GameObject btnHecho;
    [SerializeField, Range(4f, 5f)] private float showDoneAfterSeconds = 4.5f;   // tiempo antes de mostrar HECHO en pasos IA
    [SerializeField] private bool doneAlwaysVisibleOnManualSteps = true;

    [Header("UX Timing")]
    [SerializeField] private float minStepVisibleSeconds = 1.0f;
    [SerializeField] private float validatedFeedbackSeconds = 0.8f;

    [Header("Heuristic - In hand")]
    [SerializeField] private float inHandMovementFromStartMin = 0.04f;
    [SerializeField] private float inHandAreaGrowthFromStartMin = 0.002f;
    [SerializeField] private float inHandMovementFromLastMin = 0.012f;
    [SerializeField] private int requiredStableTicks = 1;

    [Header("Heuristic - Placed")]
    [SerializeField] private float placedMaxMovement = 0.03f;
    [SerializeField] private float placedMaxAreaDelta = 0.004f;
    [SerializeField] private int placedRequiredStableTicks = 2;

    [Header("Heuristic - Mesa layout")]
    [SerializeField] private float mesaForwardMin = 0.20f;
    [SerializeField] private float mesaForwardMax = 0.90f;
    [SerializeField] private float mesaCenterTolerance = 0.38f;
    [SerializeField] private float mesaSideMin = 0.06f;
    [SerializeField] private float mesaSideMax = 0.45f;
    [SerializeField] private float mesaAlongTolerance = 0.25f;
    [SerializeField] private float mesaGlassForwardMin = 0.08f;
    [SerializeField] private float mesaGlassForwardMax = 0.55f;
    [SerializeField] private float mesaGlassSideTolerance = 0.35f;
    [SerializeField] private float mesaNapkinSideMin = 0.10f;
    [SerializeField] private float mesaNapkinSideMax = 0.60f;
    [SerializeField] private int mesaLayoutRequiredTicks = 2;

    [Header("Reminder steps")]
    [SerializeField] private float reminderSeconds = 3.0f;
    [SerializeField] private float applyPastaSeconds = 4.0f;

    [Header("Audio guidance")]
    [SerializeField] private AudioSource instructionAudioSource;
    [SerializeField] private AudioSource feedbackAudioSource;
    [SerializeField] private List<InstructionAudioEntry> instructionAudioEntries = new();
    [SerializeField] private List<AudioClip> stepCompletedAudioClips = new();
    [SerializeField] private AudioClip routineCompletedAudioClip;
    [SerializeField] private List<AudioClip> wrongObjectAudioClips = new();
    [SerializeField] private List<AudioClip> wrongPlacementAudioClips = new();
    [SerializeField] private bool blockUntilInstructionAudioFinishes = true;
    [SerializeField, Min(0f)] private float postInstructionDelaySeconds = 0.2f;
    [SerializeField, Min(0f)] private float negativeFeedbackCooldownSeconds = 1.5f;
    [SerializeField, Min(0f)] private float negativeFeedbackStartDelaySeconds = 1.0f;
    [SerializeField, Min(0f)] private float negativeFeedbackConfirmSeconds = 0.8f;
    [SerializeField, Min(0f)] private float routineCompletedDelaySeconds = 2.0f;

    [Header("Debug")]
    [SerializeField] private bool log = true;
    [SerializeField] private TMPro.TMP_Text txtHeuristicDebug;

    [System.Serializable]
    public struct StepMetrics
    {
        public int index;
        public string instruction;
        public string label;
        public string type;
        public float actionStartTime;
        public float validationTime;
        public float actionDuration;
        public bool completedViaDone;
        public int correctiveMessagesPlayed;
    }

    [System.Serializable]
    public struct RoutineMetrics
    {
        public string routineName;
        public float routineStartTime;
        public float routineEndTime;
        public float totalDuration;
        public string timestampIsoUtc;
        public List<StepMetrics> steps;
        public int totalSteps;
        public int totalStepsCompletedViaDone;
        public int totalCorrectiveMessages;
    }

    private List<StepDef> _steps;
    private int _stepIndex = -1;
    private bool _running;

    private float _stepShownTime;
    private bool _canValidate;
    private bool _showingValidatedFeedback;
    private float _validatedUntil;
    private bool _doneVisible;
    private bool _instructionReady;
    private float _instructionFinishedTime;
    private bool _actionPhaseStarted;
    private float _actionPhaseStartTime;
    private AudioClip _lastCompletedFeedbackClip;
    private AudioClip _lastWrongObjectFeedbackClip;
    private AudioClip _lastWrongPlacementFeedbackClip;
    private float _lastNegativeFeedbackTime = -999f;
    private string _activeNegativeReason = "";
    private float _activeNegativeReasonSince = -999f;
    private bool _isCompletingRoutine;
    private readonly HashSet<string> _sessionDetectedLabels = new();

    public event System.Action OnRoutineCompleted;
    public event System.Action<RoutineMetrics> OnRoutineMetricsReady;

    private readonly Dictionary<string, Rect> _lastBbox = new();
    private readonly Dictionary<string, Rect> _startInHandBbox = new();
    private readonly Dictionary<string, int> _movementTicks = new();
    private readonly Dictionary<string, Rect> _lastPlacedBbox = new();
    private readonly Dictionary<string, int> _placedStableTicks = new();
    private readonly Dictionary<MesaLayoutRule, int> _mesaRuleTicks = new();
    private readonly Dictionary<string, Rect> _stepStartBboxByLabel = new();
    private readonly Dictionary<string, int> _wrongObjectTicks = new();
    private float _routineStartTime;
    private readonly List<StepMetrics> _metricsSteps = new();
    private bool _currentStepCompletedViaDone;
    private int _currentStepCorrectiveMessages;

    private enum ActiveRoutine { None, HigieneBucal, PonerMesa }
    private ActiveRoutine _activeRoutine = ActiveRoutine.None;

    private enum StepType { InHand, Placed, TimedReminder, TableLayout, OpenBrush, None }
    private enum MesaLayoutRule
    {
        None,
        PlateFrontOfUser,
        ForkBesidePlate,
        KnifeBesidePlate,
        KnifeRightOfUser,
        SpoonBesidePlate,
        SpoonRightOfUser,
        GlassFrontOfCutlery,
        NapkinSideOfPlate,
        ForkLeftOfUser,
        GlassFrontOfUser,
        NapkinSideOfUser
    }

    private struct StepDef
    {
        public StepType type;
        public string instruction;
        public string label; // normalized o raw según tipo
        public float waitSeconds;
        public MesaLayoutRule mesaRule;
        public string instructionAudioKey;

        public static StepDef InHand(string instruction, string normalizedLabel, string audioKey)
            => new StepDef { type = StepType.InHand, instruction = instruction, label = normalizedLabel, waitSeconds = 0f, mesaRule = MesaLayoutRule.None, instructionAudioKey = audioKey };

        public static StepDef Placed(string instruction, string normalizedLabel, string audioKey)
            => new StepDef { type = StepType.Placed, instruction = instruction, label = normalizedLabel, waitSeconds = 0f, mesaRule = MesaLayoutRule.None, instructionAudioKey = audioKey };

        public static StepDef TimedReminder(string instruction, float waitSeconds, string audioKey)
            => new StepDef { type = StepType.TimedReminder, instruction = instruction, label = "", waitSeconds = waitSeconds, mesaRule = MesaLayoutRule.None, instructionAudioKey = audioKey };

        public static StepDef TableLayout(string instruction, string normalizedLabel, MesaLayoutRule rule, string audioKey)
            => new StepDef { type = StepType.TableLayout, instruction = instruction, label = normalizedLabel, waitSeconds = 0f, mesaRule = rule, instructionAudioKey = audioKey };

        public static StepDef OpenBrush(string instruction, string audioKey)
            => new StepDef { type = StepType.OpenBrush, instruction = instruction, label = "", waitSeconds = 0f, mesaRule = MesaLayoutRule.None, instructionAudioKey = audioKey };

        public static StepDef None(string instruction, string audioKey)
            => new StepDef { type = StepType.None, instruction = instruction, label = "", waitSeconds = 0f, mesaRule = MesaLayoutRule.None, instructionAudioKey = audioKey };
    }

    private void Awake()
    {
        showDoneAfterSeconds = Mathf.Clamp(showDoneAfterSeconds, 4f, 5f);

        if (routineEngine == null) routineEngine = FindObjectOfType<RoutineEngine>(true);
        if (stepUI == null)
            stepUI = FindObjectOfType<DomusViStepUI>(true); // incluye inactivos

        Debug.Log($"[StepFlow] stepUI found = {(stepUI != null ? stepUI.name : "NULL")}");

        if (stepUI == null)
            Debug.LogError("[StepFlow] No encuentro DomusViStepUI en escena (ni activo ni inactivo).");
        if (routineEngine == null)
            Debug.LogError("[StepFlow] No encuentro RoutineEngine en escena.");
    }

    public void StartHigieneBucal()
    {
        _activeRoutine = ActiveRoutine.HigieneBucal;
        _sessionDetectedLabels.Clear();
        _steps = BuildHigieneBucalSteps();

        _running = true;
        _stepIndex = 0;
        _routineStartTime = Time.time;
        _metricsSteps.Clear();
        RenderStep();
        if (log) Debug.Log("[StepFlow] Started HigieneBucal");
    }

    public void StartHigieneBucal_MVP(HashSet<string> detectedLabelsAtStart = null)
    {
        if (_running)
        {
            Debug.LogWarning("[StepFlow] Start ignored (already running).");
            return;
        }

        SetSessionDetectedLabels(detectedLabelsAtStart);
        StopAllCoroutines();
        StartCoroutine(StartHigieneRoutine_Co());
    }

    public void StartPonerMesa_MVP(RoutineVersion version = RoutineVersion.Completa, HashSet<string> detectedLabelsAtStart = null)
    {
        if (_running)
        {
            Debug.LogWarning("[StepFlow] Start ignored (already running).");
            return;
        }

        SetSessionDetectedLabels(detectedLabelsAtStart);
        StopAllCoroutines();
        StartCoroutine(StartMesaRoutine_Co(version));
    }

    private System.Collections.IEnumerator StartHigieneRoutine_Co()
    {
        // esperar a que el panel esté activo (máx 1s)
        float t0 = Time.time;

        while (stepUI != null && !stepUI.gameObject.activeInHierarchy && Time.time - t0 < 1.0f)
            yield return null;

        if (stepUI == null)
            stepUI = FindObjectOfType<DomusViStepUI>(true);

        if (stepUI == null || !stepUI.gameObject.activeInHierarchy)
        {
            Debug.LogError("[StepFlow] StepUI not active. Abort start.");
            yield break;
        }

        _activeRoutine = ActiveRoutine.HigieneBucal;
        _steps = BuildHigieneBucalSteps();

        if (btnHecho != null) btnHecho.SetActive(false);
        _doneVisible = false;

        if (_running) yield break;

        _running = true;
        _stepIndex = 0;
        _routineStartTime = Time.time;
        _metricsSteps.Clear();
        RenderStep();
        Debug.Log("[StepFlow] Started HigieneBucal MVP");
    }

    private System.Collections.IEnumerator StartMesaRoutine_Co(RoutineVersion version)
    {
        float t0 = Time.time;
        while (stepUI != null && !stepUI.gameObject.activeInHierarchy && Time.time - t0 < 1.0f)
            yield return null;

        if (stepUI == null)
            stepUI = FindObjectOfType<DomusViStepUI>(true);

        if (stepUI == null || !stepUI.gameObject.activeInHierarchy)
        {
            Debug.LogError("[StepFlow] StepUI not active. Abort mesa start.");
            yield break;
        }

        _activeRoutine = ActiveRoutine.PonerMesa;
        _steps = BuildPonerMesaSteps(version);

        if (btnHecho != null) btnHecho.SetActive(false);
        _doneVisible = false;

        if (_running) yield break;

        _running = true;
        _stepIndex = 0;
        _routineStartTime = Time.time;
        _metricsSteps.Clear();
        RenderStep();
        Debug.Log($"[StepFlow] Started PonerMesa MVP ({version})");
    }

    public void Stop()
    {
        _running = false;
        _stepIndex = -1;
        _activeRoutine = ActiveRoutine.None;
        _sessionDetectedLabels.Clear();
        _isCompletingRoutine = false;
        if (btnHecho != null) btnHecho.SetActive(false);
        _doneVisible = false;
        if (log) Debug.Log("[StepFlow] Stopped");
    }

    public void OnDonePressed()
    {
        if (!_running) return;
        Debug.Log("[StepFlow] Done button pressed");
        _currentStepCompletedViaDone = true;
        MarkValidatedAndAdvanceSoon();
    }

    public void Tick()
    {
        if (!_running || _steps == null || _stepIndex < 0 || _stepIndex >= _steps.Count) return;

        var step = _steps[_stepIndex];

        // Primero esperamos a que termine el audio del paso.
        if (!EnsureStepInstructionReady())
        {
            stepUI?.SetStatus("Escucha la indicación y después haz el paso.");
            return;
        }

        // habilitar validación tras mínimo tiempo visible
        if (!_canValidate && Time.time - _actionPhaseStartTime >= minStepVisibleSeconds)
            _canValidate = true;

        // Si es paso IA validable, muestra HECHO tras X segundos sin validar
        bool canUseDoneFallback = step.type == StepType.InHand || step.type == StepType.Placed || step.type == StepType.TableLayout || step.type == StepType.OpenBrush;

        if (canUseDoneFallback && !_doneVisible)
        {
            if (Time.time - _actionPhaseStartTime >= showDoneAfterSeconds)
            {
                _doneVisible = true;
                if (btnHecho != null) btnHecho.SetActive(true);
                stepUI?.SetStatus("Si ya lo has hecho, pulsa HECHO.");
            }
        }

        // si estamos mostrando feedback, esperamos y avanzamos
        if (_showingValidatedFeedback)
        {
            if (Time.time >= _validatedUntil)
            {
                _showingValidatedFeedback = false;
                Next();
            }
            return;
        }

        if (step.type == StepType.InHand)
        {
            // Aún no validar si acaba de mostrarse el paso
            if (!_canValidate)
            {
                stepUI?.SetStatus("Cuando estés listo, acércalo hacia ti.");
                return;
            }

            if (IsObjectHandled(step.label))
            {
                stepUI?.SetStatus("Perfecto ✅");
                MarkValidatedAndAdvanceSoon();
            }
            else
            {
                if (IsWrongObjectMoving(step.label))
                {
                    stepUI?.SetStatus("Ese no es el objeto que necesitas.");
                    RequestNegativeFeedback("wrong_object", GetRandomWrongObjectFeedbackClip());
                }
                else
                {
                    ResetNegativeFeedbackTracking();
                    stepUI?.SetStatus("Muévelo o acércalo un poco.");
                }
            }
        }
        else if (step.type == StepType.TimedReminder)
        {
            ResetNegativeFeedbackTracking();
            float elapsed = Time.time - _actionPhaseStartTime;
            if (elapsed >= step.waitSeconds)
            {
                stepUI?.SetStatus("Perfecto ✅");
                MarkValidatedAndAdvanceSoon();
                return;
            }

            float remaining = Mathf.Max(0f, step.waitSeconds - elapsed);
            stepUI?.SetStatus($"Revisa este paso ({remaining:0}s).");
            return;
        }
        else if (step.type == StepType.Placed)
        {
            ResetNegativeFeedbackTracking();
            if (!_canValidate)
            {
                stepUI?.SetStatus("Déjalo en la mesa o lavabo.");
                return;
            }

            if (IsObjectPlaced(step.label))
            {
                stepUI?.SetStatus("Perfecto ✅");
                MarkValidatedAndAdvanceSoon();
            }
            else
            {
                stepUI?.SetStatus("Déjalo quieto en la mesa o lavabo.");
            }
        }
        else if (step.type == StepType.TableLayout)
        {
            if (!_canValidate)
            {
                stepUI?.SetStatus("Colócalo con calma en su posición.");
                return;
            }

            if (IsMesaLayoutRuleSatisfied(step.mesaRule, out string mesaHint))
            {
                stepUI?.SetStatus("Perfecto ✅");
                MarkValidatedAndAdvanceSoon();
            }
            else
            {
                if (HasObjectMovedFromStepStart(step.label, out _, out _))
                {
                    stepUI?.SetStatus("Ese objeto no va ahí.");
                    RequestNegativeFeedback("wrong_placement", GetRandomWrongPlacementFeedbackClip());
                }
                else if (IsWrongObjectMoving(step.label))
                {
                    stepUI?.SetStatus("Ese no es el objeto que necesitas.");
                    RequestNegativeFeedback("wrong_object", GetRandomWrongObjectFeedbackClip());
                }
                else
                {
                    ResetNegativeFeedbackTracking();
                    stepUI?.SetStatus(mesaHint);
                }
            }
        }
        else if (step.type == StepType.OpenBrush)
        {
            ResetNegativeFeedbackTracking();
            if (!_canValidate)
            {
                stepUI?.SetStatus("Cuando estés listo, abre la tapa.");
                return;
            }

            // Caso 1: ya está abierto -> validamos
            if (IsRawLabelPresent("cepillo_abierto", 0.25f))
            {
                stepUI?.SetStatus("Hecho ✅");
                MarkValidatedAndAdvanceSoon();
                return;
            }

            // Caso 2: lo vemos cerrado -> guiar
            if (IsRawLabelPresent("cepillo_cerrado", 0.25f))
            {
                stepUI?.SetStatus("Está cerrado. Abre la tapa.");
                return;
            }

            // Caso 3: no lo vemos (ocluido / fuera de cámara)
            stepUI?.SetStatus("Acerca el cepillo para verlo bien.");
            return;
        }
        else
        {
            ResetNegativeFeedbackTracking();
            // Paso sin validación aún
            stepUI?.SetStatus("Pulsa Pausa si lo necesitas.");
        }
    }

    private void MarkValidatedAndAdvanceSoon()
    {
        RecordCurrentStepMetrics();
        float feedbackDuration = validatedFeedbackSeconds;
        var clip = GetRandomCompletedFeedbackClip();
        if (feedbackAudioSource != null && clip != null)
        {
            feedbackAudioSource.PlayOneShot(clip);
            feedbackDuration = Mathf.Max(feedbackDuration, clip.length);
        }
        ResetNegativeFeedbackTracking();
        _showingValidatedFeedback = true;
        _validatedUntil = Time.time + feedbackDuration;
    }

    private bool IsObjectHandled(string label)
    {
        if (routineEngine == null || routineEngine.Stabilizer == null)
            return false;

        label = RoutineCatalog.NormalizeLabel(label);

        if (!routineEngine.Stabilizer.TryGetLastBbox(label, out var bbox))
            return false;

        if (!_lastBbox.TryGetValue(label, out var last))
        {
            _lastBbox[label] = bbox;
            _startInHandBbox[label] = bbox;
            _movementTicks[label] = 0;
            return false;
        }

        if (!_startInHandBbox.TryGetValue(label, out var start))
        {
            start = last;
            _startInHandBbox[label] = start;
        }

        float area = bbox.width * bbox.height;
        float lastArea = last.width * last.height;
        float startArea = start.width * start.height;
        float areaGrowth = area - lastArea;
        float areaGrowthFromStart = area - startArea;

        Vector2 c = new Vector2(bbox.x + bbox.width * 0.5f, bbox.y + bbox.height * 0.5f);
        Vector2 cLast = new Vector2(last.x + last.width * 0.5f, last.y + last.height * 0.5f);
        Vector2 cStart = new Vector2(start.x + start.width * 0.5f, start.y + start.height * 0.5f);
        float movementFromLast = Vector2.Distance(c, cLast);
        float movementFromStart = Vector2.Distance(c, cStart);

        bool significantChange =
            areaGrowthFromStart >= inHandAreaGrowthFromStartMin ||
            movementFromStart >= inHandMovementFromStartMin ||
            movementFromLast >= inHandMovementFromLastMin;

        int ticks = _movementTicks.TryGetValue(label, out var currentTicks) ? currentTicks : 0;
        if (txtHeuristicDebug != null)
            txtHeuristicDebug.text =
                $"pick:{label}\nmoveStart={movementFromStart:0.000}\nmoveLast={movementFromLast:0.000}\n" +
                $"dAreaStep={areaGrowthFromStart:0.000}\ndAreaLast={areaGrowth:0.000}\n" +
                $"ticks={ticks}/{Mathf.Max(1, requiredStableTicks)}";

        if (significantChange)
        {
            if (!_movementTicks.ContainsKey(label))
                _movementTicks[label] = 0;

            _movementTicks[label]++;

            if (_movementTicks[label] >= Mathf.Max(1, requiredStableTicks))
                return true;
        }
        else
        {
            _movementTicks[label] = 0;
        }

        _lastBbox[label] = bbox;
        return false;
    }

    private bool IsObjectPlaced(string label)
    {
        if (routineEngine == null || routineEngine.Stabilizer == null)
            return false;

        label = RoutineCatalog.NormalizeLabel(label);
        if (!routineEngine.Stabilizer.TryGetLastBbox(label, out var bbox))
            return false;

        if (!_lastPlacedBbox.TryGetValue(label, out var last))
        {
            _lastPlacedBbox[label] = bbox;
            _placedStableTicks[label] = 0;
            return false;
        }

        float area = bbox.width * bbox.height;
        float lastArea = last.width * last.height;
        float areaDelta = Mathf.Abs(area - lastArea);

        Vector2 c = new Vector2(bbox.x + bbox.width * 0.5f, bbox.y + bbox.height * 0.5f);
        Vector2 cLast = new Vector2(last.x + last.width * 0.5f, last.y + last.height * 0.5f);
        float movement = Vector2.Distance(c, cLast);

        bool stable = movement <= placedMaxMovement && areaDelta <= placedMaxAreaDelta;
        if (stable)
        {
            if (!_placedStableTicks.ContainsKey(label))
                _placedStableTicks[label] = 0;
            _placedStableTicks[label]++;
        }
        else
        {
            _placedStableTicks[label] = 0;
        }

        if (txtHeuristicDebug != null)
            txtHeuristicDebug.text =
                $"place:{label}\nmove={movement:0.000}\ndArea={areaDelta:0.000}\n" +
                $"ticks={_placedStableTicks[label]}/{Mathf.Max(1, placedRequiredStableTicks)}";

        _lastPlacedBbox[label] = bbox;
        return _placedStableTicks[label] >= Mathf.Max(1, placedRequiredStableTicks);
    }

    private bool IsMesaLayoutRuleSatisfied(MesaLayoutRule rule, out string hint)
    {
        hint = "Colócalo en su posición.";
        if (!TryGetUserPose2D(out var userPos, out var userForward, out var userRight))
        {
            hint = "Mira hacia la mesa para continuar.";
            return false;
        }

        bool instant = rule switch
        {
            MesaLayoutRule.PlateFrontOfUser => IsPlateFrontOfUser(userPos, userForward, userRight),
            MesaLayoutRule.ForkBesidePlate => IsObjectBesidePlate("tenedor", userForward, userRight),
            MesaLayoutRule.KnifeBesidePlate => IsObjectBesidePlate("cuchillo", userForward, userRight),
            MesaLayoutRule.KnifeRightOfUser => IsObjectAtSideOfUser("cuchillo", userPos, userForward, userRight, leftSide: false),
            MesaLayoutRule.SpoonBesidePlate => IsObjectBesidePlate("cuchara", userForward, userRight),
            MesaLayoutRule.SpoonRightOfUser => IsObjectAtSideOfUser("cuchara", userPos, userForward, userRight, leftSide: false),
            MesaLayoutRule.GlassFrontOfCutlery => IsGlassFrontOfCutlery(userForward, userRight),
            MesaLayoutRule.NapkinSideOfPlate => IsNapkinSideOfPlate(userForward, userRight),
            MesaLayoutRule.ForkLeftOfUser => IsObjectAtSideOfUser("tenedor", userPos, userForward, userRight, leftSide: true),
            MesaLayoutRule.GlassFrontOfUser => IsObjectFrontOfUser("vaso", userPos, userForward, userRight),
            MesaLayoutRule.NapkinSideOfUser => IsObjectAtSideOfUser("servilleta", userPos, userForward, userRight, leftSide: true),
            _ => false
        };

        int ticks = _mesaRuleTicks.TryGetValue(rule, out var v) ? v : 0;
        if (instant)
        {
            ticks++;
        }
        else
        {
            ticks = 0;
        }
        _mesaRuleTicks[rule] = ticks;

        if (txtHeuristicDebug != null)
        {
            txtHeuristicDebug.text = $"mesa:{rule}\ninstant={instant}\nticks={ticks}/{Mathf.Max(1, mesaLayoutRequiredTicks)}";
        }

        if (!instant)
        {
            hint = GetMesaHint(rule);
            return false;
        }

        return ticks >= Mathf.Max(1, mesaLayoutRequiredTicks);
    }

    private bool IsPlateFrontOfUser(Vector2 userPos, Vector2 userForward, Vector2 userRight)
    {
        if (!TryGetObjectCenter("plato", out var plate))
            return false;

        var rel = plate - userPos;
        float forward = Vector2.Dot(rel, userForward);
        float side = Vector2.Dot(rel, userRight);
        return forward >= mesaForwardMin && forward <= mesaForwardMax && Mathf.Abs(side) <= mesaCenterTolerance;
    }

    private bool IsObjectBesidePlate(string label, Vector2 userForward, Vector2 userRight)
    {
        if (!TryGetObjectCenter("plato", out var plate) || !TryGetObjectCenter(label, out var obj))
            return false;

        var rel = obj - plate;
        float side = Mathf.Abs(Vector2.Dot(rel, userRight));
        float forward = Mathf.Abs(Vector2.Dot(rel, userForward));
        return side >= mesaSideMin && side <= mesaSideMax && forward <= mesaAlongTolerance;
    }

    private bool IsGlassFrontOfCutlery(Vector2 userForward, Vector2 userRight)
    {
        if (!TryGetObjectCenter("vaso", out var glass))
            return false;

        var refs = new List<Vector2>(3);
        if (TryGetObjectCenter("tenedor", out var fork)) refs.Add(fork);
        if (TryGetObjectCenter("cuchillo", out var knife)) refs.Add(knife);
        if (TryGetObjectCenter("cuchara", out var spoon)) refs.Add(spoon);
        if (refs.Count == 0 && TryGetObjectCenter("plato", out var plate)) refs.Add(plate);
        if (refs.Count == 0) return false;

        Vector2 center = Vector2.zero;
        for (int i = 0; i < refs.Count; i++) center += refs[i];
        center /= refs.Count;

        var rel = glass - center;
        float forward = Vector2.Dot(rel, userForward);
        float side = Vector2.Dot(rel, userRight);
        return forward >= mesaGlassForwardMin && forward <= mesaGlassForwardMax && Mathf.Abs(side) <= mesaGlassSideTolerance;
    }

    private bool IsNapkinSideOfPlate(Vector2 userForward, Vector2 userRight)
    {
        if (!TryGetObjectCenter("servilleta", out var napkin) || !TryGetObjectCenter("plato", out var plate))
            return false;

        var rel = napkin - plate;
        float side = Mathf.Abs(Vector2.Dot(rel, userRight));
        float forward = Mathf.Abs(Vector2.Dot(rel, userForward));
        return side >= mesaNapkinSideMin && side <= mesaNapkinSideMax && forward <= mesaAlongTolerance;
    }

    private bool IsObjectFrontOfUser(string label, Vector2 userPos, Vector2 userForward, Vector2 userRight)
    {
        if (!TryGetObjectCenter(label, out var p))
            return false;

        var rel = p - userPos;
        float forward = Vector2.Dot(rel, userForward);
        float side = Vector2.Dot(rel, userRight);
        return forward >= mesaForwardMin && forward <= mesaForwardMax && Mathf.Abs(side) <= mesaCenterTolerance;
    }

    private bool IsObjectAtSideOfUser(string label, Vector2 userPos, Vector2 userForward, Vector2 userRight, bool leftSide)
    {
        if (!TryGetObjectCenter(label, out var p))
            return false;

        var rel = p - userPos;
        float side = Vector2.Dot(rel, userRight);
        float forward = Mathf.Abs(Vector2.Dot(rel, userForward));
        bool sideOk = leftSide
            ? side <= -mesaSideMin && side >= -mesaSideMax
            : side >= mesaSideMin && side <= mesaSideMax;
        return sideOk && forward <= mesaAlongTolerance;
    }

    private bool IsLeftOfReference(Vector2 rel, Vector2 userForward, Vector2 userRight)
    {
        float side = Vector2.Dot(rel, userRight);
        float forward = Mathf.Abs(Vector2.Dot(rel, userForward));
        return side <= -mesaSideMin && side >= -mesaSideMax && forward <= mesaAlongTolerance;
    }

    private bool IsRightOfReference(Vector2 rel, Vector2 userForward, Vector2 userRight)
    {
        float side = Vector2.Dot(rel, userRight);
        float forward = Mathf.Abs(Vector2.Dot(rel, userForward));
        return side >= mesaSideMin && side <= mesaSideMax && forward <= mesaAlongTolerance;
    }

    private bool TryGetUserPose2D(out Vector2 userPos, out Vector2 userForward, out Vector2 userRight)
    {
        userPos = Vector2.zero;
        userForward = Vector2.zero;
        userRight = Vector2.zero;

        var cam = Camera.main;
        if (cam == null)
            return false;

        var p3 = cam.transform.position;
        var f3 = cam.transform.forward;
        userPos = new Vector2(p3.x, p3.z);
        userForward = new Vector2(f3.x, f3.z);
        if (userForward.sqrMagnitude < 0.0001f)
            return false;

        userForward.Normalize();
        userRight = new Vector2(userForward.y, -userForward.x);
        return true;
    }

    private bool TryGetObjectCenter(string label, out Vector2 center)
    {
        center = Vector2.zero;
        if (routineEngine == null || routineEngine.Stabilizer == null)
            return false;

        if (!routineEngine.Stabilizer.TryGetLastBbox(RoutineCatalog.NormalizeLabel(label), out var bbox))
            return false;

        center = new Vector2(bbox.x + bbox.width * 0.5f, bbox.y + bbox.height * 0.5f);
        return true;
    }

    private string GetMesaHint(MesaLayoutRule rule)
    {
        return rule switch
        {
            MesaLayoutRule.PlateFrontOfUser => "Coloca el plato delante de ti.",
            MesaLayoutRule.ForkBesidePlate => "Pon el tenedor al lado del plato.",
            MesaLayoutRule.KnifeBesidePlate => "Pon el cuchillo al lado del plato.",
            MesaLayoutRule.KnifeRightOfUser => "Pon el cuchillo a tu lado derecho.",
            MesaLayoutRule.SpoonBesidePlate => "Pon la cuchara al lado del plato.",
            MesaLayoutRule.SpoonRightOfUser => "Pon la cuchara a tu lado derecho.",
            MesaLayoutRule.GlassFrontOfCutlery => "Pon el vaso delante de los cubiertos.",
            MesaLayoutRule.NapkinSideOfPlate => "Pon la servilleta a un lado del plato.",
            MesaLayoutRule.ForkLeftOfUser => "Pon el tenedor a tu lado izquierdo.",
            MesaLayoutRule.GlassFrontOfUser => "Pon el vaso delante de ti.",
            MesaLayoutRule.NapkinSideOfUser => "Pon la servilleta a un lado.",
            _ => "Colócalo en su posición."
        };
    }

    private bool IsRawLabelPresent(string rawLabel, float minConf = 0.25f)
    {
        if (routineEngine == null) return false;
        if (routineEngine.LastRawFrame < Time.frameCount - 1) return false; // seguridad (acepta frame actual o anterior)

        var raw = routineEngine.LastRawUnnormalized;
        if (raw == null) return false;

        for (int i = 0; i < raw.Count; i++)
        {
            var d = raw[i];
            if (d.confidence < minConf) continue;
            if (d.label == rawLabel) return true;
        }
        return false;
    }

    private void Next()
    {
        _stepIndex++;
        if (_stepIndex >= _steps.Count)
        {
            StartCoroutine(CompleteRoutine_Co());
            return;
        }
        RenderStep();
    }

    private System.Collections.IEnumerator CompleteRoutine_Co()
    {
        if (_isCompletingRoutine)
        {
            yield break;
        }

        _isCompletingRoutine = true;
        _running = false;
        _stepIndex = -1;

        if (btnHecho != null)
        {
            btnHecho.SetActive(false);
        }

        if (feedbackAudioSource != null && routineCompletedAudioClip != null)
        {
            feedbackAudioSource.PlayOneShot(routineCompletedAudioClip);
            yield return new WaitForSeconds(routineCompletedAudioClip.length);
        }

        if (routineCompletedDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(routineCompletedDelaySeconds);
        }

        float routineEndTime = Time.time;
        if (_metricsSteps.Count > 0)
        {
            int totalSteps = _metricsSteps.Count;
            int totalDone = 0;
            int totalCorrective = 0;
            for (int i = 0; i < _metricsSteps.Count; i++)
            {
                if (_metricsSteps[i].completedViaDone) totalDone++;
                totalCorrective += _metricsSteps[i].correctiveMessagesPlayed;
            }

            var metrics = new RoutineMetrics
            {
                routineName = _activeRoutine.ToString(),
                routineStartTime = _routineStartTime,
                routineEndTime = routineEndTime,
                totalDuration = Mathf.Max(0f, routineEndTime - _routineStartTime),
                timestampIsoUtc = DateTime.UtcNow.ToString("o"),
                steps = new List<StepMetrics>(_metricsSteps),
                totalSteps = totalSteps,
                totalStepsCompletedViaDone = totalDone,
                totalCorrectiveMessages = totalCorrective
            };

            if (log)
            {
                string json = JsonUtility.ToJson(metrics, true);
                Debug.Log($"[StepFlow][Metrics] {json}");
            }

            SaveRoutineMetricsToFile(metrics);
            OnRoutineMetricsReady?.Invoke(metrics);
        }

        Debug.Log("[StepFlow] Finished routine");
        OnRoutineCompleted?.Invoke();
        _isCompletingRoutine = false;
    }

    private void RecordCurrentStepMetrics()
    {
        if (_steps == null || _stepIndex < 0 || _stepIndex >= _steps.Count)
        {
            return;
        }

        var step = _steps[_stepIndex];

        float validationTime = Time.time;
        float actionStart = _actionPhaseStarted ? _actionPhaseStartTime : _instructionFinishedTime;
        float duration = Mathf.Max(0f, validationTime - actionStart);

        var m = new StepMetrics
        {
            index = _stepIndex,
            instruction = step.instruction,
            label = step.label,
            type = step.type.ToString(),
            actionStartTime = actionStart,
            validationTime = validationTime,
            actionDuration = duration,
            completedViaDone = _currentStepCompletedViaDone,
            correctiveMessagesPlayed = _currentStepCorrectiveMessages
        };

        _metricsSteps.Add(m);
    }

    private void SaveRoutineMetricsToFile(RoutineMetrics metrics)
    {
        try
        {
            // Carpeta donde guardar las métricas en el dispositivo (Quest)
            string root = Application.persistentDataPath;
            string dir = Path.Combine(root, "DomusViMetrics");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Nombre de archivo: rutina_fecha_hora.json (seguro para sistema de archivos)
            string safeRoutine = string.IsNullOrEmpty(metrics.routineName)
                ? "UnknownRoutine"
                : metrics.routineName;

            // yyyyMMdd_HHmmss para que sea fácil de ordenar/leer
            DateTime ts;
            if (!DateTime.TryParse(metrics.timestampIsoUtc, out ts))
            {
                ts = DateTime.UtcNow;
            }
            string stamp = ts.ToString("yyyyMMdd_HHmmss");

            string filename = $"{safeRoutine}_{stamp}.json";
            string jsonPath = Path.Combine(dir, filename);

            string json = JsonUtility.ToJson(metrics, true);
            File.WriteAllText(jsonPath, json);

            // También guardamos una versión CSV para análisis rápido (una fila por paso + resumen)
            string csvFilename = $"{safeRoutine}_{stamp}.csv";
            string csvPath = Path.Combine(dir, csvFilename);
            string csv = BuildCsvFromMetrics(metrics);
            File.WriteAllText(csvPath, csv);

            if (log)
            {
                Debug.Log($"[StepFlow][Metrics] Guardado JSON en: {jsonPath}");
                Debug.Log($"[StepFlow][Metrics] Guardado CSV en: {csvPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StepFlow][Metrics] Error al guardar métricas: {ex.Message}");
        }
    }

    private string BuildCsvFromMetrics(RoutineMetrics metrics)
    {
        var sb = new System.Text.StringBuilder();
        // Cabecera
        sb.AppendLine("routineName;timestampUtc;totalDurationSec;stepIndex;stepType;stepLabel;stepInstruction;stepDescription;actionStartTimeSec;validationTimeSec;actionDurationSec;completedViaDone;correctiveMessagesPlayed");

        string routine = string.IsNullOrEmpty(metrics.routineName) ? "UnknownRoutine" : metrics.routineName;
        string ts = string.IsNullOrEmpty(metrics.timestampIsoUtc) ? DateTime.UtcNow.ToString("o") : metrics.timestampIsoUtc;

        if (metrics.steps != null)
        {
            for (int i = 0; i < metrics.steps.Count; i++)
            {
                var s = metrics.steps[i];
                string instr = s.instruction?.Replace("\"", "'") ?? "";
                string label = s.label?.Replace("\"", "'") ?? "";
                string type = s.type ?? "";
                string description = $"Paso {s.index + 1} de {metrics.steps.Count}: {instr}".Replace("\"", "'");

                sb.Append(routine).Append(";");
                sb.Append(ts).Append(";");
                sb.Append(metrics.totalDuration.ToString("0.000")).Append(";");
                sb.Append(s.index).Append(";");
                sb.Append(type).Append(";");
                sb.Append(label).Append(";");
                sb.Append("\"").Append(instr).Append("\"").Append(";");
                sb.Append("\"").Append(description).Append("\"").Append(";");
                sb.Append(s.actionStartTime.ToString("0.000")).Append(";");
                sb.Append(s.validationTime.ToString("0.000")).Append(";");
                sb.Append(s.actionDuration.ToString("0.000")).Append(";");
                sb.Append(s.completedViaDone ? "1" : "0").Append(";");
                sb.Append(s.correctiveMessagesPlayed).AppendLine();
            }
        }

        return sb.ToString();
    }

    private void SetSessionDetectedLabels(HashSet<string> labels)
    {
        _sessionDetectedLabels.Clear();
        if (labels == null)
        {
            return;
        }

        foreach (var label in labels)
        {
            var normalized = RoutineCatalog.NormalizeLabel(label);
            if (!string.IsNullOrEmpty(normalized))
            {
                _sessionDetectedLabels.Add(normalized);
            }
        }
    }

    private bool HasSessionLabel(string normalizedLabel)
    {
        if (_sessionDetectedLabels.Count == 0)
        {
            return true;
        }

        return _sessionDetectedLabels.Contains(RoutineCatalog.NormalizeLabel(normalizedLabel));
    }

    private void CaptureStepStartBboxes()
    {
        _stepStartBboxByLabel.Clear();
        _wrongObjectTicks.Clear();

        if (routineEngine == null || routineEngine.Stabilizer == null)
        {
            return;
        }

        var labels = GetTrackedLabelsForStep();
        for (int i = 0; i < labels.Count; i++)
        {
            string label = labels[i];
            if (routineEngine.Stabilizer.TryGetLastBbox(label, out var bbox))
            {
                _stepStartBboxByLabel[label] = bbox;
            }
        }
    }

    private List<string> GetTrackedLabelsForStep()
    {
        var labels = new List<string>();
        var seen = new HashSet<string>();

        if (_sessionDetectedLabels.Count > 0)
        {
            foreach (var label in _sessionDetectedLabels)
            {
                if (seen.Add(label))
                {
                    labels.Add(label);
                }
            }
            return labels;
        }

        if (_steps == null)
        {
            return labels;
        }

        for (int i = 0; i < _steps.Count; i++)
        {
            string label = RoutineCatalog.NormalizeLabel(_steps[i].label);
            if (string.IsNullOrEmpty(label))
            {
                continue;
            }

            if (seen.Add(label))
            {
                labels.Add(label);
            }
        }

        return labels;
    }

    private bool HasObjectMovedFromStepStart(string label, out float movementFromStart, out float areaDeltaFromStart)
    {
        movementFromStart = 0f;
        areaDeltaFromStart = 0f;

        if (routineEngine == null || routineEngine.Stabilizer == null)
        {
            return false;
        }

        label = RoutineCatalog.NormalizeLabel(label);
        if (string.IsNullOrEmpty(label))
        {
            return false;
        }

        if (!_stepStartBboxByLabel.TryGetValue(label, out var start))
        {
            if (routineEngine.Stabilizer.TryGetLastBbox(label, out var firstSeen))
            {
                _stepStartBboxByLabel[label] = firstSeen;
            }
            return false;
        }

        if (!routineEngine.Stabilizer.TryGetLastBbox(label, out var current))
        {
            return false;
        }

        Vector2 cStart = new Vector2(start.x + start.width * 0.5f, start.y + start.height * 0.5f);
        Vector2 cCurrent = new Vector2(current.x + current.width * 0.5f, current.y + current.height * 0.5f);
        movementFromStart = Vector2.Distance(cCurrent, cStart);

        float startArea = start.width * start.height;
        float currentArea = current.width * current.height;
        areaDeltaFromStart = Mathf.Abs(currentArea - startArea);

        return movementFromStart >= inHandMovementFromStartMin ||
               areaDeltaFromStart >= inHandAreaGrowthFromStartMin;
    }

    private bool IsWrongObjectMoving(string expectedLabel)
    {
        // Si el objeto esperado se está moviendo, no penalizamos movimientos
        // de otros objetos que puedan ir encima o pegados (ej. cubiertos sobre el plato).
        if (HasObjectMovedFromStepStart(expectedLabel, out _, out _))
        {
            return false;
        }

        string expected = RoutineCatalog.NormalizeLabel(expectedLabel);
        var labels = GetTrackedLabelsForStep();
        for (int i = 0; i < labels.Count; i++)
        {
            string label = labels[i];
            if (label == expected)
            {
                continue;
            }

            if (HasObjectMovedFromStepStart(label, out _, out _))
            {
                int ticks = _wrongObjectTicks.TryGetValue(label, out var t) ? t + 1 : 1;
                _wrongObjectTicks[label] = ticks;
                if (ticks >= Mathf.Max(1, requiredStableTicks))
                {
                    return true;
                }
            }
            else
            {
                _wrongObjectTicks[label] = 0;
            }
        }

        return false;
    }

    private void TryPlayNegativeFeedback(AudioClip clip)
    {
        if (clip == null || feedbackAudioSource == null || _showingValidatedFeedback)
        {
            return;
        }

        if (Time.time - _lastNegativeFeedbackTime < negativeFeedbackCooldownSeconds)
        {
            return;
        }

        _lastNegativeFeedbackTime = Time.time;
        feedbackAudioSource.PlayOneShot(clip);
        _currentStepCorrectiveMessages++;
    }

    private void RequestNegativeFeedback(string reason, AudioClip clip)
    {
        if (string.IsNullOrEmpty(reason))
        {
            return;
        }

        if (Time.time - _actionPhaseStartTime < negativeFeedbackStartDelaySeconds)
        {
            return;
        }

        if (_activeNegativeReason != reason)
        {
            _activeNegativeReason = reason;
            _activeNegativeReasonSince = Time.time;
            return;
        }

        if (Time.time - _activeNegativeReasonSince < negativeFeedbackConfirmSeconds)
        {
            return;
        }

        // Evita superponer negativos con otro feedback que esté sonando.
        if (feedbackAudioSource != null && feedbackAudioSource.isPlaying)
        {
            return;
        }

        TryPlayNegativeFeedback(clip);
        _activeNegativeReasonSince = Time.time;
    }

    private void ResetNegativeFeedbackTracking()
    {
        _activeNegativeReason = "";
        _activeNegativeReasonSince = -999f;
    }

    private void RenderStep()
    {
        _stepShownTime = Time.time;
        _canValidate = false;
        _showingValidatedFeedback = false;
        _doneVisible = false;
        _instructionReady = !blockUntilInstructionAudioFinishes;
        _instructionFinishedTime = Time.time;
        _actionPhaseStarted = false;
        _actionPhaseStartTime = Time.time;
        _currentStepCompletedViaDone = false;
        _currentStepCorrectiveMessages = 0;

        var step = _steps[_stepIndex];

        // Reset heurísticas por paso para etiquetas con validación por label.
        if (step.type == StepType.InHand)
        {
            string lbl = step.label;
            _lastBbox.Remove(lbl);
            _startInHandBbox.Remove(lbl);
            _movementTicks[lbl] = 0;
        }
        else if (step.type == StepType.Placed)
        {
            string lbl = step.label;
            _lastPlacedBbox.Remove(lbl);
            _placedStableTicks[lbl] = 0;
        }
        else if (step.type == StepType.TableLayout)
        {
            _mesaRuleTicks[step.mesaRule] = 0;
        }

        Debug.Log($"[StepFlow] RenderStep() running={_running} stepIndex={_stepIndex} stepsCount={(_steps != null ? _steps.Count : -1)} stepUI={(stepUI ? stepUI.name : "NULL")}");

        stepUI?.ShowStep(_stepIndex + 1, _steps.Count, step.instruction);

        bool isManualStep = _steps[_stepIndex].type == StepType.None;

        if (isManualStep)
            stepUI?.SetStatus("Cuando termines, pulsa HECHO.");
        else
            stepUI?.SetStatus("Cuando estés listo, hazlo con calma.");

        if (btnHecho != null)
            btnHecho.SetActive(isManualStep);

        CaptureStepStartBboxes();
        StartInstructionAudio(step);
    }

    private List<StepDef> BuildHigieneBucalSteps()
    {
        bool hasCepillo = HasSessionLabel("cepillo");
        bool hasPasta = HasSessionLabel("pasta");
        bool hasVaso = HasSessionLabel("vaso");

        var steps = new List<StepDef>();
        if (hasCepillo)
        {
            steps.Add(StepDef.InHand("Coge el cepillo", "cepillo", "higiene.coge_cepillo"));
            steps.Add(StepDef.TimedReminder("Asegúrate de que el cepillo no tiene la tapa puesta", reminderSeconds, "higiene.revisa_cepillo_sin_tapa"));
        }

        if (hasPasta)
        {
            steps.Add(StepDef.InHand("Coge la pasta", "pasta", "higiene.coge_pasta"));
            steps.Add(StepDef.TimedReminder("Asegúrate de abrir la pasta", reminderSeconds, "higiene.abre_pasta"));
            steps.Add(StepDef.TimedReminder("Pon la pasta en el cepillo", applyPastaSeconds, "higiene.pon_pasta_cepillo"));
            steps.Add(StepDef.Placed("Deja la pasta", "pasta", "higiene.deja_pasta"));
        }

        if (hasCepillo)
        {
            steps.Add(StepDef.None("Cepíllate durante 2 minutos", "higiene.cepillate_2_min"));
            steps.Add(StepDef.Placed("Deja el cepillo", "cepillo", "higiene.deja_cepillo"));
        }

        if (hasVaso)
        {
            steps.Add(StepDef.InHand("Coge el vaso y llévatelo a la boca", "vaso", "higiene.coge_vaso_boca"));
            steps.Add(StepDef.Placed("Enjuágate unos segundos y deja el vaso", "vaso", "higiene.enjuaga_y_deja_vaso"));
        }

        if (steps.Count == 0)
        {
            steps.Add(StepDef.None("No se detectaron objetos de higiene al iniciar.", "higiene.sin_objetos"));
        }

        return steps;
    }

    private List<StepDef> BuildPonerMesaSteps(RoutineVersion version)
    {
        bool hasPlate = HasSessionLabel("plato");
        bool hasFork = HasSessionLabel("tenedor");
        bool hasKnife = HasSessionLabel("cuchillo");
        bool hasSpoon = HasSessionLabel("cuchara");
        bool hasGlass = HasSessionLabel("vaso");
        bool hasNapkin = HasSessionLabel("servilleta");

        var steps = new List<StepDef>();

        // Para poner la mesa, el plato es imprescindible.
        // Si no se detecta plato al inicio de la sesión, no intentamos guiar la rutina normal.
        if (!hasPlate)
        {
            steps.Add(StepDef.None(
                "No se detectó ningún plato al iniciar. Coloca un plato delante de ti y vuelve a empezar.",
                "mesa.sin_plato"));
            return steps;
        }

        // A partir de aquí asumimos que hay plato y toda la disposición se construye alrededor de él.
        AddMesaObjectSteps(
            steps,
            "plato",
            "Coge el plato",
            "mesa.coge_plato",
            "Coloca el plato delante de ti",
            MesaLayoutRule.PlateFrontOfUser,
            "mesa.coloca_plato");

        if (hasFork)
        {
            var rule = hasPlate ? MesaLayoutRule.ForkBesidePlate : MesaLayoutRule.ForkLeftOfUser;
            AddMesaObjectSteps(
                steps,
                "tenedor",
                "Coge el tenedor",
                "mesa.coge_tenedor",
                hasPlate ? "Coloca el tenedor al lado del plato" : "Coloca el tenedor a tu izquierda",
                rule,
                "mesa.coloca_tenedor");
        }

        if (hasKnife)
        {
            var rule = hasPlate ? MesaLayoutRule.KnifeBesidePlate : MesaLayoutRule.KnifeRightOfUser;
            AddMesaObjectSteps(
                steps,
                "cuchillo",
                "Coge el cuchillo",
                "mesa.coge_cuchillo",
                hasPlate ? "Coloca el cuchillo al lado del plato" : "Coloca el cuchillo a tu derecha",
                rule,
                "mesa.coloca_cuchillo");
        }

        if (hasSpoon)
        {
            var rule = hasPlate ? MesaLayoutRule.SpoonBesidePlate : MesaLayoutRule.SpoonRightOfUser;
            string instruction = hasPlate ? "Coloca la cuchara al lado del plato" : "Coloca la cuchara a tu derecha";
            AddMesaObjectSteps(
                steps,
                "cuchara",
                "Coge la cuchara",
                "mesa.coge_cuchara",
                instruction,
                rule,
                "mesa.coloca_cuchara");
        }

        if (hasGlass)
        {
            bool hasReference = hasFork || hasKnife || hasSpoon || hasPlate;
            var rule = hasReference ? MesaLayoutRule.GlassFrontOfCutlery : MesaLayoutRule.GlassFrontOfUser;
            AddMesaObjectSteps(
                steps,
                "vaso",
                "Coge el vaso",
                "mesa.coge_vaso",
                hasReference ? "Coloca el vaso delante de los cubiertos" : "Coloca el vaso delante de ti",
                rule,
                "mesa.coloca_vaso");
        }

        if (hasNapkin)
        {
            var rule = hasPlate ? MesaLayoutRule.NapkinSideOfPlate : MesaLayoutRule.NapkinSideOfUser;
            AddMesaObjectSteps(
                steps,
                "servilleta",
                "Coge la servilleta",
                "mesa.coge_servilleta",
                hasPlate ? "Coloca la servilleta a un lado del plato" : "Coloca la servilleta a un lado",
                rule,
                "mesa.coloca_servilleta");
        }

        return steps;
    }

    private static void AddMesaObjectSteps(
        List<StepDef> steps,
        string label,
        string pickInstruction,
        string pickAudioKey,
        string placeInstruction,
        MesaLayoutRule placeRule,
        string placeAudioKey)
    {
        steps.Add(StepDef.InHand(pickInstruction, label, pickAudioKey));
        steps.Add(StepDef.TableLayout(placeInstruction, label, placeRule, placeAudioKey));
    }

    private void StartInstructionAudio(StepDef step)
    {
        if (!blockUntilInstructionAudioFinishes)
        {
            _instructionReady = true;
            _instructionFinishedTime = Time.time;
            return;
        }

        if (instructionAudioSource == null)
        {
            _instructionReady = true;
            _instructionFinishedTime = Time.time;
            return;
        }

        var clip = GetInstructionClipForStep(step);
        if (clip == null)
        {
            _instructionReady = true;
            _instructionFinishedTime = Time.time;
            return;
        }

        instructionAudioSource.Stop();
        instructionAudioSource.clip = clip;
        instructionAudioSource.Play();
        _instructionReady = false;
        _instructionFinishedTime = Time.time;
    }

    private AudioClip GetInstructionClipForStep(StepDef step)
    {
        if (instructionAudioEntries == null || instructionAudioEntries.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrEmpty(step.instructionAudioKey))
        {
            return null;
        }

        for (int i = 0; i < instructionAudioEntries.Count; i++)
        {
            var entry = instructionAudioEntries[i];
            if (entry.clip == null || string.IsNullOrEmpty(entry.key))
            {
                continue;
            }

            if (entry.key == step.instructionAudioKey)
            {
                return entry.clip;
            }
        }

        return null;
    }

    private bool EnsureStepInstructionReady()
    {
        if (_instructionReady)
        {
            if (Time.time < _instructionFinishedTime + postInstructionDelaySeconds)
            {
                return false;
            }

            EnsureActionPhaseStarted();
            return true;
        }

        if (instructionAudioSource == null || !instructionAudioSource.isPlaying)
        {
            _instructionReady = true;
            _instructionFinishedTime = Time.time;
        }

        return false;
    }

    private void EnsureActionPhaseStarted()
    {
        if (_actionPhaseStarted)
        {
            return;
        }

        _actionPhaseStarted = true;
        _actionPhaseStartTime = Time.time;
    }

    private AudioClip GetRandomCompletedFeedbackClip()
    {
        if (stepCompletedAudioClips == null || stepCompletedAudioClips.Count == 0)
        {
            return null;
        }

        if (stepCompletedAudioClips.Count == 1)
        {
            _lastCompletedFeedbackClip = stepCompletedAudioClips[0];
            return _lastCompletedFeedbackClip;
        }

        int maxAttempts = 5;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int index = UnityEngine.Random.Range(0, stepCompletedAudioClips.Count);
            var clip = stepCompletedAudioClips[index];
            if (clip != null && clip != _lastCompletedFeedbackClip)
            {
                _lastCompletedFeedbackClip = clip;
                return clip;
            }
        }

        // Fallback si todos son null o se repite por azar.
        for (int i = 0; i < stepCompletedAudioClips.Count; i++)
        {
            if (stepCompletedAudioClips[i] != null)
            {
                _lastCompletedFeedbackClip = stepCompletedAudioClips[i];
                return _lastCompletedFeedbackClip;
            }
        }

        return null;
    }

    private AudioClip GetRandomWrongObjectFeedbackClip()
    {
        return GetRandomClipAvoidImmediateRepeat(wrongObjectAudioClips, ref _lastWrongObjectFeedbackClip);
    }

    private AudioClip GetRandomWrongPlacementFeedbackClip()
    {
        return GetRandomClipAvoidImmediateRepeat(wrongPlacementAudioClips, ref _lastWrongPlacementFeedbackClip);
    }

    private AudioClip GetRandomClipAvoidImmediateRepeat(List<AudioClip> clips, ref AudioClip lastClip)
    {
        if (clips == null || clips.Count == 0)
        {
            return null;
        }

        if (clips.Count == 1)
        {
            lastClip = clips[0];
            return lastClip;
        }

        int maxAttempts = 6;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int index = UnityEngine.Random.Range(0, clips.Count);
            var clip = clips[index];
            if (clip != null && clip != lastClip)
            {
                lastClip = clip;
                return clip;
            }
        }

        for (int i = 0; i < clips.Count; i++)
        {
            if (clips[i] != null)
            {
                lastClip = clips[i];
                return lastClip;
            }
        }

        return null;
    }
}
