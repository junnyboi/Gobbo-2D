using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    public Camera CloseupCam;

    public float transitionSpeed = 1;
    private Creature centredCreature = null;

    #region MONOBEHAVIOR
    void Start()
    {
        Instance = this;
    }

    void Update()
    {
        if (centredCreature == null) return;
        GradualTransition(new Vector3(centredCreature.X, centredCreature.Y), transitionSpeed);
        if (centredCreature.currentDepth != WorldController.Instance.w.currentZDepth)
            WorldController.Instance.w.ToggleLayer();
    }
    #endregion

    #region BASIC CAMERA CONTROLS
    void Move(Vector3 translation)
    {
        if (translation.magnitude == 0)
            return;

        Camera.main.transform.Translate(translation * Time.unscaledDeltaTime);
    }

    public void MoveFromInput(Vector3 translation)
    {
        if (centredCreature != null)
            centredCreature = null;

        Move(translation);
    }

    public void Zoom(float zoomSpeed)
    {
        Camera.main.orthographicSize -= zoomSpeed;
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 15f);
    }
    #endregion

    #region CENTRE CAMERA
    public void CentreWorldCamera(World w)
    {
        Camera.main.transform.position = new Vector3(w.Width / 2, w.Height / 2, Camera.main.transform.position.z);
    }
    public void CentreWorldCamera(Vector2 point)
    {
        Camera.main.transform.position = new Vector3(point.x, point.y, Camera.main.transform.position.z);
    }
    public void CentreWorldCamera(float X, float Y)
    {
        Camera.main.transform.position = new Vector3(X, Y, Camera.main.transform.position.z);
    }
    public void CentreAroundCreature(Creature c)
    {
        centredCreature = c;
    } 
    #endregion

    void GradualTransition(Vector3 dest, float speed)
    {
        Vector3 destPosition = new Vector3(dest.x, dest.y, Camera.main.transform.position.z);
        Vector3 currPosition = Camera.main.transform.position;

        if (currPosition == destPosition) 
            return;

        float d = (destPosition - currPosition).sqrMagnitude;        
        Vector3 translation = (destPosition - currPosition).normalized * speed * d;
        Move(translation);
    }

}
