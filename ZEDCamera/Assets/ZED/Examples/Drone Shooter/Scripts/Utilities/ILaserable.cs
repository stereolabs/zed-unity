using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILaserable
{
    /// <summary>
    /// Have an object inherit this if you want it to be damageable by the LaserShot_Player script in the ZED Drone Battle sample. 
    /// </summary>

    void TakeDamage(int damage);

}
