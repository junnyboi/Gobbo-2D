using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutomaticVerticalSize : MonoBehaviour
{
    public float childHeight = 20f;
    // Start is called before the first frame update
    void Start()
    {
        AdjustSize();
    }

    public void AdjustSize(float buffer = 0)
    {
        Vector2 rectSize = GetComponent<RectTransform>().sizeDelta;
        int activeChildCount = GetComponentsInChildren<Transform>().GetLength(0);
        rectSize.y = activeChildCount * childHeight  + buffer;
        GetComponent<RectTransform>().sizeDelta = rectSize;
    }
}
