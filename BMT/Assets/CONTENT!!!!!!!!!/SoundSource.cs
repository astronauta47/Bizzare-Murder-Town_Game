using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundSource : MonoBehaviour
{
    [SerializeField] int soundIndex;
    private float sleepTime;
    public bool canPlay = true;//true - debug = false

    private void Start()
    {
        sleepTime = GameManager.gameManager.audioClips[soundIndex].length;
    }

    private void OnMouseOver()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            Play(ref canPlay, soundIndex, sleepTime);
        }
    }

    public void Play(ref bool canPlay, int soundIndex, float songLength)
    {
        if (canPlay)
        {
            Player.myPlayer.playerGO.GetComponent<PhotonView>().RPC("PlaySound", PhotonTargets.All, soundIndex);
            StartCoroutine(WaitForSound(songLength));
        }
    }

    IEnumerator WaitForSound(float sleepTime)
    {
        canPlay = false;

        yield return new WaitForSeconds(sleepTime);

        canPlay = true;
    }
}
