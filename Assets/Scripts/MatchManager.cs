using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

//EventのCallを監視できる
//Eventの読み取りとかがないとエラーになる
//デフォルトだと、Unity側からはPUblicにしても見えない。単に保存するためのものなので、これを使って何かをする、というわけではない。
public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;
    private void Awake()
    {
        instance = this;
    }

    //どのEventを送るのか
    //byteはIntよりも小さく、データとして送るのに適している
    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat,
    }

    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    //ListのなかのIndex
    private int index;

    //EventCodesをUnity側から確認してみる
    public EventCodes theEvent;

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            //接続されてない場合にMapに遷移していたら、MainMenuに戻る
            Debug.Log("PhotonNetwork.IsConnected: " + PhotonNetwork.IsConnected + " So, we let you move to Main Menu");
            SceneManager.LoadScene(0);
        }
        else
        {
            //接続されていたら、NewPlayerの情報をMasterClientに送信
            NewPlayerSend(PhotonNetwork.NickName);
        }

    }

    // Update is called once per frame
    void Update()
    {
        //PlayerInfo p1 = new PlayerInfo("Bob", 0, 5, 3);
    }

    //EventCallbackで起動する
    public void OnEvent(EventData photonEvent)
    {
        //200と比較するのは、Codeは256までしかない。Photonは200以上のコードをデフォルトで使用している
        if (photonEvent.Code < 200)
        {
            //byteのタイプに変換するのはだめ
            //EventCodes theEvent = photonEvent.Code;
            //Castingを行う
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            //それぞれのデータ形式に合わせて
            object[] data = (object[])photonEvent.CustomData;

            Debug.Log("Received" + theEvent);

            switch (theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;

                case EventCodes.ListPlayers:
                    ListPlayersReceive(data);
                    break;

                case EventCodes.UpdateStat:
                    UpdateStatsReceive(data);
                    break;
            }
        }
    }
    //UnityにBuiltinのFunc
    public override void OnEnable()
    {
        //Networkに入る→OnEventの起動
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    //それぞれのClientに送受信する
    //Send先はMasterClientで、MasterClientからそれ以外のPlayerに通知できる
    public void NewPlayerSend(string username)
    {
        //4つのスロットを作る、 PlayerInfoのデータが4つあるため(string _name, int _actor, int _kills, int _deaths)
        object[] package = new object[4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        //新規プレイヤーのため、Kills, Deathsは0
        package[2] = 0;
        package[3] = 0;

        //実際にSendする
        PhotonNetwork.RaiseEvent(
            //EventCodeはByte形式で
            //EventCodes.NewPlayer,
            (byte)EventCodes.NewPlayer,
            package,
            //受け取るのはMasterClientだけ
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            //送信が必須になる（？）
            new SendOptions { Reliability = true }
            );
    }
    //dataTypeはObject配列
    public void NewPlayerReceive(object[] dataReceived)
    {
        //dataはObjectArrayで、内部の型は見れてない
        //PlayerInfo player = new PlayerInfo(dataReceived[0], dataReceived[1], dataReceived[2], dataReceived[3]);
        PlayerInfo player = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);
        allPlayers.Add(player);

        //Playerが入る度に更新していく
        ListPlayersSend();
    }
    public void ListPlayersSend()
    {
        //player一覧
        object[] package = new object[allPlayers.Count];
        //Player情報を一つ一つ送信していく
        for (int i = 0; i < allPlayers.Count; i++)
        {
            //piece stands for piece Of Package　Player一覧の中の、Player一人ひとり
            object[] piece = new object[4];
            piece[0] = allPlayers[i].name;
            piece[1] = allPlayers[i].actor;
            piece[2] = allPlayers[i].kills;
            piece[3] = allPlayers[i].deaths;
            //packageにPieceを入れる
            package[i] = piece;
        }
        //実際にSendする
        PhotonNetwork.RaiseEvent(
            //EventCodeはByte形式で
            (byte)EventCodes.ListPlayers,
            package,
            //受け取るのは全員
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            //送信が必須になる（？）
            new SendOptions { Reliability = true }
            );
    }
    public void ListPlayersReceive(object[] dataReceived)
    {
        //情報を更新するため一旦削除
        //allPlayers.Clear();
        for (int i = 0; i < dataReceived.Length; i++)
        {
            object[] piece = (object[])dataReceived[i];
            PlayerInfo player = new PlayerInfo(
                (string)piece[0],
                (int)piece[1],
                (int)piece[2],
                (int)piece[3]
                );
            allPlayers.Add(player);
            //自分自身のIndexを決めて保持しておく
            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i;
            }

            //自分で作ってみたやつ
            //Data変更があった際にエラーが出なくなりそう？なのでよくないかも、面倒だけど1つずつPieceを挿入していく方が確実
            //allPlayers[i] = (PlayerInfo)dataReceived[i];

        }
    }
    //actorSending = どのPlayerがStatsを更新したか？どのPlayerのStatsの更新が必要か？
    //statToUpdate = どのStatsを更新すべきか？
    /**
     * kills = 0
     * deaths = 1
     * 
     */
    //amountToChange = どのくらい変更すれば良いか？
    //DealDamage-PunRPCで全員に伝える-TakeDamage-currentHealth<0-MatchManagerのUpdateStatsSend(actor誰が送ってるか,KillStat、0,AmountToChange)-UpdateStatsReceived
    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange)
    {
        object[] package = new object[] { actorSending, statToUpdate, amountToChange };
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }
    public void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statType = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].actor == actor)
            {
                switch (statType)
                {
                    case 0: //kills
                        allPlayers[i].kills += amount;
                        Debug.Log("Player" + allPlayers[i].name + ": kills " + allPlayers[i].kills);
                        break;
                    case 1: //deaths
                        allPlayers[i].deaths += amount;
                        Debug.Log("Player" + allPlayers[i].name + ": kills " + allPlayers[i].deaths);
                        break;
                }
                //Forloopが自分の順番になったら表示を更新
                if (i == index)
                {
                    UpdateStatsDisplay();
                }
                //1回カウントしたらForLoopから抜ける
                break;
            }
        }

    }
    public void UpdateStatsDisplay()
    {
        if (allPlayers.Count > index)
        {
            //index = playerの番号
            UIController.instance.killsAmountText.text = "kills: " + allPlayers[index].kills;
            UIController.instance.deathsAmountText.text = "deaths: " + allPlayers[index].deaths;
        }
        //playerの人数がバグったら
        else
        {
            UIController.instance.killsAmountText.text = "kills: 0";
            UIController.instance.deathsAmountText.text = "deaths: 0";
            UIController.instance.deathsAmountText.text = "deaths: 0";
        }
    }
}
//Unity側でも表示できるようにする
[System.Serializable]
public class PlayerInfo
{
    public string name;
    //Network上の数字データ
    public int actor, kills, deaths;

    public PlayerInfo(string _name, int _actor, int _kills, int _deaths)
    {
        name = _name;
        actor = _actor;
        kills = _kills;
        deaths = _deaths;
    }
}