using System.Collections.Generic;
using UnityEngine;

namespace XRCore.Vision
{
    /// <summary>
    /// Provider de simulacion para demos/tests sin pipeline de vision real.
    /// </summary>
    public sealed class SimulationDetectionProvider : MonoBehaviour, IXRDetectionProvider
    {
        [Header("Output")]
        [SerializeField] private string providerId = "simulation";
        [SerializeField] private List<string> labels = new() { "objeto_a" };
        [SerializeField, Min(0f)] private float emitIntervalSeconds = 1f;
        [SerializeField, Range(0f, 1f)] private float confidence = 0.9f;

        private readonly List<DetectionResult> _buffer = new(1);
        private float _lastEmitTime = -999f;
        private int _nextLabelIndex;

        public IEnumerable<DetectionResult> GetDetections()
        {
            _buffer.Clear();

            if (labels == null || labels.Count == 0)
            {
                return _buffer;
            }

            float now = Time.time;
            if (emitIntervalSeconds > 0f && now - _lastEmitTime < emitIntervalSeconds)
            {
                return _buffer;
            }

            _lastEmitTime = now;
            string label = labels[_nextLabelIndex % labels.Count];
            _nextLabelIndex++;

            if (string.IsNullOrWhiteSpace(label))
            {
                return _buffer;
            }

            var detection = new Detection(label, confidence, new Rect(0.45f, 0.45f, 0.1f, 0.1f), now);
            _buffer.Add(new DetectionResult(providerId, detection));
            return _buffer;
        }
    }
}
