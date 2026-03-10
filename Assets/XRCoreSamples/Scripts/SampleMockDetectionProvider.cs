using System.Collections.Generic;
using UnityEngine;
using XRCore.Vision;

namespace XRCore.Samples
{
    /// <summary>
    /// Provider de deteccion de ejemplo para demo sin dependencias externas.
    /// </summary>
    public sealed class SampleMockDetectionProvider : MonoBehaviour, XRCore.Vision.IDetectionProvider
    {
        [SerializeField] private string providerId = "sample.mock";
        [SerializeField] private string primaryLabel = "objeto_a";
        [SerializeField] private string secondaryLabel = "objeto_b";
        [SerializeField] private bool emitSecondary = true;

        private readonly List<XRCore.Vision.DetectionResult> _buffer = new(2);

        public IEnumerable<XRCore.Vision.DetectionResult> GetDetections()
        {
            _buffer.Clear();

            float t = Time.time;
            float x = Mathf.Sin(t) * 0.5f;
            float z = 1.5f + Mathf.Cos(t) * 0.25f;

            var primary = new XRCore.Vision.Detection(
                primaryLabel,
                0.95f,
                new Rect(x, z, 0.3f, 0.2f),
                t
            );
            _buffer.Add(new XRCore.Vision.DetectionResult(providerId, primary));

            if (emitSecondary)
            {
                var secondary = new XRCore.Vision.Detection(
                    secondaryLabel,
                    0.88f,
                    new Rect(x + 0.35f, z - 0.15f, 0.25f, 0.18f),
                    t
                );
                _buffer.Add(new XRCore.Vision.DetectionResult(providerId, secondary));
            }

            return _buffer;
        }
    }
}
