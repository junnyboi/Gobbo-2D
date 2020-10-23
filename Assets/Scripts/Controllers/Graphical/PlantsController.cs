using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlantsController : MonoBehaviour
{
    #region VARIABLE DECLARATION

    public Dictionary<Plant, GameObject> plantGameObjectMap;
    public Dictionary<string, Sprite> plantSprites;

    World world { get { return WorldController.Instance.w; } }
    public static PlantsController Instance;

    #endregion

    #region INITIALIZATION
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        LoadSprites();

        // Dictionary maps GameObjects to characters being rendered
        plantGameObjectMap = new Dictionary<Plant, GameObject>();

        // Register our callback so that our GameObject gets updated whenever a char's data changes
        world.cbPlantCreated += OnPlantCreated;
        world.cbPlantRemoved += OnPlantRemoved;

        // Check for pre-existing (loaded) plants, and trigger their callbacks
        foreach (Plant p in world.plantsList)
            OnPlantCreated(p);
    }
    void LoadSprites()
    {
        plantSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Plants/");
        foreach (Sprite s in sprites)
        {
            plantSprites.Add(s.name, s);
        }
    }
    #endregion

    #region CALLBACKS FOR REGISTERING
    public void OnPlantCreated(Plant p)
    {
        // This creates a new GameObject and adds it to our scene.
        GameObject p_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        plantGameObjectMap.Add(p, p_go);

        p_go.name = "Plant";
        p_go.transform.position = new Vector3(p.X, p.Y, 0);
        p_go.transform.SetParent(this.transform, true);
        p_go.transform.localScale = new Vector3(p.size, p.size, 1);


        SpriteRenderer sr = p_go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Plant";

        try
        {
            switch (p.species)
            {
                case "Cup Mushroom":
                    sr.sprite = plantSprites["MushroomCup"];
                    break;
                case "Button Mushroom":
                    sr.sprite = plantSprites["MushroomButton"];
                    break;
                case "Toadstool":
                    sr.sprite = plantSprites["Toadstool"];
                    break;
                case "Tree":
                    sr.sprite = plantSprites["Tree_Large_1"];
                    break;
                default:
                    Debug.LogError("PlantsController :: OnPlantCreated -- No corresponding sprite found for " + p.species);
                    break;
            }
        }
        catch
        {
            Debug.LogError("PlantsController :: OnPlantCreated -- No corresponding sprite found for " + p.species);
        }

        if (p.canAge)
        {
            p.cbPlantOlder_Month += OnPlantOlder_Month;
            p.cbPlantOlder_Day += OnPlantOlder_Day;
        }

        // Register callback for multi-layer rendering
        p.tile.cbTileActive += OnTileActive;
        p.tile.cbTileInactive += OnTileInactive;
    }

    void OnTileActive(Tile t)
    {
        if (t.hasPlant)
            plantGameObjectMap[t.plant].SetActive(true);
    }

    void OnTileInactive(Tile t)
    {
        if(t.hasPlant)
            plantGameObjectMap[t.plant].SetActive(false);
    }

    void OnPlantOlder_Month(Plant p)
    {
        if (!p.canAge) return;

        SpriteRenderer sr = plantGameObjectMap[p].GetComponent<SpriteRenderer>();
        string s = sr.sprite.name;

        if (p.isExpired)
        {
            if (s.Contains("_Dead"))
                return;

            if (s.Contains("_Old"))
                s = s.Replace("_Old","_Dead");
            else s += "_Dead";

            try
            {
                sr.sprite = plantSprites[s];
            }
            catch
            {
                Debug.LogError("Sprite does not exist: " + s);
            }

            // Create job to remove dead plant, then create job to sow the plant again
            world.jobQueue.Enqueue( new Job(p.tile, "Clear Plant", (theJob) =>
            {
                world.RemovePlant(p.tile, true);
            }));

            return;
        }

        if (p.age_years >= p.lifespan_years/2 &&
            p.age_years < p.lifespan_years)
        {
            if (s.Contains("_Old"))
                return;

            s += "_Old";

            try
            {
                sr.sprite = plantSprites[s];
            }
            catch
            {
                Debug.LogError("Sprite does not exist: " + s);
            }
        }

    }

    void OnPlantOlder_Day(Plant p)
    {
        if (!p.canAge) return;

        // Increase size of plant each day by an increment dependent on harvestcycle)
        if (p.size < 1)
        {
            float increment = (0.9f) / (p.harvestCycle_months * 29);
            p.size += increment;
            GameObject p_go = plantGameObjectMap[p];
            p_go.transform.localScale = new Vector3(p.size, p.size, 1);
        }

    }

    public void OnPlantRemoved(Plant p)
    {
        if (p == null) return;

        // TODO: customize plant resource drops
        //Resource r = LootMappings.smashedLootMap[p.subspecies].Clone(p.tile);
        Resource r = ResourceDrops.lootMap["Plant"].Clone(p.tile);
        ResourceController.Instance.PlaceResourceStack(p.tile, r);

        // destroy the visual GameObject
        Destroy(plantGameObjectMap[p]);

        // remove from dictionary map
        plantGameObjectMap.Remove(p);

        // unregister callbacks
        p.cbPlantOlder_Day -= OnPlantOlder_Day;
    }

    // Only added automatically to domestic plants, must be applied manually to wild plants
    public void OnHarvestReady(Plant p)
    {
        #region Validation Checks

        if (p == null)
            return;

        if (p.isBeingHarvested || !p.canHarvest || p.isExpired)
            return;

        Tile t = p.tile;
        if (t == null)
            return;

        #endregion

        p.SetHarvestStatus(true);    // So that only 1 harvest job is created

        //  Create job
        Job j = new Job(t, "Harvest", (theJob) =>
        {
            p.Harvest();
            p.SetHarvestStatus(false);
        });

        // On job cancelled:
        j.cbJobCancel += (theJob) =>
        {
            p.SetHarvestStatus(false);
        };

        // Add pending job
        world.jobQueue.Enqueue(j);
    }
    #endregion

    /*#region DEBUG
    public void CreateMushroom(Tile tile = null)
    {
        if (tile == null)
            world.CreatePlant(world.GetTileAtWorldCentre(-1), "Mushroom");
        else
            world.CreatePlant(tile, "Mushroom");
    }
    public void CreateMushroom()
    {
        world.CreatePlant(world.creaturesList[0].currTile, "Mushroom");
    }
    public void CreateToadstool()
    {
        world.CreatePlant(world.creaturesList[0].currTile, "Toadstool");
    } 
    #endregion*/

}
