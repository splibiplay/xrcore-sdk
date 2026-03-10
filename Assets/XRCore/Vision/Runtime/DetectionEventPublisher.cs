using System;
using System.Collections.Generic;
using UnityEngine;
using XRCore.Core;

namespace XRCore.Vision
{
    /// <summary>
    /// Publica detecciones de Vision al XRCoreEventBus.
    /// </summary>
    public sealed class DetectionEventPublisher : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerBehaviour;
        [SerializeField] private bool publishOnUpdate = true;
        [SerializeField] private string source = "vision";

        private IXRDetectionProvider _provider;
        private readonly List<DetectionResult> _buffer = new(64);
        private DetectionResult[] _eventArray = Array.Empty<DetectionResult>();

        private void Awake()
        {
            _provider = providerBehaviour as IXRDetectionProvider;
            if (_provider == null && providerBehaviour != null)
            {
                Debug.LogError("[DetectionEventPublisher] providerBehaviour must implement IXRDetectionProvider.");
            }
        }

        private void Update()
        {
            if (publishOnUpdate)
            {
                PublishDetections();
            }
        }

        public void PublishDetections()
        {
            if (_provider == null)
            {
                return;
            }

            _buffer.Clear();
            foreach (var detection in _provider.GetDetections())
            {
                _buffer.Add(detection);
            }

            EnsureEventArrayCapacity(_buffer.Count);
            for (int i = 0; i < _buffer.Count; i++)
            {
                _eventArray[i] = _buffer[i];
            }

            string eventSource = ResolveSource();
            XRCoreEventBus.Publish(new XRDetectionEvent(_eventArray, _buffer.Count, Time.time, eventSource));
        }

        private void EnsureEventArrayCapacity(int count)
        {
            if (_eventArray.Length >= count)
            {
                return;
            }

            int newSize = Mathf.NextPowerOfTwo(Mathf.Max(4, count));
            _eventArray = new DetectionResult[newSize];
        }

        private string ResolveSource()
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                return source;
            }

            if (_buffer.Count > 0 && !string.IsNullOrWhiteSpace(_buffer[0].ProviderId))
            {
                return _buffer[0].ProviderId;
            }

            return "vision";
        }
    }
}
