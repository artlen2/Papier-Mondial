using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton central. Gère l'état de la partie, le score, la victoire et la défaite.
/// Tous les autres managers passent par GameManager.Instance pour lire/écrire les ressources.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Ressources partagées ──────────────────
    public int TotalPaperOveral { get; private set; } = 0;
    public int TotalPaper { get; private set; } = 0;
    public int TotalWood { get; private set; } = 200;
    public float Money { get; private set; } = 15f;
    public float TotalMoneyEarned { get; private set; } = 0f;
    public string Notification = "Les notifications sont ici.";

    [Header("Forêt (condition de victoire)")]
    [SerializeField] private int _totalForestWood = 500000;

    public int TotalForestWood => _totalForestWood;
    public int WoodHarvestedTotal { get; private set; } = 0;
    public int ForestRemaining => _totalForestWood - WoodHarvestedTotal;

    // ── État ─────────────────────────────────
    public enum Gamestate { STARTED, WIN, GAMEOVER }
    public Gamestate CurrentGamestate { get; private set; } = Gamestate.STARTED;

    // ── Score & temps ─────────────────────────
    private float _gameTime   = 0f;
    public int FinalScore  { get; private set; } = 0;

    // ── UI ───────────────────────────────────
    [Header("UI — Ressources")]
    [SerializeField] private TMP_Text _txtTotalPaper;
    [SerializeField] private TMP_Text _txtPaper;
    [SerializeField] private TMP_Text _txtWood;
    [SerializeField] private TMP_Text _txtMoney;
    [SerializeField] private TMP_Text _txtForestRemaining;

    [Header("UI — Notification")]
    [SerializeField] private TMP_Text _txtNotification;

    [Header("UI — Fin de partie")]
    [SerializeField] private GameObject _winScreen;
    [SerializeField] private GameObject _gameoverScreen;
    [SerializeField] private TMP_Text   _txtFinalScore;

    // ─────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (_winScreen) _winScreen.SetActive(false);
        if (_gameoverScreen) _gameoverScreen.SetActive(false);
        if (_txtNotification) _txtNotification.text = "Bienvenue chez Papier Mondial !";
    }

    private void Update()
    {
        if (CurrentGamestate != Gamestate.STARTED) return;
        _gameTime += Time.deltaTime;
        UpdateUI();
        CheckWinLose();
    }

    // ── Méthodes de modification des ressources ──

    public void AddPaper(int amount)
    {
        TotalPaper = Mathf.Max(0, TotalPaper + amount);
        TotalPaperOveral += amount; // compteur cumulatif, ne se soustrait jamais
    }
    public void RemovePaper(int amount){ TotalPaper = Mathf.Max(0, TotalPaper - amount); }

    public void AddWood(int amount)
    {
        int actual = Mathf.Min(amount, ForestRemaining);
        if (actual <= 0) return;
        TotalWood += actual;
        WoodHarvestedTotal += actual;
    }
    public void RemoveWood(int amount) { TotalWood = Mathf.Max(0, TotalWood - amount); }

    public void AddMoney(float amount)
    {
        Money += amount;
        TotalMoneyEarned += amount;
    }

    public bool SpendMoney(float amount)
    {
        if (Money < amount)
        {
            GameManager.Instance.ShowNotification("Pas assez d'argent ! (besoin : " + amount.ToString("F2") + "$)");
            return false;
        }
        Money -= amount;
        return true;
    }

    public void DeductMoney(float amount) { Money -= amount; } // salaires, pertes

    public void DamageForest(int amount)
    {
        _totalForestWood = Mathf.Max(0, _totalForestWood - amount);
    }

    // ── Win / Lose ────────────────────────────

    private void CheckWinLose()
    {
        if (WoodHarvestedTotal >= _totalForestWood && TotalWood <= 0 && TotalPaper <= 0)
        {
            EndGame(true);
            return;
        }
        if (Money < 0f && TotalPaper <= 0)
            EndGame(false);
    }

    private void EndGame(bool isWin)
    {
        CurrentGamestate = isWin ? Gamestate.WIN : Gamestate.GAMEOVER;
        FinalScore = CalculateScore();

        // Les autres managers peuvent lire FinalScore pour l'afficher
        if (isWin && _winScreen) _winScreen.SetActive(true);
        if (!isWin && _gameoverScreen) _gameoverScreen.SetActive(true);
        if (_txtFinalScore) _txtFinalScore.text = "Score final : " + FinalScore.ToString("N0");
    }

    // score = (argent total cumulé) * (niveau manufacture) + temps (ms)
    private int CalculateScore()
    {
        int manufacture = ProductionManager.Instance != null ? ProductionManager.Instance.ManufactureLevel : 1;
        return Mathf.FloorToInt(TotalMoneyEarned * manufacture + _gameTime * 1000f);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ── Notification centrale ─────────────────


    public void ShowNotification(string message)
    {
        if (_txtNotification == null) return;
        _txtNotification.text = message;
        StopCoroutine("ClearNotification");
        StartCoroutine(ClearNotification(4f));
    }

    private IEnumerator ClearNotification(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_txtNotification) _txtNotification.text = "";
    }

    // ── UI ───────────────────────────────────

    private void UpdateUI()
    {
        if (_txtTotalPaper) _txtTotalPaper.text = "Papier Total : "  + TotalPaperOveral;
        if (_txtPaper) _txtPaper.text = "Papier : " + TotalPaper + " feuilles";
        if (_txtWood) _txtWood.text = "Bois : " + TotalWood  + " cm";
        if (_txtMoney) _txtMoney.text = "Argent : "  + Money.ToString("F2") + "$";
        if (_txtForestRemaining) _txtForestRemaining.text = "Forêt : " + ForestRemaining + " / " + _totalForestWood;
    }
}
