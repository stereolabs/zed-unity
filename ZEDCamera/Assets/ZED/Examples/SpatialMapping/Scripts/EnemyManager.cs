using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Positions the enemies and controll their death
/// </summary>
public class EnemyManager : MonoBehaviour
{
    /// <summary>
    /// List of all the enemies
    /// </summary>
    static List<GameObject> enemies = new List<GameObject>();

    /// <summary>
    /// The prefab used to spawn the enemy
    /// </summary>
    public GameObject enemyPrefab;

    /// <summary>
    /// Check if the NavMesh is ready
    /// </summary>
    private bool isReady = false;

    /// <summary>
    /// Number of tries to set a prefab
    /// </summary>
    private int noNavMeshCount = 0;

    /// <summary>
    /// Center of the current navMesh
    /// </summary>
    private Vector3 centerNavMesh;

    /// <summary>
    /// Type of agent accepted by the NavMesh
    /// </summary>
    private int agentTypeNavMeshID = 0;

    /// <summary>
    /// Type of the agent from the prefab
    /// </summary>
    private int agentType = 0;
    void Update()
    {
        //Try to create an enemy on the navMesh
        if (isReady && enemies.Count == 0 && noNavMeshCount < 20)
        {
            Create();
        }

        //Clear all the empty items
        if (enemies.Count > 0)
        {
            enemies.RemoveAll(item => item == null);
        }
    }

    //A new navmesh has been created
    void StartNavMesh()
    {
        //Clear all the enemies
        Clear();
        isReady = false;
    }

    private void OnEnable()
    {
        ZEDSpatialMapping.OnMeshStarted += StartNavMesh;
		NavMeshSurface.OnNavMeshReady += Ready;

        //Set the ZEDLight component on the object if a light is active
        Component[] lights = enemyPrefab.GetComponentsInChildren(typeof(Light));
        foreach (Light l in lights)
        {
            if (!l.gameObject.GetComponent<ZEDLight>())
            {
                l.gameObject.AddComponent<ZEDLight>();
            }
        }
    }

    private void Start()
    {
        UnityEngine.AI.NavMeshAgent c;
        if ((c = enemyPrefab.GetComponent<UnityEngine.AI.NavMeshAgent>()) != null)
        {
            agentType = c.agentTypeID;
        }
    }

    private void OnDisable()
    {
        ZEDSpatialMapping.OnMeshStarted -= StartNavMesh;
		NavMeshSurface.OnNavMeshReady -= Ready;
    }

    /// <summary>
    /// Event sent from the NavMesh
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void Ready(object sender, NavMeshSurface.PositionEventArgs e)
    {
        centerNavMesh = e.position;
        isReady = e.valid;
        agentTypeNavMeshID = e.agentTypeID;
        Clear();
    }

    public void Ready()
    {
        isReady = true;
        Clear();
    }

    /// <summary>
    /// Destroy all the enemies and clear its container
    /// </summary>
    void Clear()
    {
        foreach (GameObject o in enemies)
        {
            Destroy(o);
        }
        enemies.Clear();
    }

    /// <summary>
    /// Remove a particular GameObject
    /// </summary>
    /// <param name="o"></param>
    static void Destroyed(GameObject o)
    {
        enemies.Remove(o);
    }

    /// <summary>
    /// Try to create an agent on the navMesh
    /// </summary>
    public void Create()
    {
        //If the agent and the navMesh have two area type different
        if (agentType != agentTypeNavMeshID)
        {
            Debug.LogWarning("The agent ID differs from the NavMesh");
            return;
        }
        //Create a gameobject to try to set it on the NavMesh
        enemies.Add(Instantiate(enemyPrefab, centerNavMesh, Quaternion.identity));
        List<GameObject> notActivated = new List<GameObject>();

        //For each enemy created move it on the navMesh
        foreach (GameObject o in enemies)
        {
            NavMeshAgentController a = o.GetComponent<NavMeshAgentController>();
            if (a.Move())
            {
                a.GetComponent<RandomWalk>().Activate();
                noNavMeshCount = 0;

            }
            else
            {
                notActivated.Add(a.gameObject);
                noNavMeshCount++;
            }
        }
        
        //Destroy the objects missing the NavMesh
        foreach (GameObject o in notActivated)
        {
            Destroy(o);
            enemies.Remove(o);
        }
    }
}
