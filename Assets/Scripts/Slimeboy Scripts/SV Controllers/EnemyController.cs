using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public bool isEnemy = true;
    public float speed = 5.0f;
    public float changeTime = 5.0f;
    public bool vertical = false;
    public ParticleSystem particleEffect;
    private Edible edible;
    
    new Rigidbody2D rigidbody2D;
    Animator animator;
    float timer;
    int direction = 1;

    bool isFollowingSlimeboy = false;
    float followTimer;
    bool isJump = false;
    float jumpTimer;
    public float jumpForce = 100;
    bool injured = false;
    AudioSource audioSource;


    // Start is called before the first frame update
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        timer = changeTime;

        animator = GetComponent<Animator>();
        animator.logWarnings = false;

        edible = GetComponent<Edible>();
        edible.enabled = false;
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (injured)
            return;

        Vector2 position = rigidbody2D.position;

        if(!isFollowingSlimeboy)
        {
            if (vertical)
            {
                Vector2 move = new Vector2(0, direction);
                rigidbody2D.AddForce(move * speed);
                //position.y += Time.deltaTime * speed * direction;
                animator.SetFloat("Move X", 0);
                animator.SetFloat("Move Y", direction);
            }
            else
            {
                Vector2 move = new Vector2(direction, 0);
                rigidbody2D.AddForce(move * speed);
                //position.x += Time.deltaTime * speed * direction;
                animator.SetFloat("Move X", direction);
                animator.SetFloat("Move Y", 0);
            }

            //rigidbody2D.MovePosition(position);

            if (timer > 0)
                timer -= Time.deltaTime;
            else
            {
                direction = -direction;
                timer = changeTime;
            }
        }
        else
        {
            FollowSlimeboy();
            followTimer -= Time.deltaTime;
            if (followTimer < 0)
            {
                isFollowingSlimeboy = false;
            }
        }

        if (isJump)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer < 0)
            {
                isJump = false;
            }
        }
    }

    // Interact with player on collision
    void OnCollisionEnter2D(Collision2D collision)
    {
        SlimeboyController player = collision.gameObject.GetComponent<SlimeboyController>();

        // ENEMY COLLISION LOGIC
        if(isEnemy)
        {
            if (player != null)
            {
                player.ChangeHealth(-1);
            }
            // Automatically jump on tile collision
            else if (collision.gameObject.layer == LayerMask.NameToLayer("Tiles"))
            {
                Jump();
            }
        }

        else // NPC COLLISION LOGIC
        {
            NPC character = gameObject.GetComponent<NPC>(); 
            if (player != null)
            {
                if (character != null)
                {
                    character.DisplayDialog();
                    followTimer = 10f;
                    isFollowingSlimeboy = true;
                }
            }
            // Automatically jump on tile collision
            else if (collision.gameObject.layer == LayerMask.NameToLayer("Tiles"))
            {
                Jump();
            }
        }        
    }

    public void Injured()
    {
        injured = true;
        
        // Stop prevents the creation of new particles, while Destroy removes all particles in play
        if(particleEffect != null)
            particleEffect.Stop();
        animator.SetTrigger("Injured");
        edible.enabled = true;
        audioSource.mute = true;
    }

    void FollowSlimeboy(float maxFollowRange = 100f, float minFollowRange = 2f)
    {
        Transform target = SlimeboyController.Instance.gameObject.transform;
        Vector2 direction = target.position - transform.position;
        float distance = direction.magnitude;
        direction.Normalize();

        if (distance < maxFollowRange && distance > minFollowRange)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position,
                                                     speed * Time.deltaTime);
            animator.SetFloat("Move X", direction.x);
            animator.SetFloat("Move Y", direction.y);
        }
    }

    void Jump()
    {
        jumpTimer = 1.0f;
        rigidbody2D.AddForce(Vector2.up * jumpForce);
        isJump = true;
    }
}
