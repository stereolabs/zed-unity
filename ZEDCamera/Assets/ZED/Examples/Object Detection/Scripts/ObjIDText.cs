using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjIDText : MonoBehaviour
{
    public TextMesh text3D;
    public Text text2D;
    public Image backgroundImage;
    public Outline backgroundOutline;
    [Space(2)]
    public bool applyColorToText2D = false;
    public bool applyColorToBackgroundImage = true;
    public bool applyColorToBackgroundOutline = false;

    public Transform boxRoot;
    public float heightAboveBoxCeiling = 0.05f;

    private Vector3 startScale;

    [Space(5)]
    public bool showID = true;
    public bool showDistance = true;

    [Space(5)]
    public bool lookAtCamera = false;

    private int currentID = -1;
    private float currentDistance = -1f;

    // Start is called before the first frame update
    void Awake()
    {
        Camera.onPreRender += PreRender;

        if (!text3D) text3D = GetComponentInChildren<TextMesh>();
        if (!text2D) text2D = GetComponentInChildren<Text>();
        if (!boxRoot) boxRoot = transform.parent;

        startScale = transform.localScale;
    }

    public void SetID(int id)
    {
        currentID = id;
        UpdateText(currentID, currentDistance);
    }

    public void SetDistance(float dist)
    {
        currentDistance = dist;
        UpdateText(currentID, currentDistance);
    }

    public void SetColor(Color col)
    {
        if (text3D)
        {
            text3D.color = new Color(col.r, col.g, col.b, text3D.color.a);
        }
        if (text2D && applyColorToText2D)
        {
            text2D.color = new Color(col.r, col.g, col.b, text2D.color.a);
        }
        if (backgroundImage && applyColorToBackgroundImage)
        {
            backgroundImage.color = new Color(col.r, col.g, col.b, backgroundImage.color.a);
        }

        if (backgroundOutline && applyColorToBackgroundOutline)
        {
            backgroundOutline.effectColor = new Color(col.r, col.g, col.b, backgroundOutline.effectColor.a);
        }


    }

    private void Update()
    {

        transform.localScale = new Vector3(1 / boxRoot.localScale.x * startScale.x,
            1 / boxRoot.localScale.y * startScale.y,
            1 / boxRoot.localScale.z * startScale.z);


        transform.localPosition = new Vector3(transform.localPosition.x, 0.5f + heightAboveBoxCeiling / boxRoot.localScale.y, transform.localPosition.z);

    }

    private void OnDestroy()
    {
        Camera.onPreRender -= PreRender;
    }

    private void PreRender(Camera cam)
    {
        if (lookAtCamera)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
        }
    }

    private void UpdateText(int id, float dist)
    {
        string newtext = "";
        if (showID) newtext += "ID: " + id.ToString();
        if (showID && showDistance) newtext += "\r\n";
        if (showDistance) newtext += dist.ToString("F2") + "m";

        if (text3D) text3D.text = newtext;
        if (text2D) text2D.text = newtext;
    }
}
