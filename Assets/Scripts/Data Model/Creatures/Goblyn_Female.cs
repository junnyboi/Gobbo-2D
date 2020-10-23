using PopulationGrowth.Events;
using PopulationGrowth.Maths;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

public class Goblyn_Female : Goblyn
{
    #region Variables Declaration
    public bool isPregnant { get; set; }
    public double pregnancyAge { get; set; }    // What age can she get pregnant?
    public double childrenCount { get; set; }   // Number of children to give birth to 
    #endregion
    public Goblyn_Female(Tile tile, int age) : base(tile, age)
    {
        SetGender(GenderType.Female);
        GoblynPopController.Instance.UpdatePopulationTally(this);
    }
    
    /// <summary>
    /// Outputs true when this lady satisfies every condition for getting pregnant
    /// </summary>
    public bool SuitableForPregnancy(int currentTime)
    {
        return age >= pregnancyAge && age >= yearToNewChild && childrenCount > 0;
    }

    /// <summary>
    /// Adds and initializes new individuals into the population. 
    /// A uniform sample is generated to first determine if the new individual will be female or male, each with a probability of 50 percent (0.5). The childrenCount value is decremented by 1, indicating that this woman has one less child to give birth to. The rest of the code relates to individual initialization and resetting some variables.
    /// </summary>
    public Goblyn GiveBirth(Dictionary<PopEvent, int[]> distributions, int currentTime)
    {
        Goblyn child;

        #region Initialize child's variables

        if (GoblynPopController.Instance.femaleCount > GoblynPopController.Instance.maleCount)
            child = new Goblyn_Male(this.currTile, 0);
        else
            child = new Goblyn_Female(this.currTile, 0);

        child.SetParents(partner, this);
        child.lifespan = new Maths().randSelect(distributions[PopEvent.BaseLifespan]);
        child.relationshipAge = new Maths().randSelect(distributions[PopEvent.DatingAge]);

        if (child is Goblyn_Female)
        {
            (child as Goblyn_Female).pregnancyAge = new Maths().randSelect(distributions[PopEvent.FirstPregnancyAge]);
            (child as Goblyn_Female).childrenCount = new Maths().randSelect(distributions[PopEvent.ChildrenCount]);
        }

        #endregion

        #region Update Mum's pregnancy status

        childrenCount--;        // One less child to give birth to

        // update timeToNewChild for the next child
        if (isEngaged && childrenCount > 0)
        {
            yearToNewChild = age + new Maths().randSelect(distributions[PopEvent.TimeBetweenPregnancies]);

            partner.yearToNewChild = yearToNewChild;
        }
        else
            yearToNewChild = 0;

        isPregnant = false; 
        #endregion

        return child;
    }
    public override string ToString()
    {
        return base.ToString() + string.Format("upcoming children: {0}"
                                               ,childrenCount);
    }
}