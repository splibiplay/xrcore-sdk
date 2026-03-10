using System.Collections.Generic;
using UnityEngine;

public class DetectionStabilizer : MonoBehaviour
{
    [Header("Stabilization")]
    [SerializeField] private float windowSeconds = 0.75f;
    [SerializeField] private int minHitsInWindow = 3;
    [SerializeField] private float minConfidence = 0.5f;

    // label -> list of detection times (within window)
    private readonly Dictionary<string, List<float>> _hits = new();
    private readonly Dictionary<string, Rect> _lastBbox = new();
    private readonly Dictionary<string, float> _lastSeen = new();

    public void PushDetections(List<Detection> detections)
    {
        float now = Time.time;

        // 1) Add hits
        for (int i = 0; i < detections.Count; i++)
        {
            var d = detections[i];
            if (d.confidence < minConfidence) continue;

            if (!_hits.TryGetValue(d.label, out var list))
            {
                list = new List<float>(8);
                _hits[d.label] = list;
            }
            list.Add(now);
            _lastBbox[d.label] = d.bbox;
            _lastSeen[d.label] = now;
        }

        // 2) Cleanup old hits + TTL para bbox/lastSeen
        var keys = ListPool<string>.Get();
        keys.AddRange(_hits.Keys);

        float bboxTtlSeconds = windowSeconds + 1.0f; // 1s extra de gracia para bbox

        for (int k = 0; k < keys.Count; k++)
        {
            string label = keys[k];
            var list = _hits[label];
            list.RemoveAll(t => now - t > windowSeconds);

            if (list.Count == 0)
            {
                // OJO: quitamos hits, pero mantenemos bbox un poco más (TTL) para tolerar pérdidas al coger
                _hits.Remove(label);
            }

            // Limpieza de bbox/seen por TTL (independiente de hits)
            if (_lastSeen.TryGetValue(label, out var last))
            {
                if (now - last > bboxTtlSeconds)
                {
                    _lastSeen.Remove(label);
                    _lastBbox.Remove(label);
                }
            }
            else
            {
                // si no hay lastSeen, aseguramos limpieza
                _lastBbox.Remove(label);
            }
        }

        ListPool<string>.Release(keys);
    }

    public bool TryGetLastBbox(string label, out Rect bbox)
    {
        return _lastBbox.TryGetValue(label, out bbox);
    }

    public bool WasSeenRecently(string label, float graceSeconds)
    {
        float now = Time.time;
        return _lastSeen.TryGetValue(label, out var t) && (now - t) <= graceSeconds;
    }

    public HashSet<string> GetStablePresentLabels()
    {
        var present = new HashSet<string>();
        foreach (var kvp in _hits)
        {
            if (kvp.Value.Count >= minHitsInWindow)
                present.Add(kvp.Key);
        }
        return present;
    }
}

/// <summary> Pool simple para evitar GC en Quest. </summary>
static class ListPool<T>
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

