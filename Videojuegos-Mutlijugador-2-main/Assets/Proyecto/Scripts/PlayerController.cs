using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    //hacia donde apunte el character
    Vector3 desiredDirection;
    public float speed = 1.0f;

    //salud del jugador
    //public int health = 100;

    //networkvariable para replicar la vida
    NetworkVariable<int> health = new NetworkVariable<int> (100,NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private UiManager hud;

    [Header("SFX")]
    public AudioClip DamageSound;
    public AudioClip DeathSound;
    AudioSource audioSource;
    Animator animator;

    public override void OnNetworkSpawn()
    {
        Debug.Log("Hola mundo soy un " + (IsClient? "cliente" : "servidor"));
        Debug.Log("IsClient = " + IsClient + ", IsServer = " + IsServer + ", IsHost = " + IsHost);
        Debug.Log(name + " Is owner " + IsOwner);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hud = GameObject.Find("GameManager").GetComponent<UiManager>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {
            desiredDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            desiredDirection.Normalize();

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
                    animator.SetFloat("movement", mag);
                }
                else
                {
                   animator.SetFloat("movement", 0f);
                }
                

                //Prueba del sistema, de vida
                if (Input.GetKeyDown(KeyCode.T))
                {
                    TakeDamage(5);
                }
                
            }
            

            hud.labelHealth.text = health.Value.ToString();
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
        Debug.Log(name + " me muero");
        audioSource.clip = DeathSound;
        audioSource.Play();
        animator.SetBool("dead", true);
        animator.SetFloat("movement", 0f);
        
        
    }

    public bool IsAlive()
    {
        return health.Value > 0;
    }
}
