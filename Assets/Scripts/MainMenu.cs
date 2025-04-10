/**
// File Name : MainMenu.cs
// Author : Jack P. Fisher
// Creation Date : March 23, 2025
//
// Brief Description : This Script allows the buttons on the main menu level select screen to load different levels.
**/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
   
        public void PlayGame()
        {
            SceneManager.LoadScene(1);
        }

    public void Level2()
    {
        SceneManager.LoadScene(2);
    }
    public void Level3()
    {
        SceneManager.LoadScene(3);
    }

}
