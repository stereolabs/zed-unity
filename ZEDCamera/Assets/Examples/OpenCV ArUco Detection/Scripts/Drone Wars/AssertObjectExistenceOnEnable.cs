using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When enabled, checks if its prefab has been instantiated and still exists. If not, makes another. 
/// Used in the ArUco Drone Wars sample so that destroyed drones are replaced, but only after its marker goes off-screen. 
/// </summary>
public class AssertObjectExistenceOnEnable : MonoBehaviour
{
    /// <summary>
    /// Prefab to respawn if it doesn't exist in OnEnable().
    /// </summary>
    [Tooltip("Prefab to respawn if it doesn't exist in OnEnable().")]
    public GameObject prefab;
    [SerializeField]
    private GameObject instantiatedPrefab;

    private void OnEnable()
    {
        if(!instantiatedPrefab || instantiatedPrefab.Equals(null))
        {
            instantiatedPrefab = Instantiate(prefab, transform.parent, false);
            instantiatedPrefab.transform.localScale = transform.localScale;
        }
    }


}
