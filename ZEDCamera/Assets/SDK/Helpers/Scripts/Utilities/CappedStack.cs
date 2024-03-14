using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Extended version of List used to give stack functionality, but with a maximum capacity. 
/// </summary>
public class CappedStack<T> : List<T>
{
    public int maxValues;

    public CappedStack(int max)
    {
        maxValues = max;
    }

    /// <summary>
    /// Adds the object to the top of the stack. If stack is longer than maxValues, 
    /// removes the oldest item. 
    /// </summary>
    /// <param name="val">Object to go on top of the stack.</param>
    public void Push(T val)
    {
        Add(val);
        if (Count > maxValues)
        {
            RemoveAt(0);
        }
    }

    /// <summary>
    /// Returns the item on top of the stack and removes it. 
    /// </summary>
    /// <returns>Top stack item. </returns>
    public T Pop()
    {
        T val = this[Count - 1];
        RemoveAt(Count - 1);
        return val;
    }
}
