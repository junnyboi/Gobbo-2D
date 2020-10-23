using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climbable : MonoBehaviour
{
    private bool stopClimbing = false;
    private Collider2D collisionExit;
    private float timer = 0.3f;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.GetComponent<SlimeboyController>() != null)
            collision.GetComponent<SlimeboyController>().Climbing(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<SlimeboyController>() != null)
        {
            stopClimbing = true;
            collisionExit = collision;
        }
    }

    private void Update()
    {
        if (stopClimbing)
        {
            timer -= Time.deltaTime;
            if (timer < 0)
            {
                //print("timer: " + timer);
                collisionExit.GetComponent<SlimeboyController>().Climbing(false);
                // reset
                stopClimbing = false;
                //print("stop climbing");
                timer = 0.3f;
                return;
            }
        }
    }
}
