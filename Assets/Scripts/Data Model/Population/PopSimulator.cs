using PopulationGrowth.Events;
using System;
using System.Collections.Generic;
using PopulationGrowth.Maths;
using System.Linq;

/// <summary>
/// Population Growth Simulator -- use Execute()
/// </summary>
public class PopSimulator
{
    #region Variables Declaration
    World world { get { return WorldController.Instance.w; } }

    public List<Goblyn> populationList;
    public int populationCap = 200;
    private int _currentYear { get { return DateTimeController.Instance.year; } }
    private readonly Dictionary<PopEvent, int[]> _distributions;
    public string print = " ";
    public string error = " ";

    public event Action cbOnExecuteSimulatorOnce;
    #endregion

    public PopSimulator(int populationCap, IEnumerable<Goblyn> population)
    {
        this.populationCap = populationCap;
        populationList = new List<Goblyn>(population);

        _distributions = new Dictionary<PopEvent, int[]>
        {
            { PopEvent.DatingAge, new int[] { 15,16,17,17,18,18,19,19,20,20,21,22 } },
            { PopEvent.FirstPregnancyAge, new int[] { 20,21,22,23,23,23,24,24,24,24,25,25,26,27 } },
            { PopEvent.ChildrenCount, new int[] { 2,3,3,4,4,5, } },
            { PopEvent.TimeBetweenPregnancies, new int[] { 3,4,4,5,5,5,6,7} },
            { PopEvent.BaseLifespan, new int[] { 40,45,45,50,50,55 } },
        };

        foreach (Goblyn gob in populationList)
        {
            GenerateRandomLifeEvents(gob);
        }
    }

    #region Simulation Logic

    /// <summary>
    /// Main simulation logic!
    /// </summary>
    public void ExecuteOnce()
    {
        // Iterate through every individual in the population this year
        for (var i = 0; i < populationList.Count; i++)
        {
            Goblyn individual = populationList[i];

            // Event -> Pregnant women from last year give birth
            Event_Birth(individual);

            // Event -> Eligible singles start dating (within 10 years of age range)
            Event_Dating(individual);

            // Events for couples only
            if (individual.isEngaged)
            {
                // Event -> Check whether a couple fulfils conditions to have a child
                Event_Pregnancy(individual);
            }
            // Event -> Check whether someone dies this year
            if (Check_Die(individual))
                Event_Die(individual, i);

            //individual.ChangeAge(1);
        }

        cbOnExecuteSimulatorOnce?.Invoke();
    }

    /// <summary>
    /// Run the population simulation for a few years
    /// </summary>
    public void ExecuteIterate(int numIterations)
    {
        for (int i = 0; i < numIterations; i++)
        {
            ExecuteOnce();
        }
    }

    public void GenerateRandomLifeEvents(Goblyn gob)
    {
        // lifeTime
        gob.lifespan = new Maths().randSelect(_distributions[PopEvent.BaseLifespan]);

        // Ready to start having relations
        gob.relationshipAge = new Maths().randSelect(_distributions[PopEvent.DatingAge]);

        // Pregnancy age (only women)
        if (gob is Goblyn_Female)
        {
            (gob as Goblyn_Female).pregnancyAge = new Maths().randSelect(_distributions[PopEvent.FirstPregnancyAge]);
            (gob as Goblyn_Female).childrenCount = new Maths().randSelect(_distributions[PopEvent.ChildrenCount]);
        }
    } 

    #endregion

    #region Life Events

    void Event_Dating(Goblyn individual)
    {
        if (individual.SuitableToBeInRelationship())
            individual.FindPartner(populationList, _currentYear, _distributions);
    }

    void Event_Birth(Goblyn individual)
    {
        if (individual is Goblyn_Male)
            return;
        if (!(individual as Goblyn_Female).isPregnant)
            return;
        if (populationList.Count >= populationCap)
            return;

        Goblyn child = (individual as Goblyn_Female).GiveBirth(_distributions, _currentYear);
        world.RegisterNewCreature(child);
    }
    void Event_Pregnancy(Goblyn individual)
    {
        if (
            individual is Goblyn_Female &&
            (individual as Goblyn_Female).SuitableForPregnancy(_currentYear)
           )
        {
            (individual as Goblyn_Female).isPregnant = true;
        }
    }
    bool Check_Die(Goblyn individual)
    {
        return individual.age == (individual.lifespan);
    }

    void Event_Die(Goblyn individual, int i)
    {
        // Case: Goblin in relationship (break relation)
        if (individual.isEngaged)
            individual.Disengage();

        // Update gender demographic count
        if (individual is Goblyn_Male)
            GoblynPopController.Instance.maleCount -= 1;
        else if (individual is Goblyn_Female)
            GoblynPopController.Instance.femaleCount -= 1;

        // Remove individual
        populationList.RemoveAt(i);
    } 

    #endregion
}
