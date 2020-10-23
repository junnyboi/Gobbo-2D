using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    void OnTriggerStay2D(Collider2D other)
    {
        //Debug.Log("<" + other + "> has triggered ");
        SlimeboyController controller = other.GetComponent<SlimeboyController>();

        if (controller != null)
        {
            if (controller.health > 0)
            {
                controller.ChangeHealth(-2);
                //Destroy(gameObject);
            }
        }
    }
}
