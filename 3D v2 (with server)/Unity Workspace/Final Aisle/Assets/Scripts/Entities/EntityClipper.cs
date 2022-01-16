using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class added to all entity game objects, used to determine if the entity is too close to the camera, and should thus be hidden
 */
public class EntityClipper : MonoBehaviour
{
    public static List<EntityClipper> EntityClippers = new List<EntityClipper>(); // gives all entity clippers a static access point

    [HideInInspector]
    public bool IsTransparent = false; // whether the skin meshes are being rendered

    [HideInInspector]
    public List<SkinnedMeshRenderer> Renderers = new List<SkinnedMeshRenderer>();

    public float CameraClipRadius = 2.5F; // distance closer than which the entity should be invisible

    [HideInInspector]
    public float HalfHeight; // used to offset the effective centre used to find transparency status

    void Start()
    {
        FindRenderers(this.gameObject);
        HalfHeight = this.gameObject.GetComponent<CapsuleCollider>().height / 2;

        EntityClippers.Add(this);
    }

    void OnDestroy()
    {
        EntityClippers.Remove(this);
    }

    /**
     * Recursively locate all skinned mesh renderers belonging to the entity
     */
    private void FindRenderers(GameObject obj)
    {
        if (obj.GetComponent<SkinnedMeshRenderer>() != null) Renderers.Add(obj.GetComponent<SkinnedMeshRenderer>());
        for(int i = 0; i < obj.transform.childCount; i++)
            FindRenderers(obj.transform.GetChild(i).gameObject);
    }
}
