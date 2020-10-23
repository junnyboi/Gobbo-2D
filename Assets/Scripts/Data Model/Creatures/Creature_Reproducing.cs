using PopulationGrowth.Events;
using PopulationGrowth.Maths;
using System.Collections.Generic;
using System.Diagnostics;

public class Creature_Reproducing : Creature
{
    #region Variables Declaration

    public Creature father { get; protected set; }
    public Creature mother { get; protected set; }
    public string fatherName { get; protected set; } = "nobody";
    public string motherName { get; protected set; } = "nobody";
    public bool hasParents { get { return father != null && mother != null; } }

    public int relationshipAge { get; set; }        // What age can this individual start a relationship

    public Creature_Reproducing partner { get; set; }      // Null if individual is single
    public double yearToNewChild { get; set; }    // Year in the simulation when individual can have children
    #endregion

    protected Creature_Reproducing(Tile tile, int age) : base(tile, age)
    {
    }

    public void SetParents(Creature father, Creature mother)
    {
        if (father != null)
        {
            this.father = father;
            fatherName = father.name;
            SetLastName(father.lastName);
        }
        if (mother != null)
        {
            this.mother = mother;
            motherName = mother.name;
            SetMiddleName(mother.middleName);
        }
    }
        
    #region GENERIC MATING RITUALS
    /// <summary>
    /// Determines whether an individual is single and age > dating age
    /// </summary>
    public bool SuitableToBeInRelationship()
    {
        if (partner != null)
            return false;
        if (this.age < relationshipAge)
            return false;
        return true;
    }

    /// <summary>
    /// Determines whether an individual is suitable for dating (NEED TO OVERRIDE)
    /// </summary>
    /// <param name="individual"></param>
    public bool SuitablePartner(Creature_Reproducing individual)
    {
        Debug.Fail("Creature_Reproducing :: SuitablePartner -- This function should be overridden!");
        return false;
    }

    /// <summary>
    /// True if individual is involved in a relation, false otherwise.
    /// </summary>
    public bool isEngaged
    {
        get { return partner != null; }
    }

    /// <summary>
    /// Ends the relation between two individuals.
    /// </summary>
    public void Disengage()
    {
        partner.partner = null;
        partner = null;
        yearToNewChild = 0;
    }

    /// <summary>
    /// Finds an available partner for the individual
    /// </summary>
    /// <param name="population"></param>
    /// <param name="currentTime"></param>
    /// <param name="distributions"></param>
    public void FindPartner(List<Creature_Reproducing> population, int currentTime,
                            Dictionary<PopEvent, int[]> distributions)
    {

        for (var i = 0; i < population.Count; i++)
        {
            Creature_Reproducing candidate = population[i];
            if (SuitablePartner(candidate) && candidate.SuitableToBeInRelationship())
            {
                // couple them
                candidate.partner = this;
                partner = candidate;

                // Set time for having child
                int childTime = new Maths().randSelect(distributions[PopEvent.TimeBetweenPregnancies]);

                // They can have children on the simulated year: 'Female's age + childTime'.
                if (candidate is Goblyn_Female)
                    candidate.yearToNewChild = yearToNewChild = candidate.age + childTime;
                else
                    candidate.yearToNewChild = yearToNewChild = age + childTime;

                // Monogamy: Stop looking for a partner when you have found one
                break;
            }
        }
    } 
    #endregion

    /// <summary>
    /// String representation of a character
    /// </summary>
    public override string ToString()
    {
        if (gender == GenderType.Male)
            return base.ToString() + string.Format("Son of {0}\nand {1}\n"
                                    , fatherName, motherName);

        else if (gender == GenderType.Female)
                return base.ToString() + string.Format("Daughter of {0}\nand {1}\n"
                                    , fatherName, motherName);

        else return base.ToString() + string.Format("Child of {0}\nand {1}\n"
                                    , fatherName, motherName);
    }
}
