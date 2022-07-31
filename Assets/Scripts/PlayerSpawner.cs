using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

//Playerを出したり、死んだときに消したり
public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner instance;
    private GameObject player;

    public GameObject deatchEffect;
    public float respawnTime = 5f;

    public void Awake()
    {
        instance = this;
    }

    public GameObject playerPrefab;

    // Start is called before the first frame update
    void Start()
    {
        //最初にSpawnする
        if(PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();
        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);

    }
    //DealDamage-PunRPCで全員に伝える-TakeDamage-currentHealth<0-Die()-MatchManagerのUpdateStatsSend(actor誰が送ってるか,DeathStatの番号、1,AmountToChange)-UpdateStatsReceived
    public void Die(string damager)
    {
        UIController.instance.deathText.text = "You were killed by" + damager;
        MatchManager.instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);


        //playerが2回Killされる、ということを防ぐ
        if(player != null)
        {
            StartCoroutine(DieCoroutine());
        }
    }

    //倒された時に独立した処理で行う
    public IEnumerator DieCoroutine()
    {
        //Playerが倒された時のパーティクルシステム
        PhotonNetwork.Instantiate(deatchEffect.name, player.transform.position, Quaternion.identity);


        PhotonNetwork.Destroy(player);
        UIController.instance.deathScreen.SetActive(true);
        player = null;
        //Yield　待つ
        //WaitForSeconds＝Coroutineを止める
        yield return new WaitForSeconds(respawnTime);

        UIController.instance.deathScreen.SetActive(false);

        SpawnPlayer();
    }
}
