using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomBackground : MonoBehaviour
{
    public RawImage image;
    public Texture[] images;

    private void Awake()
    {
        image.texture = images[Random.Range(0, images.Length)];
    }

}
