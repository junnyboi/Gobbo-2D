using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : Resource
{
    public int calories { get; protected set; }

    // TODO: Link up with time controller and implement expiry on perishables
    public int age { get; protected set; }
    public int expiry { get; protected set; }

    public Food(string name, int nutrition) : base(name)
    {
        this.calories = nutrition;
    }

    public override string ToString()
    {
        return base.ToString() + ", nutrition: " + calories;
    }
}
