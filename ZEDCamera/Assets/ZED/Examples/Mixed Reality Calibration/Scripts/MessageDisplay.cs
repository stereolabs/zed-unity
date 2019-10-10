using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Changes the text on an attached TextMesh and/or UI.Text to the specified message, with static methods
/// for sending the message to all displays at once. Also supports temporary messages.
/// Used in the ZED MR Calibration scene to update the instructions displays when they need to change. 
/// </summary>
public class MessageDisplay : MonoBehaviour
{
    /// <summary>
    /// All registered instances of MessageDisplay. 
    /// </summary>
    public static List<MessageDisplay> instances = new List<MessageDisplay>();

    /// <summary>
    /// Update every MessageDisplay with the provided message so long as it has been enabled. 
    /// </summary>
    /// <param name="message"></param>
    public static void DisplayMessageAll(string message)
    {
        foreach(MessageDisplay instance in instances)
        {
            instance.DisplayMessage(message);
        }

        lastMessage = message;
    }

    /// <summary>
    /// The last message called with DisplayMessageAll. 
    /// Used to make sure newly instantiated MessageDisplays set their message to the correct one. 
    /// </summary>
    internal static string lastMessage = "";

    /// <summary>
    /// Displays a message on every MessageDisplay for a specified amount of time.
    /// Afterwards, it reverts to the message set before it. 
    /// </summary>
    public static void DisplayTemporaryMessageAll(string message, float durationseconds = 7f)
    {
        foreach(MessageDisplay instance in instances)
        {
            instance.StartCoroutine(instance.DisplayTempMessage(message, durationseconds));
        }
    }

    /// <summary>
    /// Sets all MessageDisplays to an empty string. 
    /// </summary>
    public static void ClearAll()
    {
        foreach (MessageDisplay instance in instances)
        {
            instance.Clear();
        }

        lastMessage = "";
    }

    /// <summary>
    /// Optional 2D UI Text object to update when it receives messages. 
    /// </summary>
    [Tooltip("Optional 2D UI Text object to update when it receives messages.")]
    public Text text2D;
    /// <summary>
    /// Optional 3D Text object to update when it receives messages. 
    /// </summary>
    [Tooltip("Optional 3D Text object to update when it receives messages.")]
    public TextMesh text3D;

    /// <summary>
    /// Color to temporarily set the text color to when updating to a temporary message.
    /// </summary>
    [Space(5)]
    [Tooltip("Color to temporarily set the text color to when updating to a temporary message.")]
    public Color tempMessageTextColor = Color.yellow;

    private string currentMessage;

	/// <summary>
    /// Registers this instance to be affected when the static message update methods are called, 
    /// and updates itself based on the most recently updated message in case it was enabled late. 
    /// </summary>
	void Awake ()
    {
        instances.Add(this);
        DisplayMessage(lastMessage);
	}
	
    /// <summary>
    /// Changes the attached text object(s) to read the specified message. 
    /// </summary>
    /// <param name="message"></param>
	public void DisplayMessage(string message)
    {
        if(text2D != null)
        {
            text2D.text = message;
        }
        if(text3D != null)
        {
            text3D.text = message;
        }

        currentMessage = message;
    }

    /// <summary>
    /// Sets the attached text object(s) to an empty string. 
    /// </summary>
    public void Clear()
    {
        DisplayMessage("");
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    /// <summary>
    /// Temporarily changes the message to the one provided, along with its color, before reverting back
    /// to whatever message appeared before it. 
    /// </summary>
    private IEnumerator DisplayTempMessage(string message, float durationseconds = 3f)
    {
        //Cache old message and text colors. 
        string oldmessage = currentMessage;

        Color normalTextColor2D = Color.white;
        Color normalTextColor3D = Color.white;
        if (text2D) normalTextColor2D = text2D.color;
        if (text3D) normalTextColor3D = text3D.color;

        //Print the message. 
        DisplayMessage(message);

        //Change colors to temporary message color to make it stand out more. 
        if (text2D) text2D.color = tempMessageTextColor;
        if (text3D) text3D.color = tempMessageTextColor;

        for (float t = 0; t < durationseconds; t += Time.deltaTime)
        {
            yield return null;
        }

        //Restore the original message and text colors. 
        DisplayMessage(oldmessage);
        if (text2D) text2D.color = normalTextColor2D;
        if (text3D) text3D.color = normalTextColor3D;

    }
}
