using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectManager : Photon.MonoBehaviour
{
    public Text playerNameText;


    PhotonView pV;
    GameManager gm;
    private void Start()
    {
        gm = GetComponent<GameManager>();
        pV = GetComponent<PhotonView>();
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
       if(Input.GetKeyDown(KeyCode.L)) Player.WriteAllPlayers();
    }
    public void Connect()
    {

        PhotonNetwork.sendRate = 25;
        PhotonNetwork.sendRateOnSerialize = 25;
        PhotonNetwork.playerName = playerNameText.text;
        PhotonNetwork.ConnectUsingSettings("0.03");
    }

    //private void OnGUI()
    //{
    //    GUI.Label(new Rect(0, 0, 200, 20), PhotonNetwork.connectionStateDetailed.ToString());
    //}

    void OnJoinedLobby()
    {
        PhotonNetwork.JoinRandomRoom();
        SceneManager.LoadScene(1);
        gm.enabled = true;
    }

    void OnPhotonRandomJoinFailed()
    {
        PhotonNetwork.CreateRoom(null);
    }

    void OnPhotonPlayerConnected(PhotonPlayer pp)
    {
        if (PhotonNetwork.isMasterClient)
        {
           pV.RPC("PlayerJoin", PhotonTargets.AllBuffered, pp, 0);
        }
    }

    void OnPlayerPhotonDisconnected(PhotonPlayer pp)
    {
        pV.RPC("PlayerLeft", PhotonTargets.AllBuffered, pp);
    }

    [PunRPC]
    void PlayerJoin(PhotonPlayer pp, int team)
    {
        Player player = new Player();
        player.nick = pp.NickName;
        player.pp = pp;
        player.myTeam = (Team)team;
        Player.PlayerList.Add(player);
        
        if (pp == PhotonNetwork.player)
        {
            Player.myPlayer = player;
            gm.LoadMap(); 
        }
            
    }

    [PunRPC]
    void PlayerLeft(PhotonPlayer pp)
    {
        Player tmpPlayer = Player.FindPlayer(pp);

        if (tmpPlayer != null)
            Player.PlayerList.Remove(tmpPlayer);
    }

    void OnCreatedRoom()
    {
        pV.RPC("PlayerJoin", PhotonTargets.AllBuffered, PhotonNetwork.player, 0);
    }
}
