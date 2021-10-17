using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{

    public float speed, oringinSpeed;

  public  float speedX;
  public  float speedY;
  public Transform barA;
  public Transform barB;

    private Vector3 originPos;
    // Start is called before the first frame update
    void Start()
    {
        oringinSpeed = speed;
        originPos = this.transform.position;
        RestartPong();
    }
    bool RestLock = false;

    private void RestartPong()
    {

         transform.position=  originPos;
        speedX = 0;
        speedY = 0;

        if (!RestLock)
        StartCoroutine(DelayStart());
    }

    IEnumerator DelayStart() {
        RestLock = true;
        yield return new WaitForSeconds(3);

        speedX = oringinSpeed;
        speedY = oringinSpeed;

        
        RestLock = false;
    }





    // Update is called once per frame
    void Update()
    {
        transform.Translate(new Vector3(speedX, speedY, 0));

        
        if (transform.position.x >= 8f) {
            RestartPong();
        }
        if (transform.position.x <= -8f)
        {
            RestartPong();
        }
        




        if (transform.position.x > 9.0f) {
            speedX = speedX * -1;
        }
        if (transform.position.x < -9.0f)
        {
            speedX = speedX * -1;
        }

        if (transform.position.y > 5f)
        {
            speedY = speedY * -1;
        }
        if (transform.position.y < -5f)
        {
            speedY = speedY * -1;
        }
        if ( transform.position.x+0.5F>= barA.position.x) {

            if (transform.position.y > barA.transform.position.y - barA.localScale.y / 2 && transform.position.y < barA.transform.position.y + barA.localScale.y / 2) {

                speedX += 0.003f;

                speedX = speedX * -1;

                
            }
            

        }
        if (transform.position.x  <= barB.position.x)
        {
            if (transform.position.y > barB.transform.position.y - barB.localScale.y / 2 && transform.position.y < barB.transform.position.y + barB.localScale.y / 2)
            {
                speedX += 0.003f;
                speedX = speedX * -1;
            }
        }



    }
}
