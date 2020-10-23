using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Remoting;
using UnityEngine.Experimental.Rendering.Universal;

public class FurnitureSpriteController : MonoBehaviour
{
    #region VARIABLE DECLARATION
    // dictionary to map stuff to their gameObjects
    public Dictionary<Furniture, GameObject> mapOfFurnToGO = new Dictionary<Furniture, GameObject>();
    Dictionary<string, Sprite> furnitureSprites;
    Dictionary<string, GameObject> furniturePrefabs;

    // create a shortcut to reference the world
    World world { get { return WorldController.Instance.w; } }
    public static FurnitureSpriteController Instance; 
    #endregion

    #region INITIALIZATION
    void Start()
    {
        Instance = this;
        LoadSprites();
        LoadPrefabs();

        // Register our callback so that our GameObject gets updated whenever a furniture's data changes
        world.cbFurnitureCreated += OnFurnitureCreated;
        world.cbFurnitureRemoved += OnFurnitureRemoved;

        // Go through existing furniture that was created on World.Awake()
        foreach (Furniture furn in world.furnitureList.Keys)
        {
            OnFurnitureCreated(furn);   // call event to refresh furniture sprites
        }
    }
    void LoadSprites()
    {
        furnitureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Furniture/");
        foreach (Sprite s in sprites)
        {
            furnitureSprites.Add(s.name, s);
        }
    }
    void LoadPrefabs()
    {
        furniturePrefabs = new Dictionary<string, GameObject>();
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Furniture/");
        foreach (GameObject prefab in prefabs)
        {
            furniturePrefabs.Add(prefab.name, prefab);
        }
    }
    #endregion

