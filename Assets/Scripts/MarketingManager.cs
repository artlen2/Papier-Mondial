using TMPro;
using UnityEngine;

/// <summary>
/// Gère les niveaux de marketing.
/// Chaque niveau augmente la demande en % via GetDemandBonus().
/// Coût niveau n = c * n^p + b  (p=1.5, c=0.5, b=15)
/// </summary>
public class MarketingManager : MonoBehaviour
{
    public static MarketingManager Instance { get; private set; }

    // ── Marketing ─────────────────────────────
    [Header("Marketing")]
    [SerializeField] private float _p                  = 1.5f; // exposant
    [SerializeField] private float _c                  = 0.5f; // coefficient
    [SerializeField] private float _b                  = 400f;  // base
    [SerializeField] private float _demandBonusPerLevel = 5f;  // +5% demande par niveau
    public  int MarketingLevel { get; private set; } = 0;

    // ── UI ───────────────────────────────────
    [Header("UI — Marketing")]
    [SerializeField] private TMP_Text _txtMarketingLevel;
    [SerializeField] private TMP_Text _txtUpgradeCost;
    [SerializeField] private TMP_Text _txtDemandBonus;

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
        UpdateUI();
    }

    // ── Bouton : Améliorer le marketing ──────
    public void OnClickUpgradeMarketing()
    {
        int   nextLevel = MarketingLevel + 1;
        float cost      = GetUpgradeCost(nextLevel);

        if (!GameManager.Instance.SpendMoney(cost)) return;

        MarketingLevel = nextLevel;
        Debug.Log("Marketing niv." + MarketingLevel + " — demande bonus : +" + GetDemandBonus() + "%");
    }

    // ── Formule exponentielle ─────────────────
    // Coût niveau n = c * n^p + b
    public float GetUpgradeCost(int level)
    {
        return _c * Mathf.Pow(level, _p) + _b;
    }

    // Bonus de demande total en %
    public float GetDemandBonus()
    {
        return MarketingLevel * _demandBonusPerLevel;
    }

    // ── UI ───────────────────────────────────

    private void UpdateUI()
    {
        if (_txtMarketingLevel) _txtMarketingLevel.text = "Marketing niv. " + MarketingLevel;
        if (_txtUpgradeCost)    _txtUpgradeCost.text    = "Prochain niv. : " + GetUpgradeCost(MarketingLevel + 1).ToString("F2") + "$";
        if (_txtDemandBonus)    _txtDemandBonus.text    = "Bonus demande : +" + GetDemandBonus().ToString("F0") + "%";
    }
}
