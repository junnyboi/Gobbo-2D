using UnityEngine;
using UnityEngine.EventSystems;

public class MouseDragBehaviour : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Vector2 initialMousePosition;

    /// <summary>
    /// This method will be called on the start of the mouse drag
    /// </summary>
    /// <param name="eventData">mouse pointer event data</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        //Debug.Log("Begin Drag");
        initialMousePosition = eventData.position;
    }

    /// <summary>
    /// This method will be called during the mouse drag
    /// </summary>
    /// <param name="eventData">mouse pointer event data</param>
    public void OnDrag(PointerEventData eventData)
    {
        // Move tooltip to follow the mouse cursor
        Vector2 currentMousePosition = eventData.position;        
        Vector2 d = currentMousePosition - initialMousePosition;
        Vector3 offset = new Vector3 (d.x, d.y, 0);

        Vector3 newPosition = transform.position + offset;
        Vector3 oldPosition = transform.position;

        if (IsRectTransformInsideSreen(offset) == false)
            newPosition = oldPosition;

        initialMousePosition = currentMousePosition;
        transform.position = newPosition;
    }

    /// <summary>
    /// This method will be called at the end of mouse drag
    /// </summary>
    /// <param name="eventData"></param>
    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log("End Drag");
    }

    /// <summary>
    /// This methods will check is the rect transform is inside the screen or not
    /// </summary>
    /// <param name="rectTransform">Rect Trasform</param>
    /// <returns></returns>
    private bool IsRectTransformInsideSreen(Vector3 offset)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        bool isInside = false;
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        int visibleCorners = 0;
        Rect rect = new Rect(0, 0, Screen.width, Screen.height);
        foreach (Vector3 corner in corners)
        {
            Vector3 offsetCorner = corner + offset;
            if (rect.Contains(offsetCorner))
                visibleCorners++;
        }
        if (visibleCorners == 4)
            isInside = true;

        return isInside;
    }
}