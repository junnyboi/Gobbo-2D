using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    #region VARIABLE DECLARATION
    public GameObject circleCursorPrefab;
    public GameObject squareCursorPrefab;
    public GameObject squareCursorAnimatedPrefab;
    public GameObject tempSquareCursorAnimated { get; protected set; }
    public static Dictionary<string, Texture2D> cursors = new Dictionary<string, Texture2D>();

    // World positions of the mouse
    public Vector3 currPosition { get; protected set; }
    public Vector3 leftClickPosition { get; protected set; }
    public Vector3 rightClickPosition { get; protected set; }
    public Vector3 middleClickPosition { get; protected set; }

    // For tile drag
    int start_x;
    int start_y;
    int end_x;
    int end_y;
    List<GameObject> listOfDragPreviews_go;

    // Singleton references
    public static MouseController Instance; 
    World world { get { return WorldController.Instance.w; } }

    #endregion

    void Awake()
    {
        Instance = this;
        listOfDragPreviews_go = new List<GameObject>();
        //SimplePool.Preload(circleCursorPrefab, 100);

        Texture2D[] cursorsLoaded = Resources.LoadAll<Texture2D>("Cursors/");
        foreach (Texture2D cursor in cursorsLoaded)
        {
            cursors.Add(cursor.name, cursor);
        }

        SetCursor("Cursor_ArrowSplit");
    }

    // Update is called once per frame
    void Update()
    {
        // Track mouse position wrt world space at every frame
        currPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Check for mouse inputs
        Input_LeftClick();
        Input_RightClick();
        Input_MiddleClick();
        Input_ScrollWheel();
    }

    #region MOUSE INPUTS
    void Input_LeftClick()
    {
        // Single-click
        if (Input.GetMouseButtonDown(0))
        {
            leftClickPosition = currPosition;
            start_x = Mathf.RoundToInt(leftClickPosition.x);
            start_y = Mathf.RoundToInt(leftClickPosition.y);


            // Object investigation mode
            if (JobModeController.jobMode == JobModeType.Null && !JobModeController.isSmashMode)
                Investigate(start_x, start_y);
        }

        // Click-drag
        LeftClick_TileDrag();
    }

    void LeftClick_TileDrag()
    {
        #region Pre-Drag
        start_x = Mathf.RoundToInt(leftClickPosition.x);
        start_y = Mathf.RoundToInt(leftClickPosition.y);
        end_x = Mathf.RoundToInt(currPosition.x);
        end_y = Mathf.RoundToInt(currPosition.y);

        // Flip the start and end if dragging in opposite directions
        if (end_x < start_x)
        {
            int tmp = end_x;
            end_x = start_x;
            start_x = tmp;
        }
        if (end_y < start_y)
        {
            int tmp = end_y;
            end_y = start_y;
            start_y = tmp;
        }

        while (listOfDragPreviews_go.Count != 0)
        {
            if (listOfDragPreviews_go.Count > 1 && TooltipController.Instance.detailedTooltip.activeSelf)
            {
                Destroy(tempSquareCursorAnimated);
                TooltipController.Instance.TurnOffDetailedTooltip();
            }

            CleanDragPreviews();
        }
        #endregion

        #region While Dragging
        if (Input.GetMouseButton(0))
        {
            if (checkToCancelMouseInput)
            {
                CleanDragPreviews();
                return;
            }

            // In investigation mode
            if (JobModeController.jobMode == JobModeType.Null && !JobModeController.isSmashMode)
            {
                CleanDragPreviews();
                Investigate(Mathf.RoundToInt(currPosition.x), Mathf.RoundToInt(currPosition.y));
            }

            else
            {
                // Display preview of drag area
                for (int x = start_x; x <= end_x; x++)
                {
                    for (int y = start_y; y <= end_y; y++)
                    {
                        Tile t = WorldController.Instance.w.GetTileAt(x, y, -1);
                        if (t != null)
                        {
                            // Display building hint on top of tile position
                            GameObject go = SimplePool.Spawn(squareCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                            go.transform.SetParent(this.transform, true);

                            listOfDragPreviews_go.Add(go);
                        }
                    }
                }
            }
        }
        #endregion

        #region End Drag
        if (Input.GetMouseButtonUp(0))
        {
            if (JobModeController.jobMode == JobModeType.Null && !JobModeController.isSmashMode)
                SetCursor("Cursor_ArrowSplit");

            if (checkToCancelMouseInput) return;

            // loop through tiles in drag area
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = world.GetTileAt(x, y, -1);
                    if (t != null)
                    {
                        JobModeController.Instance.CreateJob(t);
                    }
                }
            }
        }
        #endregion
    }

    void CleanDragPreviews()
    {
        // Clean up all cursor drag previews
        foreach (GameObject drag_go in listOfDragPreviews_go)
            SimplePool.Despawn(drag_go);
        listOfDragPreviews_go.Clear();
    }

    void Investigate(int x, int y)
    {
        SetCursor("Cursor_MagnifyingGlass");

        // Initialize
        Tile t = world.GetTileAt(x, y, -1);
        GameObject tooltip = TooltipController.Instance.TurnOnDetailedTooltip();
        TMP_Text tmp = tooltip.GetComponentInChildren<TMP_Text>();

        if (tmp == null)
            Debug.LogError("ERROR: detailed tooltip TMP not found!");

        // Instantiate animated cursor over the tile
        if (tempSquareCursorAnimated != null)
            Destroy(tempSquareCursorAnimated);

        tempSquareCursorAnimated = GameObject.Instantiate(squareCursorAnimatedPrefab, new Vector3(x, y), Quaternion.identity, this.transform);

        // TODO: Cycle object selection under mouse on multiple clicks
        // TODO: Enhancement of the information displayed
        if (t.hasCreature)
        {
            Creature c = t.creaturesOnTile[0];
            tmp.text = c.ToString();

            // Cursor & tooltip should follow the creature
            GameObject c_go = CreaturesController.Instance.creatureGameObjectMap[c];

            tempSquareCursorAnimated.transform.position = new Vector3(0, 0, 0);
            tempSquareCursorAnimated.transform.SetParent(c_go.transform, false);

            float vectorScale = c_go.transform.localScale.x;
            tempSquareCursorAnimated.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f) / vectorScale;
        }
        else if (t.hasFurniture)
            tmp.text = t.furniture.ToString();
        else if (t.hasResource)
            tmp.text = t.resource.ToString();
        else if (t.hasPlant)
            tmp.text = t.plant.ToString();
        else if (t.hasRoom)
            tmp.text = t.room.ToString();
        else
            tmp.text = t.ToString();
    }

    void Input_RightClick()
    {
        // Single-click
        if (Input.GetMouseButtonDown(1))
        {
            rightClickPosition = currPosition;

            // Unselect all job modes
            JobModeController.Instance.SetMode_Null();
            TooltipController.Instance.detailedTooltip.SetActive(false);
            SetCursor("Cursor_ArrowSplit");
        }

        // Click-drag
        if (Input.GetMouseButton(1))
        {
            Vector3 translation = rightClickPosition - currPosition;
            CameraController.Instance.Move(translation);
        }
    }

    void Input_MiddleClick()
    {
        if (Input.GetMouseButtonDown(2))
        {
            world.ToggleLayer();
        }
    }

    void Input_ScrollWheel()
    {
        if (checkToCancelMouseInput) return;

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            CameraController.Instance.Zoom(Input.GetAxis("Mouse ScrollWheel") * 2f);
        }
    }
    #endregion


    #region UTILITIES

    public static void SetCursor(string name)
    {
        Texture2D cursor = cursors[name];
        Vector2 cursorOffset = new Vector2(cursor.width / 2, cursor.height / 2);
        Cursor.SetCursor(cursor, cursorOffset, CursorMode.Auto);
    }
    public Tile GetMouseOverTile()
    {
        return WorldController.Instance.w.GetTileAt(Mathf.RoundToInt(currPosition.x), 
                                                        Mathf.RoundToInt(currPosition.y), -1);
    }
    bool checkToCancelMouseInput
    { get { return EventSystem.current.IsPointerOverGameObject()
                    || Input.GetKey(KeyCode.Escape); } } 
    #endregion


}
