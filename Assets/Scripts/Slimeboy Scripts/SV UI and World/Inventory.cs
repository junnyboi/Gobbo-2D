using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public const int numItemSlots = 6;
    public Image[] itemImages = new Image[numItemSlots];
    public Item[] items = new Item[numItemSlots];

    public const int maxStackSize = 99;
    private int?[] currentStackSizes = new int?[numItemSlots];
    public TMP_Text[] textStackCount = new TMP_Text[numItemSlots];
    public TMP_Text[] textStackName = new TMP_Text[numItemSlots];

    private int selectedItem = 1;

    private void Awake()
    {
        Instance = this;
    }

    public void AddItem(Item itemToAdd)
    {
        for (int i = 0; i < items.Length; i++)
        {
            // first check for existing stack
            if (items[i] == itemToAdd)
            {
                // check if maxStackSize exceeded before adding
                if (currentStackSizes[i] <= maxStackSize)
                {
                    currentStackSizes[i] += 1;
                }

                print(itemToAdd + " added, stack size: " + currentStackSizes[i]);
                textStackCount[i].text = currentStackSizes[i].ToString();

                // terminate so that it only changes a single slot
                return;
            }

            // create a new item if no stackable slot
            else if (items[i] == null)
            {      
                // add item to the first empty slot
                items[i] = itemToAdd;
                currentStackSizes[i] = 1;

                itemImages[i].sprite = itemToAdd.sprite;

                // enable sprite images
                itemImages[i].enabled = true;

                print(itemToAdd + " added, stack size: " + currentStackSizes[i]);
                textStackCount[i].text = currentStackSizes[i].ToString();
                textStackName[i].text = itemToAdd.itemName;

                // terminate so that it only changes a single slot
                return;
            }            
        }
    }

    public void RemoveItem(Item itemToRemove)
    {
        // iterate through item slots
        for (int i = 0; i < items.Length; i++)
        {
            // first check for existing stack
            if (items[i] == itemToRemove)
            {               
                if (currentStackSizes[i] > 1)
                {
                    currentStackSizes[i] -= 1;
                    textStackCount[i].text = currentStackSizes[i].ToString();
                }
                else if (currentStackSizes[i] == 1)
                {
                    currentStackSizes[i] = null;
                    textStackCount[i].text = null;
                    textStackName[i].text = null;
                    items[i] = null;
                    itemImages[i].sprite = null;
                    itemImages[i].enabled = false;

                    print(itemToRemove + " removed, stack size: " + currentStackSizes[i]);

                    // terminate so that it only changes a single slot
                    return;
                }

                print(itemToRemove + " removed, stack size: " + currentStackSizes[i]);

                // terminate so that it only changes a single slot
                return;
            }                        
        }
    }

    public void SelectItem(int index)
    {
        if(index < numItemSlots+1 || index > -1)
        {
            selectedItem = index;
        }
    }

    public void PlaceItem(int index)
    {
        if (index < numItemSlots + 1 || index > -1)
        {
            Item itemToPlace = items[index];
            if (items[index] != null)
            {
                RemoveItem(itemToPlace);
                SlimeboyController.Instance.DropLoot(itemToPlace.itemObject);
            }
            else Debug.LogError("no item in inventory slot " + index);
        }
        else Debug.LogError("outside inventory range");
    }

    private void Update()
    {
        // PLACE ITEMS IN WORLD SPACE
        if (Input.GetKeyDown(KeyCode.Alpha1))
            Inventory.Instance.PlaceItem(0);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            Inventory.Instance.PlaceItem(1);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            Inventory.Instance.PlaceItem(2);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            Inventory.Instance.PlaceItem(3);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            Inventory.Instance.PlaceItem(4);
        if (Input.GetKeyDown(KeyCode.Alpha6))
            Inventory.Instance.PlaceItem(5);
    }
}