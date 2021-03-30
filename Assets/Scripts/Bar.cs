using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bar : MonoBehaviour
{
    public float speed;
    public bool isA;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {

        if (isA)
        {

            if (Input.GetKey(KeyCode.O))
            {
                transform.Translate(new Vector3(0, speed, 0));

            }

            if (Input.GetKey(KeyCode.L))
            {
                transform.Translate(new Vector3(0, -speed, 0));
            }
        }
        else {

            if (Input.GetKey(KeyCode.W))
            {
                transform.Translate(new Vector3(0, speed, 0));

            }

            if (Input.GetKey(KeyCode.S))
            {
                transform.Translate(new Vector3(0, -speed, 0));
            }
        }

    }
}
