using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
    public bool isDead;

    [HideInInspector]
    public int kills, SendDamageAmountToUI;

    [SerializeField]
    private int maxHealth = 100;

    [SyncVar]
    private int currentHealth;
    private bool firstSetup = true;

    [SerializeField]
    private Behaviour[] disableOnDeath;
    private bool[] wasEnabled;

    [HideInInspector]
    public bool hit, headshot, hasWon, hasDiedOnce;
    private static string winnerName;
    Player lastPlayerToDealDamage;

    [SerializeField]
    Animator animator;

    private void Update()
    {
        if (isLocalPlayer)
        {
            if (isDead)
                animator.SetBool("Dead", true);
            else
                animator.SetBool("Dead", false);
        }
        if (transform.position.y < -30 && !hasDiedOnce)
        {
            Die();
            if (lastPlayerToDealDamage)
                lastPlayerToDealDamage.AddKill();
            hasDiedOnce = true;
        }
    }
    public void SetupPlayer()
    {
        CmdBroadCastNewPlayerSetup();
    }

    [Command]
    private void CmdBroadCastNewPlayerSetup()
    {
        RpcSetupPlayerOnAllClients();
    }

    [ClientRpc]
    private void RpcSetupPlayerOnAllClients()
    {
        if (firstSetup)
        {
            wasEnabled = new bool[disableOnDeath.Length];
            for (int i = 0; i < wasEnabled.Length; i++)
            {
                wasEnabled[i] = disableOnDeath[i].enabled;
            }
            firstSetup = false;
        }

        SetDefaults();
    }
    [ClientRpc]
    public void RpcForcedJump(Vector3 direction)
    {
        UnityStandardAssets.Characters.FirstPerson.FirstPersonController firstPerson;
        firstPerson = GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>();
        firstPerson.SetMoveDirectionForPowerUp(direction);
    }

    [ClientRpc]
    public void RpcTakeDamage(int _amount, string nameTag, bool _headshot)
    {
        if (isDead)
            return;

        currentHealth -= _amount;

        Debug.Log(transform.name + " now has " + currentHealth + " health");

        Player _player = GameManager.GetPlayer(nameTag);
        _player.SendDamageAmountToUI = _amount;
        _player.StartHitMarker(_player, _headshot);
        lastPlayerToDealDamage = _player;

        if (currentHealth <= 0)
        {
            Die();
            _player.AddKill();
            GameManager.instance.onKillCallback.Invoke(transform.name, nameTag);
        }
    }
    public void SetSensitivity(float value)
    {
        UnityStandardAssets.Characters.FirstPerson.FirstPersonController firstPerson;
        firstPerson = GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>();
        firstPerson.SetSens(value);
    }

    private void StartHitMarker(Player _player, bool _headshot)
    {
        StartCoroutine(HitPlayer(_player, _headshot));
    }

    private IEnumerator HitPlayer(Player _player, bool _headshot)
    {
        if (_headshot)
            _player.headshot = true;
        else
            _player.hit = true;
        yield return new WaitForSeconds(.5f);
        _player.hit = false;
        _player.headshot = false;
    }


    public float GetHealthAmount()
    {
        return currentHealth;
    }

    private void Die()
    {
        isDead = true;

        animator.SetTrigger("Death");

        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = false;
        }

        Collider _col = GetComponent<Collider>();
        if (_col != null)
        {
            _col.enabled = false;
        }

        Debug.Log(transform.name + " is dead");

        StartCoroutine(Respawn());
    }

    private void AddKill()
    {
        kills += 1;
        print("NUMBER OF KILLS = " + kills);
        if (kills == GameManager.instance.matchSettings.killsToWin)
        {
            winnerName = transform.name;
            List<Player> allPlayers = new List<Player>();
            allPlayers = GameManager.GetAllPlayers();
            foreach(Player player in allPlayers)
            {
                player.hasWon = true;
                player.Die();
                player.kills = 0;
            }
        }
    }
    public string GetWinner()
    {
        return winnerName;
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(3);
        Transform _spawnPoint = NetworkManager.singleton.GetStartPosition();
        transform.position = _spawnPoint.position;
        transform.rotation = _spawnPoint.rotation;
        yield return new WaitForSeconds(0.1f);
        print(transform.name + " respawned");
        hasDiedOnce = false;
        SetupPlayer();
    }

    public void SetDefaults()
    {
        isDead = false;

        currentHealth = maxHealth;

        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = wasEnabled[i];
        }

        Collider _col = GetComponent<Collider>();
        if (_col != null)
        {
            _col.enabled = true;
        }
    }
}
