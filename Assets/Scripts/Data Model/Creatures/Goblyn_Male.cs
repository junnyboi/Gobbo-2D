
using PopulationGrowth.Maths;
public class Goblyn_Male : Goblyn
{
    public Goblyn_Male(Tile tile, int age) : base(tile, age)
    {
        SetGender(GenderType.Male);
        GoblynPopController.Instance.UpdatePopulationTally(this);
    }
}