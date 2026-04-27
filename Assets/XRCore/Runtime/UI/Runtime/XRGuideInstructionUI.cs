using UnityEngine;
using XRCore.Agents;

namespace XRCore.UI
{
    /// <summary>
    /// Renderer simple de instrucciones para demo (overlay OnGUI).
    /// </summary>
    public sealed class XRGuideInstructionUI : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;
        [SerializeField, Min(0.1f)] private float visibleSeconds = 3f;
        [SerializeField] private Rect overlayRect = new Rect(20f, 20f, 560f, 90f);

        private string _currentMessage = string.Empty;
        private float _hideAtTime;

        public void Present(AgentInstructionEvent instruction)
        {
            _currentMessage = instruction.Message ?? string.Empty;
            _hideAtTime = Time.time + visibleSeconds;
        }

        private void OnGUI()
        {
            if (!showOverlay || string.IsNullOrWhiteSpace(_currentMessage) || Time.time > _hideAtTime)
            {
                return;
            }

            GUI.Box(overlayRect, $"Agent: {_currentMessage}");
        }
    }
}
