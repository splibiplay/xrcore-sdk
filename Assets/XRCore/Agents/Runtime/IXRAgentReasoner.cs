namespace XRCore.Agents
{
    public interface IXRAgentReasoner
    {
        bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            float now,
            out AgentInstructionEvent instruction,
            out string decisionSource);
    }
}
