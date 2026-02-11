using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IDragHandler
{
    [SerializeField] private RectTransform target;

    public void OnDrag(PointerEventData eventData)
    {
        target.anchoredPosition += eventData.delta;
    }
}