    #region CALLBACKS
    public void ActivateDummyFurniture(Furniture furn)
    {
        try
        {
            GameObject furn_go = mapOfFurnToGO[furn];
            SpriteRenderer sr = furn_go.GetComponent<SpriteRenderer>();
            sr.color = new Color(1, 1, 1, 1);
            sr.sortingLayerName = "Furniture";
            if (furn_go.GetComponent<Light2D>() != null)
                furn_go.GetComponent<Light2D>().enabled = true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public void OnFurnitureCreated(Furniture furn)
    {
        // FIXME: Does not consider multi-tile objects nor rotated objects

        GameObject furn_go = new GameObject();

        //furn_go.transform.SetParent(this.transform, true);
        SpriteRenderer sr;

        try // check prefabs first
        {
            furn_go = Instantiate(furniturePrefabs[furn.type]);
            furn_go.name = furn.type + "_" + furn.tile.X + "_" + furn.tile.Y;
            furn_go.transform.position = new Vector3(furn.tile.X, furn.tile.Y, 0);
            sr = furn_go.GetComponent<SpriteRenderer>();
        }
        catch // else load a sprite
        {
            furn_go.name = furn.type + "_" + furn.tile.X + "_" + furn.tile.Y;
            furn_go.transform.position = new Vector3(furn.tile.X, furn.tile.Y, 0);
            sr = furn_go.AddComponent<SpriteRenderer>();
            sr.sprite = GetSpriteForFurniture(furn);
            sr.sortingLayerName = "Furniture";
        }
        furn_go.isStatic = true;    // Static batching for faster rendering

        if (furn.tile.hasDummyFixedObject)
        {
            sr.color = new Color(0.5f, 0.5f, 3, 0.5f);
            sr.sortingLayerName = "Jobs";
        }

        if (furn.type == "WallCave")
        {
            furn_go.transform.SetParent(GameObject.Find("CaveWallGameObjects").transform, true);
        }
        else
        {
            furn_go.transform.SetParent(GameObject.Find("FurnitureGameObjects").transform, true);
        }

        mapOfFurnToGO.Add(furn, furn_go);

        // Register our callback to update furniture graphics
        furn.cbOnChanged += OnFurnitureChanged;

        // Register callback for multi-layer rendering
        furn.tile.cbTileActive += OnTileActive;
        furn.tile.cbTileInactive += OnTileInactive;
    }

    public void OnTileActive(Tile t)
    {
        mapOfFurnToGO[t.furniture].SetActive(true);
    }

    public void OnTileInactive(Tile t)
    {
        mapOfFurnToGO[t.furniture].SetActive(false);
    }

    public void OnFurnitureChanged(Furniture furn)
    {
        //Debug.Log("invoked <OnFurnitureChanged>");
        // make sure the graphics are implemented
        if (mapOfFurnToGO.ContainsKey(furn) == false)
        {
            Debug.LogError("OnFurnitureChanged -- invalid sprite for furniture to change");
        }

        GameObject furn_go = mapOfFurnToGO[furn];
        furn_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
    }

    public void OnFurnitureRemoved(Furniture furn)
    {
        // Unregister callbacks
        furn.cbOnChanged -= OnFurnitureChanged;
        furn.tile.cbTileActive -= OnTileActive;
        furn.tile.cbTileInactive -= OnTileInactive;

        // Update tile data to null for furniture
        furn.tile.PlaceFurniture(null);

        // Destroy the visual GameObject
        GameObject furn_go = mapOfFurnToGO[furn];
        mapOfFurnToGO.Remove(furn);
        Destroy(furn_go);

        // Update neighbor graphics
        Furniture.UpdateNeighbours(furn);

        if (furn.type == "Wall")
            Furniture.UpdateNeighbours(furn, "Door");

        // If dummy, remove job
        if (furn.tile.hasDummyFixedObject)
        {
            furn.tile.hasDummyFixedObject = false;
            // FIXME currently cancels all jobs on tile
            furn.tile.RemovePendingJobs();
        }

    }
    #endregion

    #region GET SPRITE FUNCTIONS
    /// check 8 neighbours and get the correct furniture sprite
    public Sprite GetSpriteForFurniture(Furniture furn)
    {
        //Debug.Log("invoked <GetSpriteForFurniture> for"+furn.furnitureType);
        if (furn.isLinkedAdjacently == false)
        {
            // works well if objects only have a single sprite form
            return furnitureSprites[furn.type];
        }

        if (furn.type == "Door")
            return GetSpriteForDoor(furn);

        // otherwise sprite name is more complicated
        string spriteName = furn.type + "_";
        string cardinalDirections = "";

        int x = furn.tile.X;
        int y = furn.tile.Y;
        int z = furn.tile.Z;

        #region Check Neighbours if Linked
        /// check adjacent neighbours
        Tile t = world.GetTileAt(x, y + 1, z); // North
        if (t != null & t.hasFurniture && t.furniture.type == furn.type)
        {
            cardinalDirections += "N";
        }

        t = world.GetTileAt(x + 1, y, z); // East
        if (t != null & t.hasFurniture && t.furniture.type == furn.type)
        {
            cardinalDirections += "E";
        }

        t = world.GetTileAt(x, y - 1, z); // South
        if (t != null & t.hasFurniture && t.furniture.type == furn.type)
        {
            cardinalDirections += "S";
        }

        t = world.GetTileAt(x - 1, y, z); // West
        if (t != null & t.hasFurniture && t.furniture.type == furn.type)
        {
            cardinalDirections += "W";
        }

        /// check diagonal neighbours
        // if N and E present, check North-east
        if (cardinalDirections.Contains("N") & cardinalDirections.Contains("E"))
        {
            t = world.GetTileAt(x + 1, y + 1, z); // North-East
            if (t != null & t.hasFurniture && t.furniture.type == furn.type)
            {
                cardinalDirections += "ne";
            }
        }

        // if S and E present, check South-east
        if (cardinalDirections.Contains("S") & cardinalDirections.Contains("E"))
        {
            t = world.GetTileAt(x + 1, y - 1, z); // South-East
            if (t != null & t.hasFurniture && t.furniture.type == furn.type)
            {
                cardinalDirections += "se";
            }
        }

        // if S and Wpresent, check South-west
        if (cardinalDirections.Contains("S") & cardinalDirections.Contains("W"))
        {
            t = world.GetTileAt(x - 1, y - 1, z); // South-West
            if (t != null & t.hasFurniture && t.furniture.type == furn.type)
            {
                cardinalDirections += "sw";
            }
        }

        // if N and W present, check North-west
        if (cardinalDirections.Contains("N") & cardinalDirections.Contains("W"))
        {
            t = world.GetTileAt(x - 1, y + 1, z); // North-West
            if (t != null & t.hasFurniture && t.furniture.type == furn.type)
            {
                cardinalDirections += "nw";
            }
        }

        spriteName += cardinalDirections;
        //Debug.Log("furnitureSpriteName: " + spriteName);

        //Eg. if object has all 4 neighbours, then the string will look like Wall_NSEW

        if (furnitureSprites.ContainsKey(spriteName) == false)
        {
            if (spriteName == furn.type + "_")
            {
                return furnitureSprites[spriteName];
            }
            else
            {
                Debug.LogError(spriteName + " does not exist, using " + furn.type + "_Dirt instead.");
                return furnitureSprites[furn.type + "_Dirt"];
            }
        } 
        #endregion

        return furnitureSprites[spriteName];
    }

    public Sprite GetSpriteForFurniture(string objectType)
    {
        // OVERLOAD returns only base furniture object
        if (furnitureSprites.ContainsKey(objectType))
        {
            Debug.Log("GetSpriteForFurniture-- furnitureSprites contains key for" + objectType);
            return furnitureSprites[objectType];
        }
        if (furnitureSprites.ContainsKey(objectType + "_"))
        {
            Debug.Log("GetSpriteForFurniture-- furnitureSprites contains key for " + objectType + "_");
            return furnitureSprites[objectType + "_"];
        }
        else
            Debug.LogError(objectType + " does not exist");
        return null;
    }

    public Sprite GetSpriteForDoor(Furniture furn)
    {
        //Debug.Log("FSC::GetSpriteForDoor");

        int x = furn.tile.X;
        int y = furn.tile.Y;
        int z = furn.tile.Z;
        string doorName = "Door_";

        Tile t = world.GetTileAt(x, y + 1, z); // North
        if (t != null & t.hasFurniture && t.furniture.type == "Wall")
        {
            doorName += "N";
        }

        t = world.GetTileAt(x, y - 1, z); // South
        if (t != null & t.hasFurniture && t.furniture.type == "Wall")
        {
            doorName += "S";
        }

        t = world.GetTileAt(x + 1, y, z); // East
        if (t != null & t.hasFurniture && t.furniture.type == "Wall")
        {
            doorName += "E";
        }

        t = world.GetTileAt(x - 1, y, z); // West
        if (t != null & t.hasFurniture && t.furniture.type == "Wall")
        {
            doorName += "W";
        }

        //Debug.Log("FSC::GetSpriteForDoor -- " + doorName);

        if (doorName == "Door_NS" || doorName == "Door_EW")
        {
            // If this is a door, lets check "openness" and update the sprite
            // FIXME: all hardcoding to be generalized later
            if (furn.type == "Door")
            {
                if (furn.GetParameter("openness") < 0.1f)
                {   // door is closed
                    return furnitureSprites[doorName];
                }
                else if (furn.GetParameter("openness") < 0.5f)
                {   // door is ajar
                    doorName += "_2";
                }
                else if (furn.GetParameter("openness") < 0.9f)
                {   // door is opening
                    doorName += "_3";
                }
                else
                {   // door is fully open
                    doorName += "_4";
                }
            }

            return furnitureSprites[doorName];
        }

        return furnitureSprites["Door_"];
    } 
    #endregion
}
