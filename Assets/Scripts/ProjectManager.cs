using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Gère les projets déblocables, inspirés de Universal Paperclips.
/// Chaque projet a un coût, une condition de déblocage et un effet unique.
/// </summary>
public class ProjectManager : MonoBehaviour
{
    public static ProjectManager Instance { get; private set; }

    // ── Définition d'un projet ────────────────
    [System.Serializable]
    public class Project
    {
        public string Name;
        [TextArea] public string Description;
        public float Cost;
        public float UnlockAtMoney;    // argent total gagné pour apparaître
        public bool IsCompleted;

        // Type d'effet
        public enum EffectType { MarketingBonus, AutoMakerDiscount, WoodBatchBonus, MoneyBonus }
        public EffectType Effect;
        public float EffectValue; // valeur de l'effet (ex: 25 pour +25%)
    }

    // ── Liste des projets ─────────────────────
    [Header("Projets")]
    [SerializeField]
    private List<Project> _projects = new List<Project>()
    {
        new Project {
            Name          = "Coller des pubs dans les toilettes de l'école",
            Description   = "Pas élégant, mais efficace. [+25% marketing]",
            Cost          = 200f,
            UnlockAtMoney = 0f,
            Effect        = Project.EffectType.MarketingBonus,
            EffectValue   = 25f
        },
        new Project {
            Name          = "Sponsoriser le journal étudiant",
            Description   = "Ton logo sur chaque page. [+40% marketing]",
            Cost          = 500f,
            UnlockAtMoney = 150f,
            Effect        = Project.EffectType.MarketingBonus,
            EffectValue   = 40f
        },
        new Project {
            Name          = "Graisser la patte du concierge",
            Description   = "Il recommande ton papier à tous les profs. [+20% marketing]",
            Cost          = 350f,
            UnlockAtMoney = 80f,
            Effect        = Project.EffectType.MarketingBonus,
            EffectValue   = 20f
        },
        new Project {
            Name          = "Acheter une tronçonneuse industrielle",
            Description   = "Les bois se récoltent plus vite. [+500 cm par batch]",
            Cost          = 400f,
            UnlockAtMoney = 200f,
            Effect        = Project.EffectType.WoodBatchBonus,
            EffectValue   = 500f
        },
        new Project {
            Name          = "Contrat avec la commission scolaire",
            Description   = "Revenu immédiat garanti. [+300$]",
            Cost          = 600f,
            UnlockAtMoney = 300f,
            Effect        = Project.EffectType.MoneyBonus,
            EffectValue   = 300f
        },
        new Project {
            Name          = "Optimiser la chaîne de montage",
            Description   = "Les auto-makers coûtent 20% moins cher.",
            Cost          = 800f,
            UnlockAtMoney = 500f,
            Effect        = Project.EffectType.AutoMakerDiscount,
            EffectValue   = 0.20f
        },
    };

    // Bonus de marketing accumulé via les projets (lu par MarketingManager)
    public float ProjectMarketingBonus { get; private set; } = 0f;

    // Rabais sur les auto-makers (lu par AutoMakerManager)
    public float AutoMakerDiscount { get; private set; } = 0f;

    // ── UI ───────────────────────────────────
    [Header("UI — Projets")]
    [SerializeField] private Transform _projectContainer; // parent des boutons de projet
    [SerializeField] private GameObject _projectButtonPrefab; // prefab : bouton + TMP_Text

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

        RefreshProjectList();
    }

    // ── Affichage dynamique des projets ───────
    // Affiche seulement les projets disponibles et non complétés
    private void RefreshProjectList()
    {
        // Nettoie les anciens boutons
        foreach (Transform child in _projectContainer)
            Destroy(child.gameObject);

        foreach (Project project in _projects)
        {
            if (project.IsCompleted) continue;
            if (GameManager.Instance.TotalMoneyEarned < project.UnlockAtMoney) continue;

            // Crée le bouton
            GameObject btn = Instantiate(_projectButtonPrefab, _projectContainer);

            // Texte du bouton
            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label)
                label.text = project.Name + "\n" + project.Description
                           + "\n[" + project.Cost.ToString("F0") + "$]";

            // Couleur grisée si pas assez d'argent
            Button button = btn.GetComponent<Button>();
            if (button)
            {
                bool canAfford = GameManager.Instance.Money >= project.Cost;
                button.interactable = canAfford;

                Project captured = project; // capture pour le lambda
                button.onClick.AddListener(() => OnClickProject(captured));
            }
        }
    }

    // ── Acheter un projet ─────────────────────
    public void OnClickProject(Project project)
    {
        if (project.IsCompleted) return;
        if (!GameManager.Instance.SpendMoney(project.Cost)) return;

        project.IsCompleted = true;
        ApplyEffect(project);

        GameManager.Instance.ShowNotification("Projet complété : " + project.Name);
        SalesLog.Instance?.AddEntry(">>> Projet : " + project.Name);
    }

    // ── Application des effets ────────────────
    private void ApplyEffect(Project project)
    {
        switch (project.Effect)
        {
            case Project.EffectType.MarketingBonus:
                ProjectMarketingBonus += project.EffectValue;
                Debug.Log("Marketing +" + project.EffectValue + "% (total projets : " + ProjectMarketingBonus + "%)");
                break;

            case Project.EffectType.AutoMakerDiscount:
                AutoMakerDiscount += project.EffectValue;
                Debug.Log("Rabais auto-makers : " + (AutoMakerDiscount * 100f) + "%");
                break;

            case Project.EffectType.WoodBatchBonus:
                // Ajoute directement du bois au stock
                GameManager.Instance.AddWood(Mathf.FloorToInt(project.EffectValue));
                Debug.Log("Bois ajouté : +" + project.EffectValue + " cm");
                break;

            case Project.EffectType.MoneyBonus:
                GameManager.Instance.AddMoney(project.EffectValue);
                Debug.Log("Argent ajouté : +" + project.EffectValue + "$");
                break;
        }
    }
}