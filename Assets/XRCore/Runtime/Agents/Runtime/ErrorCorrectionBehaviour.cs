using UnityEngine;

namespace XRCore.Agents
{
    [CreateAssetMenu(menuName = "XRCore/Agents/Behaviours/Error Correction", fileName = "ErrorCorrectionBehaviour")]
    public sealed class ErrorCorrectionBehaviour : XRGuideAgentBehaviour
    {
        [SerializeField] private XRGuideInstructionChannel channel = XRGuideInstructionChannel.Text;
        [SerializeField] private string signalPrefix = "error.";
        [SerializeField] private string correctionMessage = "Revisa el paso actual y corrige la accion.";

        public override bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            out AgentInstructionEvent instruction)
        {
            instruction = default;
            if (trigger != XRGuideAgent.TriggerSignalReceived)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(context.LastSignal) ||
                !context.LastSignal.StartsWith(signalPrefix))
            {
                return false;
            }

            instruction = new AgentInstructionEvent(correctionMessage, channel, trigger, Time.time);
            return true;
        }
    }
}
