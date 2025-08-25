using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class UnityPlug : Portfish.IPlug
{
    private Queue<string> commandQueue = new Queue<string>();
    private object lockObject = new object();
    public System.Action<string> OnMessageReceived;

    public void Write(string message)
    {
        //Debug.Log("[Portfish] " + message);

        OnMessageReceived?.Invoke(message);

    }

    public string ReadLine()
    {
        // This will be called by the UCI loop to get commands
        lock (lockObject)
        {
            if (commandQueue.Count > 0)
            {
                return commandQueue.Dequeue();
            }
        }

        // Return empty string if no commands are available (non-blocking)
        return "";
    }

    public void SendCommand(string command)
    {
        lock (lockObject)
        {
            commandQueue.Enqueue(command);
        }
    }
}