using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    public void Move(Vector3 translation)
    {
        if (translation.magnitude == 0)
            return;

        Camera.main.transform.Translate(translation * Time.unscaledDeltaTime);
    }

    public void Zoom(float zoomSpeed)
    {
        Camera.main.orthographicSize -= zoomSpeed;
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 15f);
    }

    public void CentreWorldCamera(World w)
    {
        Camera.main.transform.position = new Vector3(w.Width / 2, w.Height / 2, Camera.main.transform.position.z);
    }
    public void CentreWorldCamera(Vector2 point)
    {
        Camera.main.transform.position = new Vector3(point.x, point.y, Camera.main.transform.position.z);
    }
}
