using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SalesLog : MonoBehaviour
{
    public static SalesLog Instance { get; private set; }

    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _logEntryPrefab; // prefab avec TMP_Text
    [SerializeField] private int _maxEntries = 50; // entrées max avant suppression

    private Queue<GameObject> _entries = new Queue<GameObject>();

   

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddEntry(string message)
    {
        // Crée la nouvelle entrée au bas du content
        GameObject entry = Instantiate(_logEntryPrefab, _content);
        entry.GetComponent<TMP_Text>().text = message;
        _entries.Enqueue(entry);

        // Supprime les vieilles entrées si on dépasse le max
        while (_entries.Count > _maxEntries)
        {
            GameObject old = _entries.Dequeue();
            Destroy(old);
        }

        // Force le scroll vers le bas au prochain frame
        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
    }
}