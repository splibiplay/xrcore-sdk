namespace XRCore.Agents
{
    public enum XRGuideInstructionChannel
    {
        Text = 0,
        Voice = 1,
        Animation = 2
    }

    public readonly struct AgentInstructionEvent
    {
        public readonly string Message;
        public readonly XRGuideInstructionChannel Channel;
        public readonly string Trigger;
        public readonly float Timestamp;

        public AgentInstructionEvent(string message, XRGuideInstructionChannel channel, string trigger, float timestamp)
        {
            Message = message;
            Channel = channel;
            Trigger = trigger;
            Timestamp = timestamp;
        }
    }

    public readonly struct XRGuideBehaviourExecutedEvent
    {
        public readonly string BehaviourName;
        public readonly int BehaviourPriority;
        public readonly string Trigger;
        public readonly string InstructionMessage;
        public readonly float Timestamp;

        public XRGuideBehaviourExecutedEvent(
            string behaviourName,
            int behaviourPriority,
            string trigger,
            string instructionMessage,
            float timestamp)
        {
            BehaviourName = behaviourName;
            BehaviourPriority = behaviourPriority;
            Trigger = trigger;
            InstructionMessage = instructionMessage;
            Timestamp = timestamp;
        }
    }

    public readonly struct XRGuideBehaviourEvaluationEvent
    {
        public readonly string Trigger;
        public readonly int EvaluatedBehaviours;
        public readonly string SelectedBehaviour;
        public readonly float Timestamp;

        public XRGuideBehaviourEvaluationEvent(
            string trigger,
            int evaluatedBehaviours,
            string selectedBehaviour,
            float timestamp)
        {
            Trigger = trigger;
            EvaluatedBehaviours = evaluatedBehaviours;
            SelectedBehaviour = selectedBehaviour;
            Timestamp = timestamp;
        }
    }

    public readonly struct XRGuideAgentTickEvent
    {
        public readonly float Timestamp;
        public readonly float DeltaSeconds;
        public readonly int PendingTriggers;

        public XRGuideAgentTickEvent(float timestamp, float deltaSeconds, int pendingTriggers)
        {
            Timestamp = timestamp;
            DeltaSeconds = deltaSeconds;
            PendingTriggers = pendingTriggers;
        }
    }
}
