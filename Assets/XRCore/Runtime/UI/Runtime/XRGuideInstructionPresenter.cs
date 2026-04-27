using UnityEngine;
using XRCore.Agents;
using XRCore.Core;

namespace XRCore.UI
{
    /// <summary>
    /// Presenter principal que recibe instrucciones del agente y las enruta a renderers UI/audio.
    /// </summary>
    public sealed class XRGuideInstructionPresenter : MonoBehaviour
    {
        [SerializeField] private XRGuideInstructionUI instructionUI;
        [SerializeField] private XRGuideInstructionAudio instructionAudio;
        [SerializeField] private bool logReceivedInstructions = false;

        public void ApplySettings(XRCoreSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            logReceivedInstructions = settings.PresenterLogReceivedInstructions;
        }

        private void OnEnable()
        {
            XRCoreEventBus.Subscribe<AgentInstructionEvent>(HandleInstruction);
        }

        private void OnDisable()
        {
            XRCoreEventBus.Unsubscribe<AgentInstructionEvent>(HandleInstruction);
        }

        private void HandleInstruction(AgentInstructionEvent instruction)
        {
            if (logReceivedInstructions)
            {
                XRCoreDebug.Log($"[InstructionPresenter] {instruction.Channel} -> {instruction.Message}");
            }

            instructionUI?.Present(instruction);
            instructionAudio?.Present(instruction);
        }
    }
}
