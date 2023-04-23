using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ChoiceMaps : MonoBehaviour
{
    public Text MapsText;
    int index;

    private void Start()
    {
        MapsText.text = GameManager.gameManager.mapsList[0].name;
    }

    public void NextMap(int i)
    {
        index += i;

        if (index < 0) index = 0;
        else if (index > GameManager.gameManager.mapsList.Count - 1)
            index = GameManager.gameManager.mapsList.Count - 1;

        MapsText.text = GameManager.gameManager.mapsList[index].name;
    }

    public void SelectMap()
    {
        GameManager.gameManager.GetComponent<PhotonView>().RPC("WaitForMap", PhotonTargets.AllBufferedViaServer, index);
        transform.GetChild(4).gameObject.SetActive(true);
    }
}
