using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    CharacterController characterController;

    public static float sensivity;
    float mouseUpDown;
    float restartTime;

    public float speed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float footStepSize = 0.5f;

    float footSteptmp;
    bool isJumping;
    bool leftStep;

    Vector3 pushDir;
    public bool canPush = true;
    public Team myTeam;

    Text nickNameText;

    private Vector3 moveDirection = Vector3.zero;

    public bool buyWeapon; //= true;
    public bool canMove = true;
    public static bool isCrouch;

    Text foundBoxesCountText;

    SoundSource soundSource;

    float fallTimer;
    float minFallTime = 0.8f;

    private void OnEnable()
    {
        SetPlayerHight(0, 1.84f, 0.482f, 6, 6);
        isCrouch = false;
        isJumping = false;
        foundBoxesCountText.text = "";
    }

    private void OnTriggerEnter(Collider other)
    {

        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            if (Player.PlayerList[i].playerGO == gameObject)
            {
                myTeam = Player.PlayerList[i].myTeam;
                break;
            }

        }

        if (other.CompareTag("DropGun") && myTeam != Team.Zdrajca && CompareTag("Player") && transform.GetChild(0).GetChild(0).childCount == 0)//
        {
            GameObject weapon = null;
            GameManager.objectsToDestroyInLoad.Remove(other.gameObject);
            GameManager.gameManager.GetComponent<PhotonView>().RPC("DestroyOnNetwork", PhotonTargets.AllBuffered, other.GetComponent<PhotonView>().viewID);

            GameManager.gameManager.GetWeapon(Player.myPlayer, "Gun", Shooting.WeaponType.pistol, ref weapon);
            GameManager.gameManager.SpawnWeapon(Player.myPlayer, ref weapon);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(Input.GetKey(KeyCode.W) && other.CompareTag("Ladder"))
        {
            gravity = -5f;
            soundSource.Play(ref soundSource.canPlay, 9, GameManager.gameManager.audioClips[9].length);
            fallTimer = 0;
        }
        else
            gravity = 20f;
    }

    private void OnTriggerExit(Collider other)
    {
        gravity = 20f;
    }

    void Awake()
    {
        sensivity = Settings.sensivity;
        characterController = GetComponent<CharacterController>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        nickNameText = transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>();
        foundBoxesCountText = transform.GetChild(1).GetChild(8).GetComponent<Text>();

        soundSource = GameManager.gameManager.GetComponent<SoundSource>();
    }

    void Update()
    {
        MouseMovement();
        Movement();
        RestartGame();
        Push();
        PhotoMode();
        ActiveRadar();
        Crouch();
    }

    void PhotoMode()
    {
        if (Input.GetKeyDown(KeyCode.P))
            transform.GetChild(1).gameObject.SetActive(!transform.GetChild(1).gameObject.activeInHierarchy);
    }

    public void PushMe(Vector3 direction)
    {
        pushDir = direction * 10;
        pushDir = new Vector3(pushDir.x, 2, pushDir.z);
    }

    void Push()
    {
        nickNameText.enabled = false;

        Ray ray = new Ray(transform.GetChild(0).position, transform.GetChild(0).forward);

        RaycastHit[] hits = Physics.RaycastAll(ray, 7f).OrderBy(e => e.distance).ToArray();

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root != transform)
            {
                if (hit.transform.root.CompareTag("Player"))
                {
                    ApplyNickPosition.targetPlayer = hit.collider.transform;
                    nickNameText.text = hit.collider.GetComponent<PlayerStats>().nick;
                    nickNameText.enabled = true;

                    if (Input.GetKeyDown(KeyCode.F) && hit.distance < 2 && canPush)
                    {
                        GameManager.gameManager.GetComponent<PhotonView>().RPC("Push", PhotonTargets.All, transform.GetChild(0).forward, hit.collider.GetComponent<PhotonView>().viewID);
                        StartCoroutine(Delay());

                    }
                    else if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (Player.myPlayer.myTeam == Team.Policjant && buyWeapon)
                        {

                            buyWeapon = false;
                            int hitPlayerID = ReturnHitPlayer(hit.collider.gameObject);

                            if (Player.PlayerList[hitPlayerID].myTeam == Team.Niewinny)
                            {
                                GameManager.gameManager.GetComponent<PhotonView>().RPC("GiveWeaponOtherPlayer", PhotonTargets.All, hitPlayerID, "InnocentPistol", Shooting.WeaponType.innocentPistol);
                            }
                        }
                    }

                }
                else if (hit.transform.root.CompareTag("FindObject"))
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (!GameManager.startGame)
                            return;

                        GameManager.gameManager.GetComponent<PhotonView>().RPC("RemoveObjectFromDestroyList", PhotonTargets.All, hit.transform.root.GetComponent<PhotonView>().viewID);
                        GameManager.gameManager.GetComponent<PhotonView>().RPC("DestroyOnNetwork", PhotonTargets.All, hit.transform.root.GetComponent<PhotonView>().viewID);

                        GameManager.gameManager.GetComponent<PhotonView>().RPC("CreateFindBojectInGame", PhotonTargets.MasterClient, Random.Range(0, GameManager.gameManager.findBoxesGO.Length), FindFreePosition(hit.transform.root.position));

                        GameManager.foundBoxesCount++;
                        foundBoxesCountText.text = GameManager.foundBoxesCount.ToString();

                        GameManager.gameManager.GetComponent<PhotonView>().RPC("SoundShoot", PhotonTargets.All, GetComponent<PhotonView>().viewID, 4);

                        if (GameManager.foundBoxesCount == 10 && Player.myPlayer.myTeam == Team.Niewinny)
                        {
                            GameManager.gameManager.GiveWeaponOtherPlayer(GameManager.gameManager.ReurnPlayerId(Player.myPlayer), "Gun", Shooting.WeaponType.pistol);
                        }

                    }
                }
                else if (hit.transform.parent != null && hit.transform.parent.CompareTag("Door") && hit.distance < 4)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                        hit.transform.parent.GetComponent<Door>().Open(transform.position);
                }

                    break;
            }


        }
    }

    int FindFreePosition(Vector3 pos)
    {
        while (true)
        {
            bool isNew = true;
            int n = Random.Range(0, GameManager.gameManager.findBoxesSpawnPointsPositions.Count);
            int last = 0;

            for (int i = 0; i < GameManager.gameManager.findBoxesCount; i++)
            {
                if (GameManager.gameManager.findBoxReservedPositions[i] == n)
                {
                    isNew = false;
                }
            }

            for (int i = 0; i < GameManager.gameManager.findBoxesSpawnPointsPositions.Count; i++)
            {
                if (GameManager.gameManager.findBoxesSpawnPointsPositions[i] == pos)
                {
                    for (int j = 0; j < GameManager.gameManager.findBoxesCount; j++)
                    {
                        if (GameManager.gameManager.findBoxReservedPositions[j] == i)
                        {
                            last = j;
                            break;
                        }

                    }

                }
            }

            if (isNew)
            {
                GameManager.gameManager.findBoxReservedPositions[last] = n;
                return n;
            }

        }

    }

    int ReturnHitPlayer(GameObject hit)
    {
        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            if (Player.PlayerList[i].playerGO == hit)
            {
                return i;
            }
        }

        return 0;

    }

    IEnumerator Delay()
    {
        canPush = false;

        yield return new WaitForSeconds(0.3f);

        canPush = true;
    }

    void ActiveRadar()
    {
        if(Input.GetKeyDown(KeyCode.LeftAlt) && Player.myPlayer.myTeam == Team.Zdrajca && GameManager.foundBoxesCount > 0)
        {
            GameManager.foundBoxesCount--;
            foundBoxesCountText.text = GameManager.foundBoxesCount.ToString();
            StartCoroutine(RadarTime());
        }
    }

    public void ChangeLayers(int layer)
    {
        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            if(Player.PlayerList[i] != Player.myPlayer)
            {
                Player.PlayerList[i].playerGO.GetComponent<PlayerStats>().myHead.layer = layer;
                Player.PlayerList[i].playerGO.transform.GetChild(2).GetChild(1).gameObject.layer = layer;
            }            
        }
    }

    IEnumerator RadarTime()
    {
        ChangeLayers(8);

        yield return new WaitForSeconds(3f);

        ChangeLayers(0);
    }

    void RestartGame()
    {
        if (Input.GetKey(KeyCode.Return) && PhotonNetwork.isMasterClient)
        {
            restartTime += Time.deltaTime;

            if (restartTime > 2)
            {
                GameManager.gameManager.RandomTeams();//GameManager.gameManager.GetComponent<PhotonView>().RPC("Restart", PhotonTargets.All, 0, 0);
                restartTime = 0;
            }
        }
        else if (Input.GetKeyUp(KeyCode.Return) && PhotonNetwork.isMasterClient)
        {
            restartTime = 0;
        }
    }

    void MouseMovement()
    {
        if (!canMove)
            return;

        float mouseLeftRight = Input.GetAxis("Mouse X") * sensivity;
        transform.Rotate(0, mouseLeftRight, 0);

        mouseUpDown -= Input.GetAxis("Mouse Y") * sensivity;

        mouseUpDown = Mathf.Clamp(mouseUpDown, -90, 90);
        transform.GetChild(0).localRotation = Quaternion.Euler(mouseUpDown, 0, 0);
    }

    void Movement()
    {
        if (characterController.isGrounded)
        {
            if(isJumping)
            {
                isJumping = false;
                FootStepSound();
            }


            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
            moveDirection *= speed;
            moveDirection = transform.TransformDirection(moveDirection);

            if (moveDirection.magnitude > 7)
                moveDirection /= 1.3f;

            if (Input.GetButton("Jump"))
            {
                isJumping = true;
                moveDirection.y = jumpSpeed;
            }

            //Walk Sound
            if (moveDirection.magnitude > 1 && !isCrouch && characterController.velocity.magnitude > 0.8f)
            {
                footSteptmp += Time.deltaTime;

                if (footSteptmp >= footStepSize)
                {
                    FootStepSound();
                }
            }

            if (fallTimer >= minFallTime)
            {
                Player.myPlayer.playerGO.GetComponent<PhotonView>().RPC("PlaySound", PhotonTargets.All, 11);
            }

            fallTimer = 0;
        }
        else
        {
            fallTimer += Time.deltaTime;
        }

        moveDirection.y -= gravity * Time.deltaTime;

        moveDirection += pushDir;
        pushDir /= 10;

       
        if(canMove)
            characterController.Move(moveDirection * Time.deltaTime);

        
    }

    void FootStepSound()
    {
        leftStep = !leftStep;
        footSteptmp = 0;
        int soundIndex;

        if (leftStep) soundIndex = 5;
        else soundIndex = 6;

        GameManager.gameManager.GetComponent<PhotonView>().RPC("SoundShoot", PhotonTargets.All, GetComponent<PhotonView>().viewID, soundIndex);
    }

    void Crouch()
    {
        if(Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouch = !isCrouch;

            if (isCrouch) SetPlayerHight(-0.28f, 1.3f, 0, 2, 3);
            else SetPlayerHight(0, 1.84f, 0.482f, 6, 6);

        }
    }

    void SetPlayerHight(float center, float hight, float camPostion, float mySpeed, float jumpHight)
    {
        GetComponent<CharacterController>().center = new Vector3(0, center, 0);
        GetComponent<CharacterController>().height = hight;
        transform.GetChild(0).localPosition = new Vector3(0, camPostion, 0);
        speed = mySpeed;
        jumpSpeed = jumpHight;
    }


}
