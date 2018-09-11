using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunCorona : MonoBehaviour
{

    Material mat;
    Vector2 offset;
    public Vector2 speed = new Vector2(1, 1); 
	void Start () {
        mat = GetComponent<Renderer>().sharedMaterial;
	}
	
	// Update is called once per frame
	void Update () {
        offset += speed * Time.deltaTime;
        mat.SetTextureOffset("_MainTex", offset);
	}
}
