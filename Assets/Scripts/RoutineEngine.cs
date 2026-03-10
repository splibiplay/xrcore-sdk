using System;
using System.Collections.Generic;
using UnityEngine;

public class RoutineEngine : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MonoBehaviour detectionProviderBehaviour; // debe implementar IDetectionProvider
    [SerializeField] private DetectionStabilizer stabilizer;

    [Header("Debug")]
    [SerializeField] private bool logChanges = true;
    [SerializeField] private bool logRaw = false;
    [SerializeField] private int logRawEveryNFrames = 30;

    public event Action<RoutineCandidate, HashSet<string>> OnCandidateChanged;

    public DetectionStabilizer Stabilizer => stabilizer;
    public IDetectionProvider Provider => _provider;

    private IDetectionProvider _provider;
    private RoutineCandidate _lastCandidate;
    private string _lastCandidateKey = "";

    // RAW sin normalizar (misma lista que el provider)
    private List<Detection> _lastRawUnnormalized;

    // Buffer reutilizable para detecciones normalizadas (0 GC)
    private readonly List<Detection> _normalizedBuffer = new(64);

    private int _rawLogCounter = 0;

    public List<Detection> LastRawUnnormalized => _lastRawUnnormalized;
    public int LastRawFrame { get; private set; }
    public IReadOnlyList<Detection> LastNormalizedDetections => _normalizedBuffer;

    private void Awake()
    {
        _provider = detectionProviderBehaviour as IDetectionProvider;
        if (_provider == null)
        {
            Debug.LogError("[RoutineEngine] detectionProviderBehaviour no implementa IDetectionProvider.");
        }
        if (stabilizer == null) stabilizer = GetComponent<DetectionStabilizer>();
    }

    private void Update()
    {
        if (_provider == null) return;

        var detections = _provider.GetLatestDetections();
        if (detections == null) return;

        // RAW sin normalizar para pasos finos (abrir/cerrar, etc.)
        _lastRawUnnormalized = detections;
        LastRawFrame = Time.frameCount;

        // Debug RAW: limitar para no romper FPS
        if (logRaw && detections.Count > 0)
        {
            _rawLogCounter++;
            if (_rawLogCounter % logRawEveryNFrames == 0)
            {
                int n = Mathf.Min(5, detections.Count);
                for (int i = 0; i < n; i++)
                {
                    var d = detections[i];
                    Debug.Log($"[RAW] {d.label} conf={d.confidence:0.00} bbox=({d.bbox.x:0.00},{d.bbox.y:0.00},{d.bbox.width:0.00},{d.bbox.height:0.00})");
                }
            }
        }

        // Normaliza labels en buffer separado antes de estabilizar
        _normalizedBuffer.Clear();
        for (int i = 0; i < detections.Count; i++)
        {
            var d = detections[i];
            d.label = RoutineCatalog.NormalizeLabel(d.label);
            _normalizedBuffer.Add(d);
        }

        if (stabilizer == null) return;
        stabilizer.PushDetections(_normalizedBuffer);

        var stableRaw = stabilizer.GetStablePresentLabels();
        var present = RoutineCatalog.Normalize(stableRaw);

        var candidate = RoutineCatalog.Evaluate(present);

        // Clave simple para detectar cambios sin comparar listas profundas
        string key = $"{candidate.id}:{candidate.version}:{candidate.score}:{string.Join(",", present)}";

        if (key != _lastCandidateKey)
        {
            _lastCandidateKey = key;
            _lastCandidate = candidate;

            if (logChanges)
            {
                Debug.Log($"[RoutineEngine] Candidate={candidate.id} v={candidate.version} score={candidate.score} present=[{string.Join(", ", present)}]");
            }

            OnCandidateChanged?.Invoke(candidate, present);
        }
    }
}

