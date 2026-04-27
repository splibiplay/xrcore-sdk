namespace XRCore.Tasks
{
    public readonly struct XRTaskStartedEvent
    {
        public readonly XRTaskDefinition TaskDefinition;

        public XRTaskStartedEvent(XRTaskDefinition taskDefinition)
        {
            TaskDefinition = taskDefinition;
        }
    }

    public readonly struct XRTaskStepChangedEvent
    {
        public readonly XRTaskDefinition TaskDefinition;
        public readonly int StepIndex;
        public readonly XRTaskStep Step;

        public XRTaskStepChangedEvent(XRTaskDefinition taskDefinition, int stepIndex, XRTaskStep step)
        {
            TaskDefinition = taskDefinition;
            StepIndex = stepIndex;
            Step = step;
        }
    }

    public readonly struct XRTaskCompletedEvent
    {
        public readonly XRTaskDefinition TaskDefinition;

        public XRTaskCompletedEvent(XRTaskDefinition taskDefinition)
        {
            TaskDefinition = taskDefinition;
        }
    }
}
