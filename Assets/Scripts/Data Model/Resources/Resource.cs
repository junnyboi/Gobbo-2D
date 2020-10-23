using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource
{
    #region VARIABLES DECLARATION

    public string type { get; protected set; }
    public int amount { get; protected set; }
    public int amountBlocked = 0;

    #region Ownership (for resource stacks)

    public string owner { get
        {
            if (tile != null) return "tile";
            else if (creature != null) return "creature";
            else Debug.LogError("Resource :: owner -- not found!");                
            return "ERROR";
        }
    }
    public Tile tile { get; protected set; }
    public Creature creature { get; protected set; }

    public event Action<Resource> cbResourceAmountChanged;

    #endregion

    #endregion

    #region CONSTRUCTORS

    public Resource(string type, int amount = 0, Tile ownerTile = null)
    {
        this.type = type;
        this.amount = amount;
        SetOwner(ownerTile);
    }

    // Copy constructor
    protected Resource(Resource other, Tile ownerTile = null)
    {
        type = other.type;
        amount = other.amount;
        SetOwner(ownerTile);
    }
    public virtual Resource Clone(Tile ownerTile = null)
    {
        return new Resource(this, ownerTile);
    }

    #endregion

    #region FUNCTIONS
    public int ChangeAmount(int delta)
    {
        int amount_initial = amount;
        amount += delta;
        if (amount < 0) Empty();
        cbResourceAmountChanged?.Invoke(this);

        return amount - amount_initial;
    }

    public void SetAmount(int amount)
    {
        this.amount = amount;
        if (amount < 0) Empty();
        cbResourceAmountChanged?.Invoke(this);
    }

    public void Empty()
    {
        amount = 0;
        cbResourceAmountChanged?.Invoke(this);
    }
    #endregion

    #region OWNERSHIP FUNCTIONS (for resource stacks)
    public bool SetOwner(Creature c)
    {
        creature = c;
        tile = null;
        return true;
    }
    public bool SetOwner(Tile t)
    {
        tile = t;
        creature = null;
        return true;
    }

    // FIXME: need to implement stockpile classes / ownership

    public bool UnassignOwner()
    {
        creature = null;
        tile = null;
        return true;
    }

    #endregion

    #region UTILITY
    public override string ToString()
    {
        return amount + " x " + type;
    } 
    #endregion

}
