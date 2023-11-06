using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemCollector : MonoBehaviour
{
    private int ItemCount = 0;

    [SerializeField] private TextMeshProUGUI CountText;
    [SerializeField] private AudioSource CollectSFX;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Collectable_item"))
        {
            collision.gameObject.SetActive(false);
            CollectSFX.Play();
            ItemCount++;
            CountText.text = "SCORE : " + ItemCount;
        }
    }
}
