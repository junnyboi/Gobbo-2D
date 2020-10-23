using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    public GameObject ambientForest;
    public GameObject ambientCave;

    public float soundCooldown = 0;
    World world { get { return WorldController.Instance.w; } }
    public static SoundController Instance;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        //world.cbFurnitureCreated += OnFurnitureCreated;
        world.cbFurnitureRemoved += OnFurnitureRemoved;
        world.cbTileChanged += OnTileChanged;
        world.cbChangedDepth += OnDepthChange;
    }

    // Update is called once per frame
    void Update()
    {
        if (soundCooldown > 0)
        {
            soundCooldown -= Time.deltaTime;
        }
    }

    public static AudioSource PlayClip(AudioClip clip, float volume = 0.5f)
    {
        Vector3 pos = Camera.main.transform.position;
        var temp_go = new GameObject("TempAudio"); 
        temp_go.transform.position = pos; 
        var audiosource = temp_go.AddComponent<AudioSource>();
        audiosource.clip = clip;
        audiosource.volume = volume;
        audiosource.loop = false;

        audiosource.Play(); // start the sound
        Destroy(temp_go, clip.length); // destroy object after clip duration
        return audiosource; // return the AudioSource reference
    }

public void OnTileChanged( Tile tile_data )
    {
        if (soundCooldown > 0)
            return;
        
        soundCooldown = .1f;
        AudioClip clip = Resources.Load<AudioClip>("Sounds/Floor_OnCreated");
        PlayClip(clip, 0.3f);
    }

    public void OnFurnitureCreated(Furniture furn = null)
    {
        if (soundCooldown > 0)
            return;

        if (furn != null)
        {
            AudioClip clip = Resources.Load<AudioClip>("Sounds/" + furn.type + "_OnCreated");
            PlayClip(clip);
        }
        else
        {
            AudioClip clip = Resources.Load<AudioClip>("Sounds/Wall_OnCreated");
            PlayClip(clip);
        }
    }
    public void OnFurnitureRemoved(Furniture furn)
    {
        if (soundCooldown > 0)
            return;
        
        soundCooldown = .1f;
        AudioClip clip = Resources.Load<AudioClip>("Sounds/OnRemove");
        PlayClip(clip);
    }

    public void OnDepthChange()
    {
        if (world.currentZDepth == 0)
        {
            ambientForest.SetActive(false);
            ambientCave.SetActive(true);
        }
        else
        {
            ambientForest.SetActive(true);
            ambientCave.SetActive(false);
        }
    }

    public void OnGoblynSlain()
    {
        soundCooldown = 0.5f;
        AudioClip clip = Resources.Load<AudioClip>("Sounds/Goblyn death cry");
        PlayClip(clip);
    }
}
