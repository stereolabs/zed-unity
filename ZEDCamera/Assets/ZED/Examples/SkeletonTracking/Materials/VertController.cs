using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertController : MonoBehaviour {

    public List<VertController> xNeighbors = new List<VertController>();
    public List<VertController> zNeighbors = new List<VertController>();

    public bool isMoving;
    public bool followX;
    public bool followZ;
    public Transform target;
    public VertHandler handler;
    bool setupNeighbors;
    bool passedTarget;

    private void Start()
    {
        setupNeighbors = false;
        handler = transform.parent.GetComponent<VertHandler>();
    }

    private void Update()
    {
        if(handler.handlersReady && !setupNeighbors)
        {
            int myNbr = System.Convert.ToInt32(gameObject.name.Substring(gameObject.name.Length - 1));

            for (int i = 0; i < handler.holders.Count; i++)
            {
                int neighborNbr = System.Convert.ToInt32(handler.holders[i].name.Substring(handler.holders[i].name.Length - 1));
                VertController vertControl = handler.holders[i].GetComponent<VertController>();

                if (System.Convert.ToInt32(myNbr) == System.Convert.ToInt32(neighborNbr))
                {
                    if (handler.holders[i] != gameObject)
                    {
                        xNeighbors.Add(vertControl);
                        zNeighbors.Add(vertControl);
                    }
                }

                if (myNbr == 0 && neighborNbr == 1)
                    zNeighbors.Add(vertControl);
                if (myNbr == 0 && neighborNbr == 3)
                    xNeighbors.Add(vertControl);
                if (myNbr == 1 && neighborNbr == 0)
                    zNeighbors.Add(vertControl);
                if (myNbr == 1 && neighborNbr == 2)
                    xNeighbors.Add(vertControl);
                if (myNbr == 2 && neighborNbr == 1)
                    xNeighbors.Add(vertControl);
                if (myNbr == 2 && neighborNbr == 3)
                    zNeighbors.Add(vertControl);
                if (myNbr == 3 && neighborNbr == 2)
                    zNeighbors.Add(vertControl);
                if (myNbr == 3 && neighborNbr == 0)
                    xNeighbors.Add(vertControl);
            }

            setupNeighbors = true;
        }

        if (handler.draggingObject == transform)
            isMoving = true;
        else
            isMoving = false;


        if (xNeighbors.Count > 0)
        {
            for (int i = 0; i < xNeighbors.Count; i++)
            {
                followX = false;

                if (xNeighbors[i] != null && xNeighbors[i].isMoving)
                {
                    followX = true;
                    target = xNeighbors[i].transform;
                    break;
                }
            }
        }

        if (zNeighbors.Count > 0)
        {
            for (int i = 0; i < zNeighbors.Count; i++)
            {
                followZ = false;

                if (zNeighbors[i] != null && zNeighbors[i].isMoving)
                {
                    followZ = true;
                    target = zNeighbors[i].transform;
                    break;
                }
            }
        }

        var newPos = transform.localPosition;

        if (followX)
        {
            newPos.x = target.localPosition.x;
        }
        if (followZ)
        {
            newPos.z = target.localPosition.z;
        }
        
        transform.localPosition = newPos;
    }

}
