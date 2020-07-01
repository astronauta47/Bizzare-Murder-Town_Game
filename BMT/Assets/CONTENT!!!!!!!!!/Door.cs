using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [HideInInspector]public float startRotationX;
    [HideInInspector] public bool isAnimated = true;
    public float value1 = 270, value2 = 90;
    public float direction = 90;
    public bool isTurnedDoor;
    [HideInInspector] public int id;

    private void Start()
    {
        if (isTurnedDoor)
            startRotationX = Mathf.Abs(direction);
    }

    public void Open(Vector3 dir)
    {
        if(isTurnedDoor)
        {
            if (transform.position.z - dir.z > 0 || startRotationX == value2)
            {
                if (startRotationX != value1 && isAnimated)
                    GameManager.gameManager.GetComponent<PhotonView>().RPC("OpenDoorRpc", PhotonTargets.AllBuffered, id, -direction);
            }
            if (transform.position.z - dir.z < 0 || startRotationX == value1)
            {
                if (startRotationX != value2 && isAnimated)
                    GameManager.gameManager.GetComponent<PhotonView>().RPC("OpenDoorRpc", PhotonTargets.AllBuffered, id, direction);
            }
        }
        else
        {
            if (transform.position.x - dir.x > 0 || startRotationX == value2)
            {
                if (startRotationX != value1 && isAnimated)
                    GameManager.gameManager.GetComponent<PhotonView>().RPC("OpenDoorRpc", PhotonTargets.AllBuffered, id, -direction);
            }
            if (transform.position.x - dir.x < 0 || startRotationX == value1)
            {
                if (startRotationX != value2 && isAnimated)
                    GameManager.gameManager.GetComponent<PhotonView>().RPC("OpenDoorRpc", PhotonTargets.AllBuffered, id, direction);
            }
        }
        
    }

    
}
