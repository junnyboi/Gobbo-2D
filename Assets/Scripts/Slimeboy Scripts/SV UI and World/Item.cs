using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// create shortcut for this asset in the Assets > Create submenu
[CreateAssetMenu]

// ScriptableObject creates items that don't need to be attached to GameObjects
public class Item : ScriptableObject
{
    public Sprite sprite;
    public GameObject itemObject;
    public string itemName; 
}
