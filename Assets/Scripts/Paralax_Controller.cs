using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paralax_Controller : MonoBehaviour
{
    private Transform cam;
    private float distance;

    private Vector3 camstartPos;
    private GameObject[] backgrounds;
    private Material[] BG_Mat;
    private float[] Backspeed;

    private float FarthestBack;

    [Range(0.01f, 0.05f)]
    public float ParallaxSpeed; 
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main.transform;
        camstartPos = cam.position;
        
        int backCount = transform.childCount;
        BG_Mat = new Material[backCount];
        Backspeed = new float[backCount];
        backgrounds = new GameObject[backCount];

        for(int i = 0; i < backCount; i++)
        {
            backgrounds[i] = transform.GetChild(i).gameObject;
            BG_Mat[i] = backgrounds[i].GetComponent<Renderer>().material;
        }

        BackspeedCalculate(backCount);
    }


    private void BackspeedCalculate(int backCount) // for the farthest BG
    {
        for(int i = 0; i < backCount; i++)
        {
            if ((backgrounds[i].transform.position.z - cam.position.z) > FarthestBack)
            {
                FarthestBack = backgrounds[i].transform.position.z - cam.position.z;
            }
        }

        for(int i = 0; i < backCount; i++)
        {
            Backspeed[i] = 1 - (backgrounds[i].transform.position.z) / FarthestBack;
        }
    }

    private void FixedUpdate()
    {
        distance = cam.position.x - camstartPos.x;
        transform.position = new Vector3(cam.position.x, transform.position.y, 0);

        for(int i = 0; i< backgrounds.Length; i++)
        {
            float speed = Backspeed[i] * ParallaxSpeed;
            BG_Mat[i].SetTextureOffset("_MainTex", new Vector2(speed * Time.deltaTime, 0));
        }
    }
}
