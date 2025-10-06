using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Photon.Pun;
using BepInEx.Configuration;


namespace shijian;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    private bool isSpectating = false;

    private Vector3 pos;


    internal static ManualLogSource Log { get; private set; } = null!;
    private ConfigEntry<KeyCode> toggleGhostKey;
    private ConfigEntry<bool> needCampfire;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"Plugin {Name} is loaded!");
        toggleGhostKey = Config.Bind("Hotkeys", "ToggleGhostMode", KeyCode.G, "进入鬼魂模式的按键");
        needCampfire = Config.Bind("Settings", "NeedCampfire", true, "是否需要在篝火范围内才能进入鬼魂模式");

        Logger.LogInfo($"配置的鬼魂模式热键是：{toggleGhostKey.Value}");
         Logger.LogInfo($"需要在篝火范围内：{needCampfire.Value}");
    }

    private void Update()
    {

        if (isSpectating)
        {
            if (IsAllCharactersPassOut() || Input.GetKeyDown(toggleGhostKey.Value)) TryExitGhost();
        }
        else
        {
            if (Input.GetKeyDown(toggleGhostKey.Value)) TryEnterGhost();
        }
    }

    private void TryEnterGhost()
    { 
        Character localCharacter = Character.localCharacter;
        if (localCharacter == null || localCharacter.data.dead) return;
        if (!IsInCampfireRange(localCharacter.Center)) return;
        if (PlayerHandler.GetAllPlayerCharacters().Count<= 1) return;

        PhotonView photonView = localCharacter.GetComponent<PhotonView>();
        if (!photonView.IsMine) return;
        pos = localCharacter.Center;
        photonView.RPC("RPCA_Die", RpcTarget.All, pos);
        isSpectating = true;
        Debug.Log("Set isSpectating = true");
    }

    private void TryExitGhost()
    {
        Character localCharacter = Character.localCharacter;
        if (localCharacter == null) return;

        PhotonView photonView = localCharacter.GetComponent<PhotonView>();
        if (!photonView.IsMine) return;

        photonView.RPC("RPCA_ReviveAtPosition", RpcTarget.All, pos, false);

        isSpectating = false;
        Debug.Log("Set isSpectating = false");
    }
    private bool IsAllCharactersPassOut()
    {
        bool flag = true;
        foreach (Character character in PlayerHandler.GetAllPlayerCharacters())
        {
            if (!character.data.passedOut)
            {
                flag = false;
                break;
            }
        }
        return flag;
    }

    private bool IsInCampfireRange(Vector3 position)
    {
        bool flag = false;
        var allCampfires = FindObjectsByType(typeof(Campfire), FindObjectsSortMode.None) as Campfire[];
        foreach (Campfire campfire in allCampfires)
        {
            if (campfire.Lit) continue;
            float num = Vector3.Distance(campfire.transform.position, position);
            if (num <= 15f)
            {
                Debug.Log("In campfire range: " + num);
                flag = true;
                break;
            }
            else
            {
                Debug.Log("Not in campfire range: " + num);
            }
        }
        if (!needCampfire.Value) flag = true;
        return flag;
    }
}
