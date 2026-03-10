using TMPro;
using UnityEngine;

public class DomusViStepUI : MonoBehaviour
{
    [Header("Texts (auto-wired if null)")]
    [SerializeField] private TMP_Text txtStepTitle;
    [SerializeField] private TMP_Text txtInstruction;
    [SerializeField] private TMP_Text txtStatus;

    private void Awake()
    {
        // Busca por nombre exacto en hijos (tu jerarquía coincide)
        if (txtStepTitle == null)
            txtStepTitle = transform.Find("Txt_StepTitle")?.GetComponent<TMP_Text>();

        if (txtInstruction == null)
            txtInstruction = transform.Find("Txt_Instruction")?.GetComponent<TMP_Text>();

        if (txtStatus == null)
            txtStatus = transform.Find("Txt_Status")?.GetComponent<TMP_Text>();

        Debug.Log($"[StepUI] Awake wired: title={(txtStepTitle!=null)} instr={(txtInstruction!=null)} status={(txtStatus!=null)}");
    }

    public void ShowStep(int index1Based, int total, string instruction)
    {
        Debug.Log($"[StepUI] ShowStep({index1Based}/{total}) text='{instruction}'");

        if (txtStepTitle != null) txtStepTitle.text = $"Paso {index1Based}/{total}";
        if (txtInstruction != null) txtInstruction.text = instruction;
        SetStatus("");
    }

    public void SetStatus(string msg)
    {
        if (txtStatus != null) txtStatus.text = msg;
    }
}
