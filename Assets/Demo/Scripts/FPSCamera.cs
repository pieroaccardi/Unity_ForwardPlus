using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCamera : MonoBehaviour
{
    //PUBLIC FIELDS
    public float RotateSensitivity = 0.4f;
    public float MoveSensitivity = 0.5f;

    //PRIVATE FIELDS
    private Vector3 rot;
    private bool    rotating = false;
    private Vector3 lastMousePos;
    private float   rotationY = 0;

    private void Update()
    {
        float dx = Input.GetAxis("Horizontal") * MoveSensitivity;
        float dy = Input.GetAxis("Vertical") * MoveSensitivity;

        if (Input.GetMouseButtonDown(1))
        {
            lastMousePos = Input.mousePosition;
            rotating = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            rotating = false;
        }

        //rotate
        if (rotating)
        {
            float deltaX = Input.mousePosition.x - lastMousePos.x;
            float deltaY = Input.mousePosition.y - lastMousePos.y;

            float rotationX = transform.localEulerAngles.y + deltaX * RotateSensitivity;

            rotationY += deltaY * RotateSensitivity;

            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);

            lastMousePos = Input.mousePosition;
        }

        //move
        Vector3 p = transform.position;
        p += transform.right * dx + transform.forward * dy;
        transform.position = p;
    }
}
