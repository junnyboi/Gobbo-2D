using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

public class ResourceDrops
{
    public static Dictionary<string, Resource> lootMap = new Dictionary<string, Resource>()
    {
        { "", new Resource("", 1) },
        { "Default", new Resource("Orange Gem", 1) },
        { "Plant", new Resource("Wood", 2) },
        { "Cave Wall", new Resource("Dirt Block", 1) },
    };

    public static Dictionary<string, Resource> harvestMap = new Dictionary<string, Resource>()
    {
        { "", new Resource("", 1) },
        { "Default", new Resource("Green Gem", 1) },
        { "Plant", new Resource("Mushroom", 3) },
    };
}
