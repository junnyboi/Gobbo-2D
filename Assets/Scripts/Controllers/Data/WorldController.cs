using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Remoting;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering.Universal;
using System.Linq;
using System.Text.RegularExpressions;

public class WorldController : MonoBehaviour
{
    #region VARIABLE DECLARATION
    // Singleton instance for WorldController with override protected set
    public static WorldController Instance { get; protected set; }
    public World w { get; protected set; }

    public GameObject globalLight;
    private static System.Random r = new System.Random();

    // static prevents resetting
    static bool isLoadWorld = false;
    #endregion

    #region MONOBEHAVIOUR
    void Awake()
    {
        if (Instance != null)
            Debug.LogError("There should never be two world controllers.");

        // instantiate world
        if (isLoadWorld)
        {
            isLoadWorld = false;
            CreateWorldFromSaveFile();
        }
        else
            CreateEmptyWorld(100);

        Instance = this;
    }

    private void Start()
    {
        // Initialize goblyns
        w.CreateCreature(w.GetTileAtWorldCentre(-1));
        w.CreateCreature(w.GetTileAtWorldCentre(-1));

        // Create river and fish
        CreateRivers(25, true);

        // Initialize terrain features
        foreach (Tile t in w.tiles)
        {
            if (t.Z == 0) continue;
            TileSpriteController.Instance.SetTileTypeToTerrain(t);
        }

        // Initialize some furniture
        for (int z = 0; z < w.Depth; z++)
        {
            w.GetTileAtWorldCentre(z).North.Type = TileType.Floor;
            w.GetTileAtWorldCentre(z).South.Type = TileType.Floor;
            w.PlaceFurniture("Oil Lamp", w.GetTileAtWorldCentre(z).North, true);
            w.PlaceFurniture("Stairs", w.GetTileAtWorldCentre(z).South, true);
        }
        w.linkLayerTileList.Add(w.GetTileAtWorldCentre(0).South);
        Room.FloodFillFromTile(w.GetTileAtWorldCentre(0), false);

        // Create plants
        CreatePlants();

        // Spawn horror
        // w.CreateCreature(w.GetTileAt(w.Width/4, w.Height/4, 1), SpeciesCreature.Horror);

        // Spawn gorilla
        w.CreateCreatures(w.GetTileAtWorldCentre(1), SpeciesCreature.Gorilla, 1);

        // Initialize global lighting and callback
        globalLight = Instantiate(globalLight);
        w.cbChangedDepth += ToggleGlobalLightOnChangedDepth;

        // Initialize starting depth view
        w.SwitchLayer(0);
        w.SwitchLayer(1);   //DEBUG

        // TEST island floodfill
        //Room.FloodFillFromTile(w.GetTileAtWorldCentre(1), false, true);
    }

    private void Update()
    {
        //TODO: add pause/ unpause/ speed controls
        w.Update(Time.deltaTime);
    }
    #endregion

    #region WORLD CONTROLLER LOGIC
       public void ToggleGlobalLightOnChangedDepth()
    {
        //globalLight.SetActive(world.currentZDepth == 0 ? false : true);
        //globalLight.SetActive(true);
        globalLight.GetComponent<Light2D>().intensity = w.currentZDepth == 0 ? 0.2f : 1;
    }

    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        // round down coordinate to create snapping grid
        int x = Mathf.RoundToInt(coord.x);
        int y = Mathf.RoundToInt(coord.y);

