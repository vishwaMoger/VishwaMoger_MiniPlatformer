using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerLife : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator deathAnim;

    [SerializeField] private AudioSource DeathSFX;
    [SerializeField] private AudioSource BGM;

    private Vector2 CheckPoint;

    public Image[] lives;
    public int livesRemaining;

    public GameObject GameOverPanel;
    public GameObject GamePanel;

    // Start is called before the first frame update
    void Start()
    {
        deathAnim.ResetTrigger("Death");
        CheckPoint = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Traps"))
        {
            livesRemaining--;
            PlayerDies();
            LoseLife();
        }
    }

    public void LoseLife()
    {
        ////If no lives remaining do nothing
        //if (livesRemaining == 0)
        //    return;
        ////Decrease the value of livesRemaining
        
        ////Hide one of the life images
        lives[livesRemaining].enabled = false;

        //If we run out of lives we lose the game
        if (livesRemaining == 0)
        {
            Time.timeScale = 0;
            GamePanel.SetActive(false);
            GameOverPanel.SetActive(true);
        }
    }

    private void PlayerDies()
    {
        rb.bodyType = RigidbodyType2D.Static;
        deathAnim.SetTrigger("Death");
        DeathSFX.Play();
        BGM.Stop();
        die();
    }

    public void UpdateCheckPoint(Vector2 pos)
    {
        CheckPoint = pos;
    }

    private void die()
    {
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        
        yield return new WaitForSeconds(0.5f);
        deathAnim.ResetTrigger("Death");
        transform.position = CheckPoint;
        rb.bodyType = RigidbodyType2D.Dynamic;
        BGM.Play();

    }

    //    private void RestartLevel()
    //    {
    //        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    //    }
}
