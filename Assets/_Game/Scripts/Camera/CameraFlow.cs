using UnityEngine;

public class CameraFlow : MonoBehaviour
{
    private Camera cameraMain;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraMain = Camera.main;
    }

    void LateUpdate()
    {

    }
}
