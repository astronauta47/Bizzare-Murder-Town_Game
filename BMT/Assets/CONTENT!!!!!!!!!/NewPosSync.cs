using UnityEngine;

public class NewPosSync : Photon.MonoBehaviour
{
    public Vector3 position;
    Quaternion rotation;

    public void StartPos()
    {
        position = Vector3.zero;
        rotation = Quaternion.Euler(0, 0, 0);
    }

    private void Update()
    {
        if(!photonView.isMine)
        {
            transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * 8f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 8f);
        }
        
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

        if (photonView.isMine)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            position = (Vector3)stream.ReceiveNext();
            rotation = (Quaternion)stream.ReceiveNext();
        }


    }


}
