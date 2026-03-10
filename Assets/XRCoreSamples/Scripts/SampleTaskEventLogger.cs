using UnityEngine;
using XRCore.Core;
using XRCore.Tasks;

namespace XRCore.Samples
{
    public sealed class SampleTaskEventLogger : MonoBehaviour
    {
        private void OnEnable()
        {
            XRCoreEventBus.Subscribe<XRTaskStartedEvent>(OnTaskStarted);
            XRCoreEventBus.Subscribe<XRTaskStepChangedEvent>(OnStepChanged);
            XRCoreEventBus.Subscribe<XRTaskCompletedEvent>(OnTaskCompleted);
        }

        private void OnDisable()
        {
            XRCoreEventBus.Unsubscribe<XRTaskStartedEvent>(OnTaskStarted);
            XRCoreEventBus.Unsubscribe<XRTaskStepChangedEvent>(OnStepChanged);
            XRCoreEventBus.Unsubscribe<XRTaskCompletedEvent>(OnTaskCompleted);
        }

        private static void OnTaskStarted(XRTaskStartedEvent evt)
        {
            Debug.Log($"[SampleTaskEventLogger] Task started: {evt.TaskDefinition?.name}");
        }

        private static void OnStepChanged(XRTaskStepChangedEvent evt)
        {
            Debug.Log($"[SampleTaskEventLogger] Step: {evt.StepIndex + 1} - {evt.Step?.Title}");
        }

        private static void OnTaskCompleted(XRTaskCompletedEvent evt)
        {
            Debug.Log($"[SampleTaskEventLogger] Task completed: {evt.TaskDefinition?.name}");
        }
    }
}
