using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public static Player myPlayer;
    public GameObject playerGO;
    public string nick = "";
    public Team myTeam;
    public PhotonPlayer pp;

    public static List<Player> PlayerList = new List<Player>();

    public static void WriteAllPlayers()
    {
        for (int i = 0; i < PlayerList.Count; i++)
        {
            Debug.Log(PlayerList[i].nick + " " + PlayerList[i].myTeam);
        }
    }

    public static Player FindPlayer(PhotonPlayer pp)
    {
        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].pp == pp)
                return PlayerList[i];
        }

        return null;
    }


}

public enum Team
{
    Niewinny, Zdrajca, Policjant
}


