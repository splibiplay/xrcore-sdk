using System;
using UnityEngine;
using XRCore.Agents;

namespace XRCore.Samples
{
    [CreateAssetMenu(menuName = "XRCore/Samples/Demo/XR Demo Flow Behaviour", fileName = "XRDemoFlowBehaviour")]
    public sealed class XRDemoFlowBehaviour : XRGuideAgentBehaviour
    {
        [SerializeField] private XRGuideInstructionChannel channel = XRGuideInstructionChannel.Text;
        [SerializeField] private string taskId = "demo.task.vision_assistant";
        [SerializeField] private bool requireMatchingTaskId = true;
        [SerializeField] private string lookAtMessage = "Look at the cube.";
        [SerializeField] private string lookAwayMessage = "You are looking at the cube. Look away.";

        public override bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            out AgentInstructionEvent instruction)
        {
            instruction = default;

            if (requireMatchingTaskId && !IsTaskMatch(context))
            {
                return false;
            }

            if (trigger == XRGuideAgent.TriggerTaskStepChanged)
            {
                instruction = new AgentInstructionEvent(lookAtMessage, channel, trigger, Time.time);
                return true;
            }

            if (trigger == XRGuideAgent.TriggerTaskCompleted)
            {
                instruction = new AgentInstructionEvent(lookAwayMessage, channel, trigger, Time.time);
                return true;
            }

            return false;
        }

        private bool IsTaskMatch(XRCoreContextSnapshot context)
        {
            if (context.CurrentTaskDefinition == null || string.IsNullOrWhiteSpace(taskId))
            {
                return false;
            }

            return string.Equals(context.CurrentTaskDefinition.TaskId, taskId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
