using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [PunRPC]
    public void PlaySound(int index)
    {
        GetComponent<AudioSource>().PlayOneShot(GameManager.gameManager.audioClips[index]);
    }
}
