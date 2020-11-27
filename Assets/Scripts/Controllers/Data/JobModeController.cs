using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering.Universal;

public enum JobMode
{
    Null,
    Build,
    Smash,
    Cancel
}
public enum JobModeType
{
    Null,
    Walk,
    Furniture, 
    Tile,
    Plant,
    Resource
}
public class JobModeController : MonoBehaviour
{
    #region VARIABLE DECLARATION

    World world { get { return WorldController.Instance.w; } }
    public static JobModeController Instance { get; protected set; }

    public static JobMode jobMode { get; protected set; }
    public static JobModeType jobModeType { get; protected set; }

    TileType build_tileType = TileType.Floor;
    string build_furnitureType;
    string build_material;
    string build_plantSpecies;

    #endregion

    void Start()
    {
        Instance = this;
        SetMode_Null();
    }

    #region SET BUILD MODES

    public void SetMode_Null()
    {
        jobMode = JobMode.Null;
        jobModeType = JobModeType.Null;
    }

    public void SetMode_Build()
    {
        jobMode = JobMode.Build;
        MouseController.SetCursor("Cursor_Hammer");
    }

    public void SetMode_Smash()
    {
        jobMode = JobMode.Smash;
        MouseController.SetCursor("Cursor_Destroy");
    }

    public void SetMode_Cancel()
    {
        jobMode = JobMode.Cancel;
        MouseController.SetCursor("Cursor_Cancel");
    }

    public void SetMode_Walk()
    {
        jobModeType = JobModeType.Walk;
    }

    public void SetType_Floor(string material)
    {
        jobModeType = JobModeType.Tile;
        build_tileType = TileType.Floor;
        build_material = material;
    }

    public void SetType_Water()
    {
        jobModeType = JobModeType.Tile;
        build_tileType = TileType.Water;
    }

    public void SetType_Furniture(string furnitureType)
    {
        jobModeType = JobModeType.Furniture;
        build_furnitureType = furnitureType;
    }

    public void SetType_Plant(string plantType)
    {
        jobModeType = JobModeType.Plant;
        build_plantSpecies = plantType;
    }

    #endregion

    #region JOB ASSIGNMENT LOGIC
    public void CreateJob(Tile t)
    {
        // Smash!
        if (jobMode == JobMode.Smash)
        {
            // Remove all pending jobs on tile t
            // Will remove dummy objects via cbJobRemoved
            t.RemovePendingJobs();

            // Create jobs to remove non-dummy objects
            if (t.hasPlant)
                RemovePlant(t);
            if (t.hasFurniture)
                RemoveFurniture(t);
            else if (t.hasFloor)
                RemoveFloor(t);
            // FIXME: currently removes resources, but should be collecting resources instead
            if (t.hasResource)
                RemoveResource(t);

            return;
        }

        // Cancel
        else if (jobMode == JobMode.Cancel)
        {
            t.RemovePendingJobs();
        }

        // Build 
        switch (jobModeType)
        {
            case JobModeType.Walk:
                Walk(t);                
                break;
            case JobModeType.Furniture:
                BuildFurniture(t);
                break;
            case JobModeType.Tile:
                BuildFloor(t, build_tileType);
                break;
            case JobModeType.Plant:
                BuildPlant(t);
                break;
            default:
                break;
        }
        return;
    }

    #endregion

    #region JOB CREATION

    #region Tile Jobs

    public void BuildFloor(Tile t, TileType type)
    {
        #region Validation Checks

        if (type == t.Type)
            return;

        if (t.pendingJobs.Count != 0)
        {
            foreach (Job job in t.pendingJobs)
            {
                if (job.jobType == "BuildFloor")
                    return;
            }
        }

        if (t.hasFurniture)
        {
            if (t.furniture.type == "Cave Wall") // Cannot build over cave walls
                return;
        }

        #endregion

        TileType oldType = t.Type;  // Used to revert in event of job cancellation
        string oldMaterial = t.material;
        SpriteRenderer sr = TileSpriteController.Instance.tileGameObjectMap[t].GetComponent<SpriteRenderer>();

        //  On job complete:
        Job j = new Job(t, "BuildFloor", (theJob) =>
        {
            t.hasDummyTile = TileType.Null;
            TileSpriteController.Instance.OnTileChanged(t);
            SoundController.Instance.OnFurnitureCreated();
            sr.sortingLayerName = "Tiles";

            if (t.Type == TileType.Floor)
                t.Elevation = .9f;
        });

        // On job cancelled:
        j.cbJobCancel += (theJob) =>
        {
            t.hasDummyTile = TileType.Null;
            t.material = oldMaterial;
            t.Type = oldType;
        };

        // Determine required resources based on the material
        // TODO: implement a dictionary for mapping required resources to construction
        if (type == TileType.Floor)
        {
            switch (build_material)
            {
                case "Wood":
                    j.requiredResources.Add("Wood", new Resource("Wood", 1));
                    break;
                case "Dirt":
                    //j.requiredResources.Add("Dirt Block", new Resource("Dirt Block", 1));
                    break;
                default:
                    break;
            }
        }

        // Create pending job
        world.jobQueue.Enqueue(j, 1);

        // Place dummy
        t.hasDummyTile = t.Type;
        t.material = build_material;
        t.Type = type;
    }

