using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyNickPosition : MonoBehaviour
{
    public static Transform targetPlayer;

    Camera playerCamera;

    private void Start()
    {
        playerCamera = transform.root.GetChild(0).GetComponent<Camera>();
    }

    void Update()
    {
        if(targetPlayer != null)
            transform.position = playerCamera.WorldToScreenPoint(targetPlayer.position);
    }
}
