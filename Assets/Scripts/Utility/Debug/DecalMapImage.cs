using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityAsync;

public class DecalMapImage : MonoBehaviour
{
    RawImage image;
    public Entity Target;
    void Start()
    {
        image = GetComponent<RawImage>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Target != null && Target.AdditionalTags.HasFlag(Entity.TagTypes.RecieveDecals))
        {
            image.texture = Target.DecalMap;
        }
        else if (Target != null)
        {
            Debug.LogError($"Target {Target.name} does not have RecieveDecals flag");
        }
    }
}
