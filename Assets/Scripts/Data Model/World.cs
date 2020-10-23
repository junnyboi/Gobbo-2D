using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;
using UnityEngine.Experimental.Rendering.Universal;
using System.Linq;

public class World : IXmlSerializable
{
    #region VARIABLES DECLARATION

    public Tile[,,] tiles { get; protected set; }
    public int currentZDepth { get; protected set; } = 0;

    // Global arrays to hold other data
    public List<Plant> plantsList = new List<Plant>();
    public List<Creature> creaturesList = new List<Creature>();
    public Dictionary<Furniture, Tile> furnitureList = new Dictionary<Furniture, Tile>();   // cos List.remove is slow af for large quantities
    public List<Room> roomList = new List<Room>();

    public List<Tile> linkLayerTileList = new List<Tile>();
    public List<Tile> riverTiles = new List<Tile>();

    public Path_TileGraph tileGraph;
    public JobQueue jobQueue;
    public JobQueue jobQueueAwaitingResources = new JobQueue();

    public List<TileTerrain> terrainPrototypes { get; protected set; }
    public Dictionary<string, Furniture> furniturePrototypes { get; protected set; }
    public int Width { get; protected set; }
    public int Height { get; protected set; }
    public int Depth { get; protected set; }

    public System.Random r;

    #endregion

    #region CALLBACKS DECLARATION
    public event Action<Plant> cbPlantCreated;
    public event Action<Plant> cbPlantRemoved;
    public event Action<Creature> cbCreatureCreated;
    public event Action<Creature> cbCreatureRemoved;
    public event Action<Furniture> cbFurnitureCreated;
    public event Action<Furniture> cbFurnitureRemoved;
    public event Action<Tile> cbTileChanged;
    public event Action cbChangedDepth;
    #endregion

    #region REFERENCES DECLARATION
    GoblynPopController GoblynPop { get { return GoblynPopController.Instance; } }
    #endregion

    #region World Constructor & Update

