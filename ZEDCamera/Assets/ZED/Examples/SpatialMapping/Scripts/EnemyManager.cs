using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns the specified prefab and positions it when a NavMeshSurface reports there's 
/// a new NavMesh it can walk on. Used in the ZED spatial mapping sample scene to spawn a bunny
/// to walk around your environment once you're done scanning it.  
/// </summary>
public class EnemyManager : MonoBehaviour
{
    /// <summary>
    /// The prefab used to spawn the enemy. Should contain a NavMeshAgent component. 
    /// </summary>
    [Tooltip("The prefab used to spawn the enemy. Should contain a NavMeshAgent component. ")]
    public GameObject enemyPrefab;

    /// <summary>
    /// Whether or not the NavMesh from the NavMeshSurface is ready.
    /// </summary>
    private bool isReady = false;

    /// <summary>
    /// Number of tries the script has attempted to place the prefab on the NavMesh. It stops trying at 20.
    /// </summary>
    private int noNavMeshCount = 0;

    /// <summary>
    /// Center of the current navMesh.
    /// </summary>
    private Vector3 centerNavMesh;

    /// <summary>
    /// ID of agent type accepted by the NavMesh. Agent IDs are defined in the Navigation window.
    /// </summary>
    private int agentTypeNavMeshID = 0;

    /// <summary>
    /// ID of the agent type from the prefab's NavMeshAgent component. Agent IDs are defined in the Navigation window.
    /// </summary>
    private int agentType = 0;

    /// <summary>
    /// List of all instantiated enemies. 
    /// </summary>
    static List<GameObject> enemies = new List<GameObject>();

    void Update()
    {
        //Try to create an enemy on the NavMesh.
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

    /// <summary>
    /// Called when ZEDSpatialMapping begins making a new mesh, to clear existing enemies
    /// and prevent the script from trying to place enemies. 
    /// Subscribed to ZEDSpatialMapping.OnMeshStarted in OnEnable().
    /// </summary>
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
        //Unsubscribe from the events. 
        ZEDSpatialMapping.OnMeshStarted -= StartNavMesh;
		NavMeshSurface.OnNavMeshReady -= Ready;
    }

    /// <summary>
    /// Called when the NavMesh is finished being created, to clear existing data
    /// and begin trying to place the enemy.
    /// Subscribed to NavMeshSurface.OnNavMeshReady in OnEnable(). 
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
    /// Try to create an agent on the NavMesh. 
    /// </summary>
    public void Create()
    {
        //If the agent and the NavMesh have different agent IDs, don't assign it. 
        if (agentType != agentTypeNavMeshID)
        {
            Debug.LogWarning("The agent ID differs from the NavMesh");
            return;
        }
        //Instantiate the prefab and try to place it on the NavMesh.
        enemies.Add(Instantiate(enemyPrefab, centerNavMesh, Quaternion.identity));
        List<GameObject> notActivated = new List<GameObject>();

        //For each enemy created, move it on the NavMesh.
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
        
        //Destroy any objects that were not properly added to the NavMesh.
        foreach (GameObject o in notActivated)
        {
            Destroy(o);
            enemies.Remove(o);
        }
    }
}
