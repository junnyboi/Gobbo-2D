using UnityEngine;
using UnityEngine.UI;

public class PopupController_Character : PopupController
{
    [Header("Graphics")]
    public Mask portraitMask;
    public RawImage portraitImage;
    public RawImage placeholderImage;

    public void SetActivePlaceholder(bool state)
    {
        placeholderImage.gameObject.SetActive(state);
    }
}
