using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRCore.Agents
{
    [CreateAssetMenu(menuName = "XRCore/Agents/Reasoners/State Machine", fileName = "StateMachineReasoner")]
    public sealed class StateMachineReasoner : XRGuideReasonerBase
    {
        [Serializable]
        private struct Transition
        {
            public string stateId;
            public string trigger;
            public string message;
            public XRGuideInstructionChannel channel;
            public string nextStateId;
        }

        [SerializeField] private string initialStateId = "default";
        [SerializeField] private List<Transition> transitions = new();

        private string _currentStateId;

        public override bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            float now,
            out AgentInstructionEvent instruction,
            out string decisionSource)
        {
            instruction = default;
            decisionSource = string.Empty;

            if (string.IsNullOrWhiteSpace(_currentStateId))
            {
                _currentStateId = initialStateId;
            }

            for (int i = 0; i < transitions.Count; i++)
            {
                Transition t = transitions[i];
                if (!string.Equals(t.stateId, _currentStateId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(t.trigger, trigger, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(t.message))
                {
                    return false;
                }

                instruction = new AgentInstructionEvent(t.message, t.channel, trigger, now);
                string fromState = _currentStateId;
                _currentStateId = string.IsNullOrWhiteSpace(t.nextStateId) ? _currentStateId : t.nextStateId;
                decisionSource = $"state:{fromState}->{_currentStateId}";
                return true;
            }

            return false;
        }
    }
}
