using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using UnityEngine;

public abstract class Plant
{
    #region Variable Declaration
    public string species;
    public string subspecies;
    public int lifespan_years;
    public int minLight;
    public int minWater;
    public int harvestCycle_months;
    public int harvestAmt;

    public bool isExpired { get { return age_years >= lifespan_years;  } }
    public bool canHarvest { get { return age_months == nextHarvest_months; } }
    public bool isBeingHarvested { get; protected set; } = false;

    public bool canAge = true;

    private int age_months = 0;
    public float age_years { get { return age_months/12; } }
    public int currentLight { get; protected set; } = 0;
    public int currentWater { get; protected set; } = 0;
    public int nextHarvest_months { get; protected set; }


    public Tile tile;
    public int X { get { return tile.X; } }
    public int Y { get { return tile.Y; } }

    public float size = 0.1f;
    #endregion

    #region Callback Declaration
    public event Action<Plant> cbPlantOlder_Day;
    public event Action<Plant> cbPlantOlder_Month;
    public event Action<Plant> cbPlantOlder_Year;
    public event Action<Plant> cbPlantHarvested;
    #endregion

    public Plant(int lifespan_years, int harvestCycle_months, 
                int minLight = 0, int minWater = 0)
    {
        this.lifespan_years = lifespan_years;
        this.harvestCycle_months = nextHarvest_months = harvestCycle_months;
        this.minLight = minLight;
        this.minWater = minWater;
    }

    public void OneYearOlder()
    {
        cbPlantOlder_Year?.Invoke(this);
    }
    public void OneMonthOlder()
    {
        if (age_months == lifespan_years * 12)
            return;
        else if (age_months > lifespan_years * 12)
        {
            age_months = lifespan_years * 12;
            return;
        }

        age_months += 1;
        cbPlantOlder_Month?.Invoke(this);
    }
    public void OneDayOlder()
    {
        cbPlantOlder_Day?.Invoke(this);
    }

    public void SetHarvestStatus(bool harvestStatus)
    {
        isBeingHarvested = harvestStatus;
    }

    public void Harvest()
    {
        if (isExpired) return;

        //Debug.Log("Plant :: Harvest: " + this);
        nextHarvest_months += harvestCycle_months;
        size = 0.1f;

        // TODO: drop resources here? or in the callback? Loot controller?
        cbPlantHarvested?.Invoke(this);

        OnHarvest();
    }
    public virtual void OnHarvest()
    {
        ResourceController.Instance.PlaceHarvest(tile, subspecies);
        // Allows derived classes to add more implementation
    }

    /// <summary>
    /// ToString method defines the string representation of a plant 
    /// </summary>
    public override string ToString()
    {
        if (canAge)
            return string.Format("Age(mths): {0}/{1}, Harvest in {2} mths"
                               , age_months, lifespan_years * 12, nextHarvest_months % harvestCycle_months);
        else
            return "Wild";
    }
}

#region FUNGI

public class Fungi : Plant
{
    public bool isPoisonous;
    public Fungi(int lifespan_years, int harvestCycle_months, bool isPoisonous = false) : base(lifespan_years, harvestCycle_months)
    {
    }
    public override string ToString()
    {
        return "Fungi :: " + subspecies + "\n" + base.ToString();
    }
    public override void OnHarvest()
    {
        ResourceController.Instance.ChangeStockpile(subspecies, harvestAmt);
    }
}

public class Mushroom : Fungi
{
    public Mushroom(int lifespan_years = 2, int harvestCycle_months = 2, int harvestAmt = 2, string subspecies = "") : base(lifespan_years, harvestCycle_months)
    {
        this.harvestAmt = harvestAmt;
        this.subspecies = subspecies;
    }
}

public class Toadstool : Fungi
{
    public Toadstool(int lifespan = 2, int harvestCycle_months = 4, bool isPoisonous = true, int harvestAmt = 2, string subspecies = "Toadstool") : base(lifespan, harvestCycle_months, isPoisonous)
    {
        this.harvestAmt = harvestAmt;
        this.subspecies = subspecies;
    }
}

#endregion

#region TREES
public class Tree : Plant
{
    public Tree(int lifespan_years = 100, int harvestCycle_months = 6) : base(lifespan_years, harvestCycle_months)
    {
    }
    public override string ToString()
    {
        return "Tree :: " + subspecies + "\n" + base.ToString();
    }
    public override void OnHarvest()
    {
        ResourceController.Instance.ChangeStockpile(subspecies, harvestAmt);
    }
}
#endregion