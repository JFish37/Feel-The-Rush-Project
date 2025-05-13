/**
// File Name : PlayerController.cs
// Author : Jack P. Fisher
// Creation Date : May 10, 2025
//
// Brief Description : These enemies fire a bit faster, but don't need to be defeated to progress to the next room.
**/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }
}
