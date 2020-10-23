using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


//////////////////// TODO LIST ////////////////////
/// 
/// SEGREGATE DATA MODEL FROM GRAPHICS (build-up to dwarf fortress style data model)
/// TORCHLIGHT ITEM
/// 
/// TILE MANAGEMENT (Check worldCoordinate for placing objects and null worldCoordinate when destroying)
/// BASE BUILDING (WALL DRAWING)
/// SIMPLE UI BUTTONS FOR BUILD MODE
/// JOB QUEUE SYSTEM
/// DESIGNATE TILES TO JOB QUEUE
/// COMPANION NPC AI
/// COMPANION NPC RECRUITMENT
/// LADDERS FOR NPC VERTICAL MOVEMENTS
/// PATHFINDING - COMPLEX NETWORK SYSTEM
/// SAVE/LOAD WORLD MAP (SET GENERATE WORLD TO ADHOC)
/// DOORS
/// ROOM DETECTION
/// FLOOD-FILL FOR ROOMS
/// CRAFTING UI / TOOLTIP
/// INVENTORY MANAGEMENT 2.0
/// FURNITURE JOBS (higher tier crafting that only companions can do)
/// HAULING INVENTORY
/// STOCKPILES (items get added to global resource stockpiles --> interface)
/// MULTI-TILE OBJECTS
/// DECONSTRUCTION
/// MERGING ROOMS
/// ORE MINES (workbench type jobs with unlimited resources)
/// NATURE RESERVES (sustainable, regenerating forests, can assign companions to work)
/// DYNAMIC UI BUTTONS (only show what I can build)
/// REPEATING JOBS
/// SAVING/LOADING ROOMS
/// XML DATA FILES (for things like furniture and characters)
/// IMPROVE PATHFINDING
/// MULTIPLE CHARACTERS
/// SELECTING ITEMS
/// BETTER INTERFACE (with colony & resource information)
/// DIALOG BOX FOR SAVE/LOAD
/// 
/// DAYTIME ENEMIES (POACHERS)
/// NIGHTIME ENEMIES (SPIDERS)
/// AI BATTLE BEHAVIOUR
/// SLIMEBOY DEATH MECHANICS
/// CAMOUFLAGE MECHANICS
/// ORE VEINS
/// 4 SEASONS
/// DUNGEON (min spanning tree algorithm)
/// SLOPES
/// FLUID MECHANICS
/// ENVIRONMENT SOUNDS
/// 
/// 
/////////////////////////////////////////////////// 

public class WorldManager : MonoBehaviour
{    
    public static WorldManager Instance { get; private set; }
    public TMP_Text globalPrint;
    Rigidbody2D pivot;

    GameObject selectedTile;
    public GameObject[] wallCave = new GameObject[4];
    public GameObject[] tileDirt = new GameObject[4];
    public GameObject[] tileGrass = new GameObject[4];
    public GameObject[] tileStone = new GameObject[4];
    public GameObject[] plants = new GameObject[4];
    public GameObject[] trees = new GameObject[4];
    public GameObject[] rocks = new GameObject[4];

    public bool isGenerateWorld = true;

    public int width = 100;
    public int heightUnderground = 50;
    public int peakTerrain = 10;
    public float smoothTerrain = 10;

    int[,] worldCoordinate;
    int maxHeight = 0;

    [Range(0,100)]
    public int cavePercent = 35;
    [Range(0, 50)]
    public int caveSmoothCycles = 20;
    [Range(0, 8)]
    public int caveNeighboursThreshold = 2;

    float seed;
    System.Random rand;
    System.Random randHash;

    private void Awake()
    {
        Instance = this;
        worldCoordinate = new int[2*width, 2*heightUnderground];
        rand = new System.Random();
        seed = Random.Range(-10000f, 10000f);
        randHash = new System.Random(seed.GetHashCode());
    }
    public void Print(string text)
    {
        globalPrint.text = text;
    }

    /// <summary>
    /// TILE INSTANTIATION - LEGEND
    /// -1 = cave
    /// 0 = - nil -
    /// 1 = stone
    /// 2 = dirt
    /// 3 = grass
    /// 4 = plants / flora
    /// 5 = trees (-2 layer)
    /// 6 = rocks / boulders (-2 layer)
    /// </summary>
    void Create(int x, int y)
    {
        if(x>-1 && y>-1)
        {
            // SELECT TILE
            if (worldCoordinate[x, y] == -1)
                RandomTileSelect(wallCave);
            else if (worldCoordinate[x, y] == 1)
                RandomTileSelect(tileStone);
            else if (worldCoordinate[x, y] == 2)
                RandomTileSelect(tileDirt);
            else if (worldCoordinate[x, y] == 3)
                RandomTileSelect(tileGrass);
            else if (worldCoordinate[x, y] == 4)
                RandomTileSelect(plants);
            else if (worldCoordinate[x, y] == 5)
                RandomTileSelect(trees);
            else if (worldCoordinate[x, y] == 6)
                RandomTileSelect(rocks);
            else
                selectedTile = null;

            //Debug.Log(x + "," + y + ", selected tile: " + selectedTile);
            // BUILD TILE
            Vector2 startPosition = pivot.position;
            if (selectedTile != null)
                Instantiate(selectedTile, startPosition - new Vector2(width / 2, peakTerrain + heightUnderground)
                                                + new Vector2(x, y), Quaternion.identity);
            //else Debug.LogError("selected tile missing");
            return;
        }
        
    }
    void RandomTileSelect(GameObject[] tileArray)
    {
        if (tileArray != null)
        {
            int i = rand.Next(0, tileArray.Length);
            if(tileArray[i] != null)
                selectedTile = tileArray[i];
        }
    }

