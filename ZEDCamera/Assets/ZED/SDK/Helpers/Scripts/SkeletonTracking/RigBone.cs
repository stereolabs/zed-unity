//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigBone {
  public GameObject gameObject;
  public HumanBodyBones bone;
  public bool isValid;

  public Transform transform {
    get { return animator.GetBoneTransform(bone); }
  }

 
  private Animator animator;
  private Quaternion savedValue;
  public RigBone(GameObject g, HumanBodyBones b) {
    gameObject = g;
    bone = b;
    isValid = false;
    animator = gameObject.GetComponent<Animator>();
 

    if (animator == null) {
      Debug.Log("no Animator Component");
      return;
    }
    Avatar avatar = animator.avatar;
    if (avatar == null || !avatar.isHuman || !avatar.isValid) {
      Debug.Log("Avatar is not Humanoid or it is not valid");
      return;
    }
    isValid = true;

    savedValue = animator.GetBoneTransform(bone).localRotation;
  }
  public void set(float a, float x, float y, float z) {
    set(Quaternion.AngleAxis(a, new Vector3(x,y,z)));
  }
  public void set(Quaternion q) {
    animator.GetBoneTransform(bone).localRotation = q;
    savedValue = q;
  }
  public void mul(float a, float x, float y, float z) {
    mul(Quaternion.AngleAxis(a, new Vector3(x,y,z)));
  }
  public void mul(Quaternion q) {
    Transform tr = animator.GetBoneTransform(bone);
    tr.localRotation = q * tr.localRotation;
  }
  public void offset(float a, float x, float y, float z) {
    offset(Quaternion.AngleAxis(a, new Vector3(x,y,z)));
  }
  public void offset(Quaternion q) {
    animator.GetBoneTransform(bone).localRotation = q * savedValue;
  }
  public void changeBone(HumanBodyBones b) {
    bone = b;
    savedValue = animator.GetBoneTransform(bone).localRotation;
  }
}