    public void RemoveFloor(Tile t)
    {

        switch (t.Z)
        {
            case 0:
                BuildFloor(t, TileType.Soil);
                t.material = "Soil";
                break;
            case 1:
                BuildFloor(t, world.DetermineTerrainType(0,0,t).type);
                t.material = "Grass";
                break;
            default:
                BuildFloor(t, TileType.Grass);
                t.material = "Grass";
                break;
        }

        // Delete room and unassign all tiles
        if (t.hasRoom)
            world.DeleteRoom(t.room);
    }

    #endregion

    #region Furniture Jobs

    public void BuildFurniture(Tile t, String forceType = null, bool isAutoGenerated = false)
    {
        // WIP: need to implement single placement logic eg. doors
        string furnType = build_furnitureType;

        if (forceType != null)
            furnType = forceType;

        if (world.isFurniturePlacementValid(furnType, t))
        {
            //  On job complete:
            Job j = new Job(t, furnType, (theJob) =>
            {
                t.hasDummyFixedObject = false;
                try
                {
                    Furniture furn = t.furniture;
                    GameObject furn_go = FurnitureSpriteController.Instance.mapOfFurnToGO[furn];
                    SpriteRenderer sr = furn_go.GetComponent<SpriteRenderer>();
                    sr.color = new Color(1, 1, 1, 1);
                    SoundController.Instance.OnFurnitureCreated();
                    sr.sortingLayerName = "Furniture";

                    // For light emitting furniture
                    if (furn_go.GetComponent<Light2D>() != null)
                        furn_go.GetComponent<Light2D>().enabled = true;

                    // For stairs and linkLayer furniture
                    if (furn.isLinkedVertically)
                    {
                        Tile t2 = null;
                        if (t.Above != null && !t.Above.hasFurniture && !t.Above.hasPlant)
                            t2 = t.Above;
                        else if (t.Below != null && !t.Below.hasFurniture && !t.Below.hasPlant)
                            t2 = t.Below;
                        else
                        {
                            Debug.LogError("Build Stairs -- No valid counterpart stairs can be placed!");
                            // Remove stairs if no valid counterpart can be placed
                            RemoveFurniture(t);
                        }

                        if (t2 != null)
                        {
                            Debug.Log("Constructing counterpart stairs at " + t.ToString());
                            world.PlaceFurniture(furn.type, t.Above);
                            world.linkLayerTileList.Add(t.Normalize);
                        }
                    }
                }
                catch
                {
                    Debug.Log("BuildFurniture -- Furn_go does not exist, it may already have been removed!");
                }
            }
            );

            // On job cancelled:
            j.cbJobCancel += (theJob) =>
            {
                if (t.hasDummyFixedObject)
                {
                    t.hasDummyFixedObject = false;
                    SpriteRenderer sr = FurnitureSpriteController.Instance.mapOfFurnToGO[t.furniture].GetComponent<SpriteRenderer>();
                    sr.color = new Color(1, 1, 1, 1);
                    world.RemoveFurniture(t);
                }
            };

            // Create pending job
            world.jobQueue.Enqueue(j, 3);

            // Place dummy
            t.hasDummyFixedObject = true;
            Furniture f = world.PlaceFurniture(furnType, t);

            if (f != null && f.requiresResources)
                j.requiredResources = f.requiredResources;
        }
        else if (world.furniturePrototypes[furnType].doesNotRequireFloor == false && !isAutoGenerated)
        {
            // Build floor first
            BuildFloor(t, TileType.Floor);

            // Then try constructing furniture again
            if (t.furniture == null)
                world.jobQueue.Enqueue(new Job(t, "BuildFurniture", (j) =>
                {
                    BuildFurniture(t, furnType, true);
                }));
        }
        else
        {
            // Tile must be occupied by other furniture
            // cancel all jobs on the tile to debug
            t.RemovePendingJobs();
        }
    }

