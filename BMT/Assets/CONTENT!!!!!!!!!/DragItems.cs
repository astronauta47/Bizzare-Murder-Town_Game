using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragItems : MonoBehaviour
{
    public float distance = 3f;//3
    public float speed = 5f;//5
    public bool isDrag;
    public bool isCorpse;
    public bool isEnable = true;
    Vector3 objPosition;
    Vector3 lastPosition;
    bool isStay;
    bool b;

    private void OnTriggerEnter(Collider other)
    {
        if(!other.isTrigger) 
            isStay = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger)
            isStay = false;
    }

    private void OnMouseDown()
    {
        if (!isEnable) return;

        isDrag = true;
        if (!PlayerMovement.isCrouch) Player.myPlayer.playerGO.GetComponent<PlayerMovement>().speed = 4;
        GetComponent<PhotonView>().RPC("Kinamatic", PhotonTargets.All, true);
    }

    private void OnMouseDrag()
    {
        if (!isEnable) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            isDrag = false;
            Vector3 dir = Player.myPlayer.playerGO.transform.GetChild(0).forward;
            GetComponent<PhotonView>().RPC("Kinamatic", PhotonTargets.All, false);
            GetComponent<PhotonView>().RPC("AddForce", PhotonTargets.All, dir);
        }
        if (isDrag && Vector3.Distance(Player.myPlayer.playerGO.transform.position, transform.position) < 4)
        {

            if (isStay)
            {
                transform.position = lastPosition;
                return;
            }

            if(!b)StartCoroutine(SetLastPosition());
            Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance);
            objPosition = Player.myPlayer.playerGO.transform.GetChild(0).GetComponent<Camera>().ScreenToWorldPoint(mousePosition);
            if(isCorpse) objPosition.y = transform.position.y;

            GetComponent<PhotonView>().RPC("ChangePosition", PhotonTargets.All, objPosition);
        }
    }

    IEnumerator SetLastPosition()
    {
        b = true;

        lastPosition = transform.position;

        yield return new WaitForSeconds(0.01f);

        b = false;
    }

    [PunRPC]
    void AddForce(Vector3 direction)
    {
        GetComponent<Rigidbody>().AddForce(direction * 500);
    }

    [PunRPC]
    void Kinamatic(bool isKinematci)
    {
        GetComponent<Rigidbody>().isKinematic = isKinematci;
    }

    [PunRPC]
    void ChangePosition(Vector3 objPosition)
    {
        transform.position = Vector3.Lerp(transform.position, objPosition, Time.deltaTime * speed);

        GetComponent<Rigidbody>().velocity = Vector3.zero;
    }


    private void OnMouseUp()
    {
        if (!isEnable) return;

        if(!PlayerMovement.isCrouch)Player.myPlayer.playerGO.GetComponent<PlayerMovement>().speed = 6;
        isDrag = false;
        GetComponent<PhotonView>().RPC("Kinamatic", PhotonTargets.All, false);
    }

}
