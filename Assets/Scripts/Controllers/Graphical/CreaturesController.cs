using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class CreaturesController : MonoBehaviour
{
    #region VARIABLE DECLARATION

    public Dictionary<Creature, GameObject> creatureGameObjectMap;
    Dictionary<string, GameObject> characterPrefabs;

    World w { get { return WorldController.Instance.w; } }
    public static CreaturesController Instance;

    #endregion

    #region INITIALIZATION & PREFABS
        void Start()
    {
        Instance = this;
        LoadPrefabs();

        // Dictionary maps GameObjects to characters being rendered
        creatureGameObjectMap = new Dictionary<Creature, GameObject>();

        // Register our callback so that our GameObject gets updated whenever a char's data changes
        w.cbCreatureCreated += OnCharacterCreated;
        w.cbCreatureRemoved += OnCharacterRemoved; ;

        // Check for pre-existing (loaded) characters, and trigger their callbacks
        foreach (Creature c in w.creaturesList)
            OnCharacterCreated(c);
    }

    void LoadPrefabs()
    {
        characterPrefabs = new Dictionary<string, GameObject>();
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Characters/");
        foreach (GameObject prefab in prefabs)
        {
            characterPrefabs.Add(prefab.name, prefab);
        }
    }

    #endregion

    #region CALLBACKS

    void OnCharacterCreated(Creature c)
    {
        GameObject c_go;

        // Instantiate prefab based on creature species
        switch (c.species)
        {
            case SpeciesCreature.Gorilla:
                c_go = Instantiate(characterPrefabs["Gorilla"]);
                break;
            case SpeciesCreature.Horror:
                c_go = Instantiate(characterPrefabs["Horror"]);
                break;
            case SpeciesCreature.Koi:
                c_go = Instantiate(characterPrefabs["Koi"]);
                break;
            case SpeciesCreature.Sturgeon:
                c_go = Instantiate(characterPrefabs["Serpent"]);
                c_go.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
                break;
            default:
                if (c as Goblyn is Goblyn_Male)
                    c_go = Instantiate(characterPrefabs["Goblyn_Male"]);
                else
                    c_go = Instantiate(characterPrefabs["Gnome_Female"]);
                break;
        }

        c_go.GetComponent<Animator>().logWarnings = false;
        creatureGameObjectMap.Add(c, c_go);

        c_go.transform.position = new Vector3(c.X, c.Y, 0);
        c_go.transform.localScale = new Vector3(c.size_x, c.size_y, 1);
        c_go.transform.SetParent(GameObject.Find("CreatureGameObjects").transform, true);
        c.currRotation = c_go.transform.localRotation;

        SpriteRenderer sr = c_go.GetComponent<SpriteRenderer>();
        if (c.envClass == EnvClass.Aquatic)
            sr.sortingLayerName = "AquaticCreature";
        else if (c.species == SpeciesCreature.Gorilla)
            sr.sortingLayerName = "TallObject";
        else
            sr.sortingLayerName = "Creature";

        c.cbCreatureMoved += OnMovedTile;
        c.cbCreatureOlder_year += OnOlder_year;
        c.cbCreatureChangedDepth += OnChangeDepth;
        w.cbChangedDepth += OnChangeDepth;

    }

    void OnChangeDepth(Creature c)  // For Creature :: cbCreatureMoved
    {
        if (c.currentDepth != w.currentZDepth)
        {
            Tile prevTile = w.GetTileAt(c.currTile.X, c.currTile.Y, w.currentZDepth);
            prevTile.creaturesOnTile.Remove(c);
            creatureGameObjectMap[c].SetActive(false);
        }
        else
            creatureGameObjectMap[c].SetActive(true);
    }

    void OnChangeDepth()    // For World :: cbChangedDepth
    {
        foreach (Creature c in creatureGameObjectMap.Keys)
        {
            if (c.currentDepth != w.currentZDepth)
                creatureGameObjectMap[c].SetActive(false);
            else
                creatureGameObjectMap[c].SetActive(true);
        }
    }

    void OnOlder_year(Creature c)
    {
        // Creature death due to old age
        if (c.age >= c.lifespan)
        {
            w.RemoveCreature(c);
            return;
        }

        // Increase size of character each year by 0.05
        // starting from 0.5 and ending at 1.0 (10 years to maturity)

        if (c.size_x < 1 || c.size_y < 1)
        {
            c.size_x += 0.05f;
            c.size_y += 0.05f;
        }

        GameObject char_go = creatureGameObjectMap[c];
        char_go.transform.localScale = new Vector3(c.size_x, c.size_y, 1);
    }

    void OnMovedTile(Creature c)
    {

        if (creatureGameObjectMap.ContainsKey(c) == false)
        {
            Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map.");
            return;
        }

        GameObject c_go = creatureGameObjectMap[c];

        ChangeCharacterAnimations(c, c_go);

        c_go.transform.position = new Vector3(c.X, c.Y, 0);
    }

    void OnCharacterRemoved(Creature c)
    {
        // unattach any cameras and return to camera controller
        GameObject c_go = creatureGameObjectMap[c];
        Camera attachedCam = c_go.GetComponentInChildren<Camera>();
        if (attachedCam != null)
        {
            attachedCam.gameObject.transform.SetParent(CameraController.Instance.transform, false);
            TooltipController.Instance.characterPopup.SetActivePlaceholder(true);
        }

        // destroy the visual GameObject
        Destroy(c_go);

        // remove from dictionary map
        creatureGameObjectMap.Remove(c);

        // unregister callback
        c.cbCreatureMoved -= OnMovedTile;
        c.cbCreatureOlder_year -= OnOlder_year;
    }

    #endregion

    #region ANIMATIONS
    void ChangeCharacterAnimations(Creature c, GameObject c_go)
    {
        Animator animator = c_go.GetComponent<Animator>();

        if (animator == null) return;
        animator.logWarnings = false;

        #region Variable Declaration

        float deltaX = c.X - c_go.transform.position.x;
        float deltaY = c.Y - c_go.transform.position.y;
        Vector2 move = new Vector2(deltaX, deltaY);

        #endregion

        #region Creature Movement
        // Creature rotation (for totally top down creatures)
        if (c_go.transform.localRotation != c.currRotation)
            c_go.transform.localRotation = c.currRotation;

        // If movement inputs are not stationary, set new move vector 
        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            if (!c.isRotating)
            {
                // Left-right-up-down rotations
                if (move.x < 0)
                    c_go.transform.localRotation = Quaternion.Euler(0, 0, 0);   // Left
                else
                    c_go.transform.localRotation = Quaternion.Euler(0, 180, 0); // Right

                c.currRotation = c_go.transform.localRotation;
            }

            animator.SetFloat("Speed", move.magnitude);
        }
        else
        {
            // Set negative speed to enter idle state
            animator.SetFloat("Speed", -1);

            // Retain last look direction if non-rotating
            if (!c.isRotating && c.currRotation != null)
                c_go.transform.localRotation = c.currRotation;
        }

        #endregion

        #region Working Animations

        if (c.isWorking)
        {
            animator.SetBool("Working", true);

            // Look in the direction of the job
            if (c.myJob != null)
            {
                deltaX = c.myJob.tile.X - c_go.transform.position.x;
                deltaY = c.myJob.tile.Y - c_go.transform.position.y;
                Vector2 lookDirection = new Vector2(deltaX, deltaY).normalized;
            }
        }
        else
            animator.SetBool("Working", false); 

        #endregion
    } 
    #endregion

    #region INSTANTIATING CREATURES
    public void CreateGoblyn(Tile tile = null)
    {
        if (tile == null)
            w.CreateCreature(w.GetTileAtWorldCentre(-1), SpeciesCreature.Goblyn);
        else
            w.CreateCreature(tile, SpeciesCreature.Goblyn);
    }

    public void CreateGoblyn()
    {
        Creature gob = w.CreateCreature(w.GetTileAtWorldCentre(-1), SpeciesCreature.Goblyn);
    } 
    #endregion

    public void RemoveAllCreatures()
    {
        w.RemoveAllCreatures();
    }
}
