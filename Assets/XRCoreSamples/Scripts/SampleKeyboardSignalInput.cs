using UnityEngine;
using XRCore.Core;
using XRCore.Interaction;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace XRCore.Samples
{
    /// <summary>
    /// Helper de teclado para probar senales de interaccion en editor.
    /// </summary>
    public sealed class SampleKeyboardSignalInput : MonoBehaviour
    {
        [SerializeField] private XRInteractionSignalEmitter emitter;
        [SerializeField] private KeyCode firstKey = KeyCode.Alpha1;
        [SerializeField] private string firstSignal = XRCoreSignalRegistry.TrainingStep1;
        [SerializeField] private KeyCode secondKey = KeyCode.Alpha2;
        [SerializeField] private string secondSignal = XRCoreSignalRegistry.TrainingStep2;

        private void Update()
        {
            if (emitter == null)
            {
                return;
            }

            if (IsKeyPressedThisFrame(firstKey) && !string.IsNullOrWhiteSpace(firstSignal))
            {
                emitter.RaiseSignal(firstSignal);
            }

            if (IsKeyPressedThisFrame(secondKey) && !string.IsNullOrWhiteSpace(secondSignal))
            {
                emitter.RaiseSignal(secondSignal);
            }
        }

        private static bool IsKeyPressedThisFrame(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && TryConvertToInputSystemKey(keyCode, out var inputKey))
            {
                return keyboard[inputKey].wasPressedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(keyCode);
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryConvertToInputSystemKey(KeyCode keyCode, out Key key)
        {
            switch (keyCode)
            {
                case KeyCode.Alpha0: key = Key.Digit0; return true;
                case KeyCode.Alpha1: key = Key.Digit1; return true;
                case KeyCode.Alpha2: key = Key.Digit2; return true;
                case KeyCode.Alpha3: key = Key.Digit3; return true;
                case KeyCode.Alpha4: key = Key.Digit4; return true;
                case KeyCode.Alpha5: key = Key.Digit5; return true;
                case KeyCode.Alpha6: key = Key.Digit6; return true;
                case KeyCode.Alpha7: key = Key.Digit7; return true;
                case KeyCode.Alpha8: key = Key.Digit8; return true;
                case KeyCode.Alpha9: key = Key.Digit9; return true;
                case KeyCode.Space: key = Key.Space; return true;
                case KeyCode.Return: key = Key.Enter; return true;
                case KeyCode.Escape: key = Key.Escape; return true;
                default:
                    key = default;
                    return false;
            }
        }
#endif
    }
}
