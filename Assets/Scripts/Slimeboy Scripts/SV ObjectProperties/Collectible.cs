using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    public Item item;
    void OnTriggerEnter2D(Collider2D other)
    {
        SlimeboyController controller = other.GetComponent<SlimeboyController>();

        if (controller != null)
        {
            Destroy(gameObject);
            print(gameObject + " collected!");

            Inventory.Instance.AddItem(item);
            print("Added: " + item);

            controller.CollectibleSound();
        }
    }
}