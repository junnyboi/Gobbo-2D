using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TooltipController : MonoBehaviour, inspectorTooltip
{
    #region VARIABLE DECLARATION

    [Header("Tooltips")]
    public GameObject mouseoverTooltip;
    public GameObject inspectorTooltip;

    [Header("Pop-ups")]
    public PopupController_Character characterPopup;
    public PopupController detailsPopup;

    [Header("Materials")]
    public Material highlightMat;
    public Material spriteLitDefaultMat;

    [Header("Miscellaneous")]
    public static TooltipController Instance;
    public GameObject blackBackground;
    private Vector3 offset = new Vector3(90, -90);
    MouseController mouseController { get { return MouseController.Instance; } }
    TMP_Text[] tooltipTMPs;
    List<Tile> highlightedTiles = new List<Tile>();
    #endregion
    void Start()
    {
        Instance = this;
        blackBackground.SetActive(false);
        TooltipController.Instance.TurnOffDetailedTooltip();
        TooltipController.Instance.TurnOffCharacterPopup();
        tooltipTMPs = mouseoverTooltip.GetComponentsInChildren<TMP_Text>();

        // Validation
        if (mouseController == null)
        {
            Debug.LogError("Why is the mouse controller missing!?");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Move tooltip to follow the mouse cursor
        //Vector3 newPosition = Input.mousePosition + offset;
        //transform.position = newPosition;

        // Get tile under mouse cursor
        Tile t = mouseController.GetMouseOverTile();

        // Check stuff on the tile
        tooltipTMPs[0].text = CheckRoom(t);
        tooltipTMPs[1].text = CheckTileType(t);
        tooltipTMPs[2].text = CheckFurniture(t);
        tooltipTMPs[3].text = CheckPlants(t);
        tooltipTMPs[4].text = CheckCreatures(t);
        tooltipTMPs[5].text = CheckResources(t);

        // Hide tooltip objects with nothing to display
        for (int i = 0; i < tooltipTMPs.Length; i++)
        {
            if (tooltipTMPs[i].text == "")
                tooltipTMPs[i].gameObject.SetActive(false);
            else
                tooltipTMPs[i].gameObject.SetActive(true);
        }

        // Automatically resize tooltip
        AutomaticVerticalSize AVS = mouseoverTooltip.GetComponent<AutomaticVerticalSize>();
        AVS.AdjustSize(2.5f);

    }

    #region CHECK TILE
    string CheckRoom(Tile t)
    {
        string s = "";

        if (t == null) return s;

        int roomIndex = t.w.roomList.IndexOf(t.room);

        if (!t.hasRoom)
        {
            // Unhighlight previous room and assign highlighted room to null
            HighlightRoom(null);
            return WorldController.Instance.w.currentZDepth == 0 ? "The Underground" : "The Surface";
        }
        else
            HighlightRoom(t.room);

        if (t.room.owner == null)
        {
            s += "Room " + roomIndex.ToString();

            if (t.room.ToString() != "")
                s += ": " + t.room.ToString();
        }
        else
        {
            s = t.room.ToString();
        }
        return s;
    }

    public void HighlightRoom(Room newRoom)
    {

        // Un-highlight previous room
        if (highlightedTiles.Count > 0)
        {
            foreach (Tile t in highlightedTiles.ToArray())
            {
                GameObject t_go = TileSpriteController.Instance.tileGameObjectMap[t];
                SpriteRenderer t_sr = t_go.GetComponent<SpriteRenderer>();
                //t_sr.material = spriteLitDefaultMat;
                //TileSpriteController.Instance.RefreshTileMaterial(t, t_sr);
                t_sr.color = new Color(1, 1, 1);
            }
            highlightedTiles = new List<Tile>();
        }

        // Highlight the new room
        if (newRoom != null)
        {
            foreach (Tile t in newRoom.tilesInRoom.ToArray())
            {
                GameObject t_go = TileSpriteController.Instance.tileGameObjectMap[t];
                SpriteRenderer t_sr = t_go.GetComponent<SpriteRenderer>();
                //t_sr.material = highlightMat;
                t_sr.color = new Color(0.525f, 0.812f, 0.745f);
                highlightedTiles.Add(t);
            }
        }
    }

    string CheckTileType(Tile t)
    {
        string s = "";

        if (t == null)
            return s;

        if (t.Elevation > 0)
        {
            string terrainName = t.w.DetermineTerrainType(t.Elevation, t.Moisture).name;
            s += ", Elevation: " + t.Elevation + ", Terrain: " + terrainName;
        }
        return "Tile Type: " + t.Type.ToString() + s;
    }

    string CheckFurniture(Tile t)
    {
        string s = "";

        if (t != null && t.hasFurniture)
        {
            s += "Furniture: ";
            s += t.furniture.type;
        }

        return s;
    }

    string CheckPlants(Tile t)
    {
        string s = "";

        if (t != null && t.hasPlant)
        {
            s += t.plant.ToString();
        }

        return s;
    }

    string CheckCreatures(Tile t)
    {
        string names = "";

        if (t == null)
            return names;

        int count = t.creaturesOnTile.Count;

        if (count > 0)
        {
            names = count + " Creature";

            if (count == 1)
            {
                Creature c = t.creaturesOnTile[0];
                names += ": " + c.nameShortform
                         + "\n Health: " + c.health + "/100";
            }
            else
            {
                names += "s: ";
                for (int i = 0; i < count - 1; i++)
                {
                    names += t.creaturesOnTile[i].nameShortform + ", ";
                }
                names += t.creaturesOnTile[count - 1].nameShortform;
            }
        }

        return names;
    }

    string CheckResources(Tile t)
    {
        string names = "";

        if (t == null) return names;
        if (!t.hasResource) return names;

        names = t.resource.ToString();

        return names;
    }
    #endregion

    #region UTILITY
    public GameObject TurnOnInspectorTooltip()
    {
        inspectorTooltip.SetActive(true);
        //detailedTooltip.transform.SetParent(this.transform, true);
        Vector3 newPosition = Input.mousePosition + offset;
        inspectorTooltip.transform.position = newPosition;

        return inspectorTooltip;
    }
    public void TurnOffDetailedTooltip()
    {
        if (inspectorTooltip.activeSelf)
            inspectorTooltip.SetActive(false);
    }

    public void SetTooltipOffset(Vector3 offset)
    {
        this.offset = offset;
    }

    public void TurnOnCharacterPopup(Creature c)
    {
        characterPopup.SetActive(true);
        characterPopup.SetHeader(c.ToString());
    }
    public void TurnOffCharacterPopup()
    {
         characterPopup.SetActive(false);
    }
    #endregion
}