        // return snapped tile at current depth
        return w.GetTileAt(x, y, -1);
    }

    public void NewWorld()
    {
        Debug.Log("<NewWorld> created.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void CreateEmptyWorld(int size)
    {
        // Create a world with Empty tiles
        w = new World(size, size);

        // centre the camera
        Camera.main.transform.position = new Vector3(size / 2, size / 2, Camera.main.transform.position.z);
    }

    #endregion

    #region FUN/DEBUG FUNCTIONS

    public void RandomizeTiles(int z = 0)
    {
        Debug.Log("Tiles randomized");
        for (int x = 0; x < w.Width; x++)
        {
            for (int y = 0; y < w.Height; y++)
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                    w.GetTileAt(x,y,z).Type = TileType.Soil;
                else
                    w.GetTileAt(x, y, z).Type = TileType.Floor;
            }
        }
    }

    public void CreateRandomWalk(int numberJobs = 100)
    {
        System.Random r = new System.Random();
        int furnCount = w.furnitureList.Count;
        for (int i = 0; i < numberJobs; i++)
        {
            int x = r.Next(0, w.Width);
            int y = r.Next(0, w.Height);
            Tile t = w.GetTileAt(x, y, 1);

            if (t.movementCost != 0)
                JobModeController.Instance.Walk(t);
        }
    }

    public void BuildTestPlayground(int z = 0)
    {
        Debug.Log("Invoked <BuildTestPlayground>");

        int l = w.Width / 2 - 1;
        int b = w.Height / 2 - 1;

        for (int x = l - 10; x <= l + 10; x++)
        {
            for (int y = b - 10; y <= b + 10; y++)
            {
                w.RemoveFurniture(w.GetTileAt(x, y, z));
                w.GetTileAt(x, y, z).Type = TileType.Floor;

                if ((x == l - 10 || x == l + 10 || y == b - 10 || y == b + 10 || x == l || y == b)
                    && w.GetTileAt(x, y, z) != w.GetTileAt(l, b + 5, z)
                    && w.GetTileAt(x, y, z) != w.GetTileAt(l, b - 5, z)
                    && w.GetTileAt(x, y, z) != w.GetTileAt(l + 5, b, z)
                    && w.GetTileAt(x, y, z) != w.GetTileAt(l - 5, b, z)
                   )
                    w.PlaceFurniture("Wall", w.GetTileAt(x, y, z));
            }
        }
    }

    void CreateRivers(int spacing, bool createFish)
    {
        Debug.Log("WorldController :: CreateRivers");
        // Carve Rivers
        int spacing2 = Mathf.RoundToInt(1.5f * spacing);

        w.GenerateRiver(w.GetTileAt(w.Width - spacing, 0, 1), 8);
        w.GenerateRiver(w.GetTileAt(w.Width - spacing2, 0, 1), 2);

        w.GenerateRiver(w.GetTileAt(w.Width - 1, spacing, 1), 6, true);
        w.GenerateRiver(w.GetTileAt(w.Width - 1, spacing2, 1), 3, true);

        MeshController.Instance.BuildWaterMesh();

        if (!createFish) return;

        // Spawn Fish
        int numRiverSections = 10;
        int tilesInSection = w.riverTiles.Count / numRiverSections - 1;
        
        if (w.riverTiles.Count >= 0)
        {
            for (int i = 0; i < numRiverSections; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (i % 2 == 1)
                        w.CreateCreature(w.riverTiles[i * tilesInSection + j], SpeciesCreature.Sturgeon);
                    else
                        w.CreateCreature(w.riverTiles[i * tilesInSection + j], SpeciesCreature.Koi);
                }
            }
        }
    }

    void CreatePlants()
    {
        Debug.Log("WorldController :: CreatePlants");
        Plant p;
        foreach (Tile t in w.tiles)
        {
            if (t.Z != 1) continue;
            if (t.hasFurniture) continue;

            int rNext = r.Next(40);
            if (t.Type == TileType.Water)
            {
                switch (rNext)
                {
                    case 0:
                        p = w.CreatePlant(t, "Tree", "Lotus_1");
                        break;
                    case 1:
                        p = w.CreatePlant(t, "Tree", "Lotus_2");
                        break;
                    default:
                        p = null;
                        break;
                }
            }
            else
            {
                switch (rNext)
                {
                    case 0:
                        p = w.CreatePlant(t, "Tree", "Tree_Large_1");
                        break;
                    case 1:
                        p = w.CreatePlant(t, "Tree", "Tree_Large_2");
                        break;
                    case 2:
                        p = w.CreatePlant(t, "Tree", "Tree_Small_1");
                        break;
                    case 3:
                        p = w.CreatePlant(t, "Tree", "Tree_Small_2");
                        break;
                    case 4:
                        p = w.CreatePlant(t, "Tree", "Bush_Large");
                        break;
                    case 5:
                        p = w.CreatePlant(t, "Tree", "Bush_Small");
                        break;
                    case 6:
                        p = w.CreatePlant(t, "Tree", "Flower_1");
                        break;
                    case 7:
                        p = w.CreatePlant(t, "Tree", "Flower_2");
                        break;
                    case 8:
                        p = w.CreatePlant(t, "Tree", "Bush_Large");
                        break;
                    case 9:
                        p = w.CreatePlant(t, "Tree", "Bush_Small");
                        break;
                    case 10:
                        p = w.CreatePlant(t, "Tree", "Flower_1");
                        break;
                    case 11:
                        p = w.CreatePlant(t, "Tree", "Flower_2");
                        break;
                    case 12:
                        p = w.CreatePlant(t, "Tree", "Watercolor Trees_0");
                        break;
                    case 13:
                        p = w.CreatePlant(t, "Tree", "Watercolor Trees_1");
                        break;
                    case 14:
                        p = w.CreatePlant(t, "Tree", "Watercolor Trees_2");
                        break;
                    case 15:
                        p = w.CreatePlant(t, "Tree", "Watercolor Trees_3");
                        break;
                    case 16:
                        p = w.CreatePlant(t, "Tree", "Watercolor Trees_4");
                        break;
                    case 17:
                        p = w.CreatePlant(t, "Tree", "Watercolor Trees_5");
                        break;
                    default:
                        p = null;
                        break;
                }

                if (p != null)
                {
                    p.canAge = false;   // Wild plants don't age
                    GameObject p_go = PlantsController.Instance.plantGameObjectMap[p];
                    p_go.isStatic = true;   // Mesh optimisation
                }
            }

            // Initialize plants at full size
            if (p != null)
            {
                GameObject p_go = PlantsController.Instance.plantGameObjectMap[p];
                SpriteRenderer p_sr = p_go.GetComponent<SpriteRenderer>();

                string s = Regex.Replace(p_sr.sprite.name, @"_\d", "");
                s = s.Replace("_", " ");
                p.subspecies = s;

                if (p.subspecies.Contains("Tree"))
                    p_sr.sortingLayerName = ("TallObject");
                    
                p.size = 1;
                p_go.transform.localScale = new Vector3(p.size, p.size, 1);
            }

            }
    }

    #endregion

    #region SAVE/LOAD WORLD
    public void SaveWorld()
    {
        Debug.Log("<SaveWorld>");

        // turns objects into XML
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, w);
        writer.Close();

        Debug.Log(writer.ToString());

        PlayerPrefs.SetString("SaveGame00", writer.ToString());
    }

    public void LoadWorld()
    {
        Debug.Log("<LoadWorld>");

        isLoadWorld = true;

        // Reload the scene to reset all data, and purge old references
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // when the world is reset, it'll call CreateWorldFromSaveFile()
    }

    void CreateWorldFromSaveFile()
    {
        //Debug.Log("<CreateWorldFromSaveFile>");
        // Create a world from our save file data
        String saveGameData = PlayerPrefs.GetString("SaveGame00");

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextReader reader = new StringReader(saveGameData);

        // deserialize returns a list of objects to cast into the world through world::read_xml
        w = (World)serializer.Deserialize(reader);
        reader.Close();

        // centre the camera
        CameraController.Instance.CentreWorldCamera(w);
    } 
    #endregion

}
