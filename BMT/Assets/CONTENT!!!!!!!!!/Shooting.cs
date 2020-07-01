using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Shooting : MonoBehaviour
{
    bool canShot = true;
    bool weaponEnabled = false;
    int bullets = 1;
    int magazineSize = 1;
    public WeaponType weaponType; //my weapon type
    float shotRange = 100;
    float reloadTime;

    ParticleSystem shotEffect;
    List<Transform> players = new List<Transform>(); 

    private void OnEnable()
    {
        players.Clear(); //Clear player list

        //Set players in player list
        for (int i = 0; i < Player.PlayerList.Count; i++)
        {
            players.Add(Player.PlayerList[i].playerGO.transform);
        }

        canShot = true; //Set weapon to ready to shot
        weaponEnabled = false; //Hide weapon
        shotEffect = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<ParticleSystem>(); //Set shot effect


        //Set values for all weapons
        if (weaponType == WeaponType.pistol)
        {
            shotRange = 100f;
            bullets = 1;
            magazineSize = 1;
            reloadTime = 6f;
        }
        else if(weaponType == WeaponType.knife)
        {
            shotRange = 1f;
            bullets = 1;
            magazineSize = 1;
        }
        else if(weaponType == WeaponType.innocentPistol)
        {
            shotRange = 100f;
            bullets = 5;
            magazineSize = 5;
            reloadTime = 2f;
        }

    }
    private void Update()
    {
        //Click shot button
        if(Input.GetMouseButtonDown(0) && canShot && bullets > 0 && weaponEnabled && GetComponent<PlayerMovement>().canMove)
        {
            Shoot();
            StartCoroutine(Wait(0.2f)); //Shot delay
        }
        //Click reload button
        else if(Input.GetKeyDown(KeyCode.R) && bullets != magazineSize && weaponEnabled && canShot)
        {
            Reload();
        }
        //Click hide/unhide weapon
        else if (Input.GetKeyDown(KeyCode.Alpha1) && GetComponent<PlayerMovement>().canMove)
        {
            ChangeWeaponEnabled();
        }
    }

    void ChangeWeaponEnabled()
    {
        weaponEnabled = !weaponEnabled;

        //Show weapon on the server
        GameManager.gameManager.GetComponent<PhotonView>().RPC("ShowWeapon", PhotonTargets.AllBuffered, transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<PhotonView>().viewID, weaponEnabled);

        //If weapon is knife change player speed
        if (weaponType == WeaponType.knife)
        {
            if (weaponEnabled)
                GetComponent<PlayerMovement>().speed *= 1.2f;
            else
                GetComponent<PlayerMovement>().speed /= 1.2f;
        }
    }

    //Check distance to the players
    int CheckDistance()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if(players[i].GetComponent<PhotonView>().viewID != GetComponent<PhotonView>().viewID && Vector3.Distance(players[i].transform.position, transform.position) < 2 && players[i].tag == "Player")
            {
                return players[i].GetComponent<PhotonView>().viewID; //return player network id
            }
        }

        return -1;
    }

    //Wait for reload
    void Reload()
    {
        StartCoroutine(Wait(reloadTime)); //Wait reload time
        StartCoroutine(WaitForSound()); //play sound (few times(3))

        bullets = magazineSize; //Set full magazine
    }

    IEnumerator WaitForSound()
    {
        //Play sound on the server
        GameManager.gameManager.GetComponent<PhotonView>().RPC("SoundShoot", PhotonTargets.All, GetComponent<PhotonView>().viewID, 1);

        yield return new WaitForSeconds(2.05f);

        //Reload sound play few times (3)
        if (!canShot)
            StartCoroutine(WaitForSound());
    }
    
    //Wait reload time
    IEnumerator Wait(float time)
    {
        canShot = false;

        yield return new WaitForSeconds(time);

        canShot = true;
    }

    //Check which weapon has player
    void Shoot()
    {

        if (weaponType == WeaponType.pistol)
        {
            StartShoot(100, "SoundShoot");
        }
        else if(weaponType == WeaponType.innocentPistol)
        {
            StartShoot(25, "SoundShoot");
        }
        else if (weaponType == WeaponType.knife)
        {
            Player.myPlayer.playerGO.GetComponent<SoundManager>().PlaySound(10); //Play sound without server

            int tmp = CheckDistance();

            //Change hp on the server
            if (tmp != -1)
                GameManager.gameManager.GetComponent<PhotonView>().RPC("PlayerHpChange", PhotonTargets.AllBuffered, 100, tmp, GetComponent<PhotonView>().viewID, CheckHP(100, PhotonView.Find(tmp).gameObject), transform.GetChild(0).forward);
        }
    }

    //Create raycast and play shoot sound
    void StartShoot(int hp, string soundShoot)
    {
        //--
        //Play sound on the server
        GameManager.gameManager.GetComponent<PhotonView>().RPC(soundShoot, PhotonTargets.All, GetComponent<PhotonView>().viewID, 0);
        bullets--;


        Ray ray = new Ray(transform.GetChild(0).position, transform.GetChild(0).forward);

        //Chcek which hit element is first
        RaycastHit[] hits = Physics.RaycastAll(ray, shotRange).OrderBy(e => e.distance).ToArray();

        //Chcek first hit is enemy
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root != transform)
            {
                if (hit.transform.root.CompareTag("Player"))
                {
                    //Hp change on the server
                    GameManager.gameManager.GetComponent<PhotonView>().RPC("PlayerHpChange", PhotonTargets.AllBuffered, hp, hit.collider.GetComponent<PhotonView>().viewID, GetComponent<PhotonView>().viewID, CheckHP(hp, hit.collider.gameObject), transform.GetChild(0).forward);
                }

                break;
            }
        }

        shotEffect.Play(); //particle system shoot effect
    }

    bool CheckHP(int damage, GameObject hitPlayer) //return player is alive or not
    {
        int hp = hitPlayer.GetComponent<PlayerStats>().hp; //Chcek my hp
        hp -= damage;

        if (hp <= 0)
        {
            return false;
        }

        return true;
    }

    public enum WeaponType 
    {
        pistol, knife, innocentPistol
    }
}
