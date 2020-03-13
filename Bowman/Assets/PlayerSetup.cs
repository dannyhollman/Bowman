using UnityEngine;
using UnityEngine.Networking;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField]
    Behaviour[] componentsToDisable;
    GameObject sceneCamera;
    [SerializeField]
    GameObject playerUIPrefab;
    [SerializeField]
    string dontDrawLayerName = "DontDraw";
    [SerializeField]
    GameObject playerGraphics;
    [HideInInspector]
    public GameObject playerUIInstance;

    void Start()
    {
        if (!isLocalPlayer)
        {
            for (int i = 0; i < componentsToDisable.Length; i++)
            {
                componentsToDisable[i].enabled = false;
            }
            AssignRemoteLayer();
        }
        else
        {
            //Disable player graphics for localplayer
            SetLayerRecursively(playerGraphics, LayerMask.NameToLayer(dontDrawLayerName));
            sceneCamera = GameObject.Find("Scene Camera");
            if (sceneCamera != null)
            {
                sceneCamera.SetActive(false);
            }
            //Create PlayerUI
            playerUIInstance = Instantiate(playerUIPrefab);
            playerUIInstance.name = playerUIPrefab.name;
            //Configure PlayerUI
            PlayerUI ui = playerUIInstance.GetComponent<PlayerUI>();
            if (ui == null)
                Debug.LogError("No playerui component on playerui prefab");
            ui.SetPlayer(GetComponent<Player>());
            GetComponent<Player>().SetupPlayer();
        }
    }
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    void AssignRemoteLayer()
    {
        gameObject.layer = LayerMask.NameToLayer("RemotePlayer");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        string _netID = GetComponent<NetworkIdentity>().netId.ToString();
        Player _player = GetComponent<Player>();
        GameManager.RegisterPlayer(_netID, _player);
    }

    void OnDisable()
    {
        Destroy(playerUIInstance);
        if (isLocalPlayer)
            GameManager.instance.SetSceneCameraActive(true);
        // if (sceneCamera != null)
        // {
        //     sceneCamera.gameObject.SetActive(true);
        // }
        GameManager.UnRegisterPlayer(transform.name);
    }
}
