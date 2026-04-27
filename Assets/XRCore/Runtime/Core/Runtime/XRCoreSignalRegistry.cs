using System;
using System.Collections.Generic;

namespace XRCore.Core
{
    /// <summary>
    /// Registro opcional de senales conocidas para evitar errores tipograficos.
    /// </summary>
    public static class XRCoreSignalRegistry
    {
        public const string TrainingStep1 = "training.step1";
        public const string TrainingStep2 = "training.step2";
        public const string VisionDetectObjetoA = "vision.detect.objeto_a";

        private static readonly HashSet<string> KnownSignals = new(StringComparer.OrdinalIgnoreCase)
        {
            TrainingStep1,
            TrainingStep2,
            VisionDetectObjetoA
        };

        public static bool IsKnown(string signal)
        {
            return !string.IsNullOrWhiteSpace(signal) && KnownSignals.Contains(signal);
        }

        public static void Register(string signal)
        {
            if (!string.IsNullOrWhiteSpace(signal))
            {
                KnownSignals.Add(signal);
            }
        }

        public static IReadOnlyCollection<string> GetKnownSignals()
        {
            return KnownSignals;
        }
    }
}
