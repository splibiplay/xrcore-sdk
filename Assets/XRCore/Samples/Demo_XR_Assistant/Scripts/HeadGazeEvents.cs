public readonly struct HeadGazeTargetEnteredEvent
{
    public readonly string TargetObjectName;
    public readonly float Timestamp;

    public HeadGazeTargetEnteredEvent(string targetObjectName, float timestamp)
    {
        TargetObjectName = targetObjectName;
        Timestamp = timestamp;
    }
}
