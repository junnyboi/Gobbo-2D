using UnityEngine;
using System;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public enum GenderType
{
	Male, Female, Hemaphrodite
}
public enum SpeciesCreature
{
	Goblyn, Gnome, Dragon, Triceratops, GiantKiwi,
	Koi, Sturgeon, Horror, Gorilla
}

public enum EnvClass
{
	Terrestrial, Aquatic, Amphibian, Flying,
	//Arboreal, Saxicolous, Arenicolous, Troglofauna, Lava
}

public class Creature : IXmlSerializable
{
	#region VARIABLE DECLARATION

	#region Attribute Variables

	public string name { get { return firstName + " " + middleName + " " + lastName; } }
	public string nameShortform { get
		{
			try
			{
				return firstName + " " + middleName.Substring(0, 1) + "." + lastName.Substring(0, 1);
			}
			catch
			{
				if (firstName != null)
					return firstName;
				else
					return "";
			}
		} }
	public string firstName { get; protected set; }
	public string middleName { get; protected set; }
	public string lastName { get; protected set; }

	public SpeciesCreature species { get; protected set; }
	public int age { get; protected set; }

	public int lifespan = 10000;
	public GenderType gender { get; protected set; }

	public int health { get; protected set; } = 100;

	public float size_x = 0.5f;
	public float size_y = 0.5f;

	#endregion

	#region Movement Variables

	public float X { get { return Mathf.Lerp(currTile.X, nextTile.X, movementPercentage); } }
	public float Y { get { return Mathf.Lerp(currTile.Y, nextTile.Y, movementPercentage); } }

	public Tile tempTile { get; protected set; } = null;
	public Tile currTile { get; protected set; }
	public int currentDepth { get; protected set; }

	public Tile destTile { get; protected set; }  // If we aren't moving, then destTile = currTile
	public Tile nextTile { get; protected set; }  // The next tile in the pathfinding sequence
	Path_AStar pathAStar;
	float movementPercentage; // Goes from 0 to 1 as we move from currTile to destTile
	public float speed = 1;   // Tiles per second

	public bool isRotating { get; protected set; } = false;
	public Quaternion currRotation;   // Default to null
	public Quaternion destRotation { get; protected set; }

	public bool canMove = true;
	public bool isWorking = false;
	public EnvClass envClass = EnvClass.Terrestrial;

	#endregion

	#region Other Variables

	public Room myRoom = null;
	public Furniture myBed = null;

	public bool canUpdate = true;
	public bool isFeral = false;
	bool isEngagingHostile = false;

	public Job myJob { get; protected set; }
	public bool canDoJob = true;
	public bool canUseJobqueue = true;
	public string idleJobMode = "2";
	public float idlePauseTime = 0;

	private string cancelAnimationBool;
	private string forcedAnimationBool;
	private float forcedWaitTime;

	public event Action<Creature> cbCreatureMoved;
	public event Action<Creature> cbCreatureChangedDepth;
	public event Action<Creature> cbCreatureOlder_year;
	public event Action<Creature> cbOnCreateIdleJob;

	World w { get { return WorldController.Instance.w; } }
	CreaturesController controller { get { return CreaturesController.Instance; } }

	#endregion

	#endregion

	#region CONSTRUCTORS
	public Creature() { }      // Used only for serialization

	/// <summary>
	/// Constructor for character initialization.
	/// </summary>
	/// <param name="tile">Starting tile.</param>
	/// <param name="age">(Optional) Starting age of character.</param>
	public Creature(Tile tile, int age = 0)
	{
		if (tile == null)
			tile = WorldController.Instance.w.GetTileAtWorldCentre(-1);

		currTile = destTile = nextTile = tile;
		currentDepth = tile.Z;
		this.age = age;
	}

	public Creature(Tile tile, string name, SpeciesCreature species)
	{
		if (tile == null)
			tile = WorldController.Instance.w.GetTileAtWorldCentre(-1);

		currTile = destTile = nextTile = tile;
		currentDepth = tile.Z;
		SetFirstName(name);
		this.species = species;
	}
	#endregion

	#region UPDATE
	public void Update(float deltaTime)
	{
		//Debug.Log("Character Update");
		if (!canUpdate) return;

		Update_DoJob(deltaTime);
		Update_DoBattle(deltaTime);
		Update_DoMovement(deltaTime);

		if (currTile != nextTile)
			cbCreatureMoved?.Invoke(this);
	} 
	#endregion

