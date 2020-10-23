using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Text_PopulationSummary : MonoBehaviour
{
    TMP_Text TMP;

    // Start is called before the first frame update
    void Start()
    {
        TMP = GetComponent<TMP_Text>();

        if (TMP == null)
        {
            Debug.LogError("Text_PopulationSummary -- textmeshpro missing.");
            this.enabled = false;
            return;
        }
    }
    // Update is called once per frame
    void Update()
    {
        string popSummary = DateTimeController.Instance.ToString() + "\n\n";

        popSummary += "Resources \n";
        popSummary += "--------------------------\n";
        if (ResourceController.stockpile.Count > 0)
        {
            foreach (KeyValuePair<string, Resource> resource in ResourceController.stockpile)
            {
                popSummary += resource.Key + ": " + resource.Value.ToString() + "\n";
            }
        }
        else
            popSummary += "No resources yet. \n";
        popSummary += "--------------------------\n";

        popSummary += GoblynPopController.Instance.PopulationSummary();
        TMP.text = popSummary;
    }
}
