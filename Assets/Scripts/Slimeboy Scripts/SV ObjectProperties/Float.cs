using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Float : MonoBehaviour
{
    public float directionTimer = 1f;
    public float speed = 0.1f;
    private float timer;
    private Vector3 direction = new Vector3 (0,1);
    // Start is called before the first frame update
    void Start()
    {
        timer = directionTimer;
    }
    private void Awake()
    {
        timer = directionTimer;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += Time.deltaTime * speed * direction;
        //print(gameObject + " moved: " + transform.position);

        // switch directions
        if (timer > 0)
            timer -= Time.deltaTime;
        else
        {
            direction = -direction;
            timer = directionTimer;
        }
    }
}