	#region ATTRIBUTE SETTERS
	public void SetFirstName(string name)
	{
		this.firstName = name;
	}
	public void SetMiddleName(string name)
	{
		this.middleName = name;
	}
	public void SetLastName(string name)
	{
		this.lastName = name;
	}

	public void SetSpecies(SpeciesCreature species)
	{
		this.species = species;
	}
	public void SetGender(GenderType gender)
	{
		this.gender = gender;
	}
	public void ChangeAge(int deltaAge)
	{
		age += deltaAge;

		if (deltaAge >= 1)
			cbCreatureOlder_year?.Invoke(this);
	}
	public void ChangeAge()
	{
		age += 1;
		cbCreatureOlder_year?.Invoke(this);
	}
	public void ChangeHealth(int delta)
	{
		if (health > 0)
			health += delta;
		else if (health <= 0)
		{
			health = 0;
			Debug.Log(nameShortform + "has passed away...");
			currTile.w.RemoveCreature(this);

			//TODO: Implement slain audio effects for other creatures
			SoundController.Instance.OnGoblynSlain();
		}
		else
			return;
	}
	#endregion

	#region JOB

	void Update_DoJob(float deltaTime)
	{
		if (!canDoJob && !isEngagingHostile)
		{
			AbandonJob();
			return;
		}

		// Do I have a job?
		if (myJob == null)
		{
			isWorking = false;
			FindJob();
		}

		// I currently have a job yay!
		if (myJob != null)
		{
			// [Optimization] Have I abandoned this job before?
			if (myJob.prevOwners.Contains(this))
			{
				AbandonJob();
				return;
			}

			// Aquatic creatures can only move in water
			if (envClass == EnvClass.Aquatic && myJob.tile.Type != TileType.Water)
			{
				myJob = null;
				return;
			}

			// Is the job reachable?
			if (myJob.jobMovementCostEstimate < 10000)
			{
				// NEEDS DEBUGGING
				// TODO: Does the job still require resources?
				if (myJob.requiresResources)
				{
					// FIXME -- should support more than one type of resource
					Resource rNearest = ResourceController.FindNearestResourceStack(currTile, myJob.requiredResources.First().Value.type);
					if (rNearest == null)
					{
						Debug.Log("Cancelling construction job as resources are unavailable");
						myJob.CancelJob();
						return;
					}
					// FIXME -- should support more than one type of resource
					int requiredAmount = myJob.requiredResources.First().Value.amount;
					Job originalJob = myJob;

					Job collectResource = new Job(rNearest.tile, "Collect Resources",
						(Job j) =>
						{
							// Pickup resource, subtract from stack
							string rType = rNearest.type;
							int amtCollected = rNearest.ChangeAmount(-requiredAmount);  // will be negative
							rNearest.amountBlocked = 0;

							// Swap currently held resources, if any
							if (amtCollected < 0)
							{
								if (!ResourceController.Instance.SwapResourceStack(this, rNearest.tile, -amtCollected))
								{
									// Otherwise instantiate new resource with creature as owner
									if (amtCollected < 0)
									{
										if (ResourceController.Instance.PickupResourceStack(this, new Resource(rType, -amtCollected)))
											Debug.Log(this.nameShortform + " picked up " + -amtCollected + "x " + rType);
									}
								}
								else
									Debug.Log(this.nameShortform + " swapped for " + -amtCollected + "x " + rType);
							}


							// Create job to drop resources off
							Job dropResource = new Job(originalJob.tile, "Drop Resources",
							(Job j2) =>
							{
								// Remove resource attached to creature
								Resource r_creature = ResourceController.creatureResourceMap[this];
								ResourceController.Instance.RemoveResourceStack(r_creature);

								// Update target job's resource requirements
								// TODO: Update job pending resources UI
								if (originalJob.requiredResources.ContainsKey(rType))
								{
									Resource reqResource = originalJob.requiredResources[rType];
									reqResource.ChangeAmount(amtCollected);

									if (reqResource.amount <= 0)
									{
										originalJob.requiredResources.Remove(rType);
										if (!originalJob.requiresResources)
										{
											Debug.Log("Resources satisfied for " + originalJob.ToString());
											originalJob.ClearAllRequirements();
											currTile.w.jobQueue.Enqueue(originalJob);
										}
									}
								}
							});

							AbandonJob();
							myJob = dropResource;
						});

					AbandonJob();
					myJob = collectResource;
					rNearest.amountBlocked = requiredAmount;
				}

				// Job accepted!
				myJob.owner = this;
				destTile = myJob.tile;
				myJob.cbJobRemove += RemoveJob;
			}

			else if (currTile.w.jobQueue.PriorityByMovementCost(myJob, currTile, envClass) < 100000)
			{   // If updated job is now reachable... Job accepted!
				destTile = myJob.tile;
				myJob.cbJobRemove += RemoveJob;
			}

			else
				AbandonJob();
		}

		// If I've reached my job, start working
		if (myJob != null)
		{
			if (myJob.allowWorkingFromNeighbourTile)
			{
				if (currTile.IsNeighbour(destTile, true) || currTile == destTile)
				{
					//myJob.owner = this;
					myJob.DoJob(deltaTime);
					isWorking = true;
				}
			}
			else
			{
				if (currTile == destTile)
				{
					myJob.DoJob(deltaTime);
					isWorking = true;
				}
			}
		}
	}

