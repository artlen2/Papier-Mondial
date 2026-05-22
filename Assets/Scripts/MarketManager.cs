using TMPro;
using UnityEngine;
using System.Collections;

/// <summary>
/// Gère le prix de vente, la demande en %, la vente automatique et les événements de marché.
/// Sweet spot autour de 0.03-0.05$ — le revenu/sec estimé est affiché pour aider le joueur.
/// </summary>
public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    // ── Prix ──────────────────────────────────
    [Header("Prix de vente")]
    [SerializeField] private float _sellPrice = 0.02f;
    [SerializeField] private float _minPrice = 0.01f;
    [SerializeField] private float _maxPrice = 10f;
    [SerializeField] private float _priceStep = 0.01f;
    [SerializeField] float exposant = 0.6f;
    public float SellPrice => _sellPrice;

    // ── Timers ────────────────────────────────
    [Header("Vente")]
    private float _sellTimer = 0f;
    private float _logTimer = 0f;
    [SerializeField] private float _logInterval = 5f; // résumé toutes les X secondes

    // Ventes accumulées entre deux résumés
    private int _pendingSales = 0;
    private float _pendingEarned = 0f;

    // ── Demande ───────────────────────────────
    private float _marketEventMultiplier = 1f;

    // ── Événements ────────────────────────────
    [Header("Événements de marché")]
    [SerializeField] private float[] _eventThresholds = { 100f, 300f, 700f, 1500f };
    private int _nextThresholdIndex = 0;

    // ── UI ───────────────────────────────────
    [Header("UI — Marché")]
    [SerializeField] private TMP_Text _txtSellPrice;
    [SerializeField] private TMP_Text _txtDemandPercent;

    [Header("Petits événements")]
    [SerializeField] private float _flavorEventInterval = 30f;
    private float _flavorTimer = 0f;

    private string[] _flavorEvents = {
    "Un élève a mangé sa feuille de exam.",
    "Un prof a commandé 3 ramettes pour rien.",
    "Quelqu'un a imprimé un chat en ASCII.",
    "La photocopieuse du 2e étage est encore brisée.",
    "Un élève utilise du papier comme avion.",
    "Le directeur imprime ses emails.",
    "Une feuille s'est envolée par la fenêtre.",
    "Un prof a perdu ses corrections... encore.",
};

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

        _flavorTimer += Time.deltaTime;
        if (_flavorTimer >= _flavorEventInterval)
        {
            _flavorTimer = 0f;
            if (!GameManager.Instance.IsNotificationActive)
            {
                string msg = _flavorEvents[Random.Range(0, _flavorEvents.Length)];
                GameManager.Instance.ShowNotification(msg, 5f);
            }
        }

        HandleAutoSell();
        HandleEventThresholds();
        UpdateUI();
    }

    // ── Demande en % ──────────────────────────
    // Sweet spot autour de 0.03-0.05$
    // 0.01$ = 100%,  0.02$ = 87%,  0.05$ = ~50%,  0.10$ = ~20%,  0.50$ = ~5%
    // Le marketing peut pousser au-delà de 100%
    public float GetDemandPercent()
    {
        
        float rawDemand = Mathf.Pow(_minPrice / _sellPrice, exposant) * 100f;
        float marketingBonus = (MarketingManager.Instance != null ? MarketingManager.Instance.GetDemandBonus() : 0f);
        float eventBonus = (_marketEventMultiplier - 1f) * 50f;

        return Mathf.Clamp(rawDemand + marketingBonus + eventBonus, 0f, 200f);
    }

    // ── Vente automatique ─────────────────────
    // Tick rapide si demande haute, lent si demande basse
    // Toutes les _logInterval secondes, affiche un résumé dans le SalesLog
    private void HandleAutoSell()
    {
        if (GameManager.Instance.TotalPaper <= 0)
        {
            _logTimer += Time.deltaTime;
            FlushLogIfReady();
            return;
        }

        float demand = GetDemandPercent();
        float interval = Mathf.Lerp(1f, 0.01f, demand / 150f);

        _sellTimer += Time.deltaTime;
        _logTimer += Time.deltaTime;

        // Tick de vente
        if (_sellTimer >= interval)
        {
            _sellTimer = 0f;

            // demande% = probabilité qu'une feuille soit vendue ce tick
            int toSell = Mathf.FloorToInt(demand / 20f); // à 100% = 10 feuilles par tick
            toSell = Mathf.Min(toSell, GameManager.Instance.TotalPaper);

            if (Random.value <= demand / 100f && toSell > 0)
            {
                float earned = toSell * _sellPrice;
                GameManager.Instance.RemovePaper(toSell);
                GameManager.Instance.AddMoney(earned);
                _pendingSales += toSell;
                _pendingEarned += earned;
            }
        }

        FlushLogIfReady();
    }

    // Affiche le résumé toutes les _logInterval secondes
    private void FlushLogIfReady()
    {
        if (_logTimer < _logInterval) return;
        _logTimer = 0f;

        if (_pendingSales > 0)
        {
            SalesLog.Instance?.AddEntry(
                _pendingSales + " feuilles vendues à " + _sellPrice.ToString("F2")
                + "$ — +" + _pendingEarned.ToString("F2") + "$"
            );
        }
        else
        {
            return;
            //SalesLog.Instance?.AddEntry("Personne n'a acheté.");
        }

        _pendingSales = 0;
        _pendingEarned = 0f;
    }

    // ── Boutons prix + / - ────────────────────
    public void OnClickPriceUp()
    {
        _sellPrice = Mathf.Round(Mathf.Clamp(_sellPrice + _priceStep, _minPrice, _maxPrice) * 1000f) / 1000f;
    }

    public void OnClickPriceDown()
    {
        _sellPrice = Mathf.Round(Mathf.Clamp(_sellPrice - _priceStep, _minPrice, _maxPrice) * 1000f) / 1000f;
    }

    // ── Événements de marché ──────────────────
    private void HandleEventThresholds()
    {
        if (_nextThresholdIndex >= _eventThresholds.Length) return;
        if (GameManager.Instance.TotalPaperOveral >= _eventThresholds[_nextThresholdIndex])
        {
            TriggerRandomEvent();
            _nextThresholdIndex++;
        }
    }

    private void TriggerRandomEvent()
    {
        int roll = Random.Range(0, 6);
        string name = "";
        float duration = 0f;

        switch (roll)
        {
            case 0:
                name = "Grève des enseignants";
                duration = 60f;
                StartCoroutine(ApplyMarketEvent(0.3f, duration));
                break;
            case 1:
                name = "Rentrée scolaire";
                duration = 90f;
                StartCoroutine(ApplyMarketEvent(2.5f, duration));
                break;
            case 2:
                name = "Feu de forêt";
                GameManager.Instance.DamageForest(300);
                break;
            case 3:
                name = "Crise économique";
                duration = 300f; // 5 mins
                StartCoroutine(ApplyMarketEvent(0.5f, duration));
                break;
            case 4:
                name = "Scandale environnemental";
                duration = 90f;
                StartCoroutine(ApplyMarketEvent(0.6f, duration));
                break;
            case 5:
                name = "Compétiteur en faillite";
                duration = 60f;
                StartCoroutine(ApplyMarketEvent(1.8f, duration));
                break;
        }

        string msg = duration > 0f
            ? name + " (" + Mathf.FloorToInt(duration / 60f) + "m" + (duration % 60f > 0f ? (duration % 60f) + "s" : "") + ")"
            : name;

        GameManager.Instance.ShowPersistentNotification("ÉVÉNEMENT : " + msg);
        SalesLog.Instance?.AddEntry(">>> " + msg);

        // Efface la notification quand l'événement se termine
        if (duration > 0f)
            StartCoroutine(ClearEventNotification(duration));
    }

    private IEnumerator ClearEventNotification(float duration)
    {
        yield return new WaitForSeconds(duration);
        GameManager.Instance.ShowNotification("Événement terminé.", 3f);
    }

    private IEnumerator ApplyMarketEvent(float multiplier, float duration)
    {
        _marketEventMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        _marketEventMultiplier = 1f;
    }

    // ── UI ───────────────────────────────────
    private void UpdateUI()
    {
        if (_txtSellPrice) _txtSellPrice.text = _sellPrice.ToString("F2") + "$";
        if (_txtDemandPercent) _txtDemandPercent.text = "Demande : " + GetDemandPercent().ToString("F0") + "%";
    }
}