using System;
using System.Collections.Generic;
using UnityEngine;
using PassthroughCameraSamples.MultiObjectDetection;

/// <summary>
/// Bridge desde SentisInferenceUiManager (lista de boxes/clases YOLO) a IDetectionProvider.
/// Adapta BoundingBoxData a Detection para tu pipeline.
/// </summary>
public class SentisDetectionProvider : MonoBehaviour, IDetectionProvider
{
    [SerializeField] private SentisInferenceUiManager _sentisUiManager;
    [Header("Tracking por sesión (evita saltos entre objetos iguales)")]
    [SerializeField] private bool _lockSingleInstanceObjects = true;
    [SerializeField] private List<string> _singleInstanceNormalizedLabels = new() { "cepillo", "pasta", "vaso" };
    [SerializeField] private bool _strictSingleInstancePerSession = true; // una vez fijado, no se libera en esta sesión
    [SerializeField] private float _unlockLockedLabelAfterMissingSeconds = 6f; // < 0 = no liberar nunca
    [SerializeField] private bool _lockScopeToConfirmedRoutine = true;
    [Header("Debug bloqueo por etiqueta")]
    [SerializeField] private bool _debugSingleInstanceLocking = true;
    [SerializeField] private string _debugLabelFilter = "cepillo"; // vacío = todas
    [SerializeField] private float _debugLogEverySeconds = 1.0f;
    [Header("Debug resumen provider")]
    [SerializeField] private bool _debugSummaryLogs = false;
    [SerializeField] private float _debugSummaryLogEverySeconds = 2.0f;

    private readonly Dictionary<string, Rect> _lockedBboxByNormalizedLabel = new();
    private readonly Dictionary<string, float> _missingSinceByNormalizedLabel = new();
    private readonly Dictionary<string, float> _lastDebugLogByNormalizedLabel = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _sessionScopedLabels = new(StringComparer.OrdinalIgnoreCase);
    private bool _sessionScopeLocked;
    private int _lastSummaryRaw = -1;
    private int _lastSummaryFiltered = -1;
    private float _lastSummaryLogTime = -9999f;

    private void OnValidate()
    {
        if (_sentisUiManager == null)
            _sentisUiManager = FindObjectOfType<SentisInferenceUiManager>();

        if (_strictSingleInstancePerSession)
            _unlockLockedLabelAfterMissingSeconds = -1f;
    }

    public List<Detection> GetLatestDetections()
    {
        var detections = new List<Detection>();
        if (_sentisUiManager == null) return detections;

        // m_boxDrawn es la lista actual de boxes/clases del manager
        var boxDrawn = _sentisUiManager.m_boxDrawn;
        for (int i = 0; i < boxDrawn.Count; i++)
        {
            var box = boxDrawn[i];
            var rt = box.BoxRectTransform;
            if (rt == null) continue;

            Vector2 size = rt.sizeDelta;
            Vector3 pos = rt.position;
            // Rect en plano XZ del mundo (footprint) para que bbox tenga sentido espacial
            Rect bbox = new Rect(
                pos.x - size.x * 0.5f,
                pos.z - size.y * 0.5f,
                size.x,
                size.y
            );

            detections.Add(new Detection(
                label: box.ClassName,
                confidence: 1f,
                bbox: bbox,
                time: box.lastUpdateTime
            ));
        }

        if (!_lockSingleInstanceObjects)
        {
            LogSummaryIfNeeded(detections.Count, detections.Count);
            return detections;
        }

        var filtered = new List<Detection>(detections.Count);
        var grouped = new Dictionary<string, List<Detection>>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < detections.Count; i++)
        {
            var d = detections[i];
            string normalized = RoutineCatalog.NormalizeLabel(d.label);

            if (_sessionScopeLocked && _lockScopeToConfirmedRoutine && !_sessionScopedLabels.Contains(normalized))
            {
                continue;
            }

            if (!IsSingleInstanceLabel(normalized))
            {
                filtered.Add(d);
                continue;
            }

            if (!grouped.TryGetValue(normalized, out var list))
            {
                list = new List<Detection>(4);
                grouped[normalized] = list;
            }
            list.Add(d);
        }

        float now = Time.time;
        var labelsToTrack = GetActiveSingleInstanceLabels();
        for (int i = 0; i < labelsToTrack.Count; i++)
        {
            string normalized = labelsToTrack[i];

            if (grouped.TryGetValue(normalized, out var candidates) && candidates.Count > 0)
            {
                bool hadLockBefore = _lockedBboxByNormalizedLabel.TryGetValue(normalized, out var previousLock);
                var selected = SelectLockedCandidate(normalized, candidates);
                TryLogSelectionState(normalized, candidates, selected, now, hadLockBefore, previousLock);
                filtered.Add(selected);
                _lockedBboxByNormalizedLabel[normalized] = selected.bbox;
                _missingSinceByNormalizedLabel.Remove(normalized);
            }
            else
            {
                HandleMissingLockedLabel(normalized, now);
            }
        }