	void FindJob()
	{
		if (!canUseJobqueue)
		{
			CreateIdleJob(idleJobMode, idlePauseTime);
			return;
		}

		if (currTile.w.jobQueue.count > 0)
		{
			// Are there jobs near me? (check 1 tile away = 8 tiles total)
			List<Tile> neighbours = currTile.GetNeighbours(true, 1);
			foreach (Tile neighbour in neighbours)
			{
				if (neighbour == null) continue;

				myJob = neighbour.DequeueJob();
				if (myJob != null)
				{
					nextTile = currTile;
					return;
				}
			}

			// Are there jobs a little further away? (check 2 tile away = 16 tiles total)
			neighbours = currTile.GetNeighbours(true, 2);
			foreach (Tile neighbour in neighbours)
			{
				if (neighbour == null) continue;

				myJob = neighbour.DequeueJob();
				if (myJob != null) return;
			}

			// Are there available jobs from the jobqueue?
			myJob = currTile.w.jobQueue.Dequeue();
		}
		// If jobqueue is empty, then just idle around 
		else if (myJob == null)
			CreateIdleJob(idleJobMode, idlePauseTime);
	}

	public void AbandonJob(int costIncrement = 10)
	{
		if (myJob == null) return;

		// Put the job back on the queue
		if (myJob.requiresResources)
		{
			currTile.w.jobQueueAwaitingResources.Enqueue(myJob);
			myJob.tile.pendingJobs.Remove(myJob);
		}
		else
			myJob.CancelJob();
			//currTile.w.jobQueue.Reenqueue(myJob, costIncrement);

		// unassign job from character
		//myJob.prevOwners.Add(this);
		myJob = null;

		// stop moving to job
		nextTile = destTile = currTile;
		pathAStar = null;
		isWorking = false;
	}

	/// <summary>
	/// Don't call this function from creature, call it from the job!
	/// </summary>
	void RemoveJob(Job j)
	{   // Job completed or was cancelled 

		// unassign job from character
		myJob = null;

		// stop moving to job
		nextTile = destTile = currTile;
		pathAStar = null;
		isWorking = false;
	}

	void CreateIdleJob(string mode = "2", float pauseTime = 0)
	{
		Tile moveToTile = currTile.GetRandomNeighbour(mode);

		// Validate destination
		if (moveToTile == null || moveToTile.movementCost > 1000 ||
			(envClass == EnvClass.Aquatic && moveToTile.Type != TileType.Water) ||
			(envClass == EnvClass.Terrestrial && moveToTile.Type == TileType.Water) ||
			currTile.room != moveToTile.room)
		{
			canDoJob = false;
			forcedAnimationBool = "Idle";
			forcedWaitTime = 2f;
			return;
		}

		if (currTile.IsNeighbour(moveToTile, true) || currTile == moveToTile)
		{
			Debug.LogError("Idle moveToTile is too close to creature");
			return;
		}

		// Assign destination
		myJob = new Job(moveToTile, "Idle", (job) => 
		{
			canDoJob = false;
			forcedAnimationBool = "Idle";
			forcedWaitTime = pauseTime;
		}, 0);

		myJob.cbJobCancel += (job) => 
		{ 
			//Debug.LogError("Cancelled idle job for: " + nameShortform); 
		};

		cbOnCreateIdleJob?.Invoke(this);

		// Optimization (skips A* algorithm)
		nextTile = moveToTile;
	}

