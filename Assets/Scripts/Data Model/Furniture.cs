using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.PlayerLoop;
using JetBrains.Annotations;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;


/// <summary>
/// Furniture are things like walls, doors & furniture
/// </summary>
public class Furniture : IXmlSerializable
{
    #region VARIABLE DECLARATION

    public Tile tile { get; protected set; }
    int width = 1;
    int height = 1;
    public string type { get; protected set; }
    public string material; //TODO
    public float movementCost { get; protected set; }
    public bool isRoomEnclosing { get; protected set; }
    public bool isLinkedAdjacently { get; protected set; }
    public bool isLinkedVertically { get; protected set; }
    public bool doesNotRequireFloor { get; protected set; }

    public Dictionary<string, Resource> requiredResources = new Dictionary<string, Resource>();
    public bool requiresResources { get { return requiredResources.Count > 0; } }

    public event Action<Furniture> cbOnChanged;
    Func<Tile, bool> funcPositionValidation;

    /// <summary>
    /// LUA code will bind to these dictionary next time
    /// </summary>
    Dictionary<string, float> furnParameters;
    public event Action<Furniture, float> updateActions;
    public Func<Furniture, ENTERABILITY> IsEnterable;

    #endregion

    #region CONSTRUCTORS & CLONES
    
    public Furniture() { }  // public default constructor for iXmlSerializable

    // Copy constructor -- for copying prototypes, use Clone() for sub-classing which is more virtual
    protected Furniture(Furniture other)
    {
        // Clone parameters
        this.type = other.type;
        this.movementCost = other.movementCost;
        this.width = other.width;
        this.height = other.height;
        this.isLinkedAdjacently = other.isLinkedAdjacently;
        this.IsEnterable = other.IsEnterable;
        this.isRoomEnclosing = other.isRoomEnclosing;
        this.doesNotRequireFloor = other.doesNotRequireFloor;
        this.isLinkedVertically = other.isLinkedVertically;

        if (other.requiredResources != null)
            this.requiredResources = other.requiredResources;

        // Clone callbacks

        // Clone LUA parameters
        furnParameters = new Dictionary<string, float>(other.furnParameters);
        if (other.updateActions != null)
            this.updateActions = (Action<Furniture, float>)other.updateActions.Clone();
    }

    /// <summary>
    /// Makes a copy of the current furniture, subclass should overwrite the clone
    /// </summary>
    virtual public Furniture Clone()
    {
        return new Furniture(this);
    }

    // Constructor to create furniture from parameters -- used to create prototypes
    public Furniture(string furnitureType, float movementCost = 1, int width = 1, int height = 1,
                     bool linksToNeighbour = false, bool roomEnclosure = false, bool doesNotRequireFloor = false,
                     bool linksLayers = false, Dictionary<string, Resource> requiredResources = null)
    {
        this.type = furnitureType;
        this.movementCost = movementCost;
        this.width = width;
        this.height = height;
        this.isLinkedAdjacently = linksToNeighbour;
        this.isRoomEnclosing = roomEnclosure;
        this.doesNotRequireFloor = doesNotRequireFloor;
        this.isLinkedVertically = linksLayers;

        if (requiredResources != null)
            this.requiredResources = requiredResources;

        furnParameters = new Dictionary<string, float>();
        this.funcPositionValidation = this.DEFAULT__IsValidPosition;
    }
    #endregion

    #region FUNCTIONS
    static public Furniture PlaceInstance(Furniture prototype, Tile tile)
    {
        //Debug.Log("Furniture.PlaceInstance");

        if (prototype.funcPositionValidation(tile) == false)
        {
            Debug.LogError("PlaceInstance -- Position invalid");
            return null;
        }

        // call my custom copy constructor
        Furniture furn = prototype.Clone();
        furn.tile = tile;

        // FIXME: assumes 1x1 furniture
        if (tile.PlaceFurniture(furn) == false)
        {
            Debug.LogError("PlaceInstance -- Tile occupied");
            return null;
        }

        UpdateNeighbours(furn);

        if (furn.type == "Wall")
            Furniture.UpdateNeighbours(furn, "Door");

        return furn;
    }

