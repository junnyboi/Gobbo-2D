using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

/// <summary>
/// Job holds information for a queued job
/// </summary>
public class Job: IXmlSerializable
{
    #region VARIABLE DECLARATION

    public Creature owner;
    public List<Creature> prevOwners = new List<Creature>();
    public Tile tile { get; protected set; }
    public string jobType { get; protected set; }
    float jobTime = 10f;
    public float jobMovementCostEstimate = 1;
    public bool allowWorkingFromNeighbourTile { get; protected set; } = true;
    public int countAbandoned = 0;
    public Dictionary<string, Resource> requiredResources = new Dictionary<string, Resource>();
    public bool requiresResources { get { return requiredResources.Count > 0; } }

    #region CALLBACKS
    public event Action<Job> cbJobComplete;
    public event Action<Job> cbJobCancel;
    public event Action<Job> cbJobRemove;  
    #endregion

    #endregion

    // Constructor
    public Job(Tile tile, string jobType, Action<Job> cbJobComplete, float jobTime = 0.1f, bool neigbourTileAllowed = true)
    {
        this.tile = tile;
        this.jobType = jobType;
        this.cbJobComplete += cbJobComplete;
        this.jobTime = jobTime;
        this.allowWorkingFromNeighbourTile = neigbourTileAllowed;
    }

    // Clone
    public Job Clone()
    {
        return new Job(tile, jobType, cbJobComplete, jobTime, allowWorkingFromNeighbourTile);
    }

    public void DoJob(float workTime)
    {
        jobTime -= workTime;

        if(jobTime <= 0)
        {
            RemoveJob();
            cbJobComplete?.Invoke(this);
        }
    }
    public void CancelJob()
    {
        //Debug.Log("CancelJob -- " + ToString());
        cbJobCancel?.Invoke(this);
        RemoveJob();
    }
    public void RemoveJob()
    {
        owner = null;
        prevOwners = new List<Creature>();
        cbJobRemove?.Invoke(this);
    }

    public override string ToString()
    {
        return base.ToString() + " (" + tile.X + ", " + tile.Y + ", " + tile.Z + "), " + jobType;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ClearAllRequirements()
    {
        prevOwners = new List<Creature>();
        countAbandoned = 0;
        requiredResources = new Dictionary<string, Resource>();
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", tile.X.ToString());
        writer.WriteAttributeString("Y", tile.Y.ToString());
        //writer.WriteAttributeString("jobTime", jobTime.ToString());
        writer.WriteAttributeString("jobType", jobType);
    }

    public void ReadXml(XmlReader reader)
    {   // reads from save file and sets the data

    }
}
