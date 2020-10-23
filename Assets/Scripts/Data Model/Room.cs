using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    #region VARIABLE DECLARATION
    public float atmosphere = 0;
    public float temperature = 20;  // Degrees celcius

    public string roomType { get; protected set; } = null;
    public Creature owner = null;

    public List<Tile> tilesInRoom { get; protected set; } = new List<Tile>();
    public Dictionary<string, Furniture> uniqueFurnitureInRoom { get; protected set; } = new Dictionary<string, Furniture>();
    public Dictionary<string, Plant> uniquePlantsinRoom { get; protected set; } = new Dictionary<string, Plant>();

    #endregion

    public Room()
    {
    }

    public override string ToString()
    {
        string s = "";

        if (owner != null)
            s += owner.nameShortform + "'s ";

        if (roomType != null)
            s += roomType;

        return s;
    }

    public string CheckRoomType()
    {
        #region Validation

        // TODO: Check if a refresh is needed
        //if (roomType != null)   
        //    return roomType;

        if (WorldController.Instance != null
            && WorldController.Instance.w.GetOutsideRoom() != null
            && this == WorldController.Instance.w.GetOutsideRoom())
        {
            roomType = null;
            return null;
        }

        if (tilesInRoom.Count == 0)
        {
            // Probably due to checking the outside room before it has been assigned
            roomType = null;
            return null;
        }

        #endregion

        // Type: Bedroom
        if (uniqueFurnitureInRoom.ContainsKey("Bed"))
        {
            roomType = "Bedroom";

            // Assign empty bedroom to the first gob without a bedroom
            foreach (Goblyn gob in WorldController.Instance.w.creaturesList)
            {
                if (gob.myRoom == null)
                {
                    gob.myRoom = this;
                    gob.myBed = uniqueFurnitureInRoom["Bed"];
                    owner = gob;
                    return roomType;
                }
            }
        }

        // Type: Mushroom Farms
        else if (uniquePlantsinRoom.Count > 0)
        {
            bool hasMushroom = false;
            bool hasToadstool = false;
            foreach (Plant p in uniquePlantsinRoom.Values)
            {
                if (p is Mushroom)
                    hasMushroom = true;
                else if (p is Toadstool)
                    hasToadstool = true;
            }

            if (hasMushroom)
                roomType = "Mushroom Farm";
            else if (hasToadstool)
                roomType = "Toadstool Farm";

            if (hasMushroom && hasToadstool)
                roomType = "Mixed Fungi Farm";
        }

        if (roomType != null)
            return roomType;

        // At this point there is no valid roomType configuration, so assign null
        roomType = null;
        return null;
    }

    public void AssignTile(Tile t)
    {
        #region Validation
        if (tilesInRoom.Contains(t))  // already in this room
            return;
        if (t.room != null)     // belongs to another room
            t.room.tilesInRoom.Remove(t); 
        #endregion

        t.room = this;
        tilesInRoom.Add(t);

        // Track furniture in room
        if (t.hasFurniture && !uniqueFurnitureInRoom.ContainsKey(t.furniture.type))
            uniqueFurnitureInRoom.Add(t.furniture.type, t.furniture);

        // Track plants in room
        if (t.hasPlant && !uniquePlantsinRoom.ContainsKey(t.plant.subspecies))
            uniquePlantsinRoom.Add(t.plant.subspecies, t.plant);
    }

    public void UnassignAllTiles()
    {
        for (int i = 0; i < tilesInRoom.Count; i++)
        {
            // Assign to outside
            tilesInRoom[i].room = tilesInRoom[i].w.GetOutsideRoom();    
        }

        // Unassign tiles and furniture
        tilesInRoom = new List<Tile>();
        uniqueFurnitureInRoom = new Dictionary<string, Furniture>();

        // Unassign owner
        UnassignOwner();
    }

    public void UnassignOwner()
    {
        if (owner != null)
        {
            owner.myRoom = null;
            owner.myBed = null;
            owner = null;
        }
    }

    public void AssignOwner(Creature c)
    {
        if (owner == null && c != null && roomType == "Bedroom")
        {
            c.myRoom = this;
            c.myBed = this.uniqueFurnitureInRoom["Bed"];
            this.owner = c;
        }
    }

    public static void UnassignNeighbours(Tile t)
    {
        List<Tile> neighbours = t.GetNeighbours();
        foreach (Tile n in neighbours)
        {
            if (n.hasRoom)
            {
                n.w.DeleteRoom(n.room);
            }
        }
    }

    public static void FloodFillFromTile(Tile tile, bool excludeCurrentTile, bool includeIsland = false)
    {
        World world = tile.w;
        Room oldRoom = tile.room;

        // Try building a new room starting from each of 4 directions
        foreach (Tile t in tile.GetNeighbours())
            ActualFloodFill(t, oldRoom, includeIsland);

        if (excludeCurrentTile)
            tile.room = world.GetOutsideRoom();

        if (oldRoom != null)
        {
            if (oldRoom.tilesInRoom.Contains(tile))
                oldRoom.tilesInRoom.Remove(tile);

            // If this tile was added to an existing room, delete the old room
            // and reassign all tiles to the outside room
            if (oldRoom != world.GetOutsideRoom())
            {
                if (oldRoom.tilesInRoom.Count > 0)
                    Debug.LogError("DoRoomFloodFill -- oldRoom still has tiles assigned to it, something is wrong here.");
                world.DeleteRoom(oldRoom);    // reassigns tile to outside room and removes the old roo from the world
            }
        }
    }

    protected static void ActualFloodFill(Tile tile, Room oldRoom, bool includeIsland)
    {
        #region Starting Tile Validations

        if (tile == null || oldRoom == null)
            return;

        if (tile.room != oldRoom)
            // this tile has already been assigned a new room, no need to floodfill again
            return;

        if (tile.furniture != null && tile.furniture.isRoomEnclosing)
            // this tile is blocked by roomEnclosing furniture
            return;

        if (tile.Type == TileType.Soil || tile.Type == TileType.Null)
            return;

        #endregion

        World world = tile.w;
        Room newRoom = new Room();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        // floodfill and queue all the tiles up until terminating condition
        while (tilesToCheck.Count > 0)
        {
            Tile t = tilesToCheck.Dequeue();

            if (t.room == oldRoom || t.room == world.GetOutsideRoom())
            {
                newRoom.AssignTile(t); 

                List<Tile> neighbours = t.GetNeighbours();
                foreach (Tile t2 in neighbours)
                {
                    if (!includeIsland)
                    {
                        // Stop floodfilling and unassign tiles if conditions are met:
                        if (t2 == null ||
                            t2.Type == TileType.Null || t2.Type == TileType.Water ||
                            t2.Type == TileType.Soil || t2.Type == TileType.Grass)
                        {
                            newRoom.UnassignAllTiles();
                            return;
                        }

                        // Validate tiletype before enqueuing it to the kiv list
                        if (t2.furniture == null || t2.furniture.isRoomEnclosing == false)
                            tilesToCheck.Enqueue(t2);
                    }
                    else // if includeIsland
                    {
                        if (t2 != null || t2.Type == TileType.Null) continue;

                        // Validate tiletype before enqueuing it to the kiv list
                        if (t2.Type != TileType.Water &&
                            (t2.furniture == null || t2.furniture.isRoomEnclosing == false))
                        {
                            tilesToCheck.Enqueue(t2);
                            t2.Type = TileType.Floor;
                        }
                    }
                }
            }
        }

        // Copy data from the old room into the new room
        newRoom.atmosphere = oldRoom.atmosphere;

        // Check if roomType has changed
        newRoom.CheckRoomType();

        // Add new room to the world
        tile.w.AddRoom(newRoom);
    }
}
