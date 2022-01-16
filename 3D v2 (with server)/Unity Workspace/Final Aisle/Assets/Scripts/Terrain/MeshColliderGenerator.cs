using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Auto-generates mesh colliders for static terrain objects
 */
public class MeshColliderGenerator : MonoBehaviour
{
    /*
     * Use on parent of terrain meshes! (terrain folder objects)
     */
    void Start()
    {
        var counter = 0;
        foreach (Transform child in transform)
        {
            if (child.gameObject.GetComponent<MeshCollider>() == null)
                child.gameObject.AddComponent(typeof(MeshCollider));

            counter++;
        }
        UnityEngine.Debug.Log("Generated " + counter + " terrain mesh colliders");
    }
}
