using System;
using UnityEngine;

namespace XRCore.Vision
{
    [Serializable]
    public struct Detection
    {
        [SerializeField] private string label;
        [SerializeField] private float confidence;
        [SerializeField] private Rect boundingBox;
        [SerializeField] private float timestamp;

        public string Label => label;
        public float Confidence => confidence;
        public Rect BoundingBox => boundingBox;
        public float Timestamp => timestamp;

        public Detection(string label, float confidence, Rect boundingBox, float timestamp)
        {
            this.label = label;
            this.confidence = confidence;
            this.boundingBox = boundingBox;
            this.timestamp = timestamp;
        }
    }
}