    /// <summary>
    /// POPULATE WORLD COORDINATES WITH TILES
    /// </summary>
    public void Generate(Rigidbody2D pivotObj)
    {
        pivot = pivotObj;
        // initial random assignment for caves

        if (isGenerateWorld)
        {
            print("Procedurally generating world...");
            print("World generation: laying stones and digging caves...");
            for (int x = 0; x < width; x++)
            {
                // UNEVEN TERRAIN GENERATION - perlin noise 
                int perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(seed, x / smoothTerrain) * peakTerrain) + heightUnderground;

                if (perlinHeight > maxHeight)
                    maxHeight = perlinHeight;

                for (int y = 0; y < perlinHeight; y++)
                {
                    // start laying bottom layers first
                    // CAVE GENERATION - cellular automata 
                    if (y < perlinHeight - 8)
                    {
                        worldCoordinate[x, y] = 1;
                        if (randHash.Next(0, 100) < cavePercent)
                        {
                            worldCoordinate[x, y] = -1;
                        }
                    }

                    // layers 3-8: dirt
                    else if (y < perlinHeight - 2)
                        worldCoordinate[x, y] = 2;

                    // layer 2: grass
                    else if (y < perlinHeight - 1)
                        worldCoordinate[x, y] = 3;

                    //Create(x, y);
                }              
            }

            CaveSmoothener();
            Gardening();

            for (int x = 0; x < width; x++)
            {
                for (int y = maxHeight+1; y >= 0; y--)
                {
                    Create(x,y);
                }
            }

            Boundaries();


            WorldManager.Instance.Print("Welcome home Slimeboy.");
        }
        else Debug.LogError("isGenerateWorld: " + isGenerateWorld);
    }

    // PLANTS, FORESTS AND FLORA
    void Gardening()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = maxHeight - peakTerrain - 1; y < maxHeight; y++)
            {
                // locate tiles with grasss
                if(worldCoordinate[x,y] == 3)
                {
                    // plant flora
                    if (randHash.Next(0, 100) < 30)
                    {
                        worldCoordinate[x, y + 1] = 4;
                    }
                    // plant trees
                    else if (randHash.Next(0, 100) < 30)
                    {
                        worldCoordinate[x, y + 1] = 5;
                    }
                    // put small rocks
                    else if (randHash.Next(0, 100) < 30)
                    {
                        worldCoordinate[x, y + 1] = 6;
                    }
                }
            }
        }
    }

    // LEFT/RIGHT BOUNDARIES
    void Boundaries()
    {
        Print("Constructing x boundaries");
        for (int y = 0; y < 2 * heightUnderground-1; y++)
            {
            worldCoordinate[0, y] = 1;
            worldCoordinate[width, y] = 1;
            Create(0, y);
            Create(width, y);
        }
        Print("Constructing sky boundary");
        for (int x = 0; x < width; x++)
        {
            worldCoordinate[x, 2 * heightUnderground - 1] = 1;
            Create(x, 2 * heightUnderground - 1);
        }
    }

    // CAVE SMOOTHENING - cellular automata
    // by setting cell state as a function of the state of neighbouring tiles
    void CaveSmoothener()
    {
        print("Smoothening cave structures: " + caveSmoothCycles + " cycles");
        for (int i = 0; i < caveSmoothCycles; i++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < maxHeight-peakTerrain-1; y++)
                {
                    int neighbouringTilesCount = GetNeighbours(x, y);
                    // if # neighbours exceeds threshold, do nothing
                    if (neighbouringTilesCount > caveNeighboursThreshold)
                    {
                        //worldCoordinate[x, y] = -1;
                    }
                    // if # neighbours less than threshold, create cave
                    else if (neighbouringTilesCount < caveNeighboursThreshold)
                    {
                        worldCoordinate[x, y] = -1;
                    }
                }
            }
        }
    }

    // counts how many surrounding neighbours there are
    private int GetNeighbours(int pointX, int pointY)
    {
        int neighbourCount = 0;

        // iterate through the 8 surrounding cells
        for (int x = pointX - 1; x <= pointX ; x++)
        {
            for (int y = pointY - 1; y <= pointY; y++)
            {
                // check that neighbour cells are within the domain space
                if (x >= 0 && x < width && y >= 0 && y < maxHeight - peakTerrain - 1)
                {
                    // check that neighbour cells are not x-adjacent || cells within domain height 
                    if(x != pointX || y != 0 && y < maxHeight - peakTerrain - 1)
                    {
                        // add count if not a cave wall
                        if (worldCoordinate[x,y] != -1)
                        {
                            neighbourCount++;
                        }
                    }
                }
                // add count for surrounding cells 
                else
                {
                    neighbourCount++;
                }
            }
        }
        
        // total count of surrouding tiles
        return neighbourCount;
    }
}