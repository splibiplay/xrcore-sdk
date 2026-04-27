using UnityEngine;

namespace XRCore.Agents
{
    public abstract class XRGuideReasonerBase : ScriptableObject, IXRAgentReasoner
    {
        public abstract bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            float now,
            out AgentInstructionEvent instruction,
            out string decisionSource);
    }
}
