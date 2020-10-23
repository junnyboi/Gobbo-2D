using Priority_Queue;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ResourceController : MonoBehaviour
{
    #region VARIABLE DECLARATION

    World world { get { return WorldController.Instance.w; } }
    public static ResourceController Instance;
    public static FoodQueue foodQueue = new FoodQueue();

    public static Dictionary<string, Resource> stockpile = new Dictionary<string, Resource>();
    public static Dictionary<string, List<Resource>> resourceStacks = new Dictionary<string, List<Resource>>();
    public static Dictionary<Creature, Resource> creatureResourceMap = new Dictionary<Creature, Resource>();

    public Dictionary<Resource, GameObject> resourceGameObjectMap = new Dictionary<Resource, GameObject>();
    public Dictionary<string, Sprite> resourceSprites;
    public GameObject resourceUIPrefab;

    public Resource test;
    #endregion

    #region INITIALIZATION
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        LoadSprites();

        // Check for pre-existing (loaded) resources, and trigger their callbacks
        foreach (Tile t in world.tiles)
        {
            if (t.hasResource)
                OnResourceCreated(t.resource);
        }

        // DEBUG starting food
        ChangeStockpile("Cup Mushroom", 20);
        ChangeStockpile("Button Mushroom", 10);
    }

    void LoadSprites()
    {
        resourceSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Resource/");
        foreach (Sprite s in sprites)
        {
            resourceSprites.Add(s.name, s);
        }
    }
    #endregion

    public bool ChangeStockpile(string type, int delta = 1)
    {
        if (!stockpile.ContainsKey(type))
            stockpile.Add(type, new Resource(type));

        if (stockpile[type].amount + delta < 0)
        {
            Debug.Log("Stockpile has insufficient resources for " + type + ", unable to change by " + delta);
            return false;
        }

        Debug.Log("Stockpile :: " + type + " changed by " + delta);
        stockpile[type].ChangeAmount(delta);

        // Update FoodQueue (if applicable)
        if (stockpile[type] is Food)
        {
            Food food = stockpile[type] as Food;

            if (food.amount > 0)
            {
                if (foodQueue.Contains(food) == false)
                    foodQueue.Enqueue(food);
            }
            else // food.amount == 0
            {
                if (foodQueue.Contains(food))
                    foodQueue.Remove(food);
            }
        }

        return true;
    }

    public void PlaceLoot(Tile t, string type)
    {
        Resource r = ResourceDrops.lootMap["Default"].Clone(t);

        if (ResourceDrops.lootMap.ContainsKey(type))
            r = ResourceDrops.lootMap[type].Clone(t);

        PlaceResourceStack(t, r);
    }

    public void PlaceHarvest(Tile t, string type)
    {
        Resource r = ResourceDrops.harvestMap["Default"].Clone(t);

        if (ResourceDrops.lootMap.ContainsKey(type))
            r = ResourceDrops.harvestMap[type].Clone(t);

        PlaceResourceStack(t, r);
    }

    /// <summary>
    /// Attach resource stack to creature
    /// </summary>
    public bool PickupResourceStack(Creature c, Resource r)
    {
        // Validations
        if (c == null || r == null) return false;

        // Set ownership to creature
        r.SetOwner(c);

        // Create GameObject
        OnResourceCreated(r);
        GameObject r_go = resourceGameObjectMap[r];

        // Attach to creature GameObject
        GameObject c_go = CreaturesController.Instance.creatureGameObjectMap[c];
        r_go.transform.SetParent(c_go.transform);
        r_go.transform.localPosition = new Vector3(0, 0);
        
        // Modify resource sprite
        r_go.transform.localScale = new Vector3(0.6f, 0.6f);
        SpriteRenderer r_sr = r_go.GetComponent<SpriteRenderer>();
        r_sr.sortingLayerName = "CreatureOverlay";

        creatureResourceMap.Add(c, r);
        return true;
    }

    /// <summary>
    /// Place resource stack on the ground
    /// </summary>
    public bool PlaceResourceStack(Tile t, Resource r)
    {
        //Debug.Log("PlaceResourceStack -- " + r.ToString() + " at " + t.ToString());

        bool tileWasEmpty = !t.hasResource;
        if (t.PlaceResource(r) == false) return false;

        // Original resource stack will be empty if merged with the tile stack
        if (r.amount <= 0)
        {
            //Debug.Log("PlaceResourceStack -- resource stack has been placed on tile");
            RemoveResourceStack(r);
        }

        // If tile was empty, we need to register the newly created resource stack
        if (tileWasEmpty)
        {
            //Debug.Log("PlaceResourceStack -- registering new resource stack: " + t.resource.ToString());

            if (!resourceStacks.ContainsKey(r.type))
                resourceStacks.Add(r.type, new List<Resource>());

            resourceStacks[r.type].Add(t.resource);
        }

        ConsolidateNearbyStacks(t.resource);

        // Create GameObject
        OnResourceCreated(t.resource);
        return true;
    }

    public bool SwapResourceStack(Creature c, Tile t, int amount = -1)
    {
        if (c == null || t == null
            || !creatureResourceMap.ContainsKey(c)
            || t.resource == null)
            return false;

        Resource r_dropoff = creatureResourceMap[c];
        Resource r_pickup = t.resource;

        // Find a suitable tile to drop held resources
        Tile t_dropoff = null;
        foreach (Tile neigh in c.currTile.GetNeighbours())
        {
            if (neigh.resource == null)
                t_dropoff = neigh;
        }
        if (t_dropoff == null) return false;
        PlaceResourceStack(t_dropoff, r_dropoff.Clone(t_dropoff));
        RemoveResourceStack(r_dropoff);

        // Pick up specified amount of target resource stack
        if (amount > 0)
            PickupResourceStack(c, new Resource(r_pickup.type, amount));
        // Else pick up everything
        else PickupResourceStack(c, r_pickup);

        return true;
    }

    void ConsolidateNearbyStacks(Resource r)
    {
        if (r == null || r.tile == null) return;
        List<Tile> neighTiles = r.tile.GetNeighbours();
        foreach (Tile t in neighTiles)
        {
            Resource neighResource = t.resource;
            if (t.resource != null && t.resource.type == r.type)
            {
                //Debug.Log("Consolidating nearby stack of " + neighResource.ToString() + " into  " + r.ToString());
                r.ChangeAmount(neighResource.amount);
                RemoveResourceStack(neighResource);
            }
        }
    }

    void OnResourceCreated(Resource r)
    {
        if (r == null)
        {
            //Debug.LogError("ResourceController :: OnResourceCreated -- r is null!");
            return;
        }

        //Debug.Log("OnResourceCreated -- creating GameObject");

        // This creates a new GameObject and adds it to our scene.
        GameObject r_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        resourceGameObjectMap.Add(r, r_go);

        r_go.name = r.type;
        if(r.owner == "tile")
        {
            r_go.transform.position = new Vector3(r.tile.X, r.tile.Y, 0);
            r_go.transform.SetParent(this.transform, true);

            // Register callback for multi-layer tile rendering
            r.tile.cbTileActive += OnTileActive;
            r.tile.cbTileInactive += OnTileInactive;
            r.tile.SetActive = r.tile.SetActive;    // trigger callback 

            // Add count for stack size
            GameObject r_ui = Instantiate(resourceUIPrefab);
            r_ui.transform.SetParent(r_go.transform);
            r_ui.transform.localPosition = new Vector3(0, 0);
            TMP_Text r_tmp = r_ui.GetComponentInChildren<TMP_Text>();
            r_tmp.text = r.amount.ToString();

            // Add callback for displaying resource amount
            r.cbResourceAmountChanged += (Resource r_callback) =>
            {
                r_tmp.text = r_callback.amount.ToString();
                if (r_callback.amount <= 0)
                    RemoveResourceStack(r);
            };
        }

        // Set sprite for GameObject
        SpriteRenderer sr = r_go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Resources";
        try
        {
            if (resourceSprites.ContainsKey(r.type))
                sr.sprite = resourceSprites[r.type];
            else
                sr.sprite = resourceSprites["Log"];
        }
        catch
        {
            Debug.LogError("ResourceController :: OnResourceCreated -- No default sprite found for resource");
        }

    }

    public void RemoveResourceStack(Resource r)
    {
        if (r == null)
        {
            Debug.LogError("RemoveResourceStack -- NULL resource");
            return;
        }

        if (r.owner == "creature")
        {
            //Debug.Log("RemoveResourceStack -- creature");

            // Purge data
            creatureResourceMap.Remove(r.creature);
            PurgeVisual(r);
        }
        else if (r.owner == "tile" &&  resourceStacks.ContainsKey(r.type) && resourceStacks[r.type].Contains(r))
        {
            //Debug.Log("RemoveResourceStack -- tile");

            // Unassign callbacks
            r.tile.cbTileActive -= OnTileActive;
            r.tile.cbTileInactive -= OnTileInactive;

            // Purge data
            r.tile.RemoveResource();           
            resourceStacks[r.type].Remove(r);
            PurgeVisual(r);
        }
        else if (r.owner == "tile")
        {
            //Debug.Log("RemoveResourceStack -- tile (no resourceStack & visuals)");

            // Unassign callbacks
            r.tile.cbTileActive -= OnTileActive;
            r.tile.cbTileInactive -= OnTileInactive;

            // Purge data
            // r.tile.RemoveResource();
        }
        else Debug.LogError("RemoveResourceStack error -- r.owner: " + r.owner);
    }

    void PurgeVisual(Resource r)
    {
        // Purge visual
        if (resourceGameObjectMap.ContainsKey(r))
        {
            GameObject r_go = resourceGameObjectMap[r];
            resourceGameObjectMap.Remove(r);
            GameObject.Destroy(r_go);
        }
        else Debug.LogError("RemoveResourceStack -- no GameObject found");

        r.UnassignOwner();
    }

    public static Resource FindNearestResourceStack(Tile currTile, string resourceType)
    {
        if (!resourceStacks.ContainsKey(resourceType) ||
            resourceStacks[resourceType].Count <= 0)
        {
            Debug.Log("FindNearestResourceStack -- NULL resource stack for " + resourceType);
            return null;
        }

        Resource rNearest = resourceStacks[resourceType].First();
        if (rNearest == null) Debug.LogError("rNearest resource is null!");
        if (rNearest.tile == null) Debug.LogError("rNearest resource's tile is null!");

        int d = Math.Abs(rNearest.tile.X - currTile.X) + Math.Abs(rNearest.tile.Y - currTile.Y);

        foreach (Resource r in resourceStacks[resourceType])
        {
            int d2 = Math.Abs(r.tile.X - currTile.X) + Math.Abs(r.tile.Y - currTile.Y);

            if (d2 < d)
            {
                d = d2;
                rNearest = r;
            }
        }

        if (rNearest.amountBlocked >= rNearest.amount)
        {
            Debug.Log("FindNearestResourceStack -- BLOCKED resource stack for " + resourceType);
            return null;
        }

        return rNearest;
    }

    public void ResourceStackToStockpile(Resource r)
    {
        Debug.Log("CollectResourceStack -- " + r.amount + " units of " + r.type + " from " + r.tile.ToString());
        ChangeStockpile(r.type, r.amount);
        RemoveResourceStack(r);
    }

    #region CALLBACKS FOR REGISTERING

    void OnTileActive(Tile t)
    {
        if (t.hasResource)
            resourceGameObjectMap[t.resource].SetActive(true);
    }

    void OnTileInactive(Tile t)
    {
        if (t.hasResource)
            resourceGameObjectMap[t.resource].SetActive(false);
    }
    #endregion
}
