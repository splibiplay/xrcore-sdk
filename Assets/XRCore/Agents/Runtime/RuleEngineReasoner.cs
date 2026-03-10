using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XRCore.Agents
{
    [CreateAssetMenu(menuName = "XRCore/Agents/Reasoners/Rule Engine", fileName = "RuleEngineReasoner")]
    public sealed class RuleEngineReasoner : XRGuideReasonerBase
    {
        [SerializeField] private List<XRGuideAgentBehaviour> behaviours = new();

        private readonly Dictionary<XRGuideAgentBehaviour, float> _lastExecutionByBehaviour = new();

        public override bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            float now,
            out AgentInstructionEvent instruction,
            out string decisionSource)
        {
            instruction = default;
            decisionSource = string.Empty;

            if (behaviours == null || behaviours.Count == 0)
            {
                return false;
            }

            var ordered = behaviours
                .Where(b => b != null)
                .OrderByDescending(b => b.Priority);

            foreach (var behaviour in ordered)
            {
                if (IsInCooldown(behaviour, now))
                {
                    continue;
                }

                if (!behaviour.TryCreateInstruction(context, trigger, out instruction))
                {
                    continue;
                }

                _lastExecutionByBehaviour[behaviour] = now;
                decisionSource = behaviour.name;
                return true;
            }

            instruction = default;
            decisionSource = string.Empty;
            return false;
        }

        private bool IsInCooldown(XRGuideAgentBehaviour behaviour, float now)
        {
            if (behaviour.CooldownSeconds <= 0f)
            {
                return false;
            }

            if (!_lastExecutionByBehaviour.TryGetValue(behaviour, out float lastTime))
            {
                return false;
            }

            return now - lastTime < behaviour.CooldownSeconds;
        }
    }
}
