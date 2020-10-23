using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Linq;
using System.Threading;

public class JobQueue
{
    SimplePriorityQueue<Job> jobQueue;
    Path_AStar pathAStar;

    public event Action<Job> cbJobEnqueued;

    //constructor when new JobQueue() is instantiated
    public JobQueue()
    {
        jobQueue = new SimplePriorityQueue<Job>();
    }

    public void Enqueue(Job j, int tilePriority = 3)
    {
        j.tile.pendingJobs.Enqueue(j, tilePriority);
        jobQueue.Enqueue(j, 1000000 + 
                        Math.Abs(WorldController.Instance.w.Width - j.tile.X) +
                        Math.Abs(WorldController.Instance.w.Height - j.tile.Y)
                        );
        j.jobMovementCostEstimate = 1000000;
        cbJobEnqueued?.Invoke(j);
        j.cbJobComplete += OnJobCompleted;
    }
    public Job Dequeue()
    {
        if (jobQueue.Count == 0)
            return null;

        Job j = jobQueue.Dequeue();
        j.tile.pendingJobs.Remove(j);

        return j;
    }
    /// <summary>
    /// Use instead of Dequeue when a job is abandoned and has to be re-enqueued.
    /// </summary>
    /// <param name="j">The Job</param>
    public void Reenqueue(Job j, int costIncrement = 10)
    {
        j.jobMovementCostEstimate += costIncrement;
        j.tile.pendingJobs.Enqueue(j, costIncrement);
        jobQueue.Enqueue(j, j.jobMovementCostEstimate);
        cbJobEnqueued?.Invoke(j);

        //Debug.Log("Job Reenqueued -- cost " + j.jobMovementCostEstimate);
        //DebugJobQueue();
    }
    public void Clear()
    {
        jobQueue.Clear();
    }

    public bool Contains(Job job)
    {
        return jobQueue.Contains(job);
    }

    public void Remove(Job job)
    {
        jobQueue.Remove(job);
    }

    public void UpdatePriority(Job job, double priority)
    {
        jobQueue.UpdatePriority(job, priority);
    }

    /// <summary>
    /// Returns head of queue without dequeuing it.
    /// </summary>
    /// <returns></returns>
    public Job First()
    {
        return jobQueue.First;
    }
    public int count { get { return jobQueue.Count; } }

    public double PriorityByMovementCost(Job job, Tile startTile, EnvClass envClass)
    {
        //Debug.Log("JobQueue :: PriorityByMovementCost");
        CreatePathAStarToJobTile(job, startTile, envClass); // Create path from chara to job tile

        float costEstimate = pathAStar.MovementCostTotal();

        job.jobMovementCostEstimate = costEstimate;

        return Convert.ToDouble(costEstimate);
    }

    public void CreatePathAStarToJobTile(Job job, Tile startTile, EnvClass envClasss)
    {
        //Debug.Log("JobQueue :: CreatePathAStarToJobTile");
        Tile t = job.tile;
        World w = t.w;

        if (startTile == null)
            pathAStar = new Path_AStar(w, w.GetTileAtWorldCentre(0), t, envClasss);
        else
            pathAStar = new Path_AStar(w, startTile, t, envClasss);
    }

    public void OnJobCompleted(Job j)
    {
        if (pathAStar != null)
            pathAStar = null;
    }

    public string DebugJobQueue()
    {
        string s = "JobQueue :: Count:" + count;
        int i = 0;

        foreach (Job job in jobQueue)
        {
            i++;
            s += "\n" + "--- Job " + i + "Type: " + job.jobType + "MovementCostEstimate: " + job.jobMovementCostEstimate;
        }

        Debug.Log(s);
        return s;
    }
}
