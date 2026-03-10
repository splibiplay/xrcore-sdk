// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class SentisInferenceUiManager : MonoBehaviour
    {
        private enum DetectionMarkerMode
        {
            BoundingBox,
            PointWithText,
            GlowFocus
        }

        [Header("Placement configuration")]
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [Header("Box reuse / overlap policy")]
        [SerializeField] private bool m_removeDifferentClassOverlaps = false;
        [SerializeField, Range(0f, 1f)] private float m_differentClassOverlapIouThreshold = 0.75f;
        [Header("Debug visualization")]
        [SerializeField] private bool m_useFixedDepthWhenRaycastFails = true;
        [SerializeField, Min(0.1f)] private float m_fallbackDepthMeters = 0.8f;
        [SerializeField] private DetectionMarkerMode m_markerMode = DetectionMarkerMode.BoundingBox;
        [SerializeField, Min(0.005f)] private float m_pointMarkerSize = 0.03f;
        [SerializeField, Min(0.01f)] private float m_glowMinSize = 0.08f;
        [SerializeField, Min(0.01f)] private float m_glowScaleFromBox = 1.25f;
        [SerializeField, Range(0f, 1f)] private float m_glowAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] private float m_glowPulseAmplitude = 0.25f;
        [SerializeField, Min(0.1f)] private float m_glowPulseHz = 1.4f;
        [SerializeField] private Color m_glowColor = new Color(1f, 0.95f, 0.2f, 1f);

        [SerializeField] private RectTransform m_detectionBoxPrefab;
        [Space(10)]
        public UnityEvent<int> OnObjectsDetected;

        internal readonly List<BoundingBoxData> m_boxDrawn = new();
        private string[] m_labels;
        private readonly List<BoundingBoxData> m_boxPool = new();

        internal class BoundingBoxData
        {
            public string ClassName;
            public int ClassId;
            public RectTransform BoxRectTransform;
            public float lastUpdateTime;
            public Vector2 baseSize;
        }

        private void Awake() => m_detectionBoxPrefab.gameObject.SetActive(false);

        private void Update()
        {
            // Remove boxes that haven't been updated recently
            for (int i = m_boxDrawn.Count - 1; i >= 0; i--)
            {
                var box = m_boxDrawn[i];
                const float timeToPersistBoxes = 3f;
                if (Time.time - box.lastUpdateTime > timeToPersistBoxes)
                {
                    ReturnToPool(box);
                    m_boxDrawn.RemoveAt(i);
                }
            }

            if (m_markerMode == DetectionMarkerMode.GlowFocus)
            {
                var wave = 1f + m_glowPulseAmplitude * Mathf.Sin(Time.time * m_glowPulseHz * Mathf.PI * 2f);
                for (int i = 0; i < m_boxDrawn.Count; i++)
                {
                    var box = m_boxDrawn[i];
                    box.BoxRectTransform.sizeDelta = box.baseSize * wave;
                }
            }
        }

        public void SetLabels(TextAsset labelsAsset)
        {
            if (labelsAsset == null)
            {
                m_labels = null;
                return;
            }

            // Parse labels robustly (trim CR/LF and skip empty lines)
            var raw = labelsAsset.text.Split('\n');
            var cleaned = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                var label = raw[i].Trim();
                if (!string.IsNullOrEmpty(label))
                {
                    cleaned.Add(label);
                }
            }
            m_labels = cleaned.ToArray();
        }

        public void DrawUIBoxes(List<(int classId, Vector4 boundingBox)> detections, Vector2 inputSize, Pose cameraPose)
        {
            Vector2 currentResolution = m_cameraAccess.CurrentResolution;

            if (detections.Count == 0)
            {
                OnObjectsDetected?.Invoke(0);
                return;
            }

            OnObjectsDetected?.Invoke(detections.Count);

            // Draw the bounding boxes
            for (var i = 0; i < detections.Count; i++)
            {
                var detection = detections[i];
                float x1 = detection.boundingBox[0];
                float y1 = detection.boundingBox[1];
                float x2 = detection.boundingBox[2];
                float y2 = detection.boundingBox[3];
                Rect rect = new Rect(x1, y1, x2 - x1, y2 - y1);
                // Rect rect = Rect.MinMaxRect(x1, y1, x2, y2); // todo

                Vector2 normalizedCenter = rect.center / inputSize;
                Vector2 center = currentResolution * (normalizedCenter - Vector2.one * 0.5f);

                // Get the object class name (safe against out-of-range classId)
                var classname = GetLabelSafe(detection.classId).Replace(" ", "_");

                // Get the 3D marker world position using Depth Raycast
                var ray = m_cameraAccess.ViewportPointToRay(new Vector2(normalizedCenter.x, 1.0f - normalizedCenter.y), cameraPose);
                var worldPos = m_environmentRaycast.Raycast(ray);
                if (!worldPos.HasValue)
                {
                    if (!m_useFixedDepthWhenRaycastFails)
                    {
                        Debug.Log($"RaycastManager failed, ray:{ray}, cameraPose:{cameraPose}");
                        continue;
                    }

                    // Fallback for debug: keep showing detections even when environment depth raycast fails.
                    worldPos = ray.GetPoint(m_fallbackDepthMeters);
                }
                var normRect = new Rect(
                    rect.x / inputSize.x,
                    1f - rect.yMax / inputSize.y,
                    rect.width / inputSize.x,
                    rect.height / inputSize.y
                );

                // Calculate distance and center point first
                float distance = Vector3.Distance(cameraPose.position, worldPos.Value);
                var worldSpaceCenter = m_cameraAccess.ViewportPointToRay(normRect.center, cameraPose).GetPoint(distance);
                var normal = (worldSpaceCenter - cameraPose.position).normalized;

                // Intersect corner rays with the plane perpendicular to the camera view
                var plane = new Plane(normal, worldSpaceCenter);
                var minRay = m_cameraAccess.ViewportPointToRay(normRect.min, cameraPose);
                var maxRay = m_cameraAccess.ViewportPointToRay(normRect.max, cameraPose);
                plane.Raycast(minRay, out float intersectionDistanceMin);
                plane.Raycast(maxRay, out float intersectionDistanceMax);
                var min = minRay.GetPoint(intersectionDistanceMin);
                var max = maxRay.GetPoint(intersectionDistanceMax);

                // Transform world-space positions to camera's local space to get 2D size
                var topLeftLocal = Quaternion.Inverse(cameraPose.rotation) * (min - cameraPose.position);
                var bottomRightLocal = Quaternion.Inverse(cameraPose.rotation) * (max - cameraPose.position);
                var size = new Vector2(
                    Mathf.Abs(bottomRightLocal.x - topLeftLocal.x),
                    Mathf.Abs(bottomRightLocal.y - topLeftLocal.y));

                Vector2 markerSize;
                switch (m_markerMode)
                {
                    case DetectionMarkerMode.PointWithText:
                        markerSize = new Vector2(m_pointMarkerSize, m_pointMarkerSize);
                        break;
                    case DetectionMarkerMode.GlowFocus:
                        var glowSize = Mathf.Max(m_glowMinSize, Mathf.Max(size.x, size.y) * m_glowScaleFromBox);
                        markerSize = new Vector2(glowSize, glowSize);
                        break;
                    default:
                        markerSize = size;
                        break;
                }

                var boxData = GetOrCreateBoundingBoxData(detection.classId, worldSpaceCenter, markerSize);
                var boxRectTransform = boxData.BoxRectTransform;
                if (boxRectTransform.TryGetComponent<Image>(out var markerImage))
                {
                    if (m_markerMode == DetectionMarkerMode.GlowFocus)
                    {
                        markerImage.fillCenter = true;
                        markerImage.color = new Color(m_glowColor.r, m_glowColor.g, m_glowColor.b, m_glowAlpha);
                    }
                    else if (m_markerMode == DetectionMarkerMode.PointWithText)
                    {
                        markerImage.fillCenter = true;
                        markerImage.color = Color.white;
                    }
                    else
                    {
                        markerImage.fillCenter = false;
                        markerImage.color = Color.white;
                    }
                }

                var markerText = boxRectTransform.GetComponentInChildren<Text>();
                if (markerText != null)
                {
                    if (m_markerMode == DetectionMarkerMode.GlowFocus)
                    {
                        markerText.text = string.Empty;
                        markerText.enabled = false;
                    }
                    else
                    {
                        markerText.enabled = true;
                        markerText.text = m_markerMode == DetectionMarkerMode.PointWithText
                            ? $"{classname}"
                            : $"Id: {detection.classId} Class: {classname} Center (px): {center:0.0} Center (%): {normalizedCenter:0.0}";
                    }
                }
                boxRectTransform.SetPositionAndRotation(worldSpaceCenter, Quaternion.LookRotation(normal));
                boxData.baseSize = markerSize;
                boxRectTransform.sizeDelta = markerSize;
                boxData.lastUpdateTime = Time.time;
            }
        }

        private BoundingBoxData GetOrCreateBoundingBoxData(int classId, Vector3 worldSpaceCenter, Vector2 worldSpaceSize)
        {
            if (m_markerMode == DetectionMarkerMode.PointWithText)
            {
                BoundingBoxData reusedPoint = null;
                for (int i = m_boxDrawn.Count - 1; i >= 0; i--)
                {
                    var box = m_boxDrawn[i];
                    if (box.ClassId == classId)
                    {
                        if (reusedPoint == null)
                        {
                            reusedPoint = box;
                        }
                        else
                        {
                            // Keep only one marker per class in point mode.
                            ReturnToPool(box);
                            m_boxDrawn.RemoveAt(i);
                        }
                    }
                }

                if (reusedPoint != null)
                {
                    return reusedPoint;
                }

                var pointData = GetBoxFromPoolOrCreate();
                pointData.ClassId = classId;
                pointData.ClassName = GetLabelSafe(classId).Replace(" ", "_");
                m_boxDrawn.Add(pointData);
                return pointData;
            }

            BoundingBoxData reusedBox = null;
            for (int i = m_boxDrawn.Count - 1; i >= 0; i--)
            {
                var box = m_boxDrawn[i];
                var localPos = box.BoxRectTransform.InverseTransformPoint(worldSpaceCenter);
                var newBox = new Vector4(
                    localPos.x - worldSpaceSize.x * 0.5f,
                    localPos.y - worldSpaceSize.y * 0.5f,
                    localPos.x + worldSpaceSize.x * 0.5f,
                    localPos.y + worldSpaceSize.y * 0.5f
                );

                var sizeDelta = box.BoxRectTransform.sizeDelta;
                var currentBox = new Vector4(
                    -sizeDelta.x * 0.5f,
                    -sizeDelta.y * 0.5f,
                    sizeDelta.x * 0.5f,
                    sizeDelta.y * 0.5f);

                if (box.ClassId == classId)
                {
                    // If the new box overlaps with an existing one of the same class, reuse it
                    if (SentisInferenceRunManager.CalculateIoU(newBox, currentBox) > 0f)
                    {
                        if (reusedBox == null)
                        {
                            reusedBox = box;
                        }
                        else
                        {
                            // Same overlapping class - remove the existing box
                            ReturnToPool(box);
                            m_boxDrawn.RemoveAt(i);
                        }
                    }
                }
                // Optional: remove different-class overlaps only if explicitly enabled.
                else if (m_removeDifferentClassOverlaps &&
                         SentisInferenceRunManager.CalculateIoU(newBox, currentBox) > m_differentClassOverlapIouThreshold)
                {
                    // Different overlapping class - remove the existing box
                    ReturnToPool(box);
                    m_boxDrawn.RemoveAt(i);
                }
            }

            if (reusedBox != null)
            {
                return reusedBox;
            }

            // Create a new box
            var newData = GetBoxFromPoolOrCreate();
            newData.ClassId = classId;
            newData.ClassName = GetLabelSafe(classId).Replace(" ", "_");
            m_boxDrawn.Add(newData);
            return newData;
        }

        private string GetLabelSafe(int classId)
        {
            if (m_labels == null || m_labels.Length == 0)
            {
                return "Unknown";
            }

            if (classId < 0 || classId >= m_labels.Length)
            {
                return "Unknown";
            }

            return m_labels[classId];
        }

        private BoundingBoxData GetBoxFromPoolOrCreate()
        {
            if (m_boxPool.Count > 0)
            {
                var pooled = m_boxPool[m_boxPool.Count - 1];
                pooled.BoxRectTransform.gameObject.SetActive(true);
                m_boxPool.RemoveAt(m_boxPool.Count - 1);
                return pooled;
            }

            var boxRectTransform = Instantiate(m_detectionBoxPrefab, ContentParent);
            boxRectTransform.gameObject.SetActive(true);
            return new BoundingBoxData
            {
                BoxRectTransform = boxRectTransform
            };
        }

        internal Transform ContentParent => m_detectionBoxPrefab.parent;

        private void ReturnToPool(BoundingBoxData box)
        {
            box.BoxRectTransform.gameObject.SetActive(false);
            m_boxPool.Add(box);
        }

        internal void ClearAnnotations()
        {
            foreach (var box in m_boxDrawn)
            {
                ReturnToPool(box);
            }
            m_boxDrawn.Clear();
        }
    }
}
