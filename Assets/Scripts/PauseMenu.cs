using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    public GameObject GamePanel;
    public void Pause()
    {
        pauseMenu.SetActive(true);
        GamePanel.SetActive(false);
        Time.timeScale = 0;
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
    }   
    
    public void Home()
    {
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;
    }
}
