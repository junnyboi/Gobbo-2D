using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupController : MonoBehaviour
{
    [Header("Buttons")]
    public Button closeButton;
    public Button[] buttons;

    [Header("TMP")]
    public TMP_Text header;
    public TMP_Text[] TMP_Texts;
    
    void Start()
    {
        closeButton.onClick.AddListener(()=> 
        {
            gameObject.SetActive(false);
        });
    }

    public void SetHeader(string s)
    {
        if (header != null)
            header.text = s;
    }

    public void SetActive(bool state)
    {
        gameObject.SetActive(state);
    }
}
