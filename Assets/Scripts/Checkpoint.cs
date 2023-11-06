using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private PlayerLife player_life;
    public Transform CP_Pos;
    void Awake()
    {
        player_life = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerLife>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            player_life.UpdateCheckPoint(CP_Pos.position);
        }
    }
}
