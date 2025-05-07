using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.Netcode.Components;
using TMPro;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    //hacia donde apunte el character
    Vector3 desiredDirection;
    public float speed = 1.0f;

    //salud del jugador
    //public int health = 100;

    GameManager gameManager;

    //networkvariable para replicar la vida
    NetworkVariable<int> health = new NetworkVariable<int> (100,NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private UiManager hud;
    TMP_Text playername;

    [Header("SFX")]
    public AudioClip DamageSound;
    public AudioClip DeathSound;
    AudioSource audioSource;
    Animator animator;
    NetworkAnimator NAnimator;

    //public int nameID;

    [Header("Camera")]
    public Vector3 cameraOffset = new Vector3(0, 4, -3);
    public Vector3 cameraViewOffset = new Vector3(0, 1.5f, 0);
    Camera cam;

    [Header("Weapon")]
    public GameObject proyectilePrefab;
    public Transform weaponSocket;
    public float weaponCadence = 0.8f;
    float lastShotTimer = 0;

    [Header("accesories")]
    public Transform hatSocket;
    public List<GameObject> prefabsHats = new List<GameObject>();
    bool hatSpawned = false;

    NetworkVariable<int> nameID = new NetworkVariable<int> (0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    NetworkVariable<int> hatID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        Debug.Log("Hola mundo soy un " + (IsClient? "cliente" : "servidor"));
        Debug.Log("IsClient = " + IsClient + ", IsServer = " + IsServer + ", IsHost = " + IsHost);
        Debug.Log(name + " Is owner " + IsOwner);

        hatID.OnValueChanged += SpawnHat;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hud = GameObject.Find("GameManager").GetComponent<UiManager>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        NAnimator = GetComponent<NetworkAnimator>();

        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        transform.position = gameManager.Spawn();

        //Asignar camara
        if (IsOwner)
        {
            cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            cam.transform.position = transform.position + cameraOffset;
            cam.transform.LookAt(transform.position + cameraViewOffset);

            SetNameIDRPC(hud.selectedNameIndex);
            SetHatIDRPC(hud.selectedHat);
        }

        if (!IsOwner)
        {
            SpawnHat(0, hatID.Value);
        }
        //nameID = hud.selectedNameIndex;
        CreatePlayerNameHUD();
    }

    void SpawnHat(int old, int newVal)
    {
        if (hatSpawned)
        {
            if (newVal != old) 
            {
                Destroy(hatSocket.GetChild(0).gameObject);
                hatSpawned = false;
            }
        }
        GameObject hat = Instantiate(prefabsHats[hatID.Value], hatSocket.position, hatSocket.rotation, hatSocket);
    }

    void CreatePlayerNameHUD()
    {
        if (IsClient)
        {
            playername = Instantiate(hud.playerNameTemplate, hud.PanelHUD).GetComponent<TMP_Text>();
            playername.gameObject.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {
            //inputs
            desiredDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            desiredDirection.Normalize();

            if (Input.GetButtonDown("Fire1"))
                FireWeaponRPC();

            if (IsAlive())
            {
                float mag = desiredDirection.magnitude;

                if (mag > 0)
                {
                    //transform.forward = desiredDirection;
                    //Interpolar entre la rotaci�n actual y la desesda

                    Quaternion q = Quaternion.LookRotation(desiredDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * 10);
                    transform.Translate(0, 0, speed * Time.deltaTime);
                    //animator.SetFloat("movement", mag);
                    Animate("movement", mag);
                }
                else
                {
                    //animator.SetFloat("movement", 0f);
                    Animate("movement", 0f);
                }
                

                //Prueba del sistema, de vida
                if (Input.GetKeyDown(KeyCode.K))
                {
                    TakeDamage(5);
                }
                
            }
            

            hud.labelHealth.text = health.Value.ToString();


            cam.transform.position = transform.position + cameraOffset;
            cam.transform.LookAt(transform.position + cameraViewOffset);
        }

        if (IsServer)
        {
            lastShotTimer += Time.deltaTime;
        }

        if (IsClient)
        {
            Camera mainCam = GameObject.Find("Main Camera").GetComponent<Camera>();
            playername.transform.position = mainCam.WorldToScreenPoint(transform.position + new Vector3(0, 1.2f, 0));
            playername.text = hud.namesList[nameID.Value];
        }
    }

    [Rpc(SendTo.Server)]
    public void AnimateRPC(string parameter, float value)
    {
        Animate(parameter, value);
    }

    public void Animate(string parameter, float value)
    {
        if (!IsServer)
        {
            AnimateRPC(parameter, value);
        }
        else
        {
            NAnimator.Animator.SetFloat(parameter, value);
        }
    }

    //RPC= 
    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(int amount)
    {
        Debug.Log("RPC recibido TakeDamage");
        TakeDamage(amount);
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive()) return;


        if (!IsServer) 
        {
          TakeDamageRpc(amount);
        }
        else
        {
            health.Value -= amount;

            if (health.Value <= 0)
            {
                health.Value = 0;

                if (!IsAlive())
                {
                    OnDeath();
                }
            }
            else
            {
                audioSource.clip = DamageSound;
                audioSource.Play();
            }
        }     
    }

    public void OnDeath()
    {
        //efectos
        Debug.Log(name + " se fue al lobby");
        audioSource.clip = DeathSound;
        audioSource.Play();
        animator.SetBool("dead", true);
        animator.SetFloat("movement", 0f);
        
        
    }

    public bool IsAlive()
    {
        return health.Value > 0;
    }

    [Rpc(SendTo.Server)]
    public void FireWeaponRPC()
    {
        if (lastShotTimer < weaponCadence) return;
      
        if (proyectilePrefab != null)
        {
            Poyectile proj = Instantiate(proyectilePrefab, weaponSocket.position, weaponSocket.rotation).GetComponent<Poyectile>();
            proj.direction = transform.forward;
            proj.instigator = this;

            proj.GetComponent<NetworkObject>().Spawn();
            lastShotTimer = 0;
        }
    }

    [Rpc(SendTo.Server)]
    public void SetNameIDRPC(int idx)
    {
        nameID.Value = idx;
    }

    [Rpc(SendTo.Server)]
    public void SetHatIDRPC(int idx)
    {
        hatID.Value = idx;
    }
}
