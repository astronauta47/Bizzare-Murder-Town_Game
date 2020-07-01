using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager gameManager;
    public AudioClip[] audioClips;

    public GameObject[] playerModelsGO;
    public GameObject[] findBoxesGO;

    Transform spawnPointsParent;
    Transform findBoxesSpawnPointsParent;
    [HideInInspector] public List<Vector3> spawnPointsPositions = new List<Vector3>();
    [HideInInspector] public List<Vector3> findBoxesSpawnPointsPositions = new List<Vector3>();
    [HideInInspector] public int[] findBoxReservedPositions;

    public int alivedPlayers;
    public int findBoxesCount = 3;
    bool isEndGame;
    public static bool startGame;

    public string[] nickList;
    public string[] secondNickList;

    Text timerText;
    public static float gameTimer = 1000;
    public static float roundTime = 150;
    public static float timeBeforRound = 20;

    public static List<GameObject> objectsToDestroyInLoad = new List<GameObject>();
    List<ObjectInGame> objectsInGame = new List<ObjectInGame>();

    public List<GameObject> mapsList = new List<GameObject>();
    public GameObject mapCanvas, waitingCanvas, backgroundCanvas;

    int tmpPlayerCount;

    public static bool isMusicMute;
    public static int foundBoxesCount;

    Transform doorParent;
    List<Door> doorsList = new List<Door>();

    struct ObjectInGame
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;

        public ObjectInGame(string name, Vector3 position, Quaternion rotation)
        {
            this.name = name;
            this.position = position;
            this.rotation = rotation;
        }
    }


    private void Start()
    {
        gameManager = this;
    }

    private void Update()
    {
        if (!isEndGame) gameTimer -= Time.deltaTime;

        if (timerText != null) timerText.text = Mathf.Round(gameTimer).ToString() + 's';

        if (gameTimer <= 0 && !startGame)
        {
            startGame = true;
            gameTimer = roundTime;

            GiveGun();
        }
        else if (gameTimer <= 0)
            ShowWin("Koniec czasu, wygrywają niewinni!", Team.Niewinny);
    }

    public void SpawnPlayer(int team)
    {
        spawnPointsParent = GameObject.FindGameObjectWithTag("SpawnPoints").transform;
        findBoxesSpawnPointsParent = GameObject.FindGameObjectWithTag("FindBoxesSpawnPoints").transform;
        doorParent = GameObject.FindGameObjectWithTag("DoorsParent").transform;

        for (int i = 0; i < spawnPointsParent.childCount; i++)
        {
            spawnPointsPositions.Add(spawnPointsParent.GetChild(i).position);
        }

        for (int i = 0; i < findBoxesSpawnPointsParent.childCount; i++)
        {
            findBoxesSpawnPointsPositions.Add(findBoxesSpawnPointsParent.GetChild(i).position);
        }

        CreateDoors();

        GameObject player = PhotonNetwork.Instantiate("Player", spawnPointsPositions[0], Quaternion.identity, 0);

        player.GetComponent<PlayerMovement>().enabled = true;
        player.transform.GetChild(0).GetComponent<Camera>().enabled = true;
        player.transform.GetChild(0).GetChild(1).GetComponent<Camera>().enabled = true;
        player.transform.GetChild(0).GetComponent<AudioListener>().enabled = true;
        player.GetComponent<PlayerStats>().myHead.layer = 9;
        player.transform.GetChild(1).gameObject.SetActive(true);

        Transform objectsParent = GameObject.FindGameObjectWithTag("MoveObjects").transform;

        for (int i = 0; i < objectsParent.childCount; i++)
        {
            objectsInGame.Add(new ObjectInGame(objectsParent.GetChild(i).name, objectsParent.GetChild(i).position, objectsParent.GetChild(i).rotation));
            Destroy(objectsParent.GetChild(i).gameObject);
        }

        if (PhotonNetwork.isMasterClient)
        {
            CreateObjectsInGame();
        }

        GetComponent<PhotonView>().RPC("SpawnPlayerRPC", PhotonTargets.AllBufferedViaServer, Player.myPlayer.nick, player.GetComponent<PhotonView>().viewID, team);
    }

    [PunRPC]
    void SpawnPlayerRPC(string nick, int id, int team, PhotonMessageInfo photonMessageInfo)
    {
        GameObject newPlayer = PhotonView.Find(id).gameObject;
        newPlayer.name = "Player_" + nick + team;
        Player player = Player.FindPlayer(photonMessageInfo.sender);
        player.playerGO = newPlayer;

        timerText = Player.myPlayer.playerGO.transform.GetChild(1).GetChild(6).GetComponent<Text>();

        
        if(!PhotonNetwork.isMasterClient)
            timerText.enabled = false;
    }

    [PunRPC]
    void PlayerHpChange(int hp, int id, int myID, bool isAlive, Vector3 shotDirection)
    {
        if (!startGame)
            return;

        Player.myPlayer.playerGO.GetComponent<PlayerStats>().ChangeCamera(1);

        GameObject hitPlayer = PhotonView.Find(id).gameObject;

        StartCoroutine(DamageUIEnabled(hitPlayer.transform.GetChild(1).GetChild(3).gameObject));

        if (isAlive)
        {
            hitPlayer.GetComponent<PlayerStats>().hp -= hp;
        }
        else
        {
            PlayerComponentEnabled(hitPlayer, false);

            hitPlayer.GetComponent<Rigidbody>().AddForce(shotDirection * 100);

            for (int i = 0; i < Player.PlayerList.Count; i++)
            {
                if (Player.PlayerList[i].playerGO.Equals(hitPlayer))
                {
                    alivedPlayers--;

                    if (Player.PlayerList[i].myTeam == Team.Zdrajca)
                        ShowWin("Wygrywają Niewinni", Team.Niewinny);

                    else if (alivedPlayers <= 1)
                        ShowWin("Wygrywa Zdrajca", Team.Zdrajca);

                    else if (Player.PlayerList[i].playerGO.transform.GetChild(0).GetChild(0).childCount > 0 && Player.PlayerList[i].myTeam != Team.Zdrajca && PhotonNetwork.isMasterClient)
                    {
                        GameObject g = PhotonNetwork.Instantiate("DropGun", hitPlayer.transform.position + new Vector3(0, 1f, 0), Quaternion.identity, 0);
                        GetComponent<PhotonView>().RPC("AddObjectToDestroyList", PhotonTargets.AllBuffered, g.GetComponent<PhotonView>().viewID);
                    }


                    if (PhotonNetwork.isMasterClient && Player.PlayerList[i].myTeam != Team.Zdrajca)
                    {
                        for (int j = 0; j < Player.PlayerList.Count; j++)
                        {
                            if (Player.PlayerList[j].playerGO.GetComponent<PhotonView>().viewID == myID && Player.PlayerList[j].playerGO.transform.GetChild(0).GetChild(0).childCount > 0 && Player.PlayerList[j].myTeam != Team.Zdrajca && hitPlayer.transform.GetChild(2).tag != "Good")
                            {
                                GetComponent<PhotonView>().RPC("PlayerHpChange", PhotonTargets.AllBuffered, 100, myID, 123123, false, -shotDirection);
                            }
                        }

                    }

                }
            }
        }

        hitPlayer.transform.GetChild(1).GetChild(4).GetComponent<Text>().text = hitPlayer.GetComponent<PlayerStats>().hp.ToString();

    }

    void CreateObjectsInGame()
    {
        for (int i = 0; i < objectsInGame.Count; i++)
        {
            GameObject I = PhotonNetwork.Instantiate(objectsInGame[i].name, objectsInGame[i].position, objectsInGame[i].rotation, 0);
            GetComponent<PhotonView>().RPC("AddObjectToDestroyList", PhotonTargets.AllBufferedViaServer, I.GetComponent<PhotonView>().viewID);
        }
    }

    void CreateDoors()
    {
        for (int i = 0; i < doorParent.childCount; i++)
        {
            doorsList.Add(doorParent.GetChild(i).GetComponent<Door>());
            doorParent.GetChild(i).GetComponent<Door>().id = i;
        }
    }

    [PunRPC]
    void OpenDoorRpc(int id, float dir)
    {
        StartCoroutine(RotateDoor(id, dir));
        Player.myPlayer.playerGO.GetComponent<PhotonView>().RPC("PlaySound", PhotonTargets.All, 8);
    }

    IEnumerator RotateDoor(int id, float dir)
    {
        Door door = doorsList[id];

        door.isAnimated = false;

        if (door.startRotationX + dir < 0)
            door.startRotationX = 360;
        else if (door.startRotationX + dir > 360)
            door.startRotationX = 0;

        door.transform.localRotation = Quaternion.Slerp(door.transform.localRotation, Quaternion.Euler(0, 0, door.startRotationX + dir), Time.deltaTime * 10);

        yield return new WaitForSeconds(0.01f);

        if (Mathf.Round(door.transform.localRotation.eulerAngles.z) != Mathf.Round(door.startRotationX + dir))
        {
            StartCoroutine(RotateDoor(id, dir));
        }
        else
        {
            door.startRotationX += dir;
            door.isAnimated = true;
        }
    }

    [PunRPC]
    void CreateFindBojectInGame(int boxIndex, int positionIndex)
    {
        GameObject I = PhotonNetwork.Instantiate(findBoxesGO[boxIndex].name, findBoxesSpawnPointsPositions[positionIndex], findBoxesGO[boxIndex].transform.rotation, 0);
        GetComponent<PhotonView>().RPC("AddObjectToDestroyList", PhotonTargets.AllBufferedViaServer, I.GetComponent<PhotonView>().viewID);
    }

    [PunRPC]
    public void WaitForMap(int index)
    {
        StartCoroutine(WaitForSpawn(index));
    }

    IEnumerator WaitForSpawn(int index)
    {
        yield return new WaitForSeconds(tmpPlayerCount * 0.5f + 0.5f);

        GetComponent<PhotonView>().RPC("ResetPlayerDelay", PhotonTargets.AllBufferedViaServer);

        Instantiate(mapsList[index]);

        if (!PhotonNetwork.isMasterClient)
            Destroy(waitingCanvas);
        else
            Destroy(mapCanvas);

        SpawnPlayer(0);
    }

    public void LoadMap()
    {
        tmpPlayerCount = Player.PlayerList.Count;

        if (PhotonNetwork.isMasterClient)
        {
            mapCanvas = Instantiate(mapCanvas);
        }
        else
        {
            waitingCanvas = Instantiate(waitingCanvas);
        }

        Destroy(GameObject.FindGameObjectWithTag("Background"));
    }

    [PunRPC]
    void ResetPlayerDelay()
    {
        tmpPlayerCount--;
    }

    [PunRPC]
    public void DestroyOnNetwork(int id)
    {
        Destroy(PhotonView.Find(id).gameObject);
    }

    [PunRPC]
    void AddObjectToDestroyList(int id)
    {
        objectsToDestroyInLoad.Add(PhotonView.Find(id).gameObject);
    }

    [PunRPC]
    public void RemoveObjectFromDestroyList(int id)
    {
        objectsToDestroyInLoad.Remove(PhotonView.Find(id).gameObject);
    }

    void PlayerComponentEnabled(GameObject player, bool active)
    {
        if (active)
        {
            player.tag = "Player";
        }
        else
        {
            player.tag = "Untagged";  
        }

        //player.SetActive(active);
        player.GetComponent<DragItems>().isEnable = !active;
        player.transform.GetChild(0).gameObject.SetActive(active);//Camera
        player.GetComponent<CharacterController>().enabled = active;//Character controller
        player.GetComponent<Rigidbody>().isKinematic = active;//Model

        if (player.GetComponent<PhotonView>().viewID == Player.myPlayer.playerGO.GetComponent<PhotonView>().viewID)
        {
            if (!active)
            {
                player.GetComponent<PlayerStats>().FindCameras();
                player.GetComponent<PlayerStats>().myHead.layer = 0;
            }

            player.GetComponent<Shooting>().enabled = false;
            if (!PhotonNetwork.isMasterClient) player.GetComponent<PlayerMovement>().enabled = active;
        }


        player.transform.GetChild(1).GetChild(0).gameObject.SetActive(active);//UI
        player.transform.GetChild(1).GetChild(1).gameObject.SetActive(active);//UI
        player.transform.GetChild(1).GetChild(4).gameObject.SetActive(active);//UI
        //player.transform.GetChild(1).GetChild(5).gameObject.SetActive(active);//UI
    }

    IEnumerator DamageUIEnabled(GameObject DamageUI)
    {
        DamageUI.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        DamageUI.SetActive(false);
    }

    void ShowWin(string winMessage, Team winTeam)
    {
        if (!isEndGame)
        {
            isEndGame = true;

            Player.myPlayer.playerGO.transform.GetChild(1).GetChild(2).gameObject.SetActive(true);
            Player.myPlayer.playerGO.transform.GetChild(1).GetChild(2).GetChild(0).GetComponent<Text>().text = winMessage;


            StartCoroutine(WaitForRespawn());

            if(!isMusicMute)
            {
                AudioSource audioSource = Player.myPlayer.playerGO.GetComponent<AudioSource>();

                if (Player.myPlayer.myTeam == winTeam)
                    audioSource.PlayOneShot(audioClips[2]);

                else if (Player.myPlayer.myTeam == Team.Policjant && winTeam == Team.Niewinny)
                    audioSource.PlayOneShot(audioClips[2]);

                else
                    audioSource.PlayOneShot(audioClips[3]);
            }
            
        }
    }

    IEnumerator WaitForRespawn()
    {
        yield return new WaitForSeconds(7);

        if (Player.myPlayer.pp.IsMasterClient)
            RandomTeams();
    }

    [PunRPC]
    void SoundShoot(int id, int audioIndex)
    {
        PhotonView.Find(id).GetComponent<AudioSource>().PlayOneShot(audioClips[audioIndex]);
    }

    [PunRPC]
    void ShowWeapon(int id, bool enabled)
    {
        PhotonView.Find(id).GetComponent<MeshRenderer>().enabled = enabled;
    }

    public void RandomTeams()//Game settings
    {
        int traitor;
        int police;
        int[] modelsID = new int[Player.PlayerList.Count];
        int[] spawnpointsPos = new int[spawnPointsPositions.Count];
        int[] findBoxesSpawnpointsPos = new int[findBoxesSpawnPointsPositions.Count];
        int[] nickNamesList = new int[nickList.Length];
        int[] secondNickNamesList = new int[secondNickList.Length];
        int[] findBoxesModelsID = new int[findBoxesGO.Length];

        traitor = Random.Range(0, Player.PlayerList.Count);
        police = Random.Range(0, Player.PlayerList.Count);

        while (police == traitor && Player.PlayerList.Count > 1)
            police = Random.Range(0, Player.PlayerList.Count);

        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            modelsID[i] = Random.Range(0, playerModelsGO.Length);
        }

        Repeat(ref spawnpointsPos);//Set spawnPoints
        Repeat(ref findBoxesSpawnpointsPos);//Set findBoxes spawnPoints
        Repeat(ref findBoxesModelsID);//Set findBoxes models
        Repeat(ref nickNamesList);//Set nickNames
        Repeat(ref secondNickNamesList);//Set secondNickNames

        GetComponent<PhotonView>().RPC("Restart", PhotonTargets.All, traitor, police, modelsID, findBoxesModelsID, spawnpointsPos, findBoxesSpawnpointsPos, nickNamesList, secondNickNamesList);
    }

    void Repeat(ref int[] spawnpointsPos)
    {
        for (int i = 0; i < spawnpointsPos.Length; i++)
        {
            spawnpointsPos[i] = -1;
        }

        int count = 0;

        while (true)
        {
            int value = Random.Range(0, spawnpointsPos.Length);
            bool isNew = true;

            for (int i = 0; i < count + 1; i++)
            {
                if (spawnpointsPos[i] == value)
                {
                    isNew = false;
                }
            }

            if(isNew)
            {
                spawnpointsPos[count] = value;
                count++;
            }



            if (count >= spawnpointsPos.Length)
                break;
        }

    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            Player.PlayerList[i].playerGO.SetActive(true);
            Player.PlayerList[i].playerGO.GetComponent<PlayerStats>().SetMyHead(0);
        }

        Player.myPlayer.playerGO.GetComponent<PlayerStats>().myHead.layer = 9;
    }

    [PunRPC]
    void Restart(int traitorID, int policeID, int[] modelsID, int[] findBoxesModelsID, int[] spawnpointsPos, int[] findBoxesSpawnpointsPos, int[] nickNames, int[] secondNickNames)
    {
        findBoxReservedPositions = new int[findBoxesCount];

        foreach (var item in objectsToDestroyInLoad)
        {
            Destroy(item);
        }

        Debug.Log("1");

        if(PhotonNetwork.isMasterClient)
            CreateObjectsInGame();

        gameTimer = timeBeforRound;
        isEndGame = false;
        startGame = false;
        foundBoxesCount = 0;
        //PhotonNetwork.RemoveRPCsInGroup(0);
        Debug.Log("2");
        timerText.enabled = true;
        Player.myPlayer.playerGO.transform.GetChild(1).GetChild(2).gameObject.SetActive(false);
        Player.myPlayer.playerGO.GetComponent<PlayerMovement>().canPush = true;
        Player.myPlayer.playerGO.GetComponent<PlayerMovement>().gravity = 20f;
        Debug.Log("3");
        Player.myPlayer.playerGO.GetComponent<PlayerStats>().OffAllCameras();
        Debug.Log("4");
        Player.myPlayer.playerGO.GetComponent<PlayerMovement>().ChangeLayers(0);
        Debug.Log("5");
        //Player.myPlayer.playerGO.transform.GetChild(1).GetChild(4).GetComponent<Text>().text = "100";

        alivedPlayers = Player.PlayerList.Count;

        if(PhotonNetwork.isMasterClient)
        {
            for (int i = 0; i < findBoxesCount; i++)//FindBoxes
            {
                GameObject I = PhotonNetwork.Instantiate(findBoxesGO[findBoxesModelsID[i]].name, findBoxesSpawnPointsPositions[findBoxesSpawnpointsPos[i]], Quaternion.identity, 0);
                findBoxReservedPositions[i] = findBoxesSpawnpointsPos[i];
                GetComponent<PhotonView>().RPC("AddObjectToDestroyList", PhotonTargets.AllBufferedViaServer, I.GetComponent<PhotonView>().viewID);
            }
        }

        Debug.Log("6");
        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            PlayerComponentEnabled(Player.PlayerList[i].playerGO, true);
            Player.PlayerList[i].playerGO.SetActive(false);
            Player.PlayerList[i].playerGO.GetComponent<PlayerStats>().hp = 100;
            Player.PlayerList[i].playerGO.GetComponent<PlayerStats>().nick = nickList[nickNames[i]] + " " + secondNickList[secondNickNames[i]];
        }
        Debug.Log("7");
        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            Player.PlayerList[i].playerGO.transform.position = spawnPointsPositions[spawnpointsPos[i]];
            Player.PlayerList[i].playerGO.transform.rotation = Quaternion.Euler(0, 0, 0);

            if (Player.PlayerList[i].playerGO.transform.GetChild(0).GetChild(0).childCount > 0)
            {
                GameObject w = Player.PlayerList[i].playerGO.transform.GetChild(0).GetChild(0).gameObject;

                for (int j = 0; j < w.transform.childCount; j++)
                {
                    Destroy(w.transform.GetChild(j).gameObject);
                }

            }

            Player.PlayerList[i].myTeam = Team.Niewinny;
        }
        Debug.Log("8");
        Player.PlayerList[traitorID].myTeam = Team.Zdrajca;
        Player.PlayerList[policeID].myTeam = Team.Policjant;

        Player.myPlayer.playerGO.transform.GetChild(1).GetChild(5).GetComponent<Text>().text = Player.myPlayer.playerGO.GetComponent<PlayerStats>().nick;//Set my name in UI
        Player.myPlayer.playerGO.GetComponent<Rigidbody>().velocity = Vector3.zero;//Reset velocity
        Player.myPlayer.playerGO.GetComponent<PlayerMovement>().speed = 6f;//player speed

        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            Destroy(Player.PlayerList[i].playerGO.transform.GetChild(2).gameObject);
            GameObject I = Instantiate(playerModelsGO[modelsID[i]], Player.PlayerList[i].playerGO.transform.position + new Vector3(0, -0.86f, 0), Quaternion.identity, Player.PlayerList[i].playerGO.transform);
            //if (Player.PlayerList[i].pp.ID == Player.myPlayer.pp.ID) I.transform.GetChild(0).gameObject.SetActive(false);
        }
        Debug.Log("9");
        //Doors
        for (int i = 0; i < doorsList.Count; i++)
        {
            if(doorsList[i].isTurnedDoor)
            {
                doorsList[i].startRotationX = 90;
                doorsList[i].transform.localRotation = Quaternion.Euler(0, 0, 90);
            }      
            else
            {
                doorsList[i].startRotationX = 0;
                doorsList[i].transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
                
        }

        Debug.Log("10");
        StartCoroutine(Wait());
        //StartCoroutine(WaitForStartGame());

        Player.myPlayer.playerGO.transform.GetChild(1).GetChild(1).GetComponent<Text>().text = "???";
    }

    void GiveGun()
    {
        Player me = Player.myPlayer;

        if (me.myTeam == Team.Niewinny)
        {
            me.playerGO.GetComponent<Shooting>().enabled = false;
        }
        else if (me.myTeam == Team.Zdrajca || me.myTeam == Team.Policjant)
        {
            GameObject weapon = null;

            if (me.myTeam == Team.Policjant)
            {
                GetWeapon(me, "Gun", Shooting.WeaponType.pistol, ref weapon);
            }
            else if (me.myTeam == Team.Zdrajca)
            {
                GetWeapon(me, "Knife", Shooting.WeaponType.knife, ref weapon);
            }

            SpawnWeapon(me, ref weapon);
        }

        me.playerGO.transform.GetChild(1).GetChild(1).GetComponent<Text>().text = me.myTeam.ToString();
    }

    [PunRPC]
    public void GiveWeaponOtherPlayer(int playerid, string prefabName, Shooting.WeaponType weaponType)
    {
        if (Player.PlayerList[playerid] != Player.myPlayer)
            return;
        Debug.Log("IsMyPlayer");

        GameObject weapon = null;

        GetWeapon(Player.myPlayer, prefabName, weaponType, ref weapon);
        SpawnWeapon(Player.myPlayer, ref weapon);
    }

    [PunRPC]
    public int ReurnPlayerId(Player player)
    {
        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            if(Player.PlayerList[i] == player)
            {
                return i;
            }
        }

        return -1;
    }

    public void GetWeapon(Player me, string prefabName, Shooting.WeaponType weaponType, ref GameObject weapon)
    {
        weapon = PhotonNetwork.Instantiate(prefabName, me.playerGO.transform.GetChild(0).GetChild(0).position, Quaternion.identity, 0);
        //weapon.transform.localRotation = weapon.transform.rotation;
        //weapon.transform.localRotation = Quaternion.Euler(6, -150, 5);
        me.playerGO.GetComponent<Shooting>().weaponType = weaponType;
    }

    public void SpawnWeapon(Player me, ref GameObject weapon)
    {
        weapon.layer = 8;
        weapon.transform.SetParent(me.playerGO.transform.GetChild(0).GetChild(0));
        weapon.transform.localPosition = weapon.GetComponent<WeaponStats>().postion;
        weapon.transform.localRotation = Quaternion.Euler(weapon.GetComponent<WeaponStats>().rotation);

        me.playerGO.GetComponent<Shooting>().enabled = true;

        GetComponent<PhotonView>().RPC("InstantWeapons", PhotonTargets.AllBufferedViaServer, me.playerGO.GetComponent<PhotonView>().viewID, weapon.GetComponent<PhotonView>().viewID);
    }

    [PunRPC]
    void InstantWeapons(int playerID, int weaponID)
    {
        Transform weapon = PhotonView.Find(weaponID).transform;

        PhotonView.Find(weaponID).transform.SetParent(PhotonView.Find(playerID).transform.GetChild(0).GetChild(0));

        if (Player.myPlayer.playerGO != PhotonView.Find(playerID).gameObject)
        {
            PhotonView.Find(weaponID).transform.localPosition = weapon.GetComponent<WeaponStats>().otherPostion;
            PhotonView.Find(weaponID).transform.localRotation = Quaternion.Euler(weapon.GetComponent<WeaponStats>().otherRotation);
        }

    }

    [PunRPC]
    void Push(Vector3 direction, int playerID)
    {
        if (PhotonView.Find(playerID).viewID == Player.myPlayer.playerGO.GetComponent<PhotonView>().viewID)
            Player.myPlayer.playerGO.GetComponent<PlayerMovement>().PushMe(direction);
        //else
        //PhotonView.Find(playerID).GetComponent<NewPosSync>().position += direction;
    }
}
