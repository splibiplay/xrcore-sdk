using UnityEngine;

public class DomusViBootUI : MonoBehaviour
{
    [Header("Canvas roots")]
    [SerializeField] private GameObject sampleCanvasRoot;   // DetectionUIMenuPrefab (root)
    [SerializeField] private GameObject domusViCanvasRoot;   // Canvas_DomusVi

    [Header("Boot UI elements")]
    [SerializeField] private GameObject buttonA;
    [SerializeField] private GameObject initialText;

    [Header("Input")]
    [SerializeField] private bool allowAButtonToStart = true;

    private bool _started;

    private void Start()
    {
        ReturnToStartScreen();
    }

    private void Update()
    {
        if (!_started && allowAButtonToStart && OVRInput.GetDown(OVRInput.Button.One))
        {
            StartDomusViFlow();
        }
    }

    // Puedes llamarlo también desde un botón UI si lo prefieres
    public void StartDomusViFlow()
    {
        Debug.Log("[DomusViBootUI] StartDomusViFlow()");

        _started = true;

        // Ocultar botón A y texto inicial
        if (buttonA == null) Debug.LogError("[DomusViBootUI] buttonA NULL");
        if (initialText == null) Debug.LogError("[DomusViBootUI] initialText NULL");
        if (buttonA) buttonA.SetActive(false);
        if (initialText) initialText.SetActive(false);

        // Activar canvas DomusVi (sampleCanvasRoot se deja activo; solo se ocultan botón y texto)
        if (domusViCanvasRoot == null) Debug.LogError("[DomusViBootUI] domusViCanvasRoot NULL");
        if (domusViCanvasRoot) domusViCanvasRoot.SetActive(true);

        Debug.Log($"[DomusViBootUI] domus active? {domusViCanvasRoot?.activeSelf}");
    }

    public void ReturnToStartScreen()
    {
        Debug.Log("[DomusViBootUI] ReturnToStartScreen()");

        _started = false;
        if (sampleCanvasRoot) sampleCanvasRoot.SetActive(true);
        if (domusViCanvasRoot) domusViCanvasRoot.SetActive(false);
        if (buttonA) buttonA.SetActive(true);
        if (initialText) initialText.SetActive(true);
    }
}
