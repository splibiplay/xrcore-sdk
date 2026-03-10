using System;
using UnityEngine;
using XRCore.Core;

namespace XRCore.Tasks
{
    /// <summary>
    /// Runner generico para tareas XR paso a paso.
    /// </summary>
    public sealed class XRTaskRunner : MonoBehaviour
    {
        [SerializeField] private XRTaskDefinition taskDefinition;
        [SerializeField] private bool autoStartOnEnable = false;
        [SerializeField] private bool logState = false;
        [SerializeField] private bool listenSignalsFromEventBus = true;
        [SerializeField] private bool publishLifecycleToEventBus = true;

        public event Action<XRTaskDefinition> TaskStarted;
        public event Action<int, XRTaskStep> StepChanged;
        public event Action<XRTaskDefinition> TaskCompleted;

        public XRTaskDefinition TaskDefinition => taskDefinition;
        public bool IsRunning { get; private set; }
        public int CurrentStepIndex { get; private set; } = -1;
        public XRTaskStep CurrentStep => TryGetStep(CurrentStepIndex, out var step) ? step : null;

        private float _stepStartTime;

        private void OnEnable()
        {
            if (listenSignalsFromEventBus)
            {
                XRCoreEventBus.Subscribe<XRSignalEvent>(HandleSignalEvent);
            }

            if (autoStartOnEnable)
            {
                StartTask();
            }
        }

        private void OnDisable()
        {
            if (listenSignalsFromEventBus)
            {
                XRCoreEventBus.Unsubscribe<XRSignalEvent>(HandleSignalEvent);
            }
        }

        public void StartTask()
        {
            if (taskDefinition == null || taskDefinition.Steps.Count == 0)
            {
                if (logState)
                {
                    Debug.LogWarning("[XRTaskRunner] TaskDefinition null or empty.");
                }
                return;
            }

            IsRunning = true;
            CurrentStepIndex = 0;
            _stepStartTime = Time.time;

            TaskStarted?.Invoke(taskDefinition);
            if (publishLifecycleToEventBus)
            {
                XRCoreEventBus.Publish(new XRTaskStartedEvent(taskDefinition));
            }
            NotifyStepChanged();
        }

        public void StopTask()
        {
            IsRunning = false;
            CurrentStepIndex = -1;
        }

        public void CompleteCurrentStep()
        {
            if (!IsRunning || CurrentStep == null)
            {
                return;
            }

            if (!CanAdvanceByTime(CurrentStep))
            {
                return;
            }

            AdvanceStep();
        }

        /// <summary>
        /// Completa el paso actual si la senal coincide con el paso esperado.
        /// </summary>
        public void PushSignal(string signal)
        {
            if (!IsRunning || CurrentStep == null || string.IsNullOrWhiteSpace(signal))
            {
                return;
            }

            if (CurrentStep.RequiresManualConfirm)
            {
                return;
            }

            if (!string.Equals(CurrentStep.ExpectedSignal, signal, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!CanAdvanceByTime(CurrentStep))
            {
                return;
            }

            AdvanceStep();
        }

        private bool CanAdvanceByTime(XRTaskStep step)
        {
            return Time.time - _stepStartTime >= step.MinDurationSeconds;
        }

        private void AdvanceStep()
        {
            CurrentStepIndex++;

            if (!TryGetStep(CurrentStepIndex, out _))
            {
                var completedTask = taskDefinition;
                StopTask();
                TaskCompleted?.Invoke(completedTask);
                if (publishLifecycleToEventBus)
                {
                    XRCoreEventBus.Publish(new XRTaskCompletedEvent(completedTask));
                }
                return;
            }

            _stepStartTime = Time.time;
            NotifyStepChanged();
        }

        private void NotifyStepChanged()
        {
            if (TryGetStep(CurrentStepIndex, out var step))
            {
                if (logState)
                {
                    Debug.Log($"[XRTaskRunner] Step {CurrentStepIndex + 1}/{taskDefinition.Steps.Count}: {step.Title}");
                }

                StepChanged?.Invoke(CurrentStepIndex, step);
                if (publishLifecycleToEventBus)
                {
                    XRCoreEventBus.Publish(new XRTaskStepChangedEvent(taskDefinition, CurrentStepIndex, step));
                }
            }
        }

        private void HandleSignalEvent(XRSignalEvent evt)
        {
            PushSignal(evt.Signal);
        }

        private bool TryGetStep(int index, out XRTaskStep step)
        {
            step = null;

            if (taskDefinition == null || index < 0 || index >= taskDefinition.Steps.Count)
            {
                return false;
            }

            step = taskDefinition.Steps[index];
            return step != null;
        }
    }
}
