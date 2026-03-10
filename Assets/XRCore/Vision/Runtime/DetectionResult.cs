using System;
using UnityEngine;

namespace XRCore.Vision
{
    [Serializable]
    public struct DetectionResult
    {
        [SerializeField] private string providerId;
        [SerializeField] private Detection detection;

        public string ProviderId => providerId;
        public Detection Detection => detection;
        public string Label => detection.Label;
        public float Confidence => detection.Confidence;
        public Rect BoundingBox => detection.BoundingBox;
        public float Timestamp => detection.Timestamp;

        public DetectionResult(string providerId, Detection detection)
        {
            this.providerId = providerId;
            this.detection = detection;
        }
    }
}
