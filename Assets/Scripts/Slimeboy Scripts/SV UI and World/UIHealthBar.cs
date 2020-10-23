using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    /// <summary>
    /// SINGLETON INSTANCE - ONLY 1 CAN EXIST
    /// </summary>
    // static variables/classes can be accessed globally without a reference
    public static UIHealthBar Instance { get; private set; }

    public Image mask;
    float originalSize;

    // Awake is called when instantiated
    private void Awake()
    {
        // UIHealthBar script stores itself in the static called instance when awoken
        // this creates an easy reference to the UIHealthBar script without having to assign it in Inspector manually
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        originalSize = mask.rectTransform.rect.width;
    }

    /// Resize healthbar according to the anchors set in UI canvas
    /// <param name="value"></param> is a fraction between 0 and 1
    public void SetValue(float value)
    {
        mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize * value);
    }
}
