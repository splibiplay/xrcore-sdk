using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRCore.Agents
{
    [CreateAssetMenu(menuName = "XRCore/Agents/Reasoners/Local LLM", fileName = "LocalLlmReasoner")]
    public sealed class LocalLlmReasoner : XRGuideReasonerBase
    {
        [Serializable]
        private struct TriggerResponse
        {
            public string trigger;
            public string message;
            public XRGuideInstructionChannel channel;
        }

        [SerializeField] private string modelName = "local-llm";
        [SerializeField] private bool enabledReasoning = true;
        [SerializeField] private List<TriggerResponse> responses = new();

        public override bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            float now,
            out AgentInstructionEvent instruction,
            out string decisionSource)
        {
            instruction = default;
            decisionSource = string.Empty;

            if (!enabledReasoning)
            {
                return false;
            }

            for (int i = 0; i < responses.Count; i++)
            {
                TriggerResponse response = responses[i];
                if (!string.Equals(response.trigger, trigger, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(response.message))
                {
                    return false;
                }

                instruction = new AgentInstructionEvent(response.message, response.channel, trigger, now);
                decisionSource = $"local-llm:{modelName}";
                return true;
            }

            return false;
        }
    }
}