    public World(int width = 50, int height = 50, int depth = 2)
    {
        SetupWorld(width, height, depth);
    }
    void SetupWorld(int width, int height, int depth)
    {
        #region Initialize various world variables and lists
        this.Width = width;
        this.Height = height;
        this.Depth = depth;
        tiles = new Tile[width, height, depth];

        creaturesList = new List<Creature>();
        jobQueue = new JobQueue();

        CreateTerrainPrototypes();
        CreateFurniturePrototypes();
        furnitureList = new Dictionary<Furniture, Tile>();
        linkLayerTileList = new List<Tile>();

        roomList = new List<Room>();
        roomList.Add(new Room());   // instantiate room 0 
        Debug.Log("Initialized room index: " + roomList.IndexOf(roomList.First()));
        plantsList = new List<Plant>();

        // Generate noise map for surface elevations
        float[,] surfaceElevationMap = GenerateNoiseMap(width, height, 10, 1);

        #endregion

        // Create Tiles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {               
                for (int z = 0; z < depth; z++)
                {
                    tiles[x, y, z] = new Tile(this, x, y, z);
                    tiles[x, y, z].cbTileTypeChanged += OnTileTypeChanged;
                    tiles[x, y, z].room = roomList[0];  // Default room 0 = Outside
                    tiles[x, y, z].creaturesOnTile = new List<Creature>();

                    // Setup starting tiles
                    switch (z)
                    {
                        // Underground Tiles
                        case 0:
                            // Create some starting space
                            if (!(x < width / 2 + 3  && x > width/2 - 3 &&
                                  y < height / 2 + 3 && y > height / 2 - 3)
                               )
                            {
                                Furniture wallCave = PlaceFurniture("WallCave", tiles[x, y, z]);
                            }

                            // Underground has no elevation
                            tiles[x, y, z].Elevation = -1;
                            break;

                        // Surface Tiles
                        case 1:
                            tiles[x, y, z].Type = TileType.Grass;
                            tiles[x, y, z].Elevation = surfaceElevationMap[x, y];
                            //TODO: Implement moisture map
                            tiles[x, y, z].Moisture = surfaceElevationMap[x, y];
                            break;

                        default:
                            break;
                    }                 
                }
            }
        }

        r = new System.Random();
    }
    public void Update(float deltaTime)
    {
        foreach (Creature c in creaturesList.ToArray())
            c.Update(deltaTime);
        foreach (Furniture f in furnitureList.Keys)
            f.Update(deltaTime);
        /*foreach (Room r in roomList)
            r.Update(deltaTime);*/
    }
    #endregion

    #region Layer / Depth Control
    public void SwitchLayer(int selectedLayer)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    if (z == selectedLayer)
                        tiles[x, y, z].SetActive = true;
                    else
                        tiles[x, y, z].SetActive = false;
                }
            }
        }
        currentZDepth = selectedLayer;
        cbChangedDepth?.Invoke();
    }

    public void ToggleLayer()
    {
        int selectedLayer = currentZDepth == 0 ? 1 : 0;
        SwitchLayer(selectedLayer);
    }
    #endregion

    #region Furniture Management

    void CreateFurniturePrototypes()
    {   // To be replaced by a function that reads all of our furniture data
        // from a text file in future
        furniturePrototypes = new Dictionary<string, Furniture>();

        furniturePrototypes.Add("Stairs",
            new Furniture(
            "Stairs",
            1.5f,      // passable, invalidates tilegraph
            1,      // width
            1,      // height
            false,  // links to neighbours?
            false,  // encloses a room?
            true,  // doesNotRequireFloor?
            true    // links layers?
            ));

        furniturePrototypes.Add("Bed",
            new Furniture(
            "Bed",
            1.5f,      // passable
            1,      // width
            2,      // height
            false,   // links to neighbours?
            false    // encloses a room?
            ));

        furniturePrototypes.Add("OilLamp",
            new Furniture(
            "OilLamp",
            1,      // passable
            1,      // width
            1,      // height
            false,   // links to neighbours?
            false,    // encloses a room?
            true    // does not require floor?
            ));

        furniturePrototypes.Add("WallCave",
            new Furniture(
            "WallCave",
            1000000,      // impassable
            1,      // width
            1,      // height
            false,   // links to neighbours?
            false,   // encloses a room?
            true    // does not require floor?
            ));

        furniturePrototypes.Add("Wall",
            new Furniture(
            "Wall",
            1000000,      // impassable
            1,      // width
            1,      // height
            true,   // links to neighbours?
            true,   // encloses a room?
            false,  // does not require floor?
            false,  //links layers?
            new Dictionary<string, Resource> { { "Wood", new Resource("Wood", 2) } }
            ));

        furniturePrototypes.Add("Door",
            new Furniture(
            "Door",
            1,      // passable
            1,      // width
            1,      // height
            true,   // links to neighbours?
            true    // encloses a room?
            ));
        furniturePrototypes["Door"].SetParameter("openness", 0);
        furniturePrototypes["Door"].SetParameter("is_opening", 0);
        // assign furniture actions to furniture update callback
        furniturePrototypes["Door"].updateActions += FurnitureActions.Door_UpdateAction;
        // assign isEnterable function to the door from FurnitureActions class
        furniturePrototypes["Door"].IsEnterable = FurnitureActions.Door_IsEnterable;
    }

    public Furniture PlaceFurniture(string furnitureType, Tile t, bool activateFurniture = false)
    {
        //FIXME: this function currently assumes 1x1 tiles
        if (furniturePrototypes.ContainsKey(furnitureType) == false)
        {
            Debug.LogError("furniturePrototypes does not contain a prototype for key: " + furnitureType);
            return null;
        }

        Furniture furn = Furniture.PlaceInstance(furniturePrototypes[furnitureType], t);

        if (furn == null)
            return null;    // failed to place object

        furnitureList.Add(furn, t);

        OnFurniturePlaced(furn, activateFurniture);

        return furn;
    }

    public void OnFurniturePlaced(Furniture furn, bool activateFurniture)
    {
        cbFurnitureCreated?.Invoke(furn);

        if (furn.movementCost != 1)
            InvalidateTileGraph();

        // Recalculate rooms
        if (furn.isRoomEnclosing)
            Room.FloodFillFromTile(furn.tile, true);

        if (furn.tile.hasRoom)
        {
            Room r = furn.tile.room;
            if (r != null && !r.uniqueFurnitureInRoom.ContainsKey(furn.type))
            {
                r.uniqueFurnitureInRoom.Add(furn.type, furn);
                r.CheckRoomType();
            }
        }

        // Activate dummy furniture
        if (activateFurniture)
        {
            FurnitureSpriteController.Instance.ActivateDummyFurniture(furn);
        }
    }

    public bool isFurniturePlacementValid(string furnitureType, Tile t)
    {
        return furniturePrototypes[furnitureType].IsValidPosition(t);
    }

    public Furniture GetFurniturePrototype(string objectType)
    {
        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("No furniture for: " + objectType);
        }
        return furniturePrototypes[objectType];
    }

    public void RemoveFurniture(Tile t)
    {
        // Debug.Log("World :: RemoveFurniture " + t);
        Furniture furn = t.furniture;
        if (furn == null)
            return;

        // Unassign furniture from everything
        furnitureList.Remove(furn);
        cbFurnitureRemoved?.Invoke(furn);

        // For furniture within the room
        if (furn.tile.hasRoom)
        {
            Room r = furn.tile.room;
            if (r != null && r.uniqueFurnitureInRoom != null && r.uniqueFurnitureInRoom.ContainsValue(furn))
            {
                r.uniqueFurnitureInRoom.Remove(furn.type);
                r.UnassignOwner();
                r.CheckRoomType();
            }
        }

        // For furniture enclosing the room, delete room and unassign all tiles
        if (furn.isRoomEnclosing)
        {
            Room.UnassignNeighbours(t);
            Room.FloodFillFromTile(t,false);
        }
    }

    #endregion

    #region Tile Management
    /// <summary>
    /// Gets the tile data at x and y.
    /// </summary>
    /// <returns>The <see cref="Tile"/>.</returns>
    public Tile GetTileAt(int x, int y, int z)
    {
        if (z < 0)
            z = currentZDepth;

        // Null if requested tile is out of range
        if (x >= Width || x < 0 || y >= Height || y < 0 || z >= Depth || z < 0)
            return null;

        return tiles[x, y, z];
    }

    public Tile GetTileAtWorldCentre(int Z)
    {
        if (Z < 0)
            Z = currentZDepth;

        return tiles[Width / 2, Height / 2, Z];
    }

    public Tile GetNearestLinkLayerTile(Tile startTile, EnvClass envclass)
    {
        if (linkLayerTileList.Count == 0)
        {
            Debug.LogError("GetNearestLinkLayerTile -- No link layer tiles available");
            return null;
        }

        //Debug.Log("World :: GetNearestLinkLayerTile");
        Path_AStar path;
        int startDepth = startTile.Z;
        if (startDepth != 0)
            startTile = GetTileAt(startTile.X, startTile.Y, 0);

        // Initialize first tile
        Tile nearestTile = linkLayerTileList[0]; 
        float currentCost = new Path_AStar(this, startTile, nearestTile, envclass).MovementCostTotal();

        foreach (Tile t in linkLayerTileList)
        {
            path = new Path_AStar(this, startTile, t, envclass);
            float newCost = path.MovementCostTotal();
            if (newCost < currentCost)
            {
                nearestTile = t;
                currentCost = newCost;
            }
        }

        if (startDepth != 0)
            nearestTile = GetTileAt(nearestTile.X, nearestTile.Y, startDepth);

        return nearestTile;
    }

    void OnTileTypeChanged(Tile t)
    {
        cbTileChanged?.Invoke(t);
        Room.FloodFillFromTile(t, false);
    }

    public void ResetTileGraph()
    {
        this.tileGraph = new Path_TileGraph(this);
    }

    /// <summary>
    ///This should be called whenever a change to the world renders the old pathfinding graph invalid
    /// </summary>
    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }

    #endregion

    #region Terrain Management
    public float[,] GenerateNoiseMap(int worldX, int worldY, int maxElevation,
                                    float scale = 0.99f, float offsetX = 0f, float offsetH = 0f)
    {
        float[,] noiseMap = new float[worldX, worldY];

        for (int x = 0; x < worldX; x++)
        {
            for (int y = 0; y < worldY; y++)
            {
                // calculate sample indices based on the coordinates, the scale and the offset
                float sampleX = (x + offsetX) / scale;
                float sampleH = (maxElevation + offsetH) / scale;

                float noise = 0f;
                float normalization = 0f;

                // Generate and combine 10 different waves using PerlinNoise
                for (int i = 0; i < 10; i++)
                {
                    NoiseWave wave = new NoiseWave();
                    noise += wave.amplitude * Mathf.PerlinNoise(sampleX * wave.frequency + wave.seed, sampleH * wave.frequency + wave.seed);
                    normalization += wave.amplitude;
                }
                // normalize the noise value so that it is within 0 and 1
                noise /= normalization;

                noiseMap[x, y] = noise;
            }
        }

        return noiseMap;
    }

    public void GenerateRiver(Tile startTile, int riverWidth, bool leftJoin = false)
    {
        int radius = riverWidth / 2;
        int z = startTile.Z;

        Queue<Tile> waypoints = new Queue<Tile>();
        waypoints.Enqueue(startTile);
        riverTiles.Add(startTile);

        // Carve river towards waypoints
        while (waypoints.Count > 0)
        {
            // Select starting tile
            Tile curr = waypoints.Dequeue();

            if (curr == null) continue;    // Terminate

            // Add next waypoint
            Tile next = curr.GetRandomNeighbour("River");

            // Join rivers at the top
            if (next == null && leftJoin)
            {
                // Add waypoints to the south-west
                next = curr.SouthWest;
                if (next != null && next.Type == TileType.Water) continue;  // Terminate
            }

            if (next == null) continue;    // Terminate

            waypoints.Enqueue(next);
            riverTiles.Add(next);

            #region Create the river around current tile
            for (int x = curr.X - radius; x < curr.X + radius + 1; x++)
                {
                    Tile t2 = GetTileAt(x, curr.Y, z);
                    if (t2 != null)
                    {
                        t2.Moisture = 1;
                        t2.Elevation = 1;
                        switch (Math.Abs(curr.X - x))
                        {
                            case 0:
                                t2.Elevation -= 0.5f;
                                break;
                            case 1:
                                t2.Elevation -= 0.4f;
                                break;
                            case 2:
                                t2.Elevation -= 0.3f;
                                break;
                            case 3:
                                t2.Elevation -= 0.2f;
                                break;
                            case 4:
                                t2.Elevation -= 0.1f;
                                break;
                            default:
                                break;
                        }
                    }
                }

            if (curr.X == 0 || curr.X == Width - 1)
            {
                for (int y = curr.Y - radius; y < curr.Y + radius; y++)
                {
                    Tile t2 = GetTileAt(curr.X, y, z);
                    if (t2 != null)
                    {
                        t2.Moisture = 1;
                        t2.Elevation = 1;
                        switch (Math.Abs(curr.Y - y))
                        {
                            case 0:
                                t2.Elevation -= 0.5f;
                                break;
                            case 1:
                                t2.Elevation -= 0.4f;
                                break;
                            case 2:
                                t2.Elevation -= 0.3f;
                                break;
                            case 3:
                                t2.Elevation -= 0.2f;
                                break;
                            case 4:
                                t2.Elevation -= 0.1f;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            #endregion
        }
    }

    public void CreateTerrainPrototypes()
    {
        terrainPrototypes = new List<TileTerrain>();

        // MUST BE ARRANGED IN ASCENDING ORDER OF ELEVATION, THEN MOISTURE
        terrainPrototypes.Add(new TileTerrain("Underground",
                                                TileType.Soil,
                                                -1,
                                                0
                                                ));
        // TODO: Water added via... rainfall? changing elevation?
        terrainPrototypes.Add(new TileTerrain("River",
                                                TileType.Water,
                                                0.0f,  // TEST
                                                1
                                                ));
        terrainPrototypes.Add(new TileTerrain("Subropical Desert",
                                                TileType.Null,
                                                0.25f,
                                                0.17f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Prairie Grassland",
                                                TileType.Grass,
                                                0.25f,
                                                0.33f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Tropical Seasonal Forest",
                                                TileType.Null,
                                                0.25f,
                                                0.67f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Tropical Rainforest",
                                                TileType.Null,
                                                0.25f,
                                                0.95f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Temperate Desert",
                                                TileType.Null,
                                                0.5f,
                                                0.17f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Savanna Grassland",
                                                TileType.Grass,
                                                0.5f,
                                                0.5f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Temperate Deciduous Forest",
                                                TileType.Null,
                                                0.5f,
                                                0.83f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Temperate Rainforest",
                                                TileType.Null,
                                                0.5f,
                                                0.95f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Semiarid Desert",
                                                TileType.Null,
                                                0.75f,
                                                0.34f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Shrubland",
                                                TileType.Grass,
                                                0.75f,
                                                0.67f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Taiga",
                                                TileType.Grass,
                                                0.75f,
                                                0.95f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Scorched",
                                                TileType.Soil,
                                                0.9f,
                                                0.17f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Bare",
                                                TileType.Soil,
                                                0.9f,
                                                0.34f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Tundra",
                                                TileType.Soil,
                                                0.9f,
                                                0.5f
                                                ));
        terrainPrototypes.Add(new TileTerrain("Snow",
                                                TileType.Soil,
                                                0.9f,
                                                0.95f
                                                ));
    }

    public TileTerrain DetermineTerrainType(float elevation, float moisture, Tile t = null)
    {
        if (t != null)
        {
            elevation = t.Elevation;
            moisture = t.Moisture;
        }

        foreach (TileTerrain terrain in terrainPrototypes)
        {
            // Check for water terrain first
            if (moisture >= 1 && terrain.moisture == 1)
            {
                // TODO: Factor in elevation
                return terrain;
            }

            // Return the first terrain whose height is higher than the generated one
            if (moisture < 1 && elevation < terrain.maxElevation)
            {
                if (moisture < terrain.moisture)
                {
                    return terrain;
                }
                continue;
            }
        }

        // Otherwise return the highest terrain possible
        return terrainPrototypes[terrainPrototypes.Count - 1];
    } 
    #endregion

    #region Room Management
    public Room GetOutsideRoom()
    {
        if (roomList.Count > 0)
            return roomList[0];
        else
            return null;
    }
    public void AddRoom(Room r)
    {
        roomList.Add(r);
    }
    public void DeleteRoom(Room room)
    {
        Room r = room;
        if (r == GetOutsideRoom() || r == null)
        {
            //Debug.LogError("DeleteRoom -- Tried to delete the outside room.");
            return;
        }
        roomList.Remove(r);     // Remove this room from rooms list
        r.UnassignAllTiles();   // Re-assign all tiles that belonged to this room to default
    }
    #endregion

    #region Goblyns & Population
    /////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                             GOBLINS & POPULATION
    ///
    /////////////////////////////////////////////////////////////////////////////////////

    public Creature CreateCreature(Tile t, SpeciesCreature species = SpeciesCreature.Goblyn)
    {
        switch (species)
        {
            case SpeciesCreature.Goblyn:
                Goblyn gob;
                if (GoblynPop.femaleCount < GoblynPop.maleCount)
                    gob = new Goblyn_Female(null, 1);
                else
                    gob = new Goblyn_Male(null, 1);
                RegisterNewCreature(gob);
                return gob;

            case SpeciesCreature.Sturgeon:
                CreatureTopView sturgeon = new CreatureTopView(t, "Sturgeon", species);
                sturgeon.envClass = EnvClass.Aquatic;
                RegisterNewCreature(sturgeon);
                return sturgeon;

            case SpeciesCreature.Koi:
                CreatureTopView koi = new CreatureTopView(t, "Koi", species);
                koi.envClass = EnvClass.Aquatic;
                RegisterNewCreature(koi);
                return koi;

            case SpeciesCreature.Horror:
                CreatureTopView horror = new CreatureTopView(t, "Horror", species);
                RegisterNewCreature(horror);
                horror.idleJobMode = "Faraway";
                horror.speed = 1.5f;
                return horror;

            case SpeciesCreature.Gorilla:
                Creature gorilla = new Gorilla(t, "Pinkie");
                RegisterNewCreature(gorilla);
                float rNext = r.Next(4, 7);
                gorilla.idlePauseTime = rNext;
                return gorilla;

            default:
                Creature c = new Creature(t);
                c.SetSpecies(species);
                RegisterNewCreature(c);
                return c;
        }
    }

    public void CreateCreatures(Tile t, SpeciesCreature species, int count)
    {
        if (t == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            CreateCreature(t, species);
        }
    }

    public void RegisterNewCreature(Creature c)
    {
        creaturesList.Add(c);

        // Update sprite, population and other stuff
        cbCreatureCreated?.Invoke(c);

        if (c is Goblyn)
        {
            Goblyn gob = c as Goblyn;
            DateTimeController.Instance.cbOnHourChanged += gob.OnGoblynOlder_hour;
            GoblynPopController.Instance.pop.cbOnExecuteSimulatorOnce += gob.ChangeAge;
        }
    }
    public void RemoveCreature(Creature c)
    {
        c.canUpdate = false;
        creaturesList.Remove(c);

        // Update sprite, population and other stuff
        c.canDoJob = false;
        c.AbandonJob();
        cbCreatureRemoved?.Invoke(c);

        if (c is Goblyn)
        {
            Goblyn gob = c as Goblyn;

            // Remove ownership from rooms and items
            if (gob.myRoom != null)
            {
                gob.myRoom.owner = null;
                gob.myBed = null;
                gob.myRoom = null;
            }

            DateTimeController.Instance.cbOnHourChanged -= gob.OnGoblynOlder_hour;
            GoblynPopController.Instance.pop.cbOnExecuteSimulatorOnce -= gob.ChangeAge;
        }
    }

    public void RemoveAllCreatures()
    {
        for (int i = creaturesList.Count - 1; i >= 0; i--)
        {
            RemoveCreature(creaturesList[i]);
        }
    }
    #endregion

    #region Flora & Fauna
    /////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                             FLORA AND FAUNA
    ///
    ///////////////////////////////////////////////////////////////////////////////////// 
    public Plant CreatePlant(Tile t, string species, string spriteName = null)
    {
        Plant p;
        switch (species)
        {
            case "Cup Mushroom":
                p = new Mushroom(2,3,4, "Cup Mushroom");
                break;
            case "Button Mushroom":
                p = new Mushroom(2,2,2, "Button Mushroom");
                break;
            case "Toadstool":
                p = new Toadstool();
                break;
            case "Tree":
                p = new Tree();
                break;
            default:
                p = new Mushroom();
                break;
        }

        p.species = species;
        p.tile = t;
        t.PlacePlant(p);
        plantsList.Add(p);

        // Update sprite, population and other stuff
        cbPlantCreated?.Invoke(p);
        DateTimeController.Instance.cbOnYearChanged += p.OneYearOlder;
        DateTimeController.Instance.cbOnMonthChanged += p.OneMonthOlder;
        DateTimeController.Instance.cbOnDayChanged += p.OneDayOlder;

        // Used to override sprite
        if (spriteName != null)
        {
            SpriteRenderer sr = PlantsController.Instance.plantGameObjectMap[p].GetComponent<SpriteRenderer>();
            Sprite s = PlantsController.Instance.plantSprites[spriteName];
            if (s != null) sr.sprite = s;
        }

        // Add information to existing room
        if (t.hasRoom && !t.room.uniquePlantsinRoom.ContainsKey(t.plant.subspecies))
        {
            t.room.uniquePlantsinRoom.Add(t.plant.subspecies, t.plant);
            t.room.CheckRoomType();
        }

        return p;
    }
    public void RemovePlant(Tile t, bool plantAgain = false)
    {
        Plant p = t.plant;

        if (p.canAge)
        {
            DateTimeController.Instance.cbOnYearChanged -= p.OneYearOlder;
            DateTimeController.Instance.cbOnMonthChanged -= p.OneMonthOlder;
            DateTimeController.Instance.cbOnDayChanged -= p.OneDayOlder;
        }

        if (plantAgain)
        {
            JobModeController.Instance.BuildPlant(t, p.species);
        }

        cbPlantRemoved?.Invoke(p);
        plantsList.Remove(p);
        t.PlacePlant(null);

    }
    #endregion

    #region Save & Load
    /////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                             SAVING & LOADING
    ///
    /////////////////////////////////////////////////////////////////////////////////////

    public World()
    {

    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    /// <summary>
    ///  SAVE info here
    /// </summary>
    /// <param name="writer"></param>
    public void WriteXml(XmlWriter writer)
    {
        Debug.Log("World:: <WriteXml>");

        // Main attributes
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        // Nested attributes
        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                int z = 0;
                if (tiles[x, y, z].Type != TileType.Soil && tiles[x, y, z].Type != TileType.Null)
                {
                    writer.WriteStartElement("Tile");
                    tiles[x, y, z].WriteXml(writer);   // call WriteXml function from class
                    writer.WriteEndElement();
                }
            }
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Furnitures");
        foreach (Furniture furn in furnitureList.Keys)
        {
            writer.WriteStartElement("Furniture");
            furn.WriteXml(writer);   // call WriteXml function from class
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Characters");
        foreach (Creature c in creaturesList)
        {
            writer.WriteStartElement("Character");
            c.WriteXml(writer);   // call WriteXml function from class
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Jobs");
        foreach (Creature c in creaturesList)
        {
            writer.WriteStartElement("Job");
            c.WriteXml_Job(writer);   // call WriteXml function from class
            writer.WriteEndElement();
        }
        while (jobQueue.count > 0)
        {
            writer.WriteStartElement("Job");
            Job j = jobQueue.Dequeue();
            j.WriteXml(writer);   // call WriteXml function from class
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    /// <summary>
    /// LOAD info here
    /// </summary>
    /// <param name="reader"></param>
    public void ReadXml(XmlReader reader)
    {
        //Debug.Log("World:: <ReadXml>");
        Width = int.Parse(reader.GetAttribute("Width"));
        Height = int.Parse(reader.GetAttribute("Height"));
        Height = int.Parse(reader.GetAttribute("Depth"));

        SetupWorld(Width, Height, Depth);

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Tiles":
                    ReadXml_Tiles(reader);
                    break;
                case "Furnitures":
                    ReadXml_Furnitures(reader);
                    break;
                case "Characters":
                    ReadXml_Characters(reader);
                    break;
                case "Jobs":
                    ReadXml_Jobs(reader);
                    break;
            }
        }
    }

    void ReadXml_Tiles(XmlReader reader)
    {
        //Debug.Log("ReadXml_Tiles");
        // We are in the "Tiles" element, so read elements until
        // we run out of "Tile" nodes.

        if (reader.ReadToDescendant("Tile"))
        {
            do      // do-while loops executes code before running while iteration
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                int z = 0;
                tiles[x, y, z].ReadXml(reader);
            } while (reader.ReadToNextSibling("Tile"));
        }
    }

    void ReadXml_Furnitures(XmlReader reader)
    {
        //Debug.Log("ReadXml_Furnitures");

        if (reader.ReadToDescendant("Furniture"))
        {
            do      // do-while loops executes code before running while iteration
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                int z = 0;
                string furnitureType = reader.GetAttribute("furnitureType");

                if (tiles[x, y, z].hasDummyFixedObject == true)     // let ReadXml_Jobs instantiate the dummy furniture instead
                {
                    //Debug.Log("ReadXml_Furnitures -- " + x +", " + y + " has DummyFurniture");
                    continue;
                }

                Furniture furn = PlaceFurniture(furnitureType, tiles[x, y, z]);
                furn.ReadXml(reader);
            } while (reader.ReadToNextSibling("Furniture"));
        }
    }

    void ReadXml_Characters(XmlReader reader)
    {
        //Debug.Log("ReadXml_Characters");

        if (reader.ReadToDescendant("Character"))
        {
            do      // do-while loops executes code before running while iteration
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                // Instantiate character at previously known location
                //Debug.Log("ReadXml_Characters -- creating chara at " + x + ", " + y);
                Creature c = CreateCreature(GetTileAt(x, y, -1));

                // Load other parameters into character
                c.ReadXml(reader);
            } while (reader.ReadToNextSibling("Character"));
        }
    }

    void ReadXml_Jobs(XmlReader reader)
    {
        //Debug.Log("ReadXml_Jobs");

        if (reader.ReadToDescendant("Job"))
        {
            do      // do-while loops executes code before running while iteration
            {
                if (reader.GetAttribute("X") == null || reader.GetAttribute("Y") == null)
                    continue;

                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                int z = 0;
                string jobObjectType = reader.GetAttribute("jobType");

                //Debug.Log("ReadXml_Jobs -- creating " + jobType + " job at " + x + ", " + y);

                JobModeController.Instance.BuildFurniture(tiles[x, y, z], jobObjectType);

            } while (reader.ReadToNextSibling("Job"));
        }
    } 
    #endregion
}

#region OTHER CLASSES
public class NoiseWave
{
    public float seed;
    public float frequency;
    public float amplitude;

    public NoiseWave (float seed = 3.86542f)
    {
        this.seed = seed;
        GenerateRandomWave();
    }

    public void GenerateRandomWave()
    {
        seed = UnityEngine.Random.Range(0, 99999) * 0.865942f * seed;
        frequency = UnityEngine.Random.Range(0, 10) * 0.8926516f * seed;
        amplitude = UnityEngine.Random.Range(0, 100) * 0.8432198f * seed;
    }
}

public class TileTerrain
{
    public string name;
    public TileType type;
    public float maxElevation;
    public float moisture;
    public Color color;
    public Sprite sprite;

    public TileTerrain(string name, TileType type, float maxElevation, float moisture)
    {
        this.name = name;
        this.type = type;
        this.maxElevation = maxElevation;
        this.moisture = moisture;
    }
}

#endregion
