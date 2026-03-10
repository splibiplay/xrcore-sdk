using UnityEngine;
using XRCore.Core;

public class HeadGazeDetectionProvider : MonoBehaviour
{
    public Transform head;
    public XRCore.Tasks.XRTaskRunner taskRunner;
    public float maxDistance = 5f;
    public string targetObjectName = "Cube_A";
    public float requiredGazeSeconds = 1.5f;
    public float cooldown = 2f;
    public Color highlightColor = new Color(0.2f, 1f, 0.4f, 1f);
    public float emissionIntensity = 2.5f;
    public bool restartTaskOnLookAwayAfterCompletion = true;

    private readonly XRCore.Vision.DetectionResult[] _singleDetection = new XRCore.Vision.DetectionResult[1];
    private Renderer _highlightedRenderer;
    private string _cachedColorProperty;
    private Color _originalBaseColor;
    private Color _originalEmissionColor;
    private bool _hadEmissionEnabled;
    private float _lastEmitTime = -999f;
    private Collider _lastHit;
    private bool _taskCompleted;
    private bool _pendingRestartOnLookAway;
    private Collider _gazeCandidate;
    private float _gazeCandidateStartedAt;

    private void OnEnable()
    {
        _taskCompleted = false;
        _pendingRestartOnLookAway = false;
        _lastEmitTime = -999f;
        _lastHit = null;
        _gazeCandidate = null;
        _gazeCandidateStartedAt = -1f;

        if (taskRunner == null)
        {
            taskRunner = FindFirstObjectByType<XRCore.Tasks.XRTaskRunner>();
        }

        XRCoreEventBus.Subscribe<XRCore.Tasks.XRTaskCompletedEvent>(OnTaskCompleted);
        XRCoreEventBus.Subscribe<XRCore.Tasks.XRTaskStartedEvent>(OnTaskStarted);
    }

    private void Update()
    {
        if (head == null)
        {
            ClearGazeState();
            return;
        }

        if (_taskCompleted)
        {
            ClearGazeState();

            if (_pendingRestartOnLookAway && !IsLookingAtTarget())
            {
                _pendingRestartOnLookAway = false;
                if (taskRunner != null)
                {
                    taskRunner.StartTask();
                }
            }

            return;
        }

        if (TryGetTargetHit(out RaycastHit hit))
        {
            UpdateHighlightState(hit.collider);
            UpdateGazeCandidate(hit.collider, Time.time);
            TryEmitDetection(hit, Time.time);
            return;
        }

        ClearGazeState();
    }

    private void OnDisable()
    {
        XRCoreEventBus.Unsubscribe<XRCore.Tasks.XRTaskCompletedEvent>(OnTaskCompleted);
        XRCoreEventBus.Unsubscribe<XRCore.Tasks.XRTaskStartedEvent>(OnTaskStarted);
        ClearGazeState();
    }

    private void OnTaskCompleted(XRCore.Tasks.XRTaskCompletedEvent _)
    {
        _taskCompleted = true;
        _pendingRestartOnLookAway = restartTaskOnLookAwayAfterCompletion;
        ClearGazeState();
    }

    private void OnTaskStarted(XRCore.Tasks.XRTaskStartedEvent _)
    {
        _taskCompleted = false;
        _pendingRestartOnLookAway = false;
        _lastEmitTime = -999f;
        _lastHit = null;
        _gazeCandidate = null;
        _gazeCandidateStartedAt = -1f;
    }

    private bool TryGetTargetHit(out RaycastHit hit)
    {
        Ray ray = new Ray(head.position, head.forward);
        if (!Physics.Raycast(ray, out hit, maxDistance))
        {
            return false;
        }

        return hit.collider != null && hit.collider.name == targetObjectName;
    }

    private bool IsLookingAtTarget()
    {
        return TryGetTargetHit(out _);
    }

    private void TryEmitDetection(RaycastHit hit, float now)
    {
        if (_gazeCandidate != hit.collider || _gazeCandidateStartedAt < 0f)
        {
            return;
        }

        if (requiredGazeSeconds > 0f && now - _gazeCandidateStartedAt < requiredGazeSeconds)
        {
            return;
        }

        // Lock while gazing at the same collider to avoid repeated emits.
        if (_lastHit == hit.collider)
        {
            return;
        }

        if (cooldown > 0f && now - _lastEmitTime < cooldown)
        {
            return;
        }

        var detection = new XRCore.Vision.Detection(
            label: "objeto_a",
            confidence: 1f,
            boundingBox: new Rect(hit.point.x, hit.point.z, 0.1f, 0.1f),
            timestamp: now);

        _lastHit = hit.collider;
        _lastEmitTime = now;
        _singleDetection[0] = new XRCore.Vision.DetectionResult("head.gaze", detection);
        XRCoreEventBus.Publish(new XRCore.Vision.XRDetectionEvent(_singleDetection, 1, now, "head.gaze"));
    }

    private void UpdateGazeCandidate(Collider collider, float now)
    {
        if (_gazeCandidate == collider)
        {
            return;
        }

        _gazeCandidate = collider;
        _gazeCandidateStartedAt = now;
        XRCoreEventBus.Publish(new HeadGazeTargetEnteredEvent(targetObjectName, now));
    }

    private void UpdateHighlightState(Collider collider)
    {
        var rendererToHighlight = collider.GetComponentInParent<Renderer>();
        if (rendererToHighlight == null)
        {
            RestoreCurrentHighlight();
            return;
        }

        if (_highlightedRenderer == rendererToHighlight)
        {
            return;
        }

        RestoreCurrentHighlight();
        CacheOriginalMaterialState(rendererToHighlight);
        ApplyCurrentHighlight();
    }

    private void ApplyCurrentHighlight()
    {
        if (_highlightedRenderer == null)
        {
            return;
        }

        Material material = _highlightedRenderer.material;
        if (!string.IsNullOrEmpty(_cachedColorProperty))
        {
            material.SetColor(_cachedColorProperty, highlightColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", highlightColor * emissionIntensity);
        }
    }

    private void CacheOriginalMaterialState(Renderer renderer)
    {
        _highlightedRenderer = renderer;
        Material material = _highlightedRenderer.material;

        _cachedColorProperty = material.HasProperty("_BaseColor")
            ? "_BaseColor"
            : material.HasProperty("_Color")
                ? "_Color"
                : string.Empty;

        _originalBaseColor = !string.IsNullOrEmpty(_cachedColorProperty)
            ? material.GetColor(_cachedColorProperty)
            : Color.white;

        _hadEmissionEnabled = material.IsKeywordEnabled("_EMISSION");
        _originalEmissionColor = material.HasProperty("_EmissionColor")
            ? material.GetColor("_EmissionColor")
            : Color.black;
    }

    private void ClearGazeState()
    {
        _lastHit = null;
        _gazeCandidate = null;
        _gazeCandidateStartedAt = -1f;
        RestoreCurrentHighlight();
    }

    private void RestoreCurrentHighlight()
    {
        if (_highlightedRenderer == null)
        {
            return;
        }

        Material material = _highlightedRenderer.material;
        if (!string.IsNullOrEmpty(_cachedColorProperty))
        {
            material.SetColor(_cachedColorProperty, _originalBaseColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", _originalEmissionColor);
            if (_hadEmissionEnabled)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
        }

        _highlightedRenderer = null;
        _cachedColorProperty = string.Empty;
    }
}