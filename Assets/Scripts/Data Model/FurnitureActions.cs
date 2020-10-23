using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FurnitureActions
{
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {

        if (furn.GetParameter("is_opening") >= 1)     // if trying to open
        {
            furn.ChangeParameter("openness", deltaTime * 3);
            
            if (furn.GetParameter("openness") >= 1)       // if fully open,
            {
                furn.SetParameter("is_opening", 0);      // stop trying to open
            }
        }
        else
        {   // close the door behind character
            furn.ChangeParameter("openness", - deltaTime * 2);
        }

        furn.SetParameter("openness", Mathf.Clamp01(furn.GetParameter("openness")));
        FurnitureSpriteController.Instance.OnFurnitureChanged(furn);
    }

    public static ENTERABILITY Door_IsEnterable(Furniture furn)
    {
        furn.SetParameter("is_opening", 1);      // start opening the door

        if (furn.GetParameter("openness") >= 1 ||     // if opened
            furn.tile.hasDummyFixedObject)   // or if dummy 
        {
            return ENTERABILITY.Yes;
        }

        return ENTERABILITY.Soon;
    }
}
