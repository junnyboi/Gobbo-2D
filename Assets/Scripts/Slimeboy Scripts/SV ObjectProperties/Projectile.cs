using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    new Rigidbody2D rigidbody2D;
    public GameObject SparksEffect;
    public float projectileTimer = 1.0f;

    /*    void Start()
        {
            rigidbody2D = GetComponent<Rigidbody2D>(); 
        }*/

    // Awake is similar to Start, but is used when an object is created out of thin air (like when instantiated)
    void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();    
    }

    // Update is called once per frame
    void Update()
    {
        // optimize performance by retiring projectiles too far away
        if(transform.position.magnitude > 1000f)
        {
            Destroy(gameObject);
        }
        projectileTimer -= Time.deltaTime;
        if (projectileTimer < 0)
        {
            Explosion();
            Destroy(gameObject); 
        }
    }

    public void Launch(Vector2 direction, float force)
    {
        rigidbody2D.AddForce(direction * force);
        rigidbody2D.AddTorque(10f);
    }

    // note we can avoid friendly fire by setting Layers in the inspector
    void OnCollisionEnter2D(Collision2D other)
    {
        // if the collision object has an EnemyController class, then...
        EnemyController e = other.collider.GetComponent<EnemyController>();
        if(e != null)
        {
            e.Injured();
        }

        print(gameObject + " collided with " + other.gameObject);
        Destroy(gameObject);

        Explosion();
    }

    void Explosion()
    {
        // sparks effect
        Instantiate(SparksEffect, rigidbody2D.position + Vector2.up * 0f, Quaternion.identity);
    }
}
