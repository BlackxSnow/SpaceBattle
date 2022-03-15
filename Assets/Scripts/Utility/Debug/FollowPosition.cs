using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPosition : MonoBehaviour
{
    public Transform Target;
    private Vector3 Offset;

    private void Start()
    {
        Offset = transform.position - Target.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = Target.transform.position + Offset;
    }
}
