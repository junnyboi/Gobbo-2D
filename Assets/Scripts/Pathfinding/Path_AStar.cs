using UnityEngine;
using System.Collections.Generic;
using Priority_Queue;
using System.Linq;

public class Path_AStar
{

	Queue<Tile> path;
	float movementCostTotal;

	public Tile tempTileEnd = null;

	public Path_AStar(World world, Tile tileStart, Tile tileEnd, EnvClass envClass = EnvClass.Flying)
	{
		#region PRE-PATHING CHECKS

		// If destination is on a different z-level, then reroute to nearest stairs
		if (tileStart.isSameDepth(tileEnd) == false)
		{
			//Debug.Log("Path_AStar -- Destination tile is on a different z-level, rerouting to nearest stairs first");
			if (world.linkLayerTileList.Count > 0)
			{
				// Get nearest stair tile
				tempTileEnd = world.GetNearestLinkLayerTile(tileStart, envClass);

				// Re-route to stair tile
				tileEnd = tempTileEnd;
			}
			else
				return;
		}

		movementCostTotal = 0;

		// Check to see if we have a valid tile graph
		if (world.tileGraph == null)
		{
			world.tileGraph = new Path_TileGraph(world);
		}

		// A dictionary of all nodes.
		Dictionary<Tile, Path_Node<Tile>> nodes = world.tileGraph.nodes;

		// Make sure our start/end tiles are in the list of nodes!
		if (nodes.ContainsKey(tileStart) == false)
		{
			Debug.LogError("Path_AStar: The starting tile (" + tileStart.X + ", " + tileStart.Y + ") isn't in the list of nodes!");

			return;
		}
		if (nodes.ContainsKey(tileEnd) == false)
		{
			Debug.LogError("Path_AStar: The ending tile (" + tileEnd.X + ", " + tileEnd.Y + ") isn't in the list of nodes!");
			return;
		}

		Path_Node<Tile> start = nodes[tileStart];
		Path_Node<Tile> goal = nodes[tileEnd];

		// Temporary make end tile have no movement cost, remember to reset later
		tileEnd.forceZeroMovementCost = true; 

		#endregion

		#region A* SEARCH ALGORITHM
		// Pseudocode: https://en.wikipedia.org/wiki/A*_search_algorithm\

		List<Path_Node<Tile>> ClosedSet = new List<Path_Node<Tile>>();

		// OpenSet represents discovered nodes
		SimplePriorityQueue<Path_Node<Tile>> OpenSet = new SimplePriorityQueue<Path_Node<Tile>>();
		OpenSet.Enqueue(start, 0);

		// Initialize cameFrom[n] => the node immediately preceding n on the cheapest path from start
		Dictionary<Path_Node<Tile>, Path_Node<Tile>> cameFrom = new Dictionary<Path_Node<Tile>, Path_Node<Tile>>();

		// Initialize gScore[n] => the cost of the cheapest path from start to n currently known.
		Dictionary<Path_Node<Tile>, float> gScore = new Dictionary<Path_Node<Tile>, float>();
		foreach (Path_Node<Tile> n in nodes.Values)
		{
			gScore[n] = Mathf.Infinity;
		}
		gScore[start] = 0;

		// Initialize fScore[n] => heuristic cost estimate between 2 tiles
		Dictionary<Path_Node<Tile>, float> fScore = new Dictionary<Path_Node<Tile>, float>();
		foreach (Path_Node<Tile> n in nodes.Values)
		{
			fScore[n] = Mathf.Infinity;
		}
		fScore[start] = HeuristicCostEstimate(start, start, goal);

		while (OpenSet.Count > 0)
		{
			Path_Node<Tile> current = OpenSet.Dequeue();

			// Terminating condition
			if (current == goal)
			{
				ReconstructPath(cameFrom, current);
				tileEnd.forceZeroMovementCost = false;
				return;
			}

			ClosedSet.Add(current);

			foreach (Path_Edge<Tile> edge_neighbor in current.edges)
			{
				Path_Node<Tile> neighbor = edge_neighbor.node;

				Tile neigh = neighbor.data;

				#region Validation Checks -- Based on EnvClass
				switch (envClass)
				{
					case EnvClass.Terrestrial:
						// TODO: To be implemented after I have optimized pathfinder w/ island IDs
						//if (neigh.Type == TileType.Water && neigh != goal.data)
							//continue;
						break;
					case EnvClass.Aquatic:
						if (neigh.Type != TileType.Water && neigh != goal.data)
							continue;
						break;
					case EnvClass.Amphibian:
						break;
					case EnvClass.Flying:
						break;
					default:
						break;
				}

				// Ignore neighbours on a different depth
				if (neigh.Z != current.data.Z)
					continue;

				// Ignore already completed neighbors
				if (ClosedSet.Contains(neighbor) == true)
					continue; 

				#endregion

				float movement_cost_to_neighbor = neighbor.data.movementCost * DistBetween(current, neighbor);

				float tentative_g_score = gScore[current] + movement_cost_to_neighbor;

				if (OpenSet.Contains(neighbor) && tentative_g_score >= gScore[neighbor])
					continue;

				cameFrom[neighbor] = current;
				gScore[neighbor] = tentative_g_score;
				fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, current, goal);

				if (OpenSet.Contains(neighbor) == false)
				{
					OpenSet.Enqueue(neighbor, fScore[neighbor]);
				}

			}
		} 
		#endregion
	}

	float HeuristicCostEstimate(Path_Node<Tile> start, Path_Node<Tile> current, Path_Node<Tile> goal)
	{
		//Euclidean distance heuristic
		/*float heuristic = Mathf.Sqrt(
			Mathf.Pow(a.data.X - b.data.X, 2) +
			Mathf.Pow(a.data.Y - b.data.Y, 2)
		);*/

		// Straight line heuristic w/ tiebreaker
		float dx1 = current.data.X - goal.data.X;
		float dy1 = current.data.Y - goal.data.Y;

		float dx2 = start.data.X - goal.data.X;
		float dy2 = start.data.Y - goal.data.Y;

		float cross = Mathf.Abs(dx1 * dy2 - dx2 * dy1);
		float heuristic = cross * 0.001f;

		return heuristic;
	}

	float DistBetween(Path_Node<Tile> a, Path_Node<Tile> b)
	{
		// Hori/Vert neighbours have a distance of 1
		if (Mathf.Abs(a.data.X - b.data.X) + Mathf.Abs(a.data.Y - b.data.Y) == 1)
		{
			return 1f;
		}

		// Diagonal neighbours have a distance of 1.41421356237	
		if (Mathf.Abs(a.data.X - b.data.X) == 1 && Mathf.Abs(a.data.Y - b.data.Y) == 1)
		{
			return 1.41421356237f;
		}

		// Otherwise, do the actual math.
		return Mathf.Sqrt(
			Mathf.Pow(a.data.X - b.data.X, 2) +
			Mathf.Pow(a.data.Y - b.data.Y, 2)
		);

	}

	void ReconstructPath( Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From, Path_Node<Tile> current
	)
	{
		Queue<Tile> total_path = new Queue<Tile>();
		total_path.Enqueue(current.data); // This "final" step is the path is the goal!

		while (Came_From.ContainsKey(current))
		{
			current = Came_From[current];
			total_path.Enqueue(current.data);
			//movementCostTotal += current.data.movementCost;
			movementCostTotal += 1;
		}

		// At this point, total_path is a queue that is running
		// backwards from the END tile to the START tile, so let's reverse it.

		path = new Queue<Tile>(total_path.Reverse());

		// remove the movementcost of the starting and last tiles to prevent getting stuck on immovable tiles
		movementCostTotal -= path.Peek().movementCost;
		movementCostTotal -= path.Last().movementCost;
	}

	public void ForceEnqueue(Tile t)
	{
		if (t == null)
			return;
		//Debug.Log("Path_AStar :: ForceEnqueue");
		path.Enqueue(t);
	}

	public Tile Dequeue()
	{
		try
		{
			return path.Dequeue();
		}
		catch
		{
			//Debug.LogError("Path_AStar :: Dequeue -- No tiles in path to dequeue.");
			return null;
		}
	}

	public int Length()
	{
		if (path == null)
			return 0;

		return path.Count;
	}

	public float MovementCostTotal()
	{
		if (path == null)
			return Mathf.Infinity;

		return movementCostTotal;
	}

}
