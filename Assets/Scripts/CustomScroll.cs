using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomScroll : ScrollRect
{
    public override void OnScroll(PointerEventData data)
    {
        base.OnScroll(data);
        velocity = Vector2.zero;
    }
}
