using System.Collections.Generic;

public interface IDetectionProvider
{
    /// <summary> Devuelve las detecciones del último frame procesado. </summary>
    List<Detection> GetLatestDetections();
}
