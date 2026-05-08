using UnityEngine;

public class Main : MonoBehaviour
{
    [Header("Récolte de bois")]
    [SerializeField] private int _woodPerClick = 2; // bois récolté par clic

    [Header("Timer récolte auto (optionnel)")]
    [SerializeField] private bool _autoHarvest = false;
    [SerializeField] private float _harvestInterval = 3f; // toutes les X secondes
    [SerializeField] private int _woodPerHarvest = 1;
    private float _harvestTimer = 0f;

    private void Update()
    {
        if (!_autoHarvest) return;

        _harvestTimer += Time.deltaTime;
        if (_harvestTimer >= _harvestInterval)
        {
            GameManager.Instance.AddWood(_woodPerHarvest);
            _harvestTimer = 0f;
        }
    }

    // Bouton "Récolter du bois"
    public void OnClickHarvestWood()
    {
        GameManager.Instance.AddWood(_woodPerClick);
    }
}