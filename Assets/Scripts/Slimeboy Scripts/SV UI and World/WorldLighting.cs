using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class WorldLighting : MonoBehaviour
{

    private float dayLength = 24f;
    float noon = 12f;
    float dusk = 17f;
    float night = 20f;
    public float dayMultiplier = 100f;
    private float currentTime = 5f;

    private float prevIntensity;
    private float prevDepth;
    private bool isUnderground = false;

    public Light2D worldLight;
    public Light2D playerLight;
    public Light2D nightGlow;

    void Start()
    {
        dayLength *= dayMultiplier;
         noon *= dayMultiplier;
         dusk *= dayMultiplier;
         night *= dayMultiplier;
        currentTime *= dayMultiplier;

        nightGlow.intensity = 0.2f;
    }

    void Update()
    {
        /// FOLLOW PLAYER HORIZONTAL AXIS
        Vector2 playerPosition = SlimeboyController.Instance.transform.position;
        transform.position = new Vector2(playerPosition.x, transform.position.y);

        /// CHECK DEPTH
        float playerDepth = Camera.main.ScreenToWorldPoint(playerPosition).y;

        if (playerDepth < -25 && playerDepth > -50)
        {
            Debug.LogError("Player depth: " + playerDepth);
            isUnderground = true;
            playerLight.intensity = .95f;
        }   
        else if(playerDepth <= -50)
        {
            playerLight.intensity = .9f;
        } 
        else 
        {
            isUnderground = false;
        }

       
        /// CHECK TIME
        currentTime += Time.deltaTime;

        // reset day
        if (currentTime > dayLength)
        {
            currentTime = 0;
        }
        
        // run day and night cycle
        if (isUnderground == false)
        {
            DayNightCycle();
        }        
    }

    void DayNightCycle()
    {
        // dawn
        if (currentTime >= 0 && currentTime <= noon)
        {
            worldLight.intensity = currentTime / noon;
            playerLight.intensity = 1 - currentTime / noon;
            prevIntensity = worldLight.intensity;
        }
        // day 
        else if (currentTime > noon && currentTime < dusk)
        {
            worldLight.intensity = prevIntensity;
            playerLight.intensity = 0f;
        }
        // dusk
        else if (currentTime >= dusk && currentTime <= night)
        {
            worldLight.intensity = 1 - (currentTime - dusk) / (night - dusk);
            playerLight.intensity = (currentTime - dusk) / (night - dusk);
            prevIntensity = worldLight.intensity;
        }
        // night
        else if (currentTime > night && currentTime <= dayLength)
        {
            worldLight.intensity = prevIntensity;
            playerLight.intensity = 1f;
        }        
    }
}
