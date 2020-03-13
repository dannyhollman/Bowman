using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SpawnArrow : NetworkBehaviour
{
    public GameObject firePoint;
    public List<GameObject> vfx = new List<GameObject>();
    private GameObject effectToSpawn;
    public UnityStandardAssets.Characters.FirstPerson.FirstPersonController firstPerson;
    private float increaseSpeed = 50;
    public float speed, pullbackSpeed, powerUpRefreshTime;
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private LayerMask mask;
    private bool hitScan, playedAudioOnce;
    public bool usedPowerUp, powerUpActive;
    void Start()
    {
        powerUpRefreshTime = Time.time;
        increaseSpeed = speed;
        effectToSpawn = vfx[0];
        Cursor.visible = false;
    }
    void Update()
    {
        if (!isLocalPlayer)
            return;
        if (Pause.IsOn)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKeyDown(KeyCode.E) && (Time.time - powerUpRefreshTime > 5f))
        {
            print("HAVE POWER UP!");
            powerUpActive = true;
        }
        if (Input.GetMouseButton(0))
        {
            if (increaseSpeed < 125)
                increaseSpeed += pullbackSpeed * Time.deltaTime;
            else
            {
                if (!playedAudioOnce)
                {
                    playedAudioOnce = true;
                    firstPerson.PlayPulledBackFull();
                }
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (powerUpActive)
            {
                powerUpRefreshTime = Time.time;
                increaseSpeed = 125f;
                hitScan = true;
                Shoot();
                spawnVFX(hitScan);
                usedPowerUp = true;
                powerUpActive = false;
                firstPerson.PlayKnockbackShootSound();
            }
            else
            {
                hitScan = false;
                spawnVFX(hitScan);
                firstPerson.PlayShootSound();
            }
            increaseSpeed = speed;
            playedAudioOnce = false;
        }
    }
    [Client]
    void Shoot()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        RaycastHit _hit;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out _hit, 100f, mask))
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            print(_hit.collider.tag);
            if (_hit.collider.tag == "Player")
            {
                float startTime = Time.time;
                CmdPlayerShot2(_hit.collider.name, 10, transform.name, ray.direction);
            }
        }
    }
    [Command]
    void CmdPlayerShot2(string _playerID, int _damage, string nameTag, Vector3 direction)
    {
        Debug.Log(_playerID + " has been shot!");
        Player _player = GameManager.GetPlayer(_playerID);
        _player.RpcForcedJump(direction);
        _player.RpcTakeDamage(_damage, nameTag, false);
        
    }
    public void PlayerShot(string _playerID, int _damage, string nameTag, bool _headshot)
    {
        print("SHOULDNT BE HERE");
        Player _player = GameManager.GetPlayer(_playerID);
        if (!isServer && isClient)
        {
            CmdPlayerShot(_playerID, _damage, nameTag, _headshot);
            return;
        }
        _player.RpcTakeDamage(_damage, nameTag, _headshot);
    }
    [Command]
    void CmdPlayerShot(string _playerID, int _damage, string nameTag, bool _headshot)
    {
        Player _player = GameManager.GetPlayer(_playerID);
        _player.RpcTakeDamage(_damage, nameTag, _headshot);
    }

    // [Command]
    // void CmdOnShoot(float increaseSpeed, bool _hitScan)
    // {
    //     GameObject MoveArrow;
    //     MoveArrow = Instantiate(effectToSpawn, firePoint.transform.position, Quaternion.identity);
    //     MoveArrow.GetComponent<MoveArrow>().nameTag = transform.name;
    //     MoveArrow.transform.localRotation = firstPerson.GetCamRotation();
    //     MoveArrow.GetComponent<MoveArrow>().UpdateSpeed(increaseSpeed, _hitScan);
    //     NetworkServer.Spawn(MoveArrow);
    //     increaseSpeed = speed;
    // }

    [Command]
    void CmdOnShoot(float increaseSpeed, bool _hitScan)
    {
        RpcDoShootEffect(increaseSpeed,_hitScan);
    }
    [ClientRpc]
    void RpcDoShootEffect(float increaseSpeed, bool _hitScan)
    {
        if (isLocalPlayer)
            return;
        GameObject MoveArrow;
        MoveArrow = Instantiate(vfx[1], firePoint.transform.position, Quaternion.identity);
        MoveArrow.transform.localRotation = firstPerson.GetCamRotation();
        MoveArrow.GetComponent<MoveServerArrow>().UpdateSpeed(increaseSpeed, _hitScan, null);
        increaseSpeed = speed;
    }
    void spawnVFX(bool _hitScan)
    {
        if (!isLocalPlayer)
            return;
        if (firePoint != null)
        {
            GameObject MoveArrow;
            MoveArrow = Instantiate(effectToSpawn, firePoint.transform.position, Quaternion.identity);
            MoveArrow.GetComponent<MoveArrow>().nameTag = transform.name;
            MoveArrow.transform.localRotation = firstPerson.GetCamRotation();
            MoveArrow.GetComponent<MoveArrow>().UpdateSpeed(increaseSpeed, _hitScan, gameObject);
            print(isClient + " client | server " + isServer);
            if (isClient && !isServer)
            {
                CmdOnShoot(increaseSpeed, _hitScan);
            }
            else
            {
                RpcDoShootEffect(increaseSpeed, _hitScan);
            }
        }
        else
        {
            Debug.Log("No fire point!");
        }
    }
}
