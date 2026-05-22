using TMPro;
using UnityEngine;

/// <summary>
/// Gère les auto-makers — machines qui produisent du papier automatiquement.
/// Inspiré des AutoClippers de Universal Paperclips.
/// Coût auto-maker niveau n = c * n^p + b (formule exponentielle)
/// </summary>
public class AutoMakerManager : MonoBehaviour
{
    public static AutoMakerManager Instance { get; private set; }

    // ── Auto-makers ───────────────────────────
    [Header("Auto-makers")]
    [SerializeField] private float _baseCost = 25f;  // coût du premier auto-maker
    [SerializeField] private float _costMultiplier = 1.15f; // chaque achat coûte 15% de plus
    [SerializeField] private float _paperPerSec = 1f;   // papier/sec par auto-maker

    public int AutoMakerCount { get; private set; } = 0;
    public float TotalPaperPerSec => AutoMakerCount * _paperPerSec;

    private float _fractionalPaper = 0f; // accumule les fractions de papier

    // ── UI ───────────────────────────────────
    [Header("UI — Auto-makers")]
    [SerializeField] private TMP_Text _txtAutoMakerCount;
    [SerializeField] private TMP_Text _txtAutoMakerCost;
    [SerializeField] private TMP_Text _txtPaperPerSec;

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

        if (AutoMakerCount > 0)
            HandleAutoProduction();

        UpdateUI(); // toujours appelé, même à 0 auto-makers
    }

    // ── Production automatique ────────────────
    // Utilise des fractions pour être précis même à faible vitesse
    private void HandleAutoProduction()
    {
        if (GameManager.Instance.TotalWood <= 0) return;

        _fractionalPaper += TotalPaperPerSec * Time.deltaTime;

        int toProdue = Mathf.FloorToInt(_fractionalPaper);
        if (toProdue <= 0) return;

        _fractionalPaper -= toProdue;

        // Limite par le bois disponible
        int canProduce = Mathf.Min(toProdue, GameManager.Instance.TotalWood);
        GameManager.Instance.RemoveWood(canProduce);
        GameManager.Instance.AddPaper(canProduce);
    }

    // ── Bouton : Acheter un auto-maker ────────
    public void OnClickBuyAutoMaker()
    {
        float cost = GetNextCost();
        if (!GameManager.Instance.SpendMoney(cost)) return;

        AutoMakerCount++;
        GameManager.Instance.ShowNotification(
            "Auto-maker acheté ! Total : " + AutoMakerCount
            + " (" + TotalPaperPerSec.ToString("F1") + " papier/sec)"
        );
    }

    // ── Formule de coût ───────────────────────
    // Coût = baseCost * multiplier^count
    // Exemple : 25$, 28.75$, 33.06$, 38.02$...
    public float GetNextCost()
    {
        float discount = ProjectManager.Instance != null ? ProjectManager.Instance.AutoMakerDiscount : 0f;
        return _baseCost * Mathf.Pow(_costMultiplier, AutoMakerCount) * (1f - discount);
    }

    // ── UI ───────────────────────────────────
    private void UpdateUI()
    {
        if (_txtAutoMakerCount) _txtAutoMakerCount.text = "Auto-makers : " + AutoMakerCount;
        if (_txtAutoMakerCost) _txtAutoMakerCost.text = "Prochain : " + GetNextCost().ToString("F2") + "$";
        if (_txtPaperPerSec) _txtPaperPerSec.text = "Production : " + TotalPaperPerSec.ToString("F1") + " papier/sec";
    }
}