	#endregion

	#region MOVEMENT
	void Update_DoMovement(float deltaTime)
	{
		#region Forced wait/animation sequence
		// Check for a forced wait / animation sequence
		if (!canDoJob)
		{
			Animator animator = controller.creatureGameObjectMap[this].GetComponent<Animator>();
			if (animator == null)
			{
				//Debug.LogError("Creature :: ForceAnimation -- No animator attached to creature gameobject!");
				canDoJob = true;
				return;
			}

			// Check for a forced cancel animation sequence
			if (cancelAnimationBool != null)
			{
				animator.SetBool(cancelAnimationBool, false);
				cancelAnimationBool = null;
			}

			// At this point, creature should have an animator
			if (forcedWaitTime <= 0)
			{
				// Stop forced animation
				canDoJob = true;
				if (forcedAnimationBool != null)
				{
					animator.SetBool(forcedAnimationBool, false);
					forcedAnimationBool = null;
				}
				forcedWaitTime = 0;
				return;
			}

			if (forcedAnimationBool != null)
				animator.SetBool(forcedAnimationBool, true);

			forcedWaitTime -= deltaTime;
		}
		#endregion

		#region Rotation for top-view creatures
		// Handle rotation
		if (isRotating)
		{
			if (currTile != nextTile)
			{
				Vector3 v = (currTile.vectorPos - nextTile.vectorPos);
				float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
				destRotation = Quaternion.AngleAxis(angle, Vector3.forward);

				if (currRotation != null && currRotation != destRotation)
				{
					canMove = false;
					currRotation = Quaternion.RotateTowards(currRotation, destRotation, 60 * deltaTime);
				}
				else
					canMove = true;
			}
		}
		else
		{
			// TODO: migrate code from creatures controller
		} 
		#endregion

		#region Actual movement
		// Movement terminating condition
		if (!canMove) return;
		if (currTile.IsNeighbour(destTile, true) || currTile == destTile)
		{
			//Debug.Log(this.nameShortform + " has reached destination");

			pathAStar = null;

			if (currTile.Z == destTile.Z)
				return; // I'm already were I want to be.
			else nextTile = destTile;

		}

		// Actual movement from here on...
		if (nextTile == null || nextTile == currTile || nextTile.movementCost >= 10000000)
		{
			// If a path does not exist, generate a new path to my destination
			if (pathAStar == null)
			{
				//Debug.Log(nameShortform + " is generating a path to destination.");
				pathAStar = new Path_AStar(currTile.w, currTile, destTile, envClass);

				if (pathAStar == null) // Path still doesn't exist, abandon!
				{
					AbandonJob();
				}
				else nextTile = pathAStar.Dequeue();  // remove the currTile from queue so I start pathing on next tile
			}

			if (pathAStar.Length() == 0 && myJob != null)
			{
				// Since a path exists, first check if a depth switch is required
				if (currTile == pathAStar.tempTileEnd)
				{
					//Debug.Log("Creature pathAStar -- Depth switch required to reach " + myJob.ToString());
					SwitchZDepth(currTile);
					destTile = myJob.tile;
					pathAStar = new Path_AStar(currTile.w, currTile, destTile, envClass);
					nextTile = pathAStar.Dequeue();  // remove the currTile from queue so I start pathing on next tile

					// TODO: If there is still no path and job is on the same depth, 
					// then check if there is an accessible stairwell to reach the job
					if (pathAStar.Length() == 0 && currTile.isSameDepth(destTile))
					{
						// FIXME: Interim code until I fix this
						Tile t = destTile;
						Teleport(t.X, t.Y, t.Z);
					}
				}

				// If there is still no path again, then there is truly no path to destination lol
				if (pathAStar.Length() == 0)
				{
					// Abandon the current job
					Debug.LogError("Job removed -- no path: " + myJob.jobType);
					myJob.CancelJob();
					return;
				}
			}

			// If all is well, grab the next waypoint from the pathing system
			nextTile = pathAStar.Dequeue();

			if (nextTile == currTile)
			{
				Debug.LogError("Update_DoMovement - nextTile is currTile?");
			}

		}

		// At this point I should have a valid nextTile to move to.
		// find Euclidean distance to nextTile 
		float distToTravel = Mathf.Sqrt(Mathf.Pow(currTile.X - nextTile.X, 2) +
										 Mathf.Pow(currTile.Y - nextTile.Y, 2));

		// check enterability of the nextTile
		if (nextTile.IsEnterable() == ENTERABILITY.Never || nextTile.movementCost >= 1000000)
		{
			//Debug.LogError("Creature :: " + nameShortform + " -- tried to enter an unwalkable tile! Abandoning job: " + myJob.ToString());
			// pathfinding info might be out of date, force a refresh
			nextTile = null;
			pathAStar = null;

			// FIXME: maybe I should be preventing picking up jobs with no path? 
			if (myJob.countAbandoned < 100)
			{
				myJob.countAbandoned += 1;
				AbandonJob(10000);
			}
			else
			{
				Job j = myJob;
				j.CancelJob();
			}

			return;
		}
		else if (nextTile.IsEnterable() == ENTERABILITY.Soon)
		{   // I should wait to enter
			return;
		}

		float distThisFrame = speed / nextTile.movementCost * deltaTime;
		float percThisFrame = distThisFrame / distToTravel;

		movementPercentage += percThisFrame;

		if (movementPercentage >= 1)
		{
			currTile.creaturesOnTile.Remove(this);
			nextTile.creaturesOnTile.Add(this);

			currTile = nextTile;

			CheckDepthChange();
			movementPercentage = 0;
		} 
		#endregion
	}

