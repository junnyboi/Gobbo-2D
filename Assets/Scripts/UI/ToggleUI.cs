using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleUI : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ToggleGameObject()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
