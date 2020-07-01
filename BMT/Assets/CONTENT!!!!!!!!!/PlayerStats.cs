using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    public int hp = 100;
    public string nick = "Player";

    public List<Camera> spectatorsCameras = new List<Camera>();
    public GameObject myHead;
    int index, lastIndex;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.LeftArrow))
            ChangeCamera(-1);

        else if (Input.GetKeyDown(KeyCode.RightArrow))
            ChangeCamera(1);
    }

    public void ChangeCamera(int i)
    {
        if (spectatorsCameras.Count > 0)
        {
            Apply();

            index += i;

            if (index < 0) index = spectatorsCameras.Count - 1;
            else if (index > spectatorsCameras.Count - 1) index = 0;

            UpdateCameras(lastIndex, 0, false);
            lastIndex = index;


            UpdateCameras(index, 9, true);
        }
        else
            Debug.LogError("Spectators cameras not found");
    }

    public void FindCameras()
    {
        Apply();

        UpdateCameras(0, 9, true);
    }

    void Apply()
    {
        spectatorsCameras.Clear();

        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            if (Player.PlayerList[i] != Player.myPlayer)
            {
                if(Player.PlayerList[i].playerGO.GetComponent<Rigidbody>().isKinematic)
                    spectatorsCameras.Add(Player.PlayerList[i].playerGO.transform.GetChild(0).GetComponent<Camera>());
            }
                
        }
    }

    public void OffAllCameras()
    {
        if (spectatorsCameras.Count == 0)
            return;

        try
        {
            spectatorsCameras[lastIndex].enabled = false;
            spectatorsCameras[index].transform.GetChild(1).GetComponent<Camera>().enabled = false;
            spectatorsCameras[lastIndex].transform.root.GetComponent<PlayerStats>().myHead.layer = 0;

            spectatorsCameras.Clear();
        }
        catch
        {
            for (int i = 0; i < Player.PlayerList.Count; i++)
            {
                if (Player.PlayerList[i] != Player.myPlayer)
                {
                    Player.PlayerList[i].playerGO.transform.GetChild(0).GetComponent<Camera>().enabled = false;
                }
            }

            Player.myPlayer.playerGO.transform.GetChild(0).GetComponent<Camera>().enabled = true;
            spectatorsCameras.Clear();
        }

        
    }

    public void SetMyHead(int layer)
    {
        myHead = transform.GetChild(2).GetChild(0).GetChild(1).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(0).gameObject;
        myHead.layer = layer;
    }

    void UpdateCameras(int index, int layer, bool enable)
    {
       
        spectatorsCameras[index].enabled = enable;
        spectatorsCameras[index].transform.root.GetComponent<PlayerStats>().myHead.layer = layer;
        spectatorsCameras[index].transform.GetChild(1).GetComponent<Camera>().enabled = enable;

        if (spectatorsCameras[index].transform.GetChild(0).childCount > 0)
        {
            Transform camera = spectatorsCameras[index].transform.GetChild(0).GetChild(0);

            if(enable)
            {
                camera.localPosition = camera.GetComponent<WeaponStats>().postion;
                camera.localRotation = Quaternion.Euler(camera.GetComponent<WeaponStats>().rotation);
            }
            else
            {
                camera.localPosition = camera.GetComponent<WeaponStats>().otherPostion;
                camera.localRotation = Quaternion.Euler(camera.GetComponent<WeaponStats>().otherRotation);
            }
            
        }

        Player.myPlayer.playerGO.transform.GetChild(1).GetChild(5).GetComponent<Text>().text = spectatorsCameras[index].transform.root.GetComponent<PlayerStats>().nick; //UI

    }
}
