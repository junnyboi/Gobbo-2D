using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

public enum TileType
{
	Null, Soil, Cultivated, Floor, Grass, Water,
}

public enum ENTERABILITY
{
	Yes, Never, Soon
}

public class Tile : IXmlSerializable
{
	#region VARIABLE DECLARATION

	#region Callbacks
	public event Action<Tile> cbTileTypeChanged;
	public event Action<Tile> cbTileActive;
	public event Action<Tile> cbTileInactive;
	#endregion

	#region Tile Parameters

	private TileType type = TileType.Null;
	public TileType Type
	{
		get { return type; }
		set
		{
			TileType oldType = type;
			type = value;

			// callback for updating tile sprite and data
			if (oldType != type)
				cbTileTypeChanged?.Invoke(this);
		}
	}

	public string material; //TODO: cbOnMaterialChanged

	float elevation;
	public float Elevation
	{
		get { return elevation; }
		set
		{
			float oldElevation = elevation;
			elevation = value;

			// callback for updating tile sprite and data
			if (oldElevation != elevation)
				cbTileTypeChanged?.Invoke(this);
		}
	}

	float moisture;
	public float Moisture
	{
		get { return moisture; }
		set
		{
			float oldMoisture = moisture;
			moisture = value;

			// callback for updating tile sprite and data
			if (oldMoisture != moisture)
				cbTileTypeChanged?.Invoke(this);
		}
	}

	bool isActive;
	public bool SetActive
	{
		get { return isActive; }
		set
		{
			if (value is true)
			{
				isActive = value;
				cbTileActive?.Invoke(this);
			}
			else // value is false
			{
				isActive = value;
				cbTileInactive?.Invoke(this);
			}
		}
	} 

	#endregion

	#region Stuff on the Tile

	public Room room;
	public SimplePriorityQueue<Job> pendingJobs;

	public List<Creature> creaturesOnTile = new List<Creature>();
	public Furniture furniture { get; protected set; }
	public Plant plant { get; protected set; }
	public Resource resource { get; protected set; }

	#endregion

	#region Boolean Checks
	public bool hasFloor { get { return Type == TileType.Floor; } }
	public bool hasRoom { get { return (room != null && room != w.GetOutsideRoom()); } }
	public bool hasCreature { get { return creaturesOnTile.Count > 0; } }
	public bool hasFurniture { get { return furniture != null; } }
	public bool hasPlant { get { return plant != null; } }
	public bool hasResource { get { return resource != null; } }

	// Dummy checks
	public bool hasDummyFixedObject = false;
	public TileType hasDummyTile = TileType.Null;   // Stores old tiletype for reversion

	#endregion

	#region Tile Coordinates
	public World w { get; protected set; }
	public Tile North { get { return w.GetTileAt(X, Y + 1, Z); } }
	public Tile South { get { return w.GetTileAt(X, Y - 1, Z); } }
	public Tile NorthEast { get { return w.GetTileAt(X + 1, Y + 1, Z); } }
	public Tile East { get { return w.GetTileAt(X + 1, Y, Z); } }
	public Tile SouthEast { get { return w.GetTileAt(X + 1, Y - 1, Z); } }
	public Tile NorthWest { get { return w.GetTileAt(X - 1, Y + 1, Z); } }
	public Tile West { get { return w.GetTileAt(X - 1, Y, Z); } }
	public Tile SouthWest { get { return w.GetTileAt(X - 1, Y - 1, Z); } }
	public Tile Above { get { return w.GetTileAt(X, Y, Z + 1); } }
	public Tile Below { get { return w.GetTileAt(X, Y, Z - 1); } }
	public Tile Normalize { get { return w.GetTileAt(X, Y, 0); } }

	public Vector2 vectorPos { get { return new Vector2(X, Y); } }
	public int X { get; protected set; }
	public int Y { get; protected set; }
	public int Z { get; protected set; } 
	#endregion

	public bool forceZeroMovementCost = false;
	public float movementCost
	{
		get
		{
			if (forceZeroMovementCost)
				return 0;

			if (Type != TileType.Floor)
			{
				if (furniture == null) return 1;
				else return 1 * furniture.movementCost;
			}

			// Else tile type is floor
			if (furniture == null ||
				hasDummyFixedObject)
				return 1;

			if (creaturesOnTile.Count > 0)
				return 2 * furniture.movementCost;

			return 1 * furniture.movementCost;

		}
	}

	private static System.Random r;

	#endregion

	#region CONSTRUCTOR
	/// Public function for instantiating tiles
	public Tile(World world, int x, int y, int z, TileType tileType = TileType.Soil)
	{
		// Save tile coordinates in data model
		this.w = world;
		this.X = x;
		this.Y = y;
		this.Z = z;
		this.Type = tileType;
		pendingJobs = new SimplePriorityQueue<Job>();

		// Generate static random seed on tile creation
		r = new System.Random();

	}

