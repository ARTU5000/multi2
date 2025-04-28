using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class DamageVolume : MonoBehaviour
{
    public float initialDamage = 20;
    //daño causado a cada paso
    public float damagePerStep = 10;
    //cada cantos segndos causa daño
    public float damageRate = 0.5f;

    private float damagetimer = 0;

    //como es un jugo multiplayer, puede ser que haya varios jugadores dentro del volumen
    public List<PlayerController> players;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        damagetimer += Time.deltaTime;
    }

    public void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            Debug.Log(other + " entro");
            pc.TakeDamage((int)initialDamage);

            players.Add(pc);
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (damagetimer > damageRate)
        {
            foreach (PlayerController pc in players)
            {
                pc.TakeDamage((int)damagePerStep);
            }
            damagetimer = 0;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        Debug.Log(other + " se salio");
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            players.Remove(pc);
        }
    }
}
