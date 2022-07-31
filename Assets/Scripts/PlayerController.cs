using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    // rotate a player and viewpoint(children)
    // publicにしてUnityから参照可能に
    public Transform viewPoint;
    // 
    public float mouseSensitivity = 5f;
    public float activeMoveSpeed, runSpeed = 8f;
    // viewPointの回転の限界値
    private float verticalRotationStore;
    // X, Y＝横と縦
    private Vector2 mouseInput;
    // 逆向きにするかどうかをCheck
    public bool ReverseLook = false;

    public float moveSpeed = 5f;
    // 二つ同時に定義出来る
    private Vector3 moveDirection, movement;
    // private Vector3 movement;


    public CharacterController charCon;

    // プレイヤーがいない時にカメラ生成とかも可能にできる（？）
    private Camera cam;

    //ジャンプの力、ジャンプの際の重力を滑らかにする
    public float jumpForce = 20f, gravityModifier = 5f;
    //地面接触判定
    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    //GameObject設定
    public GameObject bulletImpact;
    // 自動で撃てるように、射撃間隔を設定。
    //public float timeBetweenShots = .1f;
    // 射撃間隔を調べる
    private float shotCounter;

    //MuzzleFlashの表示時間。使わないけど表示しておくやつ
    public float muzzleDisplayTime = 1/60;
    //
    private float muzzleCounter;

    //射撃のオーバーヒート
    //CoolRateはオーバーヒートなしで撃ってない時の冷却スピードアップ。オーバーヒートありの時はゆっくり
    public float maxHeat = 10f,/*heatPerShot = 1f,*/ coolRate = 5f, overheatCoolRate = 4f;

    private float heatCounter;
    private bool overHeated;

    // Arrayを使う
    public Gun[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;

    public int maxHealth = 100;
    private int currentHealth;

    public Animator animator;

    public GameObject playerModel;
    public Transform modelGunPoint, gunHolder;


    // Start is called before the first frame update
    void Start()
    {
   
        mouseSensitivity = 5f;
        // Cursorを見えなくする→Userの画面外の挙動に影響されにくい
        Cursor.lockState = CursorLockMode.Locked;
        // Find main camera
        cam = Camera.main;

        //SwitchGun();
        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        //playerSpawnerに移行したため不要
        //Transform newTransform = SpawnManager.instance.GetSpawnPoint();
        //transform.position = newTransform.position;
        //transform.rotation = newTransform.rotation;
        currentHealth = maxHealth;

        if(photonView.IsMine)
        {
            //自分の体が見えなくなるようにする
            playerModel.SetActive(false);
            //自分以外のPlayerがRespawnしたときに更新されないようにする
            // WeaponTempSliderのMaxValueを設定（AwakeのInstance後に行う為、Startないで行う。）
            UIController.instance.weaponTempSlider.maxValue = maxHeat;
            UIController.instance.playerHealthSlider.value = currentHealth;

        }
        //他のプレイヤーのモデルの位置を変更して見えるようにする
        else
        {
            //gunHolderの場所をキャラモデルのGunをいい感じに配置できる場所に変更
            gunHolder.parent = modelGunPoint;
            //PositionとRotationをZeroにする
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }


    }

    // Update is called once per frame
    void Update()
    {
        // カメラがついてくる
        LateUpdate();
        if (photonView.IsMine)
        {
            // Input
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
            // Rotate、MouseInput.X＝横軸の動き
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            // Reverse LookはデフォルトでFalseにしてるからElseが動くけどカスタマイズ性あり。
            if (ReverseLook)
            {
                verticalRotationStore += mouseInput.y;
            }
            else
            {
                verticalRotationStore -= mouseInput.y;
            }
            // Mathf.Clamp()数値を制限＝回転の限界を設定
            verticalRotationStore = Mathf.Clamp(verticalRotationStore, -60f, 60f);
            //
            viewPoint.rotation = Quaternion.Euler(verticalRotationStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);

            // 動き
            moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            // LeftShiftでスピードアップ
            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeMoveSpeed = runSpeed;
            }
            else
            {
                activeMoveSpeed = moveSpeed;
            }


            // 重力をうまくかけられるように
            // Update内で定義すると使い捨ての定義に
            float yVelocity = movement.y;

            movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized * activeMoveSpeed;

            // 地面についていない時だけStoreしている（＝重力のかかった）Yのスピードが適用される
            if (!charCon.isGrounded)
            {
                movement.y = yVelocity;
            }
            // 地面についた瞬間にResetする
            // if(charCon.isGrounded)
            // {
            //     movement.y = 0f;
            // }
            // Raycast（Start Point、Direction To Raycast、How Longエアリーなジャンプを実現する、レイヤーRestart Point）
            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);
            // デフォルトのInputManagerを使用
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                movement.y = jumpForce;
            }
            movement.y += Physics.gravity.y * Time.deltaTime * gravityModifier;


            // これだと、1Frame毎にMoveSpeed分動くことに＝Frameのスピードはクライアント側によって異なる
            // transform.position += moveDirection * moveSpeed;
            charCon.Move(movement * Time.deltaTime);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
            //
            if (allGuns[selectedGun].muzzuleFlash.activeInHierarchy)
            {
                //Active状態の時は
                muzzleCounter -= Time.deltaTime;
                if (muzzleCounter <= 0)
                {
                    //このスクリプトの下にあるShoot等全てが実行された後にMuzzleFlashが止まるようにする→1Frameは確実に表示されてほしい
                    allGuns[selectedGun].muzzuleFlash.SetActive(false);
                }
            }
            //オーバーヒート中は撃てない
            if (!overHeated)
            {
                //MouseCLickの直後
                if (Input.GetMouseButtonDown(0))
                {
                    Shoot();
                }
                // 時間経過を測定、isAutomaticがTrueのものだけ長押しで撃てる
                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
                {
                    shotCounter -= Time.deltaTime;
                    // 
                    if (shotCounter <= 0)
                    {
                        Shoot();
                    }
                }
                //クールダウン
                heatCounter -= coolRate * Time.deltaTime;

            }
            else
            {
                //クールダウン
                heatCounter -= overheatCoolRate * Time.deltaTime;
                if (heatCounter <= overheatCoolRate)
                {
                    //heatCounterをリセット、オーバーヒート終了
                    heatCounter = 0;
                    overHeated = false;
                    //UIControllerに分離
                    UIController.instance.overheatedMessage.gameObject.SetActive(false);

                }
            }
            if (heatCounter < 0f)
            {
                heatCounter = 0f;
            }
            UIController.instance.weaponTempSlider.value = heatCounter;

            // マウスScrollで武器切り替え
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                //武器Arrayの次の武器を選択する
                selectedGun++;
                //allGunsの長さは3
                if (selectedGun >= allGuns.Length)
                {
                    //
                    selectedGun = 0;
                }
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;
                if (selectedGun < 0)
                {
                    selectedGun = allGuns.Length - 1;
                }
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);

            }
            for (int i = 0; i < allGuns.Length; i++)
            {
                //if (Input.GetKeyDown(i))
                //プログラム上は0からスタートするけど、プレイヤー側には１、２、３で切り替えできるように
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedGun = i;
                    //SwitchGun();
                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }
            }
        }

        //boolにセット
        animator.SetBool("grounded", isGrounded);
        //moveDirection.xとかだとXの値が使われるけど、マイナス（左に動く）になると判定できなくなる。
        //Magnitudeを使うとNormalizedされて単純にどのくらいの数値になったか？を渡せる
        animator.SetFloat("speed", moveDirection.magnitude);
        //Debug.Log(moveDirection.magnitude);

    }
    // PlayerControllerでしか使わないためPrivate
    private void Shoot()
    {
        allGuns[selectedGun].muzzuleFlash.SetActive(true);
        //実態として銃弾は早すぎて見えない→Shootの瞬間と銃弾がぶつかった瞬間だけで良い？
        //Pick a Point within a camera
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        //Ray　Cameraの場所がOrigin
        // hitするとHitしたものの情報を取得する
        ray.origin = cam.transform.position;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {

            if(hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                //PhotonNetworkでConnectされてるPlayer全員に見えるように
                //HitPointはUnityのデフォルトで使える
                //Quaternion.identityは便利なやつ
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                //全員にDealDamageFunctionの影響を反映する
                //DealDamage-PunRPCで全員に伝える-TakeDamage-currentHealth<0-MatchManagerのUpdateStatsSend(actor誰が送ってるか,KillStat、0,AmountToChange)-UpdateStatsReceived
                hit.collider.gameObject.GetPhotonView().RPC(
                    "DealDamage",
                    RpcTarget.All, photonView.Owner.NickName,
                    allGuns[selectedGun].shotDamage,
                    PhotonNetwork.LocalPlayer.ActorNumber
                    );
            }
            else
            {
                Debug.Log("We hit " + hit.collider.gameObject.name);
            }

            //InstantiateでBulletImpactを複製、hit.pointが場所、角度はQuaternion、hit.normalの向いてる方向
            // 表面＝LookRotationをhit.normalにする
            GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
            Destroy(bulletImpactObject, 5f);
        }
        // Shootする度に.1fを代入する
        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;
        if (heatCounter >= maxHeat)
        {
            //限界値で止まるように
            //heatCounter = maxHeat;
            overHeated = true;
            //毎回Findするのは面倒臭い
            //FindObjectOfType<UIController>().overheatedMessage.gameObject.SetActive(true);
            UIController.instance.overheatedMessage.gameObject.SetActive(true);


        }
    }
    //どんなバージョンでも動作する　リモートプロシージャコール。ネットワーク上の別の端末の関数等を呼び出す仕組み。全Playerに同時に反映させる
    //自分が撃たれたと全プレイヤーに伝える
    //Damager　ダメージを与えた人
    //damageAmount
    //RPCは引数の数でエラーを吐けない？ので追加に注意
    //DealDamage-PunRPCで全員に伝える-TakeDamage-currentHealth<0-MatchManagerのUpdateStatsSend(actor誰が送ってるか,KillStat、0,AmountToChange)-UpdateStatsReceived
    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor)
    {
        TakeDamage(damager, damageAmount, actor);

    }
    //DealDamage-PunRPCで全員に伝える-TakeDamage-currentHealth<0-MatchManagerのUpdateStatsSend(actor誰が送ってるか,KillStat、0,AmountToChange)-UpdateStatsReceived
    public void TakeDamage(string damager, int damageAmount, int actor)
    {
        if(photonView.IsMine)
        {
            Debug.Log(photonView.Owner.NickName + "has been Hit by" + damager);
            //gameObject.SetActive(false);

            currentHealth -= damageAmount;
            Debug.Log(currentHealth);
            UIController.instance.playerHealthSlider.value = currentHealth;
            if (currentHealth <= 0)
            {
                //マイナスになったHPの数値をプレイヤーに見せたくない
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);
                MatchManager.instance.UpdateStatsSend(actor, 0, 1);
            }
        }
    }
    

    /**
     * カメラがviewPointの位置と回転についてくる
     */
    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            // viewPointと同じ座標、角度
            cam.transform.position = viewPoint.position;
            cam.transform.rotation = viewPoint.rotation;

        }
    }

    void SwitchGun()
    {
        //Gunを探す
        foreach(Gun gun in allGuns)
        {
            //全てのgunをSetActiveする
            gun.gameObject.SetActive(false);
        }
        allGuns[selectedGun].gameObject.SetActive(true);
        //Switchした瞬間の射撃で発生するMuzzleFlashをInactiveにして、変な挙動を無くす
        allGuns[selectedGun].muzzuleFlash.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gunSwitchTo)
    {
        //例外的な事象を防ぐ
        if(gunSwitchTo < allGuns.Length)
        {
            selectedGun = gunSwitchTo;
            SwitchGun();
        }
    }
}
