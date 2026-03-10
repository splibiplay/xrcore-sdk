using System.Collections.Generic;
using UnityEngine;

namespace XRCore.Vision
{
    /// <summary>
    /// Provider de deteccion por raycast para experiencias XR sin dependencias externas.
    /// </summary>
    public sealed class RaycastDetectionProvider : MonoBehaviour, IXRDetectionProvider
    {
        [Header("Ray")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField, Min(0.1f)] private float maxDistance = 8f;
        [SerializeField] private LayerMask detectionLayers = ~0;

        [Header("Output")]
        [SerializeField] private string providerId = "raycast";
        [SerializeField] private string requiredTag = string.Empty;
        [SerializeField] private bool useObjectNameAsLabel = true;
        [SerializeField] private string fallbackLabel = "raycast.hit";
        [SerializeField] private bool normalizeLabelToLower = true;

        private readonly List<DetectionResult> _buffer = new(1);

        public IEnumerable<DetectionResult> GetDetections()
        {
            _buffer.Clear();

            Transform origin = ResolveOrigin();
            if (origin == null)
            {
                return _buffer;
            }

            if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, maxDistance, detectionLayers))
            {
                return _buffer;
            }

            if (!string.IsNullOrWhiteSpace(requiredTag) && !hit.collider.CompareTag(requiredTag))
            {
                return _buffer;
            }

            string label = ResolveLabel(hit.collider);
            var bbox = new Rect(hit.point.x, hit.point.z, 0.1f, 0.1f);
            var detection = new Detection(label, 1f, bbox, Time.time);
            _buffer.Add(new DetectionResult(providerId, detection));
            return _buffer;
        }

        private Transform ResolveOrigin()
        {
            if (rayOrigin != null)
            {
                return rayOrigin;
            }

            if (Camera.main != null)
            {
                return Camera.main.transform;
            }

            return transform;
        }

        private string ResolveLabel(Collider collider)
        {
            string label = useObjectNameAsLabel && collider != null
                ? collider.name
                : fallbackLabel;

            if (string.IsNullOrWhiteSpace(label))
            {
                label = "raycast.hit";
            }

            return normalizeLabelToLower ? label.ToLowerInvariant() : label;
        }
    }
}
