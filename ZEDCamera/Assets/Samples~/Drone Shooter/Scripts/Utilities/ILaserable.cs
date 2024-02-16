using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface used by LaserShot_Player to deal damage. Implement in an object you want to be able to damage with the player's laser gun. 
/// </summary>
public interface ILaserable
{
    /// <summary>
    /// Deal damage in some way. 
    /// </summary>
    /// <param name="damage">How much damage to deal.</param>
    void TakeDamage(int damage);

}
