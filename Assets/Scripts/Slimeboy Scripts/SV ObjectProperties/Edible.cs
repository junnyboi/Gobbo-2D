using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edible : MonoBehaviour
{
    new Rigidbody2D rigidbody2D;
    Animator animator;
    public GameObject loot;
    public float destroyTimer = 4f;

    private void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        //loot.SetActive(false);
    }

    /// LOOT MECHANISM
    public void Eaten()
    {
        Dead();
        Debug.Log(gameObject + " has been eaten");
    }

    void Dead()
    {
        if (rigidbody2D != null)
            rigidbody2D.simulated = false;
        gameObject.layer = 2;

        SlimeboyController.Instance.DropLoot(loot);

        if (animator != null)
        {
            animator.SetTrigger("Dead");
            Destroy(gameObject, destroyTimer);
        }
        else Destroy(gameObject, 0.1f);        

        //loot.SetActive(true);
    }

}
