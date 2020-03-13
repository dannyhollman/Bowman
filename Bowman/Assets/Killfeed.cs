using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Killfeed : MonoBehaviour
{
    [SerializeField]
    GameObject killfeed;

    void Start()
    {
        GameManager.instance.onKillCallback += Kill;
    }

    public void Kill(string player, string source)
    {
        GameObject temp = Instantiate(killfeed, this.transform);
        temp.GetComponent<Killholder>().Setup(player, source);

        Destroy(temp, 3f);
    }
}