	void CheckDepthChange()
	{
		if (currentDepth != currTile.Z)
		{
			currentDepth = currTile.Z;
			cbCreatureChangedDepth?.Invoke(this);
		}
	}

	public void SwitchZDepth(Tile tile)
	{
		int newDepth = tile.Z == 0 ? 1 : 0;
		//Debug.Log("Creature :: SwitchZDepth -- from: " + tile.Z + " to: " + newDepth);

		Teleport(tile.X, tile.Y, newDepth);
	}

	/// <summary>
	/// Teleport creature to tile.
	/// </summary>
	public void Teleport(int x, int y, int z)
	{
		//Debug.Log(nameShortform +" teleported to " + x + ", " + y + ", " + z);
		Tile t = WorldController.Instance.w.GetTileAt(x, y, z);

		// Validation
		if (t.movementCost >= 1000000)
		{
			Debug.Log("Teleport -- Cannot teleport to a tile with movementCost >= 1000000");
			return;
		}

		// Update tiles data and creature sprite position
		nextTile = destTile = currTile = t;
		controller.creatureGameObjectMap[this].transform.position = new Vector2(x, y);

		// Check for change in depth
		currentDepth = t.Z;
		cbCreatureChangedDepth?.Invoke(this);
	}

	public void Teleport(Tile t)
	{
		Debug.Log(nameShortform + " teleported to " + t.X + ", " + t.Y + ", " + t.Z);

		// Validation
		if (t.movementCost >= 1000000)
		{
			Debug.Log("Teleport -- Cannot teleport to a tile with movementCost >= 1000000");
			return;
		}

		// Update tiles data and creature sprite position
		nextTile = destTile = currTile = t;
		controller.creatureGameObjectMap[this].transform.position = new Vector2(t.X, t.Y);

		// Check for change in depth
		currentDepth = t.Z;
		cbCreatureChangedDepth?.Invoke(this);
	}

	public void ForceAnimationBool(string animationTrigger, float waitTime)
	{
		//Debug.Log("Creature :: ForceAnimation -- " + animationTrigger + " for " + waitTime + " seconds");
		canDoJob = false;
		forcedAnimationBool = animationTrigger;
		forcedWaitTime = waitTime;
	}

	#endregion

	#region BATTLE

	void Update_DoBattle(float deltaTime)
	{
		if (isEngagingHostile) return;

		else if (isFeral)
		{
			if ( SingleAttack(ScanForHostiles(1)) ) return;
			else SingleAttack(ScanForHostiles(2));
		} 
	}

