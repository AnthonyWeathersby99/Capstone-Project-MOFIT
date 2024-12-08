using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CrossPlatformInputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [System.Serializable]
    public class Vector2Event : UnityEvent<Vector2> { }

    public Vector2Event OnPress;
    public UnityEvent OnRelease;

    private bool isPressed = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        HandlePress(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        HandleRelease();
    }

    private void HandlePress(Vector2 position)
    {
        if (!isPressed)
        {
            isPressed = true;
            OnPress.Invoke(position);
        }
    }

    private void HandleRelease()
    {
        if (isPressed)
        {
            isPressed = false;
            OnRelease.Invoke();
        }
    }

    private void OnDisable()
    {
        OnPress.RemoveAllListeners();
        OnRelease.RemoveAllListeners();
    }
}