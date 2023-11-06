using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Finish : MonoBehaviour
{
    private AudioSource FinishSFX;
    [SerializeField] private Animator Transition_anim;
    [SerializeField] private GameObject PauseBtn;

    private bool levelCompleted = false;

    private void Awake()
    {
        
    }

    private void Start()
    {
        FinishSFX = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Player" && !levelCompleted)
        {
            FinishSFX.Play();
            PauseBtn.SetActive(false);
            levelCompleted = true;
            Invoke("CompleteLevel", 2f);
            levelCompleted = false;

        }
    }

    private void CompleteLevel()
    {
        StartCoroutine(LoadLevel());
        UnlockNewlevel();
    }

    IEnumerator LoadLevel()
    {
        
        Transition_anim.SetTrigger("End");
        yield return new WaitForSeconds(1);
        if (SceneManager.GetActiveScene().buildIndex <= 1)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
       
        Transition_anim.SetTrigger("start");
        PauseBtn.SetActive(true);
    }

    public void UnlockNewlevel()
    {
        if (SceneManager.GetActiveScene().buildIndex >= PlayerPrefs.GetInt("ReachedIndex"))
        {
            PlayerPrefs.SetInt("ReachedIndex", SceneManager.GetActiveScene().buildIndex + 1);
            PlayerPrefs.SetInt("unlockedLevel", PlayerPrefs.GetInt("unlockedLevel", 1) + 1);
            PlayerPrefs.Save();

        }
    }

}
