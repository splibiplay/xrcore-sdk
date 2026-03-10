using System.Collections.Generic;
using UnityEngine;

namespace XRCore.Tasks
{
    [CreateAssetMenu(menuName = "XRCore/Tasks/Task Definition", fileName = "XRTaskDefinition")]
    public sealed class XRTaskDefinition : ScriptableObject
    {
        [SerializeField] private string taskId = "task_01";
        [SerializeField] private string displayName = "XR Task";
        [SerializeField] private List<XRTaskStep> steps = new();

        public string TaskId => taskId;
        public string DisplayName => displayName;
        public IReadOnlyList<XRTaskStep> Steps => steps;
    }
}
