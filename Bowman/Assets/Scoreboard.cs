using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scoreboard : MonoBehaviour
{
    [SerializeField]
    GameObject playerScoreboardPrefab;
    [SerializeField]
    Transform playerScoreboardList;
    void OnEnable()
    {
        // get and array of players
        List<Player> allPlayers = GameManager.GetAllPlayers();
        GameManager.GetAllPlayers();

        // loop through and set up a list item for each player
        foreach(Player player in allPlayers)
        {
            GameObject itemGo = Instantiate(playerScoreboardPrefab, playerScoreboardList);
            PlayerScoreboardItem item = itemGo.GetComponent<PlayerScoreboardItem>();
            if (item)
            {
                item.Setup(player.name, player.kills);
            }
        }
    }

    void OnDisable()
    {
        // clean up list of items
        foreach (Transform child in playerScoreboardList)
        {
            Destroy(child.gameObject);
        }
    }
}
