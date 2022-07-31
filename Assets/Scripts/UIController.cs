using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//テキストメッシュプロを使う
using TMPro;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    //Singleton
    //他のControllerからアクセスできるようになる
    public static UIController instance;

    public Slider weaponTempSlider;
    public TMP_Text overheatedMessage;

    public GameObject deathScreen;
    public TMP_Text deathText;
    public TMP_Text killsAmountText;
    public TMP_Text deathsAmountText;

    public Slider playerHealthSlider;


    //ObjectがOnになった時に起動する。AwakeはStartより先に起動するため、インスタンスの起動をするのに向いてる
    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        deathScreen.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
