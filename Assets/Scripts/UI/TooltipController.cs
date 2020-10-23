using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TooltipController : MonoBehaviour
{
    #region VARIABLE DECLARATION
    private Vector3 offset = new Vector3(90, -90);

    public static TooltipController Instance;
    MouseController mouseController { get { return MouseController.Instance; } }

    public GameObject blackBackground;
    public GameObject mouseoverTooltip;
    TMP_Text[] tooltipTMPs;

    public GameObject detailedTooltip;

    #endregion
    void Start()
    {
        Instance = this;
        blackBackground.SetActive(false);
        TooltipController.Instance.TurnOffDetailedTooltip();
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
        //detailedTooltip.transform.position = newPosition;

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

        if (t == null || t.room == null)
            return s;

        int roomIndex = t.w.roomList.IndexOf(t.room);

        if (roomIndex <= 0)
            return WorldController.Instance.w.currentZDepth == 0 ? "The Underground" : "The Surface";

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

        if (t != null && t.furniture != null)
        {
            s += "Furniture: ";
            s += t.furniture.type;
        }

        return s;
    }

    string CheckPlants(Tile t)
    {
        string s = "";

        if (t != null && t.plant != null)
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
    public GameObject TurnOnDetailedTooltip()
    {
        detailedTooltip.SetActive(true);
        //detailedTooltip.transform.SetParent(this.transform, true);
        Vector3 newPosition = Input.mousePosition + offset;
        detailedTooltip.transform.position = newPosition;

        return detailedTooltip;
    }
    public void TurnOffDetailedTooltip()
    {
        if (detailedTooltip.activeSelf)
        {
            detailedTooltip.SetActive(false);
        }
    }

    public void SetTooltipOffset(Vector3 offset)
    {
        this.offset = offset;
    } 
    #endregion
}
