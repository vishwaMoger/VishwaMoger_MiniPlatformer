using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelMenu : MonoBehaviour
{
    public Button[] buttons;
    public Image[] checkmark;

    private void Awake()
    {
        int unlockedLevel = PlayerPrefs.GetInt("unlockedLevel", 1);
        for(int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
            checkmark[i].enabled = false;
        }

        for(int i = 0; i < unlockedLevel; i++)
        {
            buttons[i].interactable = true;
            checkmark[i].enabled = true;
        }
    }
    public void OpenLevel(int levelID)
    {
        string LevelName = "MP_Level" + levelID;
        SceneManager.LoadScene(LevelName);

    }

}