	Creature ScanForHostiles(int radius = 1)
	{
		List<Tile> scanTiles = currTile.GetNeighbours(true, radius);

		foreach (Tile t in scanTiles)
		{
			if (t != null && t.hasCreature)
			{
				List<Creature> scanCreatures = t.creaturesOnTile;
				foreach (Creature creatureOnTile in scanCreatures)
				{
					if (creatureOnTile is Goblyn)
					{
						//Debug.Log(nameShortform + " located a hostile: " + creatureOnTile.ToString());
						return creatureOnTile;
					}
				}
			}
		}
		return null;
	}

	bool SingleAttack(Creature hostile)
	{
		if (hostile == null) return false;

		// Cancel current job
		// TODO: Do I want to abandon non-idle jobs instead of cancelling?
		if (myJob != null && isEngagingHostile == false)
		{
			switch (myJob.jobType)
			{
				case "Idle":
					myJob.CancelJob();
					break;
				case "Sleep":
					Debug.Log(nameShortform + " is sleeping and unable to attack.");
					return false;
				case "Attack":
					Debug.LogError(nameShortform + " cancelled an attack to attack something else.");
					myJob.CancelJob();
					break;
				default:
					AbandonJob();
					break;
			}
		}

		//Debug.Log(nameShortform + " is engaging a hostile: " + hostile.ToString());

		Tile moveToTile = hostile.currTile;

		// Validate destination
		if (moveToTile == null || moveToTile.movementCost > 1000 ||
			(envClass == EnvClass.Aquatic && moveToTile.Type != TileType.Water) ||
			(envClass == EnvClass.Terrestrial && moveToTile.Type == TileType.Water) ||
			currTile.room != moveToTile.room)
			return false;

		isEngagingHostile = true;
		cancelAnimationBool = "Idle";

		// Assign destination
		myJob = new Job(moveToTile, "Attack", (job) =>
		{
			Debug.Log(nameShortform + " is attacking " + hostile.ToString());

			// Trigger attack animations
			cancelAnimationBool = "Idle";
			ForceAnimationBool("Attack", 3f);
			isEngagingHostile = false;

			// TODO: Trigger attack audio dynamically, currently hardcoded
			if (this is Gorilla)
			{
				if (SoundController.Instance.soundCooldown > 0)
					return;
				SoundController.Instance.soundCooldown = 3f;

				AudioClip clip = Resources.Load<AudioClip>("Sounds/Gorilla smash");
				SoundController.PlayClip(clip);

				hostile.ChangeHealth(-20);
			}
		});

		myJob.cbJobCancel += (job) =>
		{
			//Debug.LogError("Cancelled attack job for: " + nameShortform);
			isEngagingHostile = false;
		};

		// Straight line interpolated movement (skips A* algorithm)
		nextTile = moveToTile;
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
		writer.WriteAttributeString("X", currTile.X.ToString());
		writer.WriteAttributeString("Y", currTile.Y.ToString());
	}
	public void WriteXml_Job(XmlWriter writer)
	{
		if (myJob != null)
			myJob.WriteXml(writer);
	}

	public void ReadXml(XmlReader reader)
	{   // reads from save file and sets the data

	}
	#endregion

	public override string ToString()
	{
		return string.Format("{0} \n{1} {2}, age {3}\n"
							, name, gender, species, age);
	}
}

public class CreatureTopView : Creature
{
	public CreatureTopView(Tile t, string firstName, SpeciesCreature species)
	{
		if (t == null)
			t = WorldController.Instance.w.GetTileAtWorldCentre(-1);
		currTile = destTile = nextTile = t;
		currentDepth = t.Z;

		SetFirstName(firstName);
		SetSpecies(species);
		speed = 0.5f;
		canUseJobqueue = false;
		isRotating = true;
	}
}

public class Gorilla: Creature
{
	public Gorilla(Tile t, string firstName)
	{
		if (t == null)
			t = WorldController.Instance.w.GetTileAtWorldCentre(-1);

		currTile = destTile = nextTile = t;
		currentDepth = t.Z;

		SetFirstName(firstName);
		SetSpecies(SpeciesCreature.Gorilla);
		speed = 1.5f;
		canUseJobqueue = false;
		idlePauseTime = 4f;
		isRotating = false;
		isFeral = true;
	}
}