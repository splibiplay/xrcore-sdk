using System;

namespace XRCore.Vision
{
    /// <summary>
    /// Evento global de deteccion para el bus del framework.
    /// </summary>
    [Serializable]
    public readonly struct XRDetectionEvent
    {
        public readonly DetectionResult[] Detections;
        public readonly int Count;
        public readonly float Timestamp;
        public readonly string Source;

        public XRDetectionEvent(DetectionResult[] detections, int count, float timestamp, string source)
        {
            Detections = detections;
            Count = count;
            Timestamp = timestamp;
            Source = source;
        }
    }
}
