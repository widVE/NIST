using UnityEngine;
using System;
using System.IO;

public class ConsoleLogger : MonoBehaviour
{
    //A unity script to capture the console output of the debug logs and write them to a file with a date and timestamp in the file name.
    private string path;
    private string fileName;
    private string fullPath;

    public bool logToConsole = false;

    private void Start()
    {
        path = Application.persistentDataPath + "/Logs/";
        fileName = "log-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt";
        fullPath = path + fileName;

        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        if (logToConsole) Application.logMessageReceived += Log;
    }

    private void OnDestroy()
    {
        if (logToConsole) Application.logMessageReceived -= Log;
    }

    private void Log(string condition, string stackTrace, LogType type)
    {
        using (StreamWriter writer = File.AppendText(fullPath))
        {
            writer.WriteLine("[" + type + "] : " + condition);
            writer.WriteLine(stackTrace);
            writer.WriteLine();
        }
    } 
}
