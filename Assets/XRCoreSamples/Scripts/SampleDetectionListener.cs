using System.Text;
using UnityEngine;
using XRCore.Core;
using XRCore.Vision;

namespace XRCore.Samples
{
    public sealed class SampleDetectionListener : MonoBehaviour
    {
        [SerializeField] private bool logEachUpdate = true;
        [SerializeField] private int maxLabelsToPrint = 5;

        private readonly StringBuilder _builder = new(256);

        private void OnEnable()
        {
            XRCoreEventBus.Subscribe<XRDetectionEvent>(OnDetectionEvent);
        }

        private void OnDisable()
        {
            XRCoreEventBus.Unsubscribe<XRDetectionEvent>(OnDetectionEvent);
        }

        private void OnDetectionEvent(XRDetectionEvent evt)
        {
            if (!logEachUpdate || evt.Detections == null || evt.Count <= 0)
            {
                return;
            }

            _builder.Clear();
            _builder.Append("[SampleDetectionListener] source=");
            _builder.Append(evt.Source);
            _builder.Append(" count=");
            _builder.Append(evt.Count);
            _builder.Append(" labels=[");

            int limit = Mathf.Min(maxLabelsToPrint, evt.Count);
            for (int i = 0; i < limit; i++)
            {
                if (i > 0)
                {
                    _builder.Append(", ");
                }

                _builder.Append(evt.Detections[i].Label);
            }

            _builder.Append(']');
            Debug.Log(_builder.ToString());
        }
    }
}
