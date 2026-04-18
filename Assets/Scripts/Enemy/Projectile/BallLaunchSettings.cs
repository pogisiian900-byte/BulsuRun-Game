using UnityEngine;

public class BallLaunchSettings : MonoBehaviour
{
    [Header("Override launch (if true)")]
    public bool useCustomLaunch = true;

    [Header("Arc Forces")]
    public float throwForce = 12f;
    public float upForce = 5f;

    [Header("Extra")]
    public float randomForceOffset = 2f;

    [Header("Roll Mode (bowling)")]
    public bool rollOnly = false;     // if true, no upForce
    public float rollForce = 12f;     // used when rollOnly is true
}