    public void RemoveFurniture(Tile t)
    {
        if (!t.hasFurniture) return;

        Furniture f = t.furniture;
        Job j;

        if (f.isLinkedVertically)
        {
            j = new Job(t, "Deconstruct_" + f.type, (theJob) =>
            {
                ResourceController.Instance.PlaceLoot(t, f.type);
                world.RemoveFurniture(t);
                if (t.Above != null && !t.Above.hasFurniture)
                    world.RemoveFurniture(t.Above);
                else if (t.Below != null && !t.Below.hasFurniture)
                    world.RemoveFurniture(t.Below);
            });
        }
        else
        {
            j = new Job(t, "Deconstruct_" + f.type, (theJob) =>
            {
                ResourceController.Instance.PlaceLoot(t, f.type);
                world.RemoveFurniture(t);
            });
        }

        // Change to red color for feedback
        SpriteRenderer sr = FurnitureSpriteController.Instance.mapOfFurnToGO[f].GetComponent<SpriteRenderer>();
        sr.color = new Color(1, 0.5f, 0.5f, 0.9f);
        sr.sortingLayerName = "Jobs";

        j.cbJobCancel += (theJob) =>
        {
            sr.color = new Color(1,1,1,1);
            sr.sortingLayerName = "Furniture";
        };

        // Add job to the queue
        world.jobQueue.Enqueue(j, 2);
    }

    #endregion

    #region Plant Jobs

    public void BuildPlant(Tile t, string forceSpecies = null)
    {
        // First prepare the soil for seeding
        switch (t.Type)
        {
            case TileType.Soil:
                BuildFloor(t, TileType.Cultivated);
                break;
            case TileType.Cultivated:
                break;
            default:
                return;
        }

        // Decide what species to plant
        string plantSpecies = build_plantSpecies;

        if (forceSpecies != null)
            plantSpecies = forceSpecies;

        // Create job
        if (t.ValidateFixedObjectPlacement())
        {            
            //  On job complete:
            Job j = new Job(t, plantSpecies, (theJob) =>
            {
                Plant p = world.CreatePlant(t, plantSpecies);
                SoundController.Instance.OnFurnitureCreated();

                // Automatically add harvest callback for domestic plants (but not wild plants)
                p.cbPlantOlder_Month += PlantsController.Instance.OnHarvestReady;
            }
            );

            // On job cancelled:
            j.cbJobCancel += (theJob) =>
            {
                world.RemovePlant(t);
            };

            // Create pending job
            world.jobQueue.Enqueue(j, 3);
        }
    }
    public Job RemovePlant(Tile t)
    {
        if (t.Type == TileType.Cultivated)
            BuildFloor(t, TileType.Soil);

        if (t.plant == null) return null;

        Plant p = t.plant;

        Job j = new Job(t, "Deconstruct_" + p.species, (theJob) =>
        {
            //execute these codes when the job complete
            p.cbPlantOlder_Month -= PlantsController.Instance.OnHarvestReady;
            world.RemovePlant(t);

            SoundController.Instance.OnFurnitureCreated();
        } );

        // Change to red color for feedback
        SpriteRenderer sr = PlantsController.Instance.plantGameObjectMap[p].GetComponent<SpriteRenderer>();
        sr.color = new Color(1, 0.5f, 0.5f, 0.9f);

        j.cbJobCancel += (theJob) =>
        {
            sr.color = new Color(1, 1, 1, 1);
        };

        // add job to the queue
        world.jobQueue.Enqueue(j, 2);
        return j;
    }

    #endregion

    #region Null Jobs

    public void Walk(Tile t)
    {
        // Validation checks
        if (t.movementCost < 1000000)
        {
            // create job to walk to tile t
            Job j = new Job(t, "Walk", (job) =>
            {
                if (t.hasFurniture && t.furniture.isLinkedVertically)
                {
                    job.owner.SwitchZDepth(t);
                }
            }, 0);

            // add job to the queue
            world.jobQueue.Enqueue(j, 0);
        }
    }

    #endregion

    #region Resource Jobs

    #endregion
    public void RemoveResource(Tile t)
    {
        if (!t.hasResource) return;
        Resource r = t.resource;

        Job j = new Job(t, "RemoveResource_" + r.type, (theJob) =>
        {
            ResourceController.Instance.RemoveResourceStack(r);
        });

        // Change to red color for feedback
        SpriteRenderer sr = ResourceController.Instance.resourceGameObjectMap[r].GetComponent<SpriteRenderer>();
        sr.color = new Color(1, 0.5f, 0.5f, 0.9f);
        sr.sortingLayerName = "Jobs";

        j.cbJobCancel += (theJob) =>
        {
            sr.color = new Color(1, 1, 1, 1);
            sr.sortingLayerName = "Resource";
        };

        // Add job to the queue
        world.jobQueue.Enqueue(j, 3);
    }
    #endregion
}
