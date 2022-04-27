using System;
using TMPro;
using UnityEngine;

public enum CameraAngle
{
    menu = 0,
    whiteTeamOrPlayer1 = 1,
    blackTeamOrPlayer2 = 2
}

public class Game_ui : MonoBehaviour
{
    public static Game_ui Instance { set; get;}

    public Server server;
    public Client client;

    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private GameObject[] cameraAngles;

    public Action<bool> SetLocalGame;

    private void Awake()
    {
        Instance = this;
        RegisterEvents();
    }

    //Cameras
    public void ChangeCamera(CameraAngle index)
    {
        for (int i = 0; i < cameraAngles.Length; i++)
            cameraAngles[i].SetActive(false);

        cameraAngles[(int)index].SetActive(true);
    }

    //Buttons
    public void OnLocalGameGutton()
    {
        //Debug.Log("OnLocalGameButton");
        menuAnimator.SetTrigger("InGameMenu");
        SetLocalGame?.Invoke(true);
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
    }
    public void OnOnlineGameButton()
    {
        //Debug.Log("OnOnlineGameButton");
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnOnlineHostButton()
    {
        SetLocalGame?.Invoke(false);
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
        //Debug.Log("OnOnlineHostButton");
        menuAnimator.SetTrigger("HostMenu");
    }

    public void OnOnlineConnectButton()
    {
        SetLocalGame?.Invoke(false);
        client.Init(addressInput.text, 8007);
        //Debug.Log("OnOnlineConnectButton"); $$$$
    }

    public void OnOnlineBackButton()
    {
        //Debug.Log("OnOnlineBackButton");
        menuAnimator.SetTrigger("StartMenu");
    }

    public void OnHostBackButton()
    {
        server.Shutdown();
        client.Shutdown();
        //Debug.Log("OnOnlineBackButton");
        menuAnimator.SetTrigger("OnlineMenu");
    }

    //
    #region
    private void RegisterEvents()
    {
        NetUtility.C_START_GAME += OnStartGameClient;
    }
    private void UnRegisterEvents()
    {
        NetUtility.C_START_GAME -= OnStartGameClient;
    }
    private void OnStartGameClient(NetMessage obj)
    {
        menuAnimator.SetTrigger("InGameMenu");
    }
    #endregion
}

