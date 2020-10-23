using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class constructs a path-finding compatible graph of the world.
/// Each tile is a node. Only walkable neighbours are linked via edges.
/// </summary>
public class Path_TileGraph
{
    public Dictionary<Tile, Path_Node<Tile>> nodes;
    public Path_TileGraph(World world)
    {

        nodes = new Dictionary<Tile, Path_Node<Tile>>();

        // loop through all tiles of the world and create nodes
        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                for (int z = 0; z < world.Depth; z++)
                {
                    Tile t = world.GetTileAt(x, y, z);
                    Path_Node<Tile> n = new Path_Node<Tile>();
                    n.data = t;
                    nodes.Add(t, n);
                }
            }
        }

        int countEdges = 0;
        // loop through all tiles and create edges for neighbours
        foreach (Tile t in nodes.Keys)
        {
            Path_Node<Tile> n = nodes[t];
            List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();

            #region Create edges to neighbours
            // get a list of all neighbours on the same depth
            List<Tile> neighbours = t.GetNeighbours(true,1);

            // create edges to walkable neighbours
            foreach (Tile neighbour in neighbours)
            {
                if (neighbour != null && neighbour.movementCost > 0)
                {
                    if (IsClippingCorner(t, neighbour))
                        continue;   // skip to the next neighbour without building edge

                    Path_Edge<Tile> e = new Path_Edge<Tile>();
                    e.node = nodes[neighbour];

                    // add the edge to temporary list
                    edges.Add(e);
                    countEdges++;
                }
            }

            #endregion

            // convert list of neighbours to array and update the node
            n.edges = edges.ToArray();
        }

    }

    bool IsClippingCorner( Tile a, Tile b)
    {   // check if tile b is diagonal to tile a
        if (Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 2)
        {
            int dX = a.X - b.X;
            int dY = a.Y - b.Y;

            if ( a.w.GetTileAt(a.X - dX, a.Y, a.Z).movementCost == 0 ||
                 a.w.GetTileAt(a.X - dX, a.Y, a.Z).movementCost >= 100000)
            {   // if E or W tiles are blocked, then pathfinder is attempting to clip a corner
                return true;
            }
            if (a.w.GetTileAt(a.X, a.Y - dY, a.Z).movementCost == 0 ||
                a.w.GetTileAt(a.X, a.Y - dY, a.Z).movementCost >= 100000)
            {   // if N or S tiles are blocked, then pathfinder is attempting to clip a corner
                return true;
            }
        }
        // if movement from currTile to neighbourTile is diagonal (eg. N -> E)
        // then it is flagged as clipping a corner

        return false;
    }
}
