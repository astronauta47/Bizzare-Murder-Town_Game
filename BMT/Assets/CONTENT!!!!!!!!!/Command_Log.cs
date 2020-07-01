using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Command_Log : MonoBehaviour
{
    string command;
    InputField commandText;
    Text compliteCommandText;
    GameObject console;
    List<string> command_Parts = new List<string>();
    List<char> commandChars = new List<char>();

    PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        console = transform.GetChild(1).GetChild(7).gameObject;
        commandText = console.transform.GetChild(1).GetComponent<InputField>();
        compliteCommandText = commandText.transform.GetChild(2).GetComponent<Text>();
    }

    private void OnEnable()
    {
        Cursor.visible = false;
        GetComponent<PlayerMovement>().canMove = true;
        Cursor.lockState = CursorLockMode.Locked;
        console.SetActive(false);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.BackQuote))
        {
            console.SetActive(!console.activeInHierarchy);
            Cursor.visible = console.activeInHierarchy;
            GetComponent<PlayerMovement>().canMove = !console.activeInHierarchy;
            if (console.activeInHierarchy) Cursor.lockState = CursorLockMode.None;
            else Cursor.lockState = CursorLockMode.Locked;

            commandText.ActivateInputField();
            compliteCommandText.text = "";
        }

        if(Input.GetKeyDown(KeyCode.Return) && console.activeInHierarchy)
        {
            command = commandText.text;
            commandText.text = "";

            if (command.Length <= 1)
                return;
            
            for (int i = 0; i < command.Length; i++)
            {
                if(command[i] != ' ')
                {
                    commandChars.Add(command[i]);
                }
                else
                {
                    command_Parts.Add(new string(commandChars.ToArray()));
                    commandChars.Clear();
                }
                
            }

            if(commandChars.Count > 0)
            {
                command_Parts.Add(new string(commandChars.ToArray()));
                commandChars.Clear();
            }

            CheckCommands();
        }
    }

    void CheckCommands()
    {

        try
        {
            if (PhotonNetwork.isMasterClient)
            switch (command_Parts[0])
            {

                case "Kill":
                    KillPlayer(command_Parts[1]);
                    break;

                case "SpawnObject":
                    SpawnObject(command_Parts[1]);
                    break;

                case "SetModel":
                    ChangeModel(command_Parts[1]);
                    break;

                case "Time":
                    photonView.RPC("ChangeTime", PhotonTargets.AllBuffered, int.Parse(command_Parts[1]));
                    break;

                case "RoundTime":
                    photonView.RPC("ChangeRoundTime", PhotonTargets.AllBuffered, int.Parse(command_Parts[1]));
                    break;

                case "BeforeTime":
                    photonView.RPC("ChangeBeforeTime", PhotonTargets.AllBuffered, int.Parse(command_Parts[1]));
                    break;

                case "Restart":
                    GameManager.gameManager.RandomTeams();
                    break;

                case "SetName":
                    ChangeName(command_Parts[1], command_Parts[2]);
                    break;

                case "GiveGun":
                    GiveGun(command_Parts[1]);
                    break;

                case "FindBoxes":
                    ChangeFindBoxesCount(command_Parts[1]);
                    break;

                    default:
                        compliteCommandText.text = "Unknown command";
                        break;
            }

            switch (command_Parts[0])
            {
                case "Mute":
                    MuteMusic(true);
                    break;

                case "UnMute":
                    MuteMusic(false);
                    break;

                default:
                    if(!PhotonNetwork.isMasterClient)
                        compliteCommandText.text = "Unknown command";
                    break;
            }


            ClearConsole();

        }
        catch
        {
            ClearConsole();
        }
    }

    void ClearConsole()
    {
        command_Parts.Clear();
        commandChars.Clear();
        command = "";
        commandText.ActivateInputField();
    }

    void ChangeFindBoxesCount(string countString)
    {
        try
        {
            int count = int.Parse(countString);

            if (count >= 0 && count < 9)
            {
                GameManager.gameManager.findBoxesCount = count;
                compliteCommandText.text = "Find boxes count changed";
            }
               
            else
                compliteCommandText.text = "Count is out off range";
        }
        catch
        {
            compliteCommandText.text = "It's not a number";
        }
        
    }

    void KillPlayer(string playerName)
    {

        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            if(Player.PlayerList[i].playerGO.GetComponent<PlayerStats>().nick == playerName + " ")
            {
                GameManager.gameManager.GetComponent<PhotonView>().RPC("PlayerHpChange", PhotonTargets.All, 100, Player.PlayerList[i].playerGO.GetComponent<PhotonView>().viewID, 0, false, transform.forward);
                compliteCommandText.text = "Player killed!";
                break;
            }

            compliteCommandText.text = "Player not found";
        }
    }

    void MuteMusic(bool isMute)
    {
        GameManager.isMusicMute = isMute;

        if(isMute)
            compliteCommandText.text = "Mute";
        else
            compliteCommandText.text = "UnMute";
    }

    void SpawnObject(string objectName)
    {
        try
        {
            GameObject I = PhotonNetwork.Instantiate(objectName, transform.forward + transform.position, Quaternion.identity, 0);
            GameManager.gameManager.GetComponent<PhotonView>().RPC("AddObjectToDestroyList", PhotonTargets.AllBufferedViaServer, I.GetComponent<PhotonView>().viewID);
            compliteCommandText.text = objectName + " Spawned!";
        }
        catch
        {
            compliteCommandText.text = objectName + " does not exist";
        }
        
    }

    void GiveGun(string playerName)
    {
        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            if (Player.PlayerList[i].playerGO.GetComponent<PlayerStats>().nick == playerName + " ")
            {
                GameManager.gameManager.GetComponent<PhotonView>().RPC("GiveWeaponOtherPlayer", PhotonTargets.All, i, "Gun", Shooting.WeaponType.pistol);
                compliteCommandText.text = "The weapon was given";
                break;
            }

            compliteCommandText.text = "Player not found";
        }
            
    }

    void ChangeName(string playerName, string newName)
    {
        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            if (Player.PlayerList[i].playerGO.GetComponent<PlayerStats>().nick == playerName + " ")
            {
                photonView.RPC("ChangeNameRpc", PhotonTargets.AllBuffered, i, newName);
                compliteCommandText.text = "Name changed";
                break;
            }
            
            compliteCommandText.text = "Player not found";
        }
    }

    [PunRPC]
    void ChangeNameRpc(int playerId, string nick)
    {
        Player.PlayerList[playerId].playerGO.GetComponent<PlayerStats>().nick = nick + " ";
        Player.PlayerList[playerId].playerGO.transform.GetChild(1).GetChild(5).GetComponent<Text>().text = nick;
    }

    void ChangeModel(string playerName)
    {
        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            if (Player.PlayerList[i].playerGO.GetComponent<PlayerStats>().nick == playerName + " ")
            {
                try
                {
                    int part = int.Parse(command_Parts[2]);

                    if (part > 0 && part < GameManager.gameManager.playerModelsGO.Length)
                    {
                        photonView.RPC("RpcChangeModel", PhotonTargets.All, i, command_Parts[2]);
                        compliteCommandText.text = "Model Changed";
                    }
                    else
                        compliteCommandText.text = "Index is out of range";

                }
                catch
                {
                    compliteCommandText.text = "Index is out of range";
                }
                
                break;
            }

            compliteCommandText.text = "Player not found";
        }
    }

    [PunRPC]
    void RpcChangeModel(int playerId, string modelName)
    {
        try
        {
            Instantiate(GameManager.gameManager.playerModelsGO[int.Parse(modelName)], Player.PlayerList[playerId].playerGO.transform.position + new Vector3(0, -0.86f, 0), transform.rotation, Player.PlayerList[playerId].playerGO.transform);
            Destroy(Player.PlayerList[playerId].playerGO.transform.GetChild(2).gameObject);

            if(Player.PlayerList[playerId] == Player.myPlayer)
            {
                StartCoroutine(Wait());
            }
        }
        catch
        {
            compliteCommandText.text = "ModelId does not exist";
        }
        
    }

    IEnumerator Wait()
    {
        yield return new WaitForEndOfFrame();
        Player.myPlayer.playerGO.GetComponent<PlayerStats>().SetMyHead(9);
    }


    [PunRPC]
    void ChangeTime(int time)
    { 
        if(time >= 0 && time < 10000)
        {
            GameManager.gameTimer = time;
            compliteCommandText.text = "Time Changed!";
        }
        else
            compliteCommandText.text = "Time is out of reach";

    }

    [PunRPC]
    void ChangeBeforeTime(int time)
    {
        if (time >= 0 && time < 10000)
        { 
            GameManager.timeBeforRound = time;
            compliteCommandText.text = "Before Round Time Changed!";
        }
        else
            compliteCommandText.text = "Time is out of reach";

    }

    [PunRPC]
    void ChangeRoundTime(int time)
    {
        if (time >= 0 && time < 10000)
        {
            GameManager.roundTime = time;
            compliteCommandText.text = "Round Time Changed!";
        }
        else
            compliteCommandText.text = "Time is out of reach";

    }
}
