using UnityEngine.Networking.Match;
using UnityEngine.Networking;
using UnityEngine;

public class Pause : MonoBehaviour
{
    public static bool IsOn = false;
    private NetworkManager networkManager;

    void Start()
    {
        networkManager = NetworkManager.singleton;
    }
    public void LeaveRoom()
    {
        print("CLICKED");
        MatchInfo matchInfo = networkManager.matchInfo;
        networkManager.matchMaker.DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, networkManager.OnDropConnection);
        networkManager.StopHost();
    }
}