	#endregion

	#region OBJECT PLACEMENT

	public bool ValidateFixedObjectPlacement()
	{
		if (plant != null || furniture != null)
			return false;

		return true;
	}

	public bool PlaceFurniture(Furniture furn = null)
	{
		if (furn == null)
		{
			this.furniture = null;  // Uninstall furniture
			return true;
		}

		if (hasPlant)
		{
			// Create job to remove plant before building furniture
			Job j = JobModeController.Instance.RemovePlant(this);
			j.cbJobComplete += (theJob) =>
			{
				JobModeController.Instance.BuildFurniture(this, furn.type);
			};
			return false;
		}

		if (hasFurniture)
		{
			Debug.Log("Trying to assign " + furn.type + " to a tile that already has furniture on it!");
			return false;
		}
		else
		{
			this.furniture = furn;  // Install furniture
			return true;
		}
	}

	public bool PlacePlant(Plant plant = null)
	{
		if (plant == null)
		{
			this.plant = null;   // remove plant by assigning null
			return true;
		}

		if (!ValidateFixedObjectPlacement())
		{
			//Debug.LogError("Trying to assign Plant to a tile that already has fixed objects on it!");
			return false;
		}
		else
		{
			this.plant = plant;
			return true;
		}
	} 
	#endregion

	#region NEIGHBOUR LOGIC
	// Tells us if the two tiles are adjacent/diagonal
	public bool IsNeighbour(Tile tile, bool diagonalOkay = true, bool levelsOkay = false)
	{
		return
			// Check horizontals
			(Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 && this.Z.Equals(tile.Z)) ||

			// Check diagonals
			(diagonalOkay && (Mathf.Abs(this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1) && this.Z.Equals(tile.Z)) ||

			// Check depth
			(levelsOkay && (this.X.Equals(tile.X) && this.Y.Equals(tile.Y) && Mathf.Abs(this.Z - tile.Z) == 1))
			;


	}

	public List<Tile> GetNeighbours(bool diagonalOk = true, int radius = 1, bool multiLayerOK = false)
	{
		List<Tile> neighbours = new List<Tile>();

		switch (radius)
		{
			case 2:

				#region Logic
				neighbours.Add(w.GetTileAt(X, Y + 2, Z));
				neighbours.Add(w.GetTileAt(X + 2, Y, Z));
				neighbours.Add(w.GetTileAt(X, Y - 2, Z));
				neighbours.Add(w.GetTileAt(X - 2, Y, Z));

				if (diagonalOk)
				{
					neighbours.Add(w.GetTileAt(X + 2, Y + 1, Z));
					neighbours.Add(w.GetTileAt(X + 2, Y - 1, Z));
					neighbours.Add(w.GetTileAt(X - 2, Y - 1, Z));
					neighbours.Add(w.GetTileAt(X - 2, Y + 1, Z));

					neighbours.Add(w.GetTileAt(X + 1, Y + 2, Z));
					neighbours.Add(w.GetTileAt(X + 1, Y - 2, Z));
					neighbours.Add(w.GetTileAt(X - 1, Y - 2, Z));
					neighbours.Add(w.GetTileAt(X - 1, Y + 2, Z));

					neighbours.Add(w.GetTileAt(X + 2, Y + 2, Z));
					neighbours.Add(w.GetTileAt(X + 2, Y - 2, Z));
					neighbours.Add(w.GetTileAt(X - 2, Y - 2, Z));
					neighbours.Add(w.GetTileAt(X - 2, Y + 2, Z));
				}
				if (multiLayerOK)
				{
					if (Z < w.Depth - 1)
						neighbours.Add(w.GetTileAt(X, Y, Z + 1));

					if (Z > 0)
						neighbours.Add(w.GetTileAt(X, Y, Z - 1));

				}
				#endregion
				return neighbours;

			default: // radius == 1

				#region Logic
				neighbours.Add(North);
				neighbours.Add(East);
				neighbours.Add(South);
				neighbours.Add(West);

				if (diagonalOk == true)
				{
					neighbours.Add(NorthEast);
					neighbours.Add(SouthEast);
					neighbours.Add(SouthWest);
					neighbours.Add(NorthWest);
				}
				if (multiLayerOK == true)
				{
					if (Z < w.Depth - 1)
					{
						neighbours.Add(Above);
					}

					if (Z > 0)
					{
						neighbours.Add(Below);
					}
				}
				#endregion
				return neighbours;
		}
	}

	public Tile GetRandomNeighbour(string mode)
	{
		int neighbourIndex;
		Tile t = null;

		if (mode == "1")
		{
			List<Tile> neighbours = GetNeighbours(true, 1);
			neighbourIndex = r.Next(0, neighbours.Count - 1);
			t = neighbours[neighbourIndex];

			if (t == null) t = GetRandomNeighbour(mode);
		}

		else if(mode == "2")
		{
			List<Tile> neighbours = GetNeighbours(true, 2);
			neighbourIndex = r.Next(0, neighbours.Count - 1);
			t = neighbours[neighbourIndex];

			if (t == null) t = GetRandomNeighbour(mode);
		}

		else if (mode == "River")
		{
			neighbourIndex = r.Next(0, 5);
			switch (neighbourIndex)
			{
				case 0:
					t = North;
					if (t == null) t = GetRandomNeighbour(mode);
					break;
				case 1:
					t = South;
					if (t == null) t = GetRandomNeighbour(mode);
					break;
				case 2:
					t = East;
					if (t == null) t = GetRandomNeighbour(mode);
					break;
				case 3:
					t = West;
					if (t == null) t = NorthEast;
					if (t == null) t = GetRandomNeighbour(mode);
					break;
				case 4:
					t = NorthWest;
					if (t == null) t = NorthEast;
					break;
				default:
					break;
			}
		}

		else if(mode == "Faraway")
		{
			neighbourIndex = r.Next(2);
			switch (neighbourIndex)
			{
				case 0:
					t = w.GetTileAtWorldCentre(Z);
					break;
				case 1:
					t = w.GetTileAt(w.Width-1, w.Height-1, Z);
					break;
				case 2:
					t = w.GetTileAt(0, 0, Z);
					break;
				default:
					break;
			}
		}

		else
		{
			t = this;
		}

		return t;
	}		

	public bool isSameDepth(Tile t)
	{
		if (t.Z == Z)
			return true;
		return false;
	}
	#endregion

	#region JOB MANAGEMENT
	public void RemovePendingJobs()
	{
		while (pendingJobs.Count > 0)
		{
			Job j = pendingJobs.Dequeue();
			w.jobQueue.Remove(j);
			j.CancelJob();
		}
		// For extra measure
		pendingJobs = new SimplePriorityQueue<Job>();
	}

	public Job DequeueJob()
	{
		if (pendingJobs.Count > 0)
		{
			Job j = pendingJobs.Dequeue();
			try
			{
				w.jobQueue.Remove(j);
			}
			catch
			{
				w.jobQueueAwaitingResources.Remove(j);
			}
			return j;
		}

		//Debug.LogError("Tile :: DequeueJob -- No jobs in pendingJobs.");
		return null;
	}

    #endregion

    #region RESOURCE MANAGEMENT
	public bool PlaceResource(Resource r)
	{
		if (r == null)
		{
			Debug.Log("Tile :: PlaceResource -- null");
			resource = null;
			return true;
		}

		// Check for existing resources on tile to stack
		if (resource != null)
		{
			// Incompatible stack
			if (resource.type != r.type)
			{
				Debug.LogError("Trying to assign resource to a tile that already has a different resource on it");
				return false;
			}
			// Compatible stack
			else
			{
				Debug.Log("Compatible stack!");
				resource.ChangeAmount(r.amount);
			}
		}
		// Create new resource stack
		else
		{
			resource = r.Clone();
			resource.SetOwner(this);
			//Debug.Log("Created a new stack of: " + resource.ToString());
		}

		r.Empty();
		return true;
	}

	public bool RemoveResource()
	{
		if (!hasResource) return false;

		resource = null;
		return true;
	}

    #endregion

    #region UTILITY
    public ENTERABILITY IsEnterable()
	{
		if (movementCost == 0)
		{
			//Debug.Log("Tile :: IsEnterable -- ENTERABILITY: Never ");
			return ENTERABILITY.Never;
		}

		// else check furniture to see if it can be entered
		if (furniture != null && furniture.IsEnterable != null)
		{
			//Debug.Log("Tile :: IsEnterable -- ENTERABILITY: check furniture ");
			return (furniture.IsEnterable(furniture));
		}

		return ENTERABILITY.Yes;
	}

	public override string ToString()
	{
		return Type.ToString() + " (" + X + ", " + Y + ", " + Z + "): ";
	}

	#endregion

	#region SAVE / LOAD
	public XmlSchema GetSchema()
	{
		return null;
	}

	public void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("X", X.ToString());
		writer.WriteAttributeString("Y", Y.ToString());
		writer.WriteAttributeString("Type", ((int)Type).ToString());
		writer.WriteAttributeString("hasDummyFixedObject", hasDummyFixedObject.ToString());
	}

	public void ReadXml(XmlReader reader)
	{
		Type = (TileType)int.Parse(reader.GetAttribute("Type"));
		hasDummyFixedObject = bool.Parse(reader.GetAttribute("hasDummyFixedObject"));
	}

	#endregion
}
