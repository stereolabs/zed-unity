using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunBursts : MonoBehaviour {

    public List<GameObject> sunBurstsGO = new List<GameObject>();
	// Use this for initialization
	IEnumerator Start () {

        for (int i = 0; i < sunBurstsGO.Count; i++)
        {
            yield return new WaitForSeconds(2f);
            sunBurstsGO[i].SetActive(true);
        }
	}
}
