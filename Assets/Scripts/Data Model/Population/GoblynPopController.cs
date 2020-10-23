using PopulationGrowth.Maths;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class GoblynPopController : MonoBehaviour
{
    #region Population Variables
    public List<Goblyn> goblynsList;
    World world { get { return WorldController.Instance.w; } }
    public static GoblynPopController Instance;
    //private float nextUpdate = 0;

    public PopSimulator pop;
    public int year { get { return DateTimeController.Instance.year; } }
    public int maleCount = 0;
    public int femaleCount = 0;
    public int populationCap = 20;
    #endregion

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
        goblynsList = new List<Goblyn>();

        // Starting population: can I take this from an external editable?
        pop = new PopSimulator(populationCap, InitializeGoblinPopulation(0));
        pop.ExecuteIterate(20);

        // Callbacks
        world.cbCreatureCreated += OnCharacterCreated;
        world.cbCreatureRemoved += OnCharacterRemoved;
        DateTimeController.Instance.cbOnYearChanged += pop.ExecuteOnce;

        // Check for pre-existing (loaded) characters, and trigger their callbacks
        foreach (Creature c in world.creaturesList)
            OnCharacterCreated(c);
    }

    List<Goblyn> InitializeGoblinPopulation(int n)
    {
        switch (n)
        {
            case 0: return new List<Goblyn>();
            default:
                return new List<Goblyn>
                    {
                        new Goblyn_Male(null, 2), new Goblyn_Female(null, 3),
                        new Goblyn_Male(null, 4), new Goblyn_Female(null, 5),
                        new Goblyn_Male(null, 6), new Goblyn_Female(null, 7)
                    };
        }

    }

    public void UpdatePopulationTally(Goblyn gob, int i = 1)
    {
        if (gob == null)
            return;

        switch (gob is Goblyn_Male)
        {
            case true:
                maleCount += i;
                break;
            default:
                femaleCount += i;
                break;
        }
    }
    void OnCharacterCreated(Creature c)
    {
        if (c.species != SpeciesCreature.Goblyn)
            return;

        Goblyn gob = c as Goblyn;
        gob.GenerateName();
        c.currTile.creaturesOnTile.Add(c);

        pop.populationList.Add(gob);
        pop.GenerateRandomLifeEvents(gob);
        UpdatePopulationTally(gob,1);
        goblynsList.Add(gob);
    }
    void OnCharacterRemoved(Creature c)
    {
        Goblyn gob = c as Goblyn;

        // Remove from population
        UpdatePopulationTally(gob,-1);
        pop.populationList.Remove(gob);

        // Remove name from tile
        c.currTile.creaturesOnTile.Remove(c);

        // Remove from dictionary map
        goblynsList.Remove(gob);        
    }

    public string PopulationSummary()
    {
        if (pop.error != " ")
            Debug.LogError(pop.error);
        if (pop.print != " ")
            Debug.Log(pop.print);

        //string s = "Population Summary \n";
        string s = "";

        if (pop != null)
        {
            //s += "Year " + year + ", "; 
            s += "Population size: " + pop.populationList.Count + "\n\n";
        }

        foreach (var i in pop.populationList)
        {
            s += i.ToString();

            if (i.partner != null)
                s += " <attached> ";// + i.couple.ToString();
            else
                s += " <single> ";

            s += "\n\n";
        }

        return s;
        //Debug.Log(s);
    }

}
