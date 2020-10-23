using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Remoting;
using UnityEditor;
using UnityEngine.Playables;
using System.Linq;

public class TileSpriteController : MonoBehaviour
{
    #region VARIABLE DECLARATION
    GameObject defaultSpriteGO;
    Dictionary<string, Sprite> tileSprites;
    Dictionary<string, List<Sprite>> tileSpritesheets = new Dictionary<string, List<Sprite>>();
    public Dictionary<Tile, GameObject> tileGameObjectMap { get; protected set; } = new Dictionary<Tile, GameObject>();
    public List<GameObject> listOfTileLayers { get; protected set; } = new List<GameObject>();

    // shortcut to reference the world
    World world { get { return WorldController.Instance.w; } }
    public static TileSpriteController Instance;
    #endregion

    #region MONOBEHAVIOUR & LOADING
    void Start()
    {
        Instance = this;

        LoadSprites();
        LoadSpritesheets();

        // Create parent GameObjects for each layer
        for (int z = 0; z < world.Depth; z++)
        {
            GameObject layer_go = new GameObject("Tile Layer -" + z);
            listOfTileLayers.Add(layer_go);
            layer_go.transform.SetParent(GameObject.Find("InstantiatedGameObjects").transform);
        }

        // Create a GameObject for each tile
        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                for (int z = 0; z < world.Depth; z++)
                {
                    // Get the tile data
                    Tile tile = world.GetTileAt(x, y, z);

                    // This creates a new GameObject and adds it to our scene.
                    GameObject tile_go = Instantiate(defaultSpriteGO, new Vector3(tile.X, tile.Y, tile.Z),
                                                     Quaternion.identity, listOfTileLayers[z].transform);
                    //new GameObject("Tile_" + x + "_" + y + "_" + z);
                    tile_go.transform.position = new Vector3(tile.X, tile.Y, tile.Z);

                    // Add our tile/GO pair to the dictionary.
                    tileGameObjectMap.Add(tile, tile_go);

                    // Set tile object as the child of a "Layer" GameObject
                    //tile_go.transform.SetParent(listOfTileLayers[z].transform, true);

                    SpriteRenderer sr = tile_go.GetComponent<SpriteRenderer>();
                    //sr.sortingLayerName = "Tiles";

                    // Initialize Empty as default tile sprite
                    //sr.sprite = tileSprites["Soil"];

                    // Update tile sprites
                    OnTileChanged(tile);

                    // Register callback for multi-layer rendering
                    tile.cbTileActive += (Tile) => { tile_go.SetActive(true); };
                    tile.cbTileInactive += (Tile) => { tile_go.SetActive(false); };
                }
            }
        }

        // Register our callback so that our GameObject gets updated whenever a tile's data changes
        world.cbTileChanged += OnTileChanged;
    }

    void LoadSprites()
    {
        defaultSpriteGO = Resources.LoadAll<GameObject>("Prefabs/Tiles/")[0];
        tileSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Tiles/");
        foreach (Sprite s in sprites)
        {
            tileSprites.Add(s.name, s);
        }
    }

    void LoadSpritesheets()
    {
        List<Sprite> riverSpritesheet = new List<Sprite>();

        foreach (Sprite s in tileSprites.Values)
        {
            if (s.name.Contains("River"))
                riverSpritesheet.Add(s);
        }

        if (riverSpritesheet.Count > 0)
            tileSpritesheets.Add("River", riverSpritesheet);
        else
            Debug.LogError("ERROR while loading riverSpritesheet.");
    } 
    #endregion


    public void OnTileChanged(Tile t)
    {
        if(tileGameObjectMap.ContainsKey(t) == false)
        {
            Debug.LogError("tileGameObjectMap does not contain this tile_data -- " +
                           "did you forget to add the tile to the dictionary?");
            return;
        }
            
        GameObject t_go = tileGameObjectMap[t];
        SpriteRenderer sr = t_go.GetComponent<SpriteRenderer>();

        if (t_go == null)
        {
            Debug.LogError("GameObject mapped to " + t + " is a null.");
            return;
        }

        RefreshTileSprite(t, sr);        

        if (t.hasDummyTile != TileType.Null)
        {
            sr.color = new Color(0.5f, 0.5f, 3, 1f);
        }
    }

    public void SetTileTypeToTerrain(Tile t)
    {
        // NOTE: currently for surface tiles only
        if (t.Z != 0 && t.Elevation > 0)
        {
            // Check tile terrain and change tile type
            TileTerrain terrain = t.w.DetermineTerrainType(t.Elevation, t.Moisture);
            t.Type = terrain.type;
        }
    }

    void ColorByElevation(Tile t, SpriteRenderer sr)
    {
        if (t.Z != 0 && t.Elevation > 0)
            sr.color = Color.Lerp(Color.black, Color.white, t.Elevation);
        else if (t.Z == 0)
            sr.color = new Color(1, 1, 1, 1);
    }

    void RefreshTileSprite(Tile t, SpriteRenderer sr)
    {
        ColorByElevation(t, sr);

            switch (t.Type)
        {
            case TileType.Soil:
                sr.sprite = tileSprites["Soil"];
                break;
            case TileType.Cultivated:
                sr.sprite = tileSprites["Cultivated"];
                break;
            case TileType.Floor:
                sr.sprite = tileSprites["Wood"];
                break;
            case TileType.Grass:
                sr.sprite = tileSprites["Grass"];
                break;
            case TileType.Water:
                sr.color = Color.Lerp(new Color(0, 0, 0, 0.8f), new Color(0, 0, 0, 0f), t.Elevation);
                sr.sprite = tileSprites["Null"];

                // TEST -- alpha gradient transition over 2 tiles
                List<Tile> neighbours = t.GetNeighbours();
                List<Tile> neighbours2 = new List<Tile>();
                foreach (Tile t2 in neighbours)
                {
                    if (t2 != null && t2.Type != TileType.Water)
                    {
                        SpriteRenderer sr2 = tileGameObjectMap[t2].GetComponent<SpriteRenderer>();
                        sr2.color = new Color(1, 1, 1, 0.5f);
                        neighbours2 = t2.GetNeighbours();
                    }

                    foreach (Tile t3 in neighbours2)
                    {
                        if (t3 != null && t3.Type != TileType.Water && !neighbours.Contains(t3))
                        {
                            SpriteRenderer sr3 = tileGameObjectMap[t3].GetComponent<SpriteRenderer>();
                            if (sr3.color.a == 1)
                                sr3.color = new Color(1, 1, 1, 0.8f);
                        }
                    }
                }

                break;
            default:
                sr.sprite = null;
                // Create animated tile
                /*Animator animator = tileGameObjectMap[t].AddComponent<Animator>();
                PlayableGraph playableGraph = PlayableGraph.Create();
                AnimationClip clip = CreateTileSpriteAnimationClip("River_Anim", tileSpritesheets["River"]);
                AnimationPlayableUtilities.PlayClip(animator, clip, out playableGraph);*/
                break;
        }
    }

    #region UTILITY
    void DestroyAllTileGameObjects()
    {
        // use this function when changing floors/levels
        // destroys all visual GameObjects -- but not the actual tile data!

        foreach (KeyValuePair<Tile, GameObject> item in tileGameObjectMap)
        {
            Tile tile_data = item.Key;
            GameObject tile_go = item.Value;

            // remove the pair from the dictionary map (by removing key)
            tileGameObjectMap.Remove(tile_data);

            // unregister the callback
            tile_data.cbTileTypeChanged -= OnTileChanged;

            // destroy visual gameObject
            Destroy(tile_go);
        }

        Debug.Log("All gameObjects destroyed, ready to load new scene.");
    }

    private AnimationClip CreateTileSpriteAnimationClip(string name, List<Sprite> sprites)
    {
        int framecount = sprites.Count;
        float frameLength = 1f / framecount;

        // Create a blank clip and configure settings
        AnimationClip clip = new AnimationClip();
        clip.frameRate = framecount;
        clip.name = name;
        clip.wrapMode = WrapMode.Loop;

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Create curveBinding
        EditorCurveBinding curveBinding = new EditorCurveBinding();
        curveBinding.type = typeof(SpriteRenderer);
        curveBinding.propertyName = "m_Sprite";

        // Assign sprites to keyframe from the list of sprites
        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[framecount];

        for (int i = 0; i < framecount; i++)
        {
            ObjectReferenceKeyframe kf = new ObjectReferenceKeyframe();
            kf.time = i * frameLength;
            kf.value = sprites[i];
            keyFrames[i] = kf;
        }

        // Combine to create clip
        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);

        return clip;
    } 
    #endregion
}
