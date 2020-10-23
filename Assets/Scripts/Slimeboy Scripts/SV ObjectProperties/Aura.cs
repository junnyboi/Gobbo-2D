using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Aura : MonoBehaviour
{
    public float timeAura = 3f;
    bool auraActive;
    float auraTimer;

    private Transform target;
    private float speed = 0;

    // Awake is similar to Start, but is used when an object is created out of thin air (like when instantiated)
    void Awake()
    {
        auraTimer = timeAura;
        auraActive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (auraActive)
        {
            auraTimer -= Time.deltaTime;
            if (auraTimer < 0)
            {
                auraActive = false;
                Destroy(gameObject);
            }
        }

        // chase the target gameobject around
        if (target != null)
            transform.Translate((target.position - transform.position).normalized * speed * Time.deltaTime);
    }

    public void SetTarget(Rigidbody2D newTarget, float chaseSpeed)
    {
        target = newTarget.transform;
        speed = chaseSpeed;
    }
}
