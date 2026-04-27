using UnityEngine;

namespace XRCore.Agents
{
    public abstract class XRGuideAgentBehaviour : ScriptableObject
    {
        [SerializeField] private int priority = 0;
        [SerializeField, Min(0f)] private float cooldownSeconds = 0f;

        public int Priority => priority;
        public float CooldownSeconds => cooldownSeconds;

        public abstract bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            out AgentInstructionEvent instruction);
    }
}
