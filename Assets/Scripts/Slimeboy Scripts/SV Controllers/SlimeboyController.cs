using Cinemachine;
using Cinemachine.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// Class defines a new component
/// </summary>
public class SlimeboyController : MonoBehaviour
{
    public static SlimeboyController Instance { get; private set; }

    // use property definition for currentHealth to allow other scripts to read but not write
    public int health { get { return currentHealth; } }
    public int maxHealth = 10;
    int currentHealth;

    public float speed = 10.0f;
    public float spitForce = 500f;

    bool isInvincible = true;
    public float invincibleTime = 2.0f;
    float invincibleTimer;

    bool isCamouflaged = false;
    public float camouflageTime = 5.0f;
    float camouflageTimer;

    bool isLaunched = false;
    public float launchDelay = 0.5f;
    float launchTimer;

    bool isEating = false;
    public float eatDelay = 0.5f;
    float eatTimer;

    public int hunger { get { return currentHunger; } }
    public int maxHunger = 100;
    int currentHunger;
    public float hungerTime = 5f;
    float hungerTimer;

    bool isClimb = false;
    float jumpTimer;
    bool isJump = false;

    public GameObject projectilePrefab;
    public GameObject slimeTrail;
    Edible eatObject = null;

    new Rigidbody2D rigidbody2D;
    Animator animator;
    AudioSource audioSource;
    public AudioClip audioSpit;
    public AudioClip audioEat;
    public AudioClip audioCollectible;

    public float cameraOrthographic = 8.0f;

    // initialize direction
    Vector2 lookDirection = new Vector2(1, 0);

    private void Awake()
    {
        Instance = this;
    }
    // Start is initialized once the game starts
    void Start()
    {
        animator = GetComponent<Animator>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;
        launchTimer = launchDelay;
        eatTimer = eatDelay;

        hungerTimer = hungerTime;
        currentHunger = maxHunger;

        WorldManager.Instance.Generate(rigidbody2D);
        Debug.Log("Health: " + currentHealth + "/" + maxHealth);
        Debug.Log("Health: " + currentHunger + "/" + maxHunger);

        /// SETTINGS - VSYNC & FPS (default is 60)
/*        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 10;*/
    }

    /// Update is called once per frame
    void Update()
    {
        Movement();

        /// SETTINGS
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ChangeCameraDistance();
        }

        /// HUNGER
        hungerTimer -= Time.deltaTime;
        if (hungerTimer < 0)
        {
            hungerTimer = hungerTime;
            ChangeHunger(-1);
        }

        /// SKILLS
        // INVINCIBILITY TIMER
        if (isInvincible)
        {
            Invincible();
        }

