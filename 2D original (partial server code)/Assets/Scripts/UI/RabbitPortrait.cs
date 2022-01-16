using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RabbitPortrait : MonoBehaviour
{
    public GameObject RabbitPlayer; // the actual player character whose portrait is being represented
    private Animator animator;

    void Start()
    {
        // Update colours of portrait
        gameObject.transform.Find("Back of Ears").GetComponent<SpriteRenderer>().color = RabbitPlayer.transform.Find("Back of Ear Colour").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Colour 1").GetComponent<SpriteRenderer>().color = RabbitPlayer.transform.Find("Body Colour 1").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Colour 2").GetComponent<SpriteRenderer>().color = RabbitPlayer.transform.Find("Body Colour 2").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Eyes").GetComponent<SpriteRenderer>().color = RabbitPlayer.transform.Find("Eyes").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Eyes Shiny").GetComponent<SpriteRenderer>().color = RabbitPlayer.transform.Find("Eyes Shiny").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Inner Ear").GetComponent<SpriteRenderer>().color = RabbitPlayer.transform.Find("Inner Ear Colour").GetComponent<SpriteRenderer>().color;

        animator = GetComponent<Animator>();
    }

    public void Blink()
    {
        animator.SetTrigger("Blink");
    }
}
