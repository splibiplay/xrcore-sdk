using System.Collections.Generic;

namespace XRCore.Vision
{
    public interface IXRDetectionProvider
    {
        IEnumerable<DetectionResult> GetDetections();
    }

    // Backward compatibility for existing providers/components.
    public interface IDetectionProvider : IXRDetectionProvider
    {
    }
}