    static public void UpdateNeighbours(Furniture furn, string furnType = null)
    {
        if (furn.isLinkedAdjacently)
        {
            // an object that links to neighbors like a wall should inform neighbours
            // that they have a new buddy and trigger their OnChangedCallback to update sprites

            int x = furn.tile.X;
            int y = furn.tile.Y;
            int z = furn.tile.Z;
            Tile t;

            #region Update neighbours in 8-directions
            if (furnType == null)
                furnType = furn.type;

            t = furn.tile.w.GetTileAt(x, y + 1, z); // North
            if (t != null && t.hasFurniture && t.furniture.type == furnType)
            {
                // if northern neighbour exists with the same obj type, tell the neighbour that it has changed by firing its callback
                t.furniture.cbOnChanged?.Invoke(t.furniture);
            }
            t = furn.tile.w.GetTileAt(x + 1, y + 1, z); // North-East
            if (t != null && t.hasFurniture && t.furniture.type == furnType)
            {
                t.furniture.cbOnChanged?.Invoke(t.furniture);
            }
            t = furn.tile.w.GetTileAt(x + 1, y, z); // East
            if (t != null && t.hasFurniture && t.furniture.type == furnType)
            {
                t.furniture.cbOnChanged?.Invoke(t.furniture);
            }
            t = furn.tile.w.GetTileAt(x + 1, y - 1, z); // South-East
            if (t != null && t.hasFurniture && t.furniture.type == furnType)
            {
                t.furniture.cbOnChanged?.Invoke(t.furniture);
            }
            t = furn.tile.w.GetTileAt(x, y - 1, z); // South
            if (t != null && t.hasFurniture && t.furniture.type == furnType)
            {
                t.furniture.cbOnChanged?.Invoke(t.furniture);
            }
            t = furn.tile.w.GetTileAt(x - 1, y - 1, z); // South-West
            if (t != null && t.hasFurniture && t.furniture.type == furnType)
            {
                t.furniture.cbOnChanged?.Invoke(t.furniture);
            }
            t = furn.tile.w.GetTileAt(x - 1, y, z); // West
            if (t != null && t.hasFurniture && t.furniture.type == furnType)
            {
                t.furniture.cbOnChanged?.Invoke(t.furniture);
            }
            t = furn.tile.w.GetTileAt(x - 1, y + 1, z); // North-West
            if (t != null && t.hasFurniture && t.furniture.type == furnType)
            {
                t.furniture.cbOnChanged?.Invoke(t.furniture);
            } 
            #endregion
        }
    }

    public void Update(float deltaTime)
    {
        updateActions?.Invoke(this, deltaTime);
    }

    #endregion

    #region PLACEMENT VALIDATION
    public bool IsValidPosition(Tile t)
    {
        return funcPositionValidation(t);
    }

    /// <summary>
    /// This will be replaced by validation checks from LUA files that are
    /// customizable for each piece of furniture
    /// </summary>
    protected bool DEFAULT__IsValidPosition(Tile t)
    {
        if (t.Type != TileType.Floor && !doesNotRequireFloor)
        {
            //Debug.Log("Tile does not have a floor");
            return false;
        }

        if (t.furniture != null)
        {
            if (t.hasDummyFixedObject == true)
                Debug.LogError("Tile is occupied by dummy furniture");
            else
                Debug.LogError("Tile is already occupied");

            return false;
        }

        return true;
    }

    #endregion

    #region SAVE / LOAD
    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", tile.X.ToString());
        writer.WriteAttributeString("Y", tile.Y.ToString());
        writer.WriteAttributeString("furnitureType", type.ToString());
        //writer.WriteAttributeString("movementCost", movementCost.ToString());
        //writer.WriteAttributeString("width", width.ToString());
        //writer.WriteAttributeString("height", height.ToString());
        writer.WriteAttributeString("linksToNeighbour", isLinkedAdjacently.ToString());

        foreach (string k in furnParameters.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", furnParameters[k].ToString());
            writer.WriteEndElement();
        }
    }

    public void ReadXml(XmlReader reader)
    {   // reads from save file and sets the data
        // X, Y and furnitureType have already been set and assigned to tile, just read extra data here
        //movementCost = int.Parse(reader.GetAttribute("movementCost"));
        //width = int.Parse(reader.GetAttribute("width"));
        //height = int.Parse(reader.GetAttribute("height"));
        isLinkedAdjacently = bool.Parse(reader.GetAttribute("linksToNeighbour"));

        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));
                furnParameters[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }
    #endregion

    #region LUA PARAMETERS
    public float GetParameter(string key, float default_value = 0)
    {
        if (furnParameters.ContainsKey(key) == false)
            return default_value;

        return furnParameters[key];
    }
    public void SetParameter(string key, float value)
    {
        furnParameters[key] = value;
    }
    public void ChangeParameter(string key, float value)
    {
        if (furnParameters.ContainsKey(key) == false)
        {
            SetParameter(key, value);
            return;
        }

        furnParameters[key] += value;
    }
    #endregion

    public override string ToString()
    {
        return type;
    }
}
