using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPartCollisions : MonoBehaviour
{
    private bool touchingGround = false;
    private bool touchingPole = false;

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "platform")
        {
            touchingGround = true;
        }
        if(collision.gameObject.tag == "pole")
        {
            touchingPole = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.tag == "platform")
        {
            touchingGround = false;
        }
        if (collision.gameObject.tag == "pole")
        {
            touchingPole = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "pole")
        {
            touchingPole = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "pole")
        {
            touchingPole = false;
        }
    }

    public bool getTouchingGround()
    {
        return touchingGround;
    }

    public bool getTouchingPole()
    {
        return touchingPole;
    }

    public void ResetPoleChecker()
    {  
        touchingPole = false;
    }

    public void ResetGroundChecker()
    {
        touchingGround = false;
    }
}
