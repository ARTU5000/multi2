using Unity.Netcode;
using UnityEngine;

public class Poyectile : NetworkBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;
    public PlayerController instigator; //quien disparo el proyectil
    public Vector3 direction;
    public float damage = 55;
    public GameObject impactPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            lifeTime -= Time.deltaTime;

            if (lifeTime <= 0)
                GetComponent<NetworkObject>().Despawn();

            //el servidor tiene la autoridad sobre el proyectil

            transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController otherPlayer = other.GetComponent<PlayerController>();
        if (otherPlayer != null && otherPlayer != instigator)
        {
            otherPlayer.TakeDamage((int)damage);
            OnImpactRPC();
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void OnImpactRPC()
    {
        GameObject impact = Instantiate(impactPrefab, transform.position, Quaternion.identity);
        Destroy(impact, 2f);
    }
}
