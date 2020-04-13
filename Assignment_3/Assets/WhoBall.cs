using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhoBall : MonoBehaviour
{

    public string WhoTag; 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(collision.gameObject.name);
        if(collision.gameObject.tag == "Red")
        {
            Debug.Log("Red hit!");
            WhoTag="Red";

        }else if(collision.gameObject.tag == "Blue"){
            Debug.Log("Blue hit!");
            WhoTag="Blue";
        }
        
    }
}
