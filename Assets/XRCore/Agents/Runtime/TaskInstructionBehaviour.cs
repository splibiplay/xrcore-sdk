using UnityEngine;

namespace XRCore.Agents
{
    [CreateAssetMenu(menuName = "XRCore/Agents/Behaviours/Task Instruction", fileName = "TaskInstructionBehaviour")]
    public sealed class TaskInstructionBehaviour : XRGuideAgentBehaviour
    {
        [SerializeField] private XRGuideInstructionChannel channel = XRGuideInstructionChannel.Text;

        public override bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            out AgentInstructionEvent instruction)
        {
            instruction = default;
            if (trigger != XRGuideAgent.TriggerTaskStepChanged || context.CurrentStep == null)
            {
                return false;
            }

            string message = string.IsNullOrWhiteSpace(context.CurrentStep.Instruction)
                ? $"Paso {context.CurrentStepIndex + 1}"
                : context.CurrentStep.Instruction;

            instruction = new AgentInstructionEvent(message, channel, trigger, Time.time);
            return true;
        }
    }
}
