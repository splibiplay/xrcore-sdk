using UnityEngine;

namespace XRCore.Agents
{
    [CreateAssetMenu(menuName = "XRCore/Agents/Behaviours/Detection Comment", fileName = "DetectionCommentBehaviour")]
    public sealed class DetectionCommentBehaviour : XRGuideAgentBehaviour
    {
        [SerializeField] private XRGuideInstructionChannel channel = XRGuideInstructionChannel.Text;
        [SerializeField] private string noDetectionMessage = "No detecto objetos en este momento.";
        [SerializeField] private string detectionMessageTemplate = "He detectado {0} objetos.";

        public override bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            out AgentInstructionEvent instruction)
        {
            instruction = default;
            if (trigger != XRGuideAgent.TriggerDetectionUpdated)
            {
                return false;
            }

            int count = context.LastDetections.Count;
            string message = count <= 0
                ? noDetectionMessage
                : string.Format(detectionMessageTemplate, count);

            instruction = new AgentInstructionEvent(message, channel, trigger, Time.time);
            return true;
        }
    }
}
