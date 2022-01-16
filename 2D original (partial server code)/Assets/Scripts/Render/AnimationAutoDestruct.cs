using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationAutoDestruct : MonoBehaviour
{
    public float Delay = 0.0F;

    void Start()
    {
        Destroy(gameObject, this.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + Delay);   
    }
}