        // ACTIONS
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if(!isJump)
            {
                jumpTimer = 1.0f;
                rigidbody2D.AddForce(Vector2.up * 275);
                isJump = true;
            }
        }

        if(isJump)
        {
            jumpTimer -= Time.deltaTime;
            if(jumpTimer < 0)
            {
                isJump = false;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            // map Input.mousePosition to world space and call Interaction(mousePos)
            Interaction(Camera.main.ScreenToWorldPoint(Input.mousePosition)); 
        }

        if (isEating && !isCamouflaged)
        {
            eatTimer -= Time.deltaTime;
            if (eatTimer < 0)
            {
                isEating = false;
                eatTimer = eatDelay;
                Debug.Log("Slimeboy is eating " + eatObject);                                
                Eat(eatObject);
                PlaySound(audioEat);
            }
        }

        // SPIT PROJECTILE
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (!isLaunched && !isCamouflaged && !isEating)
            {
                animator.SetTrigger("Launch");
                PlaySound(audioSpit);
                isLaunched = true;
            }
        }

        if (isLaunched && !isCamouflaged)
        {
            launchTimer -= Time.deltaTime;
            if (launchTimer < 0)
            {
                isLaunched = false;
                Launch();
                launchTimer = launchDelay;
            }
        }

        // CAMOUFLAGE PUDDLE
        if (Input.GetKeyDown(KeyCode.C))
        {
            if(!isCamouflaged)
            {
                camouflageTimer = camouflageTime;
                isCamouflaged = true;
                invincibleTimer = camouflageTime;
                isInvincible = true;

                animator.SetTrigger("Camouflage");
                slimeTrail.SetActive(false);
            }
        }

        if (isCamouflaged)
        {
            camouflageTimer -= Time.deltaTime;
            if (camouflageTimer < 0)
            {
                isCamouflaged = false;
                animator.SetTrigger("Camouflage");
                slimeTrail.SetActive(true);
            }
        }
    }
    void Invincible()
    {
        // INVINCIBILITY TIMER
        invincibleTimer -= Time.deltaTime;
        if (invincibleTimer < 0)
            isInvincible = false;
    }
    public void ChangeHealth(int amount)
    {
        // if damaged, check for invincibility
        if (amount < 0)
        {
            if (isInvincible)
                return;
            else
            {
                animator.SetTrigger("Hit");
                isInvincible = true;
                invincibleTimer = invincibleTime;
            }
        }
        // change health if within acceptable limits
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        //print("Health: " + currentHealth + "/" + maxHealth);
        // (float) converts int to floating point
        UIHealthBar.Instance.SetValue(currentHealth / (float)maxHealth);

        if (currentHealth < 1)
        {
            print("Slimeboy has died");
        }
    }

    public void ChangeHunger(int amount)
    {
        // if damaged, check for invincibility
        if (amount < 0)
        {
            {
                hungerTimer = hungerTime;
            }
        }
        // change hunger if within acceptable limits
        currentHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);
        // (float) converts int to floating point
        UIHungerBar.Instance.SetValue(currentHunger / (float)maxHunger);

        // lose health if hunger bar is empty
        if (currentHunger < 1)
        {
            ChangeHealth(-1);
        }
    }

        void Movement()
    {
        /// HORIZONTAL & VERTICAL MOVEMENT
        // every frame, check input axis: down/left is -1, up/right is +1
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 move = new Vector2(horizontal, vertical);

        // if movement inputs are not stationary, set new move vector 
        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            lookDirection.Set(move.x, move.y);
            lookDirection.Normalize();
            // only normalize vectors storing direction, but never for positions

            // access animator and set X,Y and speed parameters
            animator.SetFloat("Look X", lookDirection.x);
            animator.SetFloat("Look Y", lookDirection.y);

            // magnitude of the 'move' vector is a scalar for 'speed' 
            animator.SetFloat("Speed", move.magnitude);

            // WALL CLIMBING PHYSICS
            if (isClimb)
            {
                // declare variable & store current position
                Vector2 position = rigidbody2D.position;

                // update position and link speed to deltaTime so it's independent of framerate
                position += move * speed * 1 * Time.deltaTime;

                // move object & prevent jittering on 2d collision
                rigidbody2D.MovePosition(position);
            }
            else if (!Mathf.Approximately(move.x, 0.0f) || move.y < 0)
                rigidbody2D.AddForce(move * speed);
        }
    }

    void Interaction(Vector3 targetPos)
    {
        // convert to 2D plane
        Vector3 targetPos2D = new Vector2(targetPos.x, targetPos.y);

        // set lookDirection
        Vector2 heading = targetPos2D - transform.position;
        lookDirection.Set(heading.x, heading.y);
        lookDirection.Normalize();
        animator.SetFloat("Look X", lookDirection.x);
        animator.SetFloat("Look Y", lookDirection.y);
        Vector3 transformPos2D = new Vector2(transform.position.x + lookDirection.x, transform.position.y + lookDirection.y);


        // raycasting
        RaycastHit2D targetHit = Physics2D.Raycast(targetPos2D, Vector2.zero);
        RaycastHit2D playerHit = Physics2D.Raycast(transformPos2D, lookDirection, 3);
        Debug.DrawLine(transformPos2D, targetPos2D, Color.cyan, 1, false);

        if (targetHit.collider != null)
        {
            if (playerHit.collider.gameObject != null)
            {
                if (playerHit.collider.gameObject == targetHit.collider.gameObject)
                {
                    WorldManager.Instance.Print(targetHit.collider.gameObject.name);

                    if (targetHit.transform.gameObject.layer == LayerMask.NameToLayer("Edible")
                        || targetHit.transform.gameObject.layer == LayerMask.NameToLayer("Tiles"))
                    {
                        animator.SetTrigger("Eat");
                        if (!isEating && !isCamouflaged && !isLaunched)
                        {
                            isEating = true;
                            eatObject = targetHit.collider.GetComponent<Edible>();
                        }
                    }
                    else if (targetHit.transform.gameObject.layer == LayerMask.NameToLayer("NPC"))
                    {
                        NPC character = targetHit.collider.GetComponent<NPC>();
                        Debug.Log("NPC: " + character);
                        if (character != null)
                        {
                            character.DisplayDialog();
                        }
                    }
                }
                else
                {
                    WorldManager.Instance.Print("Out of reach: " + targetHit.collider.gameObject.name);
                }
            }
        }
    }

    public void Launch()
    {
        // clones prefab of projectile and spawns it with some offset to put it near the character's hands
        // Quaternion.identity means 'no rotation'
        GameObject projectileObject = Instantiate(projectilePrefab, rigidbody2D.position 
                                      + Vector2.down * 0.1f, Quaternion.identity);

        // calls Launch function from Projectile script with the parameters: direction = lookDirection, force = 300
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(lookDirection, spitForce);

/*        animator.SetTrigger("Launch");
*/    }

    public void DropLoot(GameObject loot)
    {
        Vector2 newPosition = rigidbody2D.position + lookDirection;
        float x = Mathf.Round(newPosition.x);
        float y = Mathf.Round(newPosition.y);
        Instantiate(loot, new Vector2 (x,y), Quaternion.identity);
    }    

    void Eat(Edible gameObject)
    {
        if(gameObject != null)
        {
            gameObject.Eaten();
            ChangeHunger(2);
            //Debug.Log("Slimeboy has eaten " + gameObject);
        }
    }

    public void Climbing(bool check)
    {
        isClimb = check;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log(hit.collider.gameObject);
    }

    void ChangeCameraDistance()
    {
        print("Switching camera mode");
        var camera = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
        camera.m_Lens.OrthographicSize = cameraOrthographic;
        if (camera.m_Lens.OrthographicSize == 8)
            cameraOrthographic = 30;
        else cameraOrthographic = 8;
    }

    // plays audio clips at player's position, passed from other scripts
    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
    public void CollectibleSound()
    {
        audioSource.PlayOneShot(audioCollectible);
    }
}
