using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthCollectible : MonoBehaviour
{
    public int healAmount = 1;
    public int hungerAmount = 10;
    public GameObject HealEffect;
    public AudioClip collectedClip;
    void OnTriggerEnter2D(Collider2D other)
    {
        WorldManager.Instance.Print("Yummy!");

        SlimeboyController controller = other.GetComponent<SlimeboyController>();

        if (controller != null)
        {
            if (controller.health < controller.maxHealth  || controller.hunger < controller.maxHunger)
            {
                print("Health collected!");
                Destroy(gameObject);

                // update controller stats
                controller.ChangeHealth(healAmount);
                controller.ChangeHunger(hungerAmount);
                controller.PlaySound(collectedClip);

                CreateAura(controller);
            }
            else print("Your health and stomach are full.");
        }
    }

    void CreateAura(SlimeboyController controller)
    {
        // Heal aura
        Rigidbody2D rigidbody2D = controller.GetComponent<Rigidbody2D>();
        GameObject HealAura = Instantiate(HealEffect, rigidbody2D.position
                                      + Vector2.up * 0.5f, Quaternion.identity);

        HealAura.GetComponent<Aura>().SetTarget(controller.GetComponent<Rigidbody2D>(), 50f);
    }
}
