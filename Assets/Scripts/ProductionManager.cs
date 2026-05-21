using TMPro;
using UnityEngine;

/// <summary>
/// Gère la production de papier (manuelle + auto) et l'achat/upgrade de bois.
/// 1 cm de bois = 1 feuille de papier.
/// </summary>
public class ProductionManager : MonoBehaviour
{
    public static ProductionManager Instance { get; private set; }

    // ── Manufacture ───────────────────────────
    public int ManufactureLevel   { get; private set; } = 1;
    private int _autoProductionRate = 0; // papier/sec (cumulé des upgrades)
    private float _autoTimer = 0f;

    // ── Batch de bois ─────────────────────────
    [Header("Achat de bois")]
    [SerializeField] private int   _woodBatchBase   = 1000; // cm de départ par achat
    [SerializeField] private int   _woodBatchBonus  = 500;  // cm supplémentaires par niveau
    [SerializeField] private float _woodBatchCost   = 10f;  // coût d'un achat
    public  int   WoodBatchLevel { get; private set; } = 0;

    // Coût upgrade batch = c * n^p + b
    [Header("Formule upgrade batch")]
    [SerializeField] private float _upgradeP = 1.5f;
    [SerializeField] private float _upgradeC = 1f;
    [SerializeField] private float _upgradeB = 20f;

    // Coût upgrade manufacture = même formule, coefficients x10
    [Header("Formule upgrade manufacture")]
    [SerializeField] private float _manufactureP = 1.5f;
    [SerializeField] private float _manufactureC = 0.5f;
    [SerializeField] private float _manufactureB = 30f;

    // ── UI ───────────────────────────────────
    [Header("UI — Production")]
    [SerializeField] private TMP_Text _txtBatchSize;
    [SerializeField] private TMP_Text _txtBatchCost;
    [SerializeField] private TMP_Text _txtBatchUpgradeCost;
    [SerializeField] private TMP_Text _txtManufactureLevel;
    [SerializeField] private TMP_Text _txtManufactureCost;

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

        HandleAutoProduction();
        UpdateUI();
    }

    // ── Bouton : Faire du papier (manuel) ────
    // 1 cm bois -> 1 feuille
    public void OnClickProducePaper()
    {
        if (GameManager.Instance.TotalWood <= 0)
        {
            Debug.Log("Pas de bois — achète une batch !");
            GameManager.Instance.ShowNotification("Pas de bois — achète une batch !");
            return;
        }
        GameManager.Instance.RemoveWood(1);
        GameManager.Instance.AddPaper(1);
    }

    // ── Production automatique ────────────────
    // (manufacture upgrades )
    private void HandleAutoProduction()
    {
        int totalRate = _autoProductionRate;
        if (totalRate <= 0) return;

        _autoTimer += Time.deltaTime;
        if (_autoTimer >= 1f)
        {
            int canProduce = Mathf.Min(totalRate, GameManager.Instance.TotalWood);
            GameManager.Instance.RemoveWood(canProduce);
            GameManager.Instance.AddPaper(canProduce);
            _autoTimer = 0f;
        }
    }

    // ── Bouton : Acheter du bois ──────────────
    public void OnClickBuyWood()
    {
        if (!GameManager.Instance.SpendMoney(_woodBatchCost)) return;

        int batchSize = GetCurrentBatchSize();
        if (GameManager.Instance.ForestRemaining <= 0)
        {
            GameManager.Instance.ShowNotification ("Plus de forêt disponible...");
            GameManager.Instance.DeductMoney(-_woodBatchCost); // rembourse
            return;
        }

        GameManager.Instance.AddWood(batchSize);
        GameManager.Instance.ShowNotification("Acheté " + batchSize + " cm de bois pour " + _woodBatchCost.ToString("F2") + "$");

    }

    // ── Bouton : Améliorer la batch de bois ──
    public void OnClickUpgradeWoodBatch()
    {
        float cost = GetWoodUpgradeCost();
        if (!GameManager.Instance.SpendMoney(cost)) return;

        WoodBatchLevel++;
        GameManager.Instance.ShowNotification( "Batch niv." + WoodBatchLevel + " — " + GetCurrentBatchSize() + " cm par achat");
    }

    // ── Bouton : Améliorer la manufacture ────
    public void OnClickUpgradeManufacture()
    {
        float cost = GetManufactureCost();
        if (!GameManager.Instance.SpendMoney(cost)) return;

        ManufactureLevel++;
        _autoProductionRate += 2; // +2 papier/sec par niveau
        GameManager.Instance.ShowNotification( "Manufacture niv." + ManufactureLevel + " — auto : " + _autoProductionRate + " papier/sec");
    }

    // ── Formules ──────────────────────────────

    public int GetCurrentBatchSize()
    {
        return _woodBatchBase + WoodBatchLevel * _woodBatchBonus;
    }

    // Coût upgrade batch : c * n^p + b
    public float GetWoodUpgradeCost()
    {
        int next = WoodBatchLevel + 1;
        return _upgradeC * Mathf.Pow(next, _upgradeP) + _upgradeB;
    }

    // Coût upgrade manufacture : c * n^p + b (coefficients plus élevés)
    public float GetManufactureCost()
    {
        int next = ManufactureLevel + 1;
        return _manufactureC * Mathf.Pow(next, _manufactureP) * 10f + _manufactureB;
    }


    // ── UI ───────────────────────────────────

    private void UpdateUI()
    {
        if (_txtBatchSize) _txtBatchSize.text = "Batch : " + GetCurrentBatchSize() + " cm";
        if (_txtBatchCost) _txtBatchCost.text = "Coût achat : " + _woodBatchCost.ToString("F2") + "$";
        if (_txtBatchUpgradeCost) _txtBatchUpgradeCost.text = "LVL up batch : " + GetWoodUpgradeCost().ToString("F2") + "$";
        if (_txtManufactureLevel) _txtManufactureLevel.text = "( " + ManufactureLevel + " )";
        if (_txtManufactureCost) _txtManufactureCost.text = "LVL up la manufacturie : " + GetManufactureCost().ToString("F2") + "$";
    }
}
