using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    public TMP_Text buttonText;
    // Room情報
    private RoomInfo info;


    //Call fromLauncher　RoomButtonを複製する
    public void SetButtonDetails(RoomInfo inputInfo)
    {
        info = inputInfo;
        buttonText.text = info.Name;
    }

    public void OpenRoom()
    {
        //LauncherにButtonに表示されているInfoを渡す
        Launcher.instance.JoinRoom(info);
    }


}
