using TMPro;
using UnityEngine;
using System.Collections;

/// <summary>
/// Gère le prix de vente, la demande en %, la vente automatique et les événements de marché.
/// La demande% détermine la vitesse à laquelle le stock de papier se vend.
/// Plus le prix est bas -> demande haute -> papier vendu plus vite.
/// </summary>
public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    // ── Prix ──────────────────────────────────
    [Header("Prix de vente")]
    [SerializeField] private float _sellPrice = 0.2f;
    [SerializeField] private float _minPrice  = 0.01f;
    [SerializeField] private float _maxPrice  = 10f;
    [SerializeField] private float _priceStep = 0.05f; 
    public float SellPrice => _sellPrice;

    // ── Vente auto ────────────────────────────
    // Toutes les X secondes, le jeu vend floor(stock * demande% / 100) feuilles
    [Header("Vente automatique")]
    [SerializeField] private float _sellInterval = 3f;
    private float _sellTimer = 0f;

    // ── Demande ───────────────────────────────
    // demande% = clamp(100 - (prix / prixMax * 100) + bonusMarketing + bonusEvent, 0, 100)
    private float _marketEventMultiplier = 1f; // modifié par les événements

    // ── Événements ────────────────────────────
    [Header("Événements de marché")]
    [SerializeField] private float[] _eventThresholds = { 100f, 300f, 700f, 1500f };
    private int _nextThresholdIndex = 0;

    // ── UI ───────────────────────────────────
    [Header("UI — Marché")]
    [SerializeField] private TMP_Text _txtSellPrice;
    [SerializeField] private TMP_Text _txtDemandPercent;
    [SerializeField] private TMP_Text _txtEventNotification;

    // ─────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (_txtEventNotification) _txtEventNotification.text = "";
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentGamestate != GameManager.Gamestate.STARTED) return;

        HandleAutoSell();
        HandleEventThresholds();
        UpdateUI();
    }

    // ── Demande en % ──────────────────────────
    // Prix bas  -> priceFactor petit  -> demande haute
    // Prix élevé -> priceFactor grand -> demande basse
    public float GetDemandPercent()
    {
        float priceFactor    = (_sellPrice / _maxPrice) * 100f;
        float marketingBonus = MarketingManager.Instance != null
            ? MarketingManager.Instance.GetDemandBonus()
            : 0f;
        float eventBonus     = (_marketEventMultiplier - 1f) * 50f;

        return Mathf.Clamp(100f - priceFactor + marketingBonus + eventBonus, 0f, 100f);
    }

    // ── Vente automatique ─────────────────────
    private void HandleAutoSell()
    {
        if (GameManager.Instance.TotalPaper <= 0) return;

        _sellTimer += Time.deltaTime;
        if (_sellTimer < _sellInterval) return;

        float demand = GetDemandPercent();
        int   sold   = Mathf.FloorToInt(GameManager.Instance.TotalPaper * (demand / 100f));

        if (sold > 0)
        {
            float earned = sold * _sellPrice;
            GameManager.Instance.RemovePaper(sold);
            GameManager.Instance.AddMoney(earned);
            Debug.Log("Vendu " + sold + " papiers à " + _sellPrice.ToString("F2") + " $ — +" + earned.ToString("F2") + " $");
        }

        _sellTimer = 0f;
    }

    // ── Boutons prix + / - ────────────────────
    public void OnClickPriceUp()
    {
        _sellPrice = Mathf.Round(Mathf.Clamp(_sellPrice + _priceStep, _minPrice, _maxPrice) * 100f) / 100f;
    }

    public void OnClickPriceDown()
    {
        _sellPrice = Mathf.Round(Mathf.Clamp(_sellPrice - _priceStep, _minPrice, _maxPrice) * 100f) / 100f;
    }

    // ── Événements ────────────────────────────
    private void HandleEventThresholds()
    {
        if (_nextThresholdIndex >= _eventThresholds.Length) return;
        if (GameManager.Instance.TotalMoneyEarned >= _eventThresholds[_nextThresholdIndex])
        {
            TriggerRandomEvent();
            _nextThresholdIndex++;
        }
    }

    private void TriggerRandomEvent()
    {
        int    roll     = Random.Range(0, 6);
        string name     = "";
        float  duration = 15f;

        switch (roll)
        {
            case 0: name = "Grève des enseignants";    StartCoroutine(ApplyMarketEvent(0.3f, duration)); break;
            case 1: name = "Rentrée scolaire";         StartCoroutine(ApplyMarketEvent(2.5f, duration)); break;
            case 2:
                name = "Feu de forêt"; duration = 0f;
                GameManager.Instance.DamageForest(30000);
                break;
            case 3: name = "Crise économique";         StartCoroutine(ApplyMarketEvent(0.5f, duration)); break;
            case 4: name = "Scandale environnemental"; StartCoroutine(ApplyMarketEvent(0.6f, duration)); break;
            case 5: name = "Compétiteur en faillite";  StartCoroutine(ApplyMarketEvent(1.8f, duration)); break;
        }

        ShowNotification(duration > 0f ? name + " (" + duration + "s)" : name);
    }

    private IEnumerator ApplyMarketEvent(float multiplier, float duration)
    {
        _marketEventMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        _marketEventMultiplier = 1f;
    }

    private void ShowNotification(string message)
    {
        if (_txtEventNotification == null) return;
        StopCoroutine("ClearNotification");
        _txtEventNotification.text = "ÉVÉNEMENT : " + message;
        StartCoroutine(ClearNotification(5f));
    }

    private IEnumerator ClearNotification(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_txtEventNotification) _txtEventNotification.text = "";
    }

    // ── UI ───────────────────────────────────

    private void UpdateUI()
    {
        if (_txtSellPrice)     _txtSellPrice.text     = ""    + _sellPrice.ToString("F2") + " $";
        if (_txtDemandPercent) _txtDemandPercent.text = "Demande : " + GetDemandPercent().ToString("F0") + "%";
    }
}
