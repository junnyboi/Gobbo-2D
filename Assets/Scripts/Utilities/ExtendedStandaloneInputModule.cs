using UnityEngine.EventSystems;

public class ExtendedStandaloneInputModule : StandaloneInputModule
{
    /// <summary>
    ///  Pointer ids refer to specific pointer events: -1, -2, and -3 are left, right, and middle mouse buttons; 1, 2, 3+ refer to first, second, third, touch input fingers, etc.
    /// </summary>
    /// <param name="pointerId"></param>
    /// <returns></returns>
    public static PointerEventData GetPointerEventData(int pointerId = -1)
    {
        PointerEventData eventData;
        _instance.GetPointerData(pointerId, out eventData, true);
        return eventData;
    }

    private static ExtendedStandaloneInputModule _instance;

    protected override void Awake()
    {
        base.Awake();
        _instance = this;
    }
}
