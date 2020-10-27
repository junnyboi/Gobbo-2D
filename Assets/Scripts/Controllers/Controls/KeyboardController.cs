using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardController : MonoBehaviour
{
    #region VARIABLE DECLARATION
    public static KeyboardController Instance;
    public GameObject go_summaryUI;
    public GameObject go_GUICanvas;
    public GameObject go_BuildFurniture;
    public GameObject go_BuildPlant;
    public int camPanSpeed = 4;
    World world { get { return WorldController.Instance.w; } }
    DateTimeController DateTimeController { get { return DateTimeController.Instance; } }
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        #region Speed Controls
        // Pause / Unpause

        if (Input.GetKeyDown(KeyCode.Space))
        {
            DateTimeController.TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = 1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Time.timeScale = 2;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Time.timeScale = 3;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Time.timeScale = 4;
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Time.timeScale = 5;
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            DateTimeController.ToggleSpeedUp();
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            DateTimeController.ToggleSpeedDown();
        }

        #endregion

        #region Camera Controls

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {            
            Vector3 translation = new Vector3(0, camPanSpeed);
            CameraController.Instance.Move(translation);
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            Vector3 translation = new Vector3(0, -camPanSpeed);
            CameraController.Instance.Move(translation);
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            Vector3 translation = new Vector3(-camPanSpeed, 0);
            CameraController.Instance.Move(translation);
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            Vector3 translation = new Vector3(camPanSpeed, 0);
            CameraController.Instance.Move(translation);
        }

        // Full screen (Map)
        if (Input.GetKeyDown(KeyCode.M))
        {
            Camera.main.orthographicSize = Camera.main.orthographicSize == 45 ? 5 : 45;
        }

        // Z-Layer toggle
        if (Input.GetKeyDown(KeyCode.Z))
        {
            world.ToggleLayer();
        }

        #endregion

        #region UI Controls

        // Hide UI
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            go_GUICanvas.GetComponent<ToggleUI>().ToggleGameObject();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            go_GUICanvas.GetComponent<ToggleUI>().ToggleGameObject();
            go_BuildFurniture.SetActive(go_GUICanvas.activeSelf);
            go_BuildPlant.SetActive(go_GUICanvas.activeSelf);
        }

        #endregion

        #region Game Controls

        if (Input.GetKeyDown(KeyCode.X))
        {
            JobModeController.Instance.SetMode_Smash();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            JobModeController.Instance.SetMode_Cancel();
        }

        #endregion


    }

}
