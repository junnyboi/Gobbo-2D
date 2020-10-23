/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(Inventory))]
public class InventoryEditor : Editor
{
    private SerializedProperty itemImagesProperty;
    private SerializedProperty itemsProperty;
    private SerializedProperty stackCountProperty;
    private bool[] showItemSlots = new bool[Inventory.numItemSlots];

    private const string inventoryPropItemImagesName = "itemImages";
    private const string inventoryPropItemsName = "items";
    private const string inventoryPropStackCount = "stackCount";

    private void OnEnable()
    {
        itemImagesProperty = serializedObject.FindProperty(inventoryPropItemImagesName);
        itemsProperty = serializedObject.FindProperty(inventoryPropItemsName);
        stackCountProperty = serializedObject.FindProperty(inventoryPropStackCount);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        for (int i = 0; i < Inventory.numItemSlots; i++)
        {
            ItemSlotGUI(i);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ItemSlotGUI(int index)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;

        // showItemSlots bool value decides if foldout is open or closed
        showItemSlots[index] = EditorGUILayout.Foldout(showItemSlots[index], "Item slot" + index);


        if (showItemSlots[index])
        {
            EditorGUILayout.PropertyField(itemImagesProperty.GetArrayElementAtIndex(index));
            EditorGUILayout.PropertyField(itemsProperty.GetArrayElementAtIndex(index));
            EditorGUILayout.PropertyField(stackCountProperty.GetArrayElementAtIndex(index));
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }
}
*/