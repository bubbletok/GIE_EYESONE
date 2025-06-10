using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum RotateAxis
{
    X,
    Y,
    Z
}
public class RotateObject : MonoBehaviour
{
    public RotateAxis rotateAxis = RotateAxis.Z; // Default rotation axis
    // Update is called once per frame
    void Update()
    {
        // Rotate the object around its Y-axis at a constant speed
        switch (rotateAxis)
        {
            case RotateAxis.X:
                transform.Rotate(Vector3.right, 20 * Time.deltaTime, Space.World);
                break;
            case RotateAxis.Y:
                transform.Rotate(Vector3.up, 20 * Time.deltaTime, Space.World);
                break;
            case RotateAxis.Z:
                transform.Rotate(Vector3.forward, 20 * Time.deltaTime, Space.World);
                break;
        }
    }
}
