using TMPro;
using UnityEngine;

/// <summary>
/// Gère l'embauche des employés, les salaires automatiques et le bonus de production.
/// Chaque employé ajoute ProductionBonus papier/sec à la production automatique.
/// </summary>
public class EmployeeManager : MonoBehaviour
{
    public static EmployeeManager Instance { get; private set; }

    // ── Employés ──────────────────────────────
    [Header("Employés")]
    [SerializeField] private float _hireCost          = 20f;
    [SerializeField] private float _salaryPerEmployee = 2f;
    [SerializeField] private float _salaryInterval    = 10f; // cycle de paie en secondes
    public  float ProductionBonus { get; private set; } = 0.5f; // papier/sec par employé
    public  int   EmployeeCount   { get; private set; } = 0;

    private float _salaryTimer = 0f;

    // ── UI ───────────────────────────────────
    [Header("UI — Employés")]
    [SerializeField] private TMP_Text _txtEmployeeCount;
    [SerializeField] private TMP_Text _txtHireCost;
    [SerializeField] private TMP_Text _txtNextSalary;

    // ─────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentGamestate != GameManager.Gamestate.STARTED) return;

        HandleSalaries();
        UpdateUI();
    }

    // ── Bouton : Engager un employé ──────────
    public void OnClickHireEmployee()
    {
        if (!GameManager.Instance.SpendMoney(_hireCost)) return;
        EmployeeCount++;
        Debug.Log("Employé engagé. Total : " + EmployeeCount);
    }

    // ── Salaires automatiques ─────────────────
    private void HandleSalaries()
    {
        if (EmployeeCount <= 0) return;

        _salaryTimer += Time.deltaTime;
        if (_salaryTimer >= _salaryInterval)
        {
            float total = EmployeeCount * _salaryPerEmployee;
            GameManager.Instance.DeductMoney(total);
            _salaryTimer = 0f;
            Debug.Log("Salaires payés : " + total.ToString("F2") + "$");
        }
    }

    // ── UI ───────────────────────────────────

    private void UpdateUI()
    {
        if (_txtEmployeeCount) _txtEmployeeCount.text = "Employés : "  + EmployeeCount;
        if (_txtHireCost)      _txtHireCost.text      = "Embauche : "  + _hireCost.ToString("F2") + "$";

        if (_txtNextSalary)
        {
            float timeLeft = _salaryInterval - _salaryTimer;
            _txtNextSalary.text = "Prochain salaire dans : " + Mathf.CeilToInt(timeLeft) + "s"
                + "  (" + (EmployeeCount * _salaryPerEmployee).ToString("F2") + "$)";
        }
    }
}
