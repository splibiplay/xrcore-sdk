using UnityEngine;
using PassthroughCameraSamples.MultiObjectDetection;

public class UIInputBridge : MonoBehaviour
{
    [SerializeField] private DetectionManager detectionManager;
    [SerializeField] private DetectionUiMenuManager uiMenuManager;
    [SerializeField] private DomusViBootUI bootUI;

    public void PressA()
    {
        Debug.Log("[UIInputBridge] PressA()");

        if (bootUI == null)
        {
            Debug.LogError("[UIInputBridge] bootUI NULL (no asignado en inspector)");
            return;
        }

        Debug.Log("[UIInputBridge] Calling bootUI.StartDomusViFlow()");
        bootUI.StartDomusViFlow();

        // Activar reconocimiento: cerrar menú "Press A" y arrancar detección
        if (uiMenuManager != null) uiMenuManager.NotifyActionAPressed();
        if (detectionManager != null) detectionManager.TriggerActionA();
    }

    /// <summary>
    /// Llamado al pulsar el botón B (cancelar / borrar / retroceder).
    /// </summary>
    public void PressB()
    {
        Debug.Log("PressB called");
        if (detectionManager == null) { Debug.LogWarning("DetectionManager is NULL"); return; }
        detectionManager.TriggerActionB();
    }

}

