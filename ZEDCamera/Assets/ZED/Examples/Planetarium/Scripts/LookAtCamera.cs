using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour {

    public Transform target;
    bool canLook = false;

	void Update () {
        if(!canLook && transform.position - target.position != Vector3.zero)
            canLook = true;

        if(canLook)
        transform.rotation = Quaternion.LookRotation(transform.position - target.position);
	}
}
