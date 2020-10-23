using PopulationGrowth.Events;
using PopulationGrowth.Maths;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public abstract class Goblyn: Creature_Reproducing
{
    #region GOBLYN NAME LISTS
    public static List<string> firstNames_male
    {
        get
        {
            return new List<string>()
            {
                "Jun",
                "Ol'dapo",
                "Kar'zay",
                "Drun'tu",
                "Traeg'mai",
                "Miro",
                "Xaag'min",
                "Uvupa",
                "Wom'tu",
                "Hun'jisa",
                "Jusen",
                "Waa'mik",
                "Wurah",
                "Mungir",
                "Zalez",
                "Xelek",
                "Jen'tur",
                "Sen'riy",
                "Pa'jun",
                "Vuh'sen",
                "Ko'thon",
            };
        }
    }
    public static List<string> firstNames_female
    {
        get
        {
            return new List<string>()
            {
                "Michelle",
                "Aenneh",
                "Hiz'ka",
                "Kajoye",
                "Huh'zo",
                "Zilroh",
                "Eiyoz'ko",
                "Lonu",
                "Jukien'ko",
                "Ra'minzo",
                "Teinzishe",
                "Izae",
                "Yuh'ku",
                "Liz'ki",
                "Xolmo",
                "Izro",
                "Vehda",
                "Nilzo",
                "Ohmoth",
                "I'mi",
                "Shende",
            };
        }
    }
    public static List<string> middleNames
    {
        get
        {
            return new List<string>()
            {
                "Aloe",
                "Vera",
                "Lichen",
                "Rosemary",
                "Thyme",
                "Sage",
                "Alder",
                "Almond",
                "Arizona",
                "Bamboo",
                "Oak",
                "Maple",
                "Ash",
                "Bay",
                "Beech",
                "Birch",
                "Weed",
                "Cornel",
                "Cotton",
                "Cress",
                "Fennel",
                "Fairymoss",
                "Fig",
                "Foxglove",
                "Flax",
                "Gilly",
                "Holly",
                "Hellebore",
                "Hedge",
                "Ivy",
                "Keek",
                "Kinnikinnik",
                "Kousa",
                "Kudzu",
                "Kumarahou",
                "Lupin",
                "Mesquite",
                "Milkweed",
                "Mahogany",
                "Nightshade",
                "Fuschia",
                "Osier",
                "Pea",
                "Pine",
                "Pistachio",
                "Ragweed",
                "Ragwort",
                "Ribwort",
                "Rice",
                "Rocket",
                "Rye",
                "Shadblow",
                "Shadbush",
                "Sativus",
                "Sneezewort",
                "Sycamore",
                "Tickleweed",
                "Toothwort",
                "Tulsi",
                "Palm",
                "Lily",
                "Violet",
                "Walnut",
                "Peanut",
                "Chestnut",
                "Hazelnut",
                "Honeysuckle",
                "Wormwood",
                "Zedoary"
            };
        }
    }
    public static List<string> lastNames
    {
        get
        {
            return new List<string>()
            {
                "Boh",
                "Lim",
                "Capuchin",
                "Armadillo",
                "Wolf",
                "Sugarglider",
                "Tarantula",
                "Macaw",
                "Tegu",
                "Fennec",
                "Axolotl",
                "Cheetah",
                "Wildebeest",
                "Bison",
                "Squirrel",
                "Grizzly",
                "Opossum",
                "Mara",
                "Kinkajou",
                "Caecilian",
                "Hog",
                "Jerboa",
                "Bushbaby",
                "Fox",
                "Capybara",
                "Urchin",
                "Tamandua",
                "Giraffe",
                "Rhino"
            };
        }
    }
    #endregion

    #region VARIABLE DECLARATION

    float hungerBurnRate = 0.005f;   // per hour?
    float hungerThreshold = 0.2f;    // triggers auto-feeding 
    float hungerCurrent = 1;

    int caloriesBurnRate = 1;
    int caloriesThreshold = 2000;   // triggers increased hunger and energy consumption
    int caloriesCurrent = 4000;
    bool isCalorieDeficient = false;

    float energyBurnRate = 0.01f;   // sleep in 20 hours
    float energyCurrent = 1;    // trigger rest when no energy left

    public event Action<Goblyn> cbGoblynOlder_hour;
    #endregion
    protected Goblyn(Tile tile, int age) : base(tile, age)
    {
        SetSpecies(SpeciesCreature.Goblyn);
        envClass = EnvClass.Terrestrial;
        idlePauseTime = 1.5f;
    }

    public void GenerateName()
    {
        if(firstName == null)
            switch (gender)
            {
                case GenderType.Male:
                    SetFirstName(new Maths().randSelect(firstNames_male));
                    break;
                case GenderType.Female:
                    SetFirstName(new Maths().randSelect(firstNames_female));
                    break;
                case GenderType.Hemaphrodite:
                    SetFirstName(new Maths().randSelect(firstNames_female));
                    break;
                default:
                    SetFirstName(new Maths().randSelect(firstNames_male));
                    break;
            }
        if(middleName == null)
            SetMiddleName(new Maths().randSelect(middleNames));
        if (lastName == null)
            SetLastName(new Maths().randSelect(lastNames));
    }

    public void OnGoblynOlder_hour()
    {
        Update_Energy();
        Update_Hunger();
        Update_Calories();

        cbGoblynOlder_hour?.Invoke(this);
    }

    void Update_Energy()
    {
        if (energyCurrent > 0)
        {
            energyCurrent -= isCalorieDeficient ? energyBurnRate * 1.5f : energyBurnRate;
            energyCurrent = Mathf.Clamp01(energyCurrent);
        }

        else
        {
            if (myJob != null)
            {
                if (myJob.jobType == "Sleep")
                    return;
            }
            else
                AbandonJob();

            if (myBed == null)
            {
                Debug.Log(this.nameShortform + " is searching for an empty bedroom to claim.");
                // Find an empty bedroom to claim
                foreach (Room r in WorldController.Instance.w.roomList)
                {
                    if (r.roomType == "Bedroom")
                    {
                        r.AssignOwner(this);
                        break;
                    }
                }

                // If I still can't find a bed, then give up
                if (myBed == null) return;
            }

            Debug.Log(this.nameShortform + " is going to bed.");

            // Go to bed and sleep
            myJob = new Job(myBed.tile, "Sleep", (theJob) =>
            {
                // Room changed or the owner probably died before he could sleep
                if (theJob.tile.room == null || 
                    theJob.tile.room.owner == null ||
                    theJob.tile.room.CheckRoomType() != "Bedroom" ||
                    theJob.tile.furniture == null ||
                    theJob.tile.furniture.type != "Bed")
                {
                    Debug.LogError("Invalid sleep job: " + theJob);
                }
                else
                {
                    Debug.Log(this.nameShortform + " is sleeping.");
                    energyCurrent = 1;
                    Teleport(myBed.tile);
                    ForceAnimationBool("Sleeping", 5);
                }
            });
        }
    }

    void Update_Hunger()
    {
        if (hungerCurrent > 0)
            hungerCurrent -= isCalorieDeficient? hungerBurnRate * 1.5f : hungerBurnRate;

        hungerCurrent = Mathf.Clamp01(hungerCurrent);

        if (hungerCurrent < hungerThreshold)
        {            
            EatSomething();
        }
    }

    void Update_Calories()
    {
        if (caloriesCurrent > 0)
        {
            caloriesCurrent -= caloriesBurnRate;

            // Check if calorie deficient
            isCalorieDeficient = caloriesCurrent < caloriesThreshold ? true : false;
        }
        else
        {
            //Debug.Log(this.nameShortform + " has run out of calories!");
            // Goblyn has no calories, he should be half dead at this point omg
            ChangeHealth(-1);
        }
    }

    void EatSomething()
    {
        // Peek at the top of the FoodQueue
        Food food = ResourceController.foodQueue.First();

        if (food == null)
        {
            Debug.Log(this.nameShortform + " has no food to eat! -- FoodQueue empty");
            return;
        }

        if( ResourceController.Instance.ChangeStockpile(food.type, -1))
        {
            Debug.Log(this.nameShortform + " is eating a mushroom.");
            hungerCurrent = 1;
            caloriesCurrent += food.calories/100;
            ForceAnimationBool("Eating", 2);
        }
        else
        {
            Debug.Log(this.nameShortform + " has no food to eat! -- Someone beat you to the food");
        }
    }

    #region GOBLYN MATING RITUALS
    /// <summary>
    /// Determines whether a Goblyn is suitable for dating (age difference and opposite sex)
    /// </summary>
    /// <param name="candidate"></param>
    public bool SuitableMate(Goblyn candidate)
    {
        System.Random r = new System.Random();

        if ((candidate.gender == this.gender))
            return false;
        if (System.Math.Abs(candidate.age - age) > 15)
            return false;
        if (this.hasParents && candidate.hasParents)
        {
            if (this.father == candidate.father || this.mother == candidate.mother)
                return false;
        }
        if (this.lastName == candidate.lastName)    // 10% chance of cousin marriage
        {
            if (r.Next(10) == 1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Finds an available Goblyn partner
    /// </summary>
    public void FindPartner(List<Goblyn> population, int currentTime,
                            Dictionary<PopEvent, int[]> distributions)
    {

        for (var i = 0; i < population.Count; i++)
        {
            Goblyn candidate = population[i];
            if (SuitableMate(candidate) && candidate.SuitableToBeInRelationship())
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

    public override string ToString()
    {
        return base.ToString() + string.Format("Hunger: {0}%, {1} Calories, Energy: {2}%"
                                               , Mathf.RoundToInt(hungerCurrent*100)
                                               , caloriesCurrent
                                               , Mathf.RoundToInt(energyCurrent *100)
                                              );
    }
}