        LogSummaryIfNeeded(detections.Count, filtered.Count);
        return filtered;
    }

    public void LockSessionObjects(IEnumerable<string> normalizedLabels)
    {
        _sessionScopedLabels.Clear();
        if (normalizedLabels != null)
        {
            foreach (var rawLabel in normalizedLabels)
            {
                string normalized = RoutineCatalog.NormalizeLabel(rawLabel);
                if (!string.IsNullOrEmpty(normalized))
                {
                    _sessionScopedLabels.Add(normalized);
                }
            }
        }

        _sessionScopeLocked = _sessionScopedLabels.Count > 0;

        // Limpia locks que no formen parte de la sesión confirmada.
        var staleKeys = new List<string>();
        foreach (var key in _lockedBboxByNormalizedLabel.Keys)
        {
            if (!_sessionScopedLabels.Contains(key))
            {
                staleKeys.Add(key);
            }
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            string key = staleKeys[i];
            _lockedBboxByNormalizedLabel.Remove(key);
            _missingSinceByNormalizedLabel.Remove(key);
            _lastDebugLogByNormalizedLabel.Remove(key);
        }

        Debug.Log($"[SentisDetectionProvider] Session scope locked labels=[{string.Join(", ", _sessionScopedLabels)}]");
    }

    public void ClearSessionObjectScope()
    {
        _sessionScopeLocked = false;
        _sessionScopedLabels.Clear();
        _missingSinceByNormalizedLabel.Clear();
        Debug.Log("[SentisDetectionProvider] Session scope cleared.");
    }

    private bool IsSingleInstanceLabel(string normalizedLabel)
    {
        for (int i = 0; i < _singleInstanceNormalizedLabels.Count; i++)
        {
            if (string.Equals(_singleInstanceNormalizedLabels[i], normalizedLabel, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private List<string> GetActiveSingleInstanceLabels()
    {
        var labels = new List<string>();
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (_sessionScopeLocked)
        {
            foreach (var label in _sessionScopedLabels)
            {
                if (IsSingleInstanceLabel(label) && processed.Add(label))
                {
                    labels.Add(label);
                }
            }
            return labels;
        }

        for (int i = 0; i < _singleInstanceNormalizedLabels.Count; i++)
        {
            string normalized = RoutineCatalog.NormalizeLabel(_singleInstanceNormalizedLabels[i]);
            if (processed.Add(normalized))
            {
                labels.Add(normalized);
            }
        }
        return labels;
    }

    private Detection SelectLockedCandidate(string normalizedLabel, List<Detection> candidates)
    {
        if (!_lockedBboxByNormalizedLabel.TryGetValue(normalizedLabel, out var lockedBbox))
        {
            // Primera fijación por sesión: elegimos el objeto más grande.
            return SelectLargestArea(candidates);
        }

        if (candidates.Count == 1)
            return candidates[0];

        int bestIdx = 0;
        float bestScore = float.MinValue;
        Vector2 lockedCenter = GetRectCenter(lockedBbox);

        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            float area = c.bbox.width * c.bbox.height;
            float iou = ComputeIoU(lockedBbox, c.bbox);
            float dist = Vector2.Distance(lockedCenter, GetRectCenter(c.bbox));
            float score = iou * 1000f - dist * 10f + area;

            if (score > bestScore)
            {
                bestScore = score;
                bestIdx = i;
            }
        }

        return candidates[bestIdx];
    }

    private static Detection SelectLargestArea(List<Detection> candidates)
    {
        int bestIdx = 0;
        float bestArea = float.MinValue;
        for (int i = 0; i < candidates.Count; i++)
        {
            float area = candidates[i].bbox.width * candidates[i].bbox.height;
            if (area > bestArea)
            {
                bestArea = area;
                bestIdx = i;
            }
        }
        return candidates[bestIdx];
    }

    private void HandleMissingLockedLabel(string normalizedLabel, float now)
    {
        if (!_lockedBboxByNormalizedLabel.ContainsKey(normalizedLabel))
            return;

        if (_strictSingleInstancePerSession)
            return;

        if (_unlockLockedLabelAfterMissingSeconds < 0f)
            return;

        if (!_missingSinceByNormalizedLabel.TryGetValue(normalizedLabel, out var missingSince))
        {
            _missingSinceByNormalizedLabel[normalizedLabel] = now;
            if (ShouldLogForLabel(normalizedLabel, now))
                Debug.Log($"[SentisDetectionProvider][LOCK] label={normalizedLabel} missing=true startTimer");
            return;
        }

        if (now - missingSince >= _unlockLockedLabelAfterMissingSeconds)
        {
            if (ShouldLogForLabel(normalizedLabel, now))
                Debug.Log($"[SentisDetectionProvider][LOCK] label={normalizedLabel} unlocked_after_missing={now - missingSince:0.00}s");
            _lockedBboxByNormalizedLabel.Remove(normalizedLabel);
            _missingSinceByNormalizedLabel.Remove(normalizedLabel);
        }
    }

    private void TryLogSelectionState(string normalizedLabel, List<Detection> candidates, Detection selected, float now, bool hadLockBefore, Rect previousLock)
    {
        if (!ShouldLogForLabel(normalizedLabel, now))
            return;

        float selectedArea = selected.bbox.width * selected.bbox.height;
        float selectedIou = hadLockBefore ? ComputeIoU(previousLock, selected.bbox) : -1f;
        float selectedDist = hadLockBefore ? Vector2.Distance(GetRectCenter(previousLock), GetRectCenter(selected.bbox)) : -1f;

        Debug.Log(
            $"[SentisDetectionProvider][LOCK] label={normalizedLabel} candidates={candidates.Count} " +
            $"hadLockBefore={hadLockBefore} selectedArea={selectedArea:0.000} selectedIoU={selectedIou:0.000} selectedDist={selectedDist:0.000}");

        if (candidates.Count <= 1)
            return;

        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            float area = c.bbox.width * c.bbox.height;
            float iou = hadLockBefore ? ComputeIoU(previousLock, c.bbox) : -1f;
            float dist = hadLockBefore ? Vector2.Distance(GetRectCenter(previousLock), GetRectCenter(c.bbox)) : -1f;
            bool isSelected = ApproximatelySameRect(c.bbox, selected.bbox);
            Debug.Log($"[SentisDetectionProvider][LOCK]   cand#{i} area={area:0.000} iou={iou:0.000} dist={dist:0.000} selected={isSelected}");
        }
    }

    private bool ShouldLogForLabel(string normalizedLabel, float now)
    {
        if (!_debugSingleInstanceLocking)
            return false;

        if (!string.IsNullOrWhiteSpace(_debugLabelFilter) &&
            !string.Equals(normalizedLabel, RoutineCatalog.NormalizeLabel(_debugLabelFilter), StringComparison.OrdinalIgnoreCase))
            return false;

        if (_debugLogEverySeconds <= 0f)
            return true;

        if (!_lastDebugLogByNormalizedLabel.TryGetValue(normalizedLabel, out var last))
        {
            _lastDebugLogByNormalizedLabel[normalizedLabel] = now;
            return true;
        }

        if (now - last < _debugLogEverySeconds)
            return false;

        _lastDebugLogByNormalizedLabel[normalizedLabel] = now;
        return true;
    }

    private static bool ApproximatelySameRect(Rect a, Rect b)
    {
        const float eps = 0.0001f;
        return Mathf.Abs(a.x - b.x) < eps &&
               Mathf.Abs(a.y - b.y) < eps &&
               Mathf.Abs(a.width - b.width) < eps &&
               Mathf.Abs(a.height - b.height) < eps;
    }

    private void LogSummaryIfNeeded(int rawCount, int filteredCount)
    {
        if (!_debugSummaryLogs)
            return;

        float now = Time.time;
        bool changed = rawCount != _lastSummaryRaw || filteredCount != _lastSummaryFiltered;
        bool periodic = now - _lastSummaryLogTime >= Mathf.Max(0.1f, _debugSummaryLogEverySeconds);

        if (!changed && !periodic)
            return;

        _lastSummaryRaw = rawCount;
        _lastSummaryFiltered = filteredCount;
        _lastSummaryLogTime = now;
        Debug.Log($"[SentisDetectionProvider] detections={rawCount} filtered={filteredCount}");
    }

    private static Vector2 GetRectCenter(Rect rect)
    {
        return new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
    }

    private static float ComputeIoU(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);

        float interW = Mathf.Max(0f, xMax - xMin);
        float interH = Mathf.Max(0f, yMax - yMin);
        float interArea = interW * interH;

        float unionArea = a.width * a.height + b.width * b.height - interArea;
        if (unionArea <= 0f) return 0f;
        return interArea / unionArea;
    }
}
