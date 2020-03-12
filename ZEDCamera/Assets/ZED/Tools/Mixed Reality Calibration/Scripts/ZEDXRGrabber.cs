using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Checks for trigger colliders with IXRHoverable components added, and lets user click or grab them if they
/// have IXRClickable or IXRGrabbable scripts on them. 
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZEDXRGrabber : MonoBehaviour
{
    public ZEDControllerTracker_DemoInputs zedController;

    private IXRHoverable hoveringObject;
    private IXRGrabbable grabbingObject;

    private bool isGrabbing
    {
        get
        {
            return grabbingObject != null;
        }
    }

    private List<IXRHoverable> collidingHoverables = new List<IXRHoverable>();
    private List<IXRClickable> collidingClickables = new List<IXRClickable>();
    private List<IXRGrabbable> collidingGrabbables = new List<IXRGrabbable>();

    [Space(5)]
    [Header("Sounds")]
    public AudioClip clickSound;
    public AudioClip grabStartSound;
    public AudioClip grabEndSound;
    public AudioClip hoverStartSound;

    // Use this for initialization
    void Start()
    {
        if (!zedController) zedController = GetComponentInParent<ZEDControllerTracker_DemoInputs>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckListElementsForValidity();
        CheckHovers();
    }

    private void OnTriggerEnter(Collider collision)
    {
        IXRHoverable newhoverable = collision.gameObject.GetComponent<IXRHoverable>();
        if (newhoverable != null && !collidingHoverables.Contains(newhoverable)) collidingHoverables.Add(newhoverable);

        IXRClickable newclickable = collision.gameObject.GetComponent<IXRClickable>();
        if (newclickable != null && !collidingClickables.Contains(newclickable)) collidingClickables.Add(newclickable);

        IXRGrabbable newgrabbable = collision.gameObject.GetComponent<IXRGrabbable>();
        if (newgrabbable != null && !collidingGrabbables.Contains(newgrabbable)) collidingGrabbables.Add(newgrabbable);
    }

    private void OnTriggerExit(Collider collision)
    {
        IXRHoverable newhoverable = collision.gameObject.GetComponent<IXRHoverable>();
        if (newhoverable != null && collidingHoverables.Contains(newhoverable)) collidingHoverables.Remove(newhoverable);

        IXRClickable newclickable = collision.gameObject.GetComponent<IXRClickable>();
        if (newclickable != null && collidingClickables.Contains(newclickable)) collidingClickables.Remove(newclickable);

        IXRGrabbable newgrabbable = collision.gameObject.GetComponent<IXRGrabbable>();
        if (newgrabbable != null && collidingGrabbables.Contains(newgrabbable)) collidingGrabbables.Remove(newgrabbable);
    }

    private void CheckHovers()
    {
        //If we're grabbing something, we shouldn't be hovering over anything. 
        if(grabbingObject != null)
        {
            if(hoveringObject != null)
            {
                if(!Equals(hoveringObject, null)) hoveringObject.OnHoverEnd();
                hoveringObject = null;
            }
            return;
        }


        float nearestdist = Mathf.Infinity;
        IXRHoverable newhover = null;
        foreach(IXRHoverable hover in collidingHoverables)
        {
            float dist = Vector3.Distance(transform.position, hover.GetTransform().position);
            if (dist < nearestdist) newhover = hover;
        }

        if(hoveringObject != newhover)
        {
            if (hoveringObject != null && !hoveringObject.Equals(null))
            {
                hoveringObject.OnHoverEnd();
            }
            if(newhover != null)
            {
                PlaySoundIfValid(hoverStartSound);
                newhover.OnHoverStart();
            }

            hoveringObject = newhover;
        }
    }

    /// <summary>
    /// Checks the interface lists to make sure that they all still exist. Otherwise, if something is destroyed
    /// or disabled while still in range, we'll still try to interact with it. 
    /// </summary>
    private void CheckListElementsForValidity()
    {
        List<IXRHoverable> toremove = new List<IXRHoverable>();
        foreach(IXRHoverable hoverable in collidingHoverables)
        {
            if(hoverable.Equals(null))
            {
                toremove.Add(hoverable);
                continue;
            }
            Collider col = hoverable.GetTransform().GetComponent<Collider>();
            if(col == null || col.enabled == false)
            {
                toremove.Add(hoverable);
            }
        }

        foreach (IXRHoverable deadhoverable in toremove)
        {
            collidingHoverables.Remove(deadhoverable);
        }
    }

    public void TryClick()
    {
        if (hoveringObject == null) return;

        IXRClickable newclick = hoveringObject.GetTransform().GetComponent<IXRClickable>();
        if (newclick == null) return;

        PlaySoundIfValid(clickSound);
        newclick.OnClick(this);
    }

    public void TryGrab()
    {
        if (grabbingObject != null)
        {
            Debug.Log("Somehow grabbed something while already grabbing something else. Calling ReleaseGrab on that object.");
            ReleaseGrab();
        }

        if (hoveringObject == null) return;

        IXRGrabbable newgrab = hoveringObject.GetTransform().GetComponent<IXRGrabbable>();
        if (newgrab == null) return;

        if (!Equals(hoveringObject, null)) hoveringObject.OnHoverEnd();
        hoveringObject = null;

        newgrab.OnGrabStart(transform);
        grabbingObject = newgrab;

        PlaySoundIfValid(grabStartSound);
    }

    public void ReleaseGrab()
    {
        if (grabbingObject == null) return;

        grabbingObject.OnGrabEnd();
        grabbingObject = null;

        PlaySoundIfValid(grabEndSound);
    }

    /// <summary>
    /// Plays the given clip, but in a temporary gameobject that appears where the ball is. 
    /// This lets the sound finish playing even if this object is disabled, such as if you press
    /// the switch hands button. 
    /// </summary>
    private void PlaySoundIfValid(AudioClip clip)
    {
        if (clip == null) return;

        //audioSource.PlayOneShot(clip);

        GameObject soundgo = new GameObject("Temp Audio Object: " + clip.name);
        soundgo.transform.position = transform.position;
        TempAudioObject sound = soundgo.AddComponent<TempAudioObject>();
        sound.Setup(clip);
    }

    private void OnDisable()
    {
        if(hoveringObject != null)
        {
            if (!Equals(hoveringObject, null)) hoveringObject.OnHoverEnd();
            hoveringObject = null;
        }

        if(grabbingObject != null)
        {
            grabbingObject.OnGrabEnd();
            grabbingObject = null;
        }

        collidingHoverables.Clear();
        collidingGrabbables.Clear();
        collidingClickables.Clear();
    }
}


public interface IXRHoverable
{
    Transform GetTransform();

    void OnHoverStart();

    void OnHoverEnd();
}

public interface IXRClickable
{
    Transform GetTransform();

    void OnClick(ZEDXRGrabber clicker);
}

public interface IXRGrabbable
{
    Transform GetTransform();

    void OnGrabStart(Transform grabtransform);

    void OnGrabEnd();
}
