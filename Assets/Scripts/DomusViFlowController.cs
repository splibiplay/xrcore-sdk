using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DomusViFlowController : MonoBehaviour
{
    public enum FlowState
    {
        Idle,
        CandidateShown,
        ConfirmedRoutine,
        GuidedSteps,
        Finished
    }

    [Header("Refs")]
    [SerializeField] private RoutineEngine routineEngine;

    [Header("Refs - UI")]
    [SerializeField] private DomusViRoutineUI routineUI;
    [SerializeField] private DomusViStepFlow stepFlow;

    [Header("UI Panels")]
    [SerializeField] private GameObject panelPropuestaRutina;
    [SerializeField] private GameObject panelGuidanceSteps;
    [SerializeField] private GameObject panelFeedbackFinal;

    [SerializeField] private bool keepDebugPanelVisible = true;
    [SerializeField] private GameObject panelDebug; // opcional

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;

    public FlowState State { get; private set; } = FlowState.Idle;

    private RoutineCandidate _currentCandidate;
    private HashSet<string> _currentPresent = new();

    private void Awake()
    {
        if (routineEngine == null)
        {
            routineEngine = FindObjectOfType<RoutineEngine>();
        }

        if (routineUI == null)
            routineUI = FindObjectOfType<DomusViRoutineUI>();

        if (stepFlow == null)
            stepFlow = FindObjectOfType<DomusViStepFlow>();

        if (routineEngine == null)
        {
            Debug.LogError("[DomusViFlow] No se encontró RoutineEngine.");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (routineEngine != null)
            routineEngine.OnCandidateChanged += HandleCandidateChanged;

        if (routineUI != null)
            routineUI.OnRoutineConfirmed += HandleRoutineConfirmed;

        if (stepFlow != null)
            stepFlow.OnRoutineCompleted += HandleRoutineCompleted;
    }

    private void OnDisable()
    {
        if (routineEngine != null)
            routineEngine.OnCandidateChanged -= HandleCandidateChanged;

        if (routineUI != null)
            routineUI.OnRoutineConfirmed -= HandleRoutineConfirmed;

        if (stepFlow != null)
            stepFlow.OnRoutineCompleted -= HandleRoutineCompleted;
    }

    private void HandleCandidateChanged(RoutineCandidate candidate, HashSet<string> present)
    {
        _currentCandidate = candidate;
        _currentPresent = present;

        if (State == FlowState.GuidedSteps)
        {
            return; // en guiado ignoramos cambios de candidato (tick va en Update)
        }

        SetState(FlowState.CandidateShown);
        Debug.Log($"[DomusViFlow] Candidate update -> id={candidate.id} v={candidate.version} score={candidate.score} present=[{string.Join(", ", present)}]");
    }

    private void HandleRoutineConfirmed(RoutineCandidate chosen)
    {
        Debug.Log("[DomusViFlow] HandleRoutineConfirmed RECEIVED");
        _currentCandidate = chosen;

        // Solo gestionamos el estado/panel desde aquí; el StepFlow se arranca explícitamente
        // desde DomusViRoutineUI.OnStartPressed (anti doble llamada).
        SetState(FlowState.GuidedSteps);     // activa Panel_GuidanceSteps
    }

    private void HandleRoutineCompleted()
    {
        Debug.Log("[DomusViFlow] Routine completed.");
        RestartFromScratch();
    }

    public void RestartFromScratch()
    {
        var scene = SceneManager.GetActiveScene();
        Debug.Log($"[DomusViFlow] Restarting scene: {scene.name}");
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void Update()
    {
        if (State == FlowState.GuidedSteps)
            stepFlow?.Tick();
    }

    // Esto lo conectarás a tu botón Poke (WhenSelect)
    public void ConfirmCurrentCandidate()
    {
        if (_currentCandidate == null || _currentCandidate.id == RoutineId.None)
        {
            Debug.LogWarning("[DomusViFlow] No hay candidato válido para confirmar.");
            return;
        }

        SetState(FlowState.ConfirmedRoutine);
        Debug.Log($"[DomusViFlow] CONFIRMED routine id={_currentCandidate.id} v={_currentCandidate.version}");
        
        // Paso siguiente (aún no implementado): StartGuidedSteps(_currentCandidate.id);
    }

    // Esto lo conectarás a tu botón "Salir/Reset"
    public void ResetFlow()
    {
        _currentCandidate = null;
        _currentPresent.Clear();
        stepFlow?.Stop();
        routineUI?.Unlock();
        if (routineEngine != null && routineEngine.Provider is SentisDetectionProvider sentisProvider)
        {
            sentisProvider.ClearSessionObjectScope();
        }
        SetState(FlowState.Idle);
        Debug.Log("[DomusViFlow] Reset -> Idle");
    }

    private void SetState(FlowState newState)
    {
        if (State == newState) return;
        State = newState;

        if (logStateChanges)
            Debug.Log($"[DomusViFlow] State -> {State}");

        if (panelPropuestaRutina != null)
            panelPropuestaRutina.SetActive(State == FlowState.CandidateShown);

        if (panelGuidanceSteps == null)
        {
            Debug.LogError("[DomusViFlow] panelGuidanceSteps no asignado en inspector.");
        }
        else
        {
            panelGuidanceSteps.SetActive(State == FlowState.GuidedSteps);
        }

        if (panelFeedbackFinal != null)
            panelFeedbackFinal.SetActive(State == FlowState.Finished);

        if (!keepDebugPanelVisible && panelDebug != null)
            panelDebug.SetActive(State == FlowState.CandidateShown);
    }
}
