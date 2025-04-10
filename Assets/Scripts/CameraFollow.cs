/**
// File Name : CameraFollow.cs
// Author : Jack P. Fisher
// Creation Date : March 23, 2025
//
// Brief Description : This script makes the camera follow the player. 
**/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] Vector3 offset;

    //this function will allow the camera to follow the player
    void LateUpdate()
    {
        transform.position = player.transform.position + offset;
    }

}
