using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarFollow : MonoBehaviour
{
    //public Vector3 PositionTop, PositionBottom;
    public Transform followObject;
    
    public float ByTop, ByDown;

    public float oXRight, oXleft;

    public float scale = 1.2f;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float barY= map(followObject.position.x, ByTop, ByDown, oXleft,oXRight);

       float result = Mathf.Lerp(ByTop, ByDown, Mathf.InverseLerp(oXleft, oXRight, followObject.position.x));




        this.transform.position = new Vector3(this.transform.position.x, result, this.transform.position.z);


    }


   float map(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
