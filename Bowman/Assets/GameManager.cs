using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public MatchSettings matchSettings;
    public static GameManager instance;
    [SerializeField]
    private GameObject sceneCamera;
    public delegate void OnKillCallback(string player, string source);
    public OnKillCallback onKillCallback;

    void Awake()
    {
        if (instance != null)
        {
            return;
        }
        else
        {
            instance = this;
        }
    }

    private static Dictionary<string, Player> players = new Dictionary<string, Player>();

    public static void RegisterPlayer(string _netID, Player _player)
    {
        players.Add("Player " + _netID, _player);
        _player.transform.name = "Player " + _netID;
    }

    public static Player GetPlayer(string _playerID)
    {
        return players[_playerID];
    }

    public static List<Player> GetAllPlayers()
    {
        List<Player> allPlayers = new List<Player>();
        foreach(Player player in players.Values)
            allPlayers.Add(player);
        return allPlayers;
    }
    public void SetSceneCameraActive(bool isActive)
    {
        if (sceneCamera == null)
            return;
        sceneCamera.SetActive(isActive);
    }
    public static void UnRegisterPlayer(string _playerID)
    {
        players.Remove(_playerID);
    }
}
