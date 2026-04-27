using System;
using UnityEngine;

namespace XRCore.Tasks
{
    [Serializable]
    public sealed class XRTaskStep
    {
        [SerializeField] private string id = "step_01";
        [SerializeField] private string title = "Step";
        [SerializeField, TextArea(2, 4)] private string instruction = "Do action";
        [SerializeField] private float minDurationSeconds = 0f;
        [SerializeField] private bool requiresManualConfirm = true;
        [SerializeField] private string expectedSignal = "";

        public string Id => id;
        public string Title => title;
        public string Instruction => instruction;
        public float MinDurationSeconds => Mathf.Max(0f, minDurationSeconds);
        public bool RequiresManualConfirm => requiresManualConfirm;
        public string ExpectedSignal => expectedSignal;
    }
}
