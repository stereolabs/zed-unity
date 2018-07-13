using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DestroyAfterTime : MonoBehaviour
{
    public float DeathClock = 2f;

	// Update is called once per frame
	void Update ()
    {
        DeathClock -= Time.deltaTime;

        if(DeathClock <= 0f)
        {
            Destroy(gameObject);
        }
	}
}
