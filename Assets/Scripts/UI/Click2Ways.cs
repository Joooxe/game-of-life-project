using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Click2Ways : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent onLeft;
    public UnityEvent onRight;

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
        {
            onLeft?.Invoke();
        }

        if (e.button == PointerEventData.InputButton.Right)
        {
            onRight?.Invoke();
        }
    }
}