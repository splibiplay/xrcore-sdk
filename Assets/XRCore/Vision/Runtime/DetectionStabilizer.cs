using System.Collections.Generic;
using UnityEngine;

namespace XRCore.Vision
{
    public sealed class DetectionStabilizer : MonoBehaviour
    {
        [Header("Stabilization")]
        [SerializeField] private float windowSeconds = 0.75f;
        [SerializeField] private int minHitsInWindow = 3;
        [SerializeField] private float minConfidence = 0.5f;

        private readonly Dictionary<string, List<float>> _hits = new();
        private readonly Dictionary<string, DetectionResult> _lastByLabel = new();
        private readonly Dictionary<string, float> _lastSeen = new();

        public void PushDetections(IEnumerable<DetectionResult> detections)
        {
            float now = Time.time;
            if (detections != null)
            {
                foreach (var result in detections)
                {
                    if (result.Confidence < minConfidence || string.IsNullOrWhiteSpace(result.Label))
                    {
                        continue;
                    }

                    string label = result.Label;
                    if (!_hits.TryGetValue(label, out var list))
                    {
                        list = new List<float>(8);
                        _hits[label] = list;
                    }

                    list.Add(now);
                    _lastByLabel[label] = result;
                    _lastSeen[label] = now;
                }
            }

            Cleanup(now);
        }

        public bool TryGetLastDetection(string label, out DetectionResult detection)
        {
            return _lastByLabel.TryGetValue(label, out detection);
        }

        public bool TryGetLastBoundingBox(string label, out Rect boundingBox)
        {
            if (_lastByLabel.TryGetValue(label, out var detection))
            {
                boundingBox = detection.BoundingBox;
                return true;
            }

            boundingBox = default;
            return false;
        }

        public bool WasSeenRecently(string label, float graceSeconds)
        {
            return _lastSeen.TryGetValue(label, out var last) && (Time.time - last) <= graceSeconds;
        }

        public HashSet<string> GetStablePresentLabels()
        {
            var present = new HashSet<string>();
            foreach (var pair in _hits)
            {
                if (pair.Value.Count >= minHitsInWindow)
                {
                    present.Add(pair.Key);
                }
            }

            return present;
        }

        private void Cleanup(float now)
        {
            var keys = ListPool<string>.Get();
            keys.AddRange(_hits.Keys);

            float keepSeconds = windowSeconds + 1f;
            for (int i = 0; i < keys.Count; i++)
            {
                string label = keys[i];
                var hitList = _hits[label];
                hitList.RemoveAll(t => now - t > windowSeconds);
                if (hitList.Count == 0)
                {
                    _hits.Remove(label);
                }

                if (_lastSeen.TryGetValue(label, out var last) && now - last > keepSeconds)
                {
                    _lastSeen.Remove(label);
                    _lastByLabel.Remove(label);
                }
            }

            ListPool<string>.Release(keys);
        }
    }

    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new();

        public static List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>(16);
        }

        public static void Release(List<T> list)
        {
            list.Clear();
            Pool.Push(list);
        }
    }
}
