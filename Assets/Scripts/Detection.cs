using UnityEngine;

public struct Detection
{
    public string label;
    public float confidence;
    public Rect bbox;
    public float time;

    public Detection(string label, float confidence, Rect bbox, float time)
    {
        this.label = label;
        this.confidence = confidence;
        this.bbox = bbox;
        this.time = time;
    }
}

