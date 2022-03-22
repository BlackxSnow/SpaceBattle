using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float Speed = 10;
    public float Boost = 50;
    public float RollSpeed = 90;

    public float HorizontalRotationSpeed = 60;
    public float VerticalRotationSpeed = 90;

    private bool _IsLooking = false;

    private void Start()
    {
        Management.GameManager.Controls.Spectate.EnableLook.started += StartLook;
        Management.GameManager.Controls.Spectate.EnableLook.canceled += EndLook;
    }

    void Update()
    {
        PerformMove();
        PerformRoll();
        PerformRotate();
    }

    private void StartLook(InputAction.CallbackContext context)
    {
        _IsLooking = true;
    }

    private void EndLook(InputAction.CallbackContext context)
    {
        _IsLooking = false;
    }

    private void PerformMove()
    {
        float speed = Management.GameManager.Controls.Spectate.Boost.ReadValue<float>() > 0 ? Boost : Speed;
        Vector3 input = Management.GameManager.Controls.Spectate.Movement.ReadValue<Vector3>();
        transform.Translate(input * speed * Time.deltaTime);
    }

    private void PerformRoll()
    {
        float roll = -Management.GameManager.Controls.Spectate.Roll.ReadValue<float>() * RollSpeed * Time.deltaTime;
        transform.Rotate(0, 0, roll, Space.Self);
    }

    private void PerformRotate()
    {
        if (_IsLooking)
        {
            Vector2 input = Management.GameManager.Controls.Spectate.Look.ReadValue<Vector2>();
            input.y *= -1;
            Quaternion horizontalRot = Quaternion.AngleAxis(HorizontalRotationSpeed * Time.deltaTime * input.x, transform.up);
            transform.rotation = horizontalRot * transform.rotation;
            Quaternion verticalRot = Quaternion.AngleAxis(VerticalRotationSpeed * Time.deltaTime * input.y, transform.right);
            transform.rotation = verticalRot * transform.rotation;
        }
    }
}
