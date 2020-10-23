using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeedSlider : MonoBehaviour
{
    public static SpeedSlider Instance;
    public Slider slider { get; protected set; }

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        slider = gameObject.GetComponent<Slider>();
        slider.onValueChanged.AddListener( delegate { OnValueChanged(); } );
    }

    void OnValueChanged()
    {
        DateTimeController.Instance.ChangeSpeed(slider.value);
    }
}
