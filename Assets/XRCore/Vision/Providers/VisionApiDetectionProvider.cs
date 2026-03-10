using System.Collections.Generic;
using UnityEngine;

namespace XRCore.Vision
{
    /// <summary>
    /// Provider para inyectar detecciones desde un cliente externo (REST/WebSocket/etc).
    /// </summary>
    public sealed class VisionApiDetectionProvider : MonoBehaviour, IXRDetectionProvider
    {
        [SerializeField] private string providerId = "vision.api";
        [SerializeField] private bool clearAfterRead = true;
        [SerializeField] private bool normalizeLabelToLower = true;

        private readonly List<DetectionResult> _latest = new(32);
        private readonly List<DetectionResult> _readBuffer = new(32);

        public IEnumerable<DetectionResult> GetDetections()
        {
            _readBuffer.Clear();
            _readBuffer.AddRange(_latest);

            if (clearAfterRead)
            {
                _latest.Clear();
            }

            return _readBuffer;
        }

        public void ReplaceDetections(IReadOnlyList<DetectionResult> detections)
        {
            _latest.Clear();
            if (detections == null)
            {
                return;
            }

            for (int i = 0; i < detections.Count; i++)
            {
                _latest.Add(detections[i]);
            }
        }

        public void ReplaceDetectionsFromLabels(IReadOnlyList<string> labels, float confidence = 1f)
        {
            _latest.Clear();
            if (labels == null)
            {
                return;
            }

            float now = Time.time;
            for (int i = 0; i < labels.Count; i++)
            {
                string label = labels[i];
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                if (normalizeLabelToLower)
                {
                    label = label.ToLowerInvariant();
                }

                var detection = new Detection(label, confidence, new Rect(0.45f, 0.45f, 0.1f, 0.1f), now);
                _latest.Add(new DetectionResult(providerId, detection));
            }
        }

        public void ClearDetections()
        {
            _latest.Clear();
            _readBuffer.Clear();
        }
    }
}
