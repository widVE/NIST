/*
 * Hand tracking and gesture recognition
 * Source: https://github.com/Duke-I3T-Lab/Hand-gesture-recognition
 * Authors: Tianyi Hu and Maria Gorlatova
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using Microsoft.MixedReality.Toolkit;
using System.Threading;
using System.Diagnostics;
using UnityEngine.Networking;

using System.Net;
using System.Net.Sockets;
using TMPro;

#if WINDOWS_UWP
using Windows.Storage;
#endif

// This script establishes a UDP connection with Nvidia Jetson for real time 
// hand gesture recognition.

public class UDP_TrackingLogger : MonoBehaviour
{
    #region Constants to modify
    private const string DataSuffix = "UDPTracking";
    private const string CSVHeader = "Label," + "Time," + "Counter,"
                                     + "IndexDistalJoint," + "IndexKnuckle," + "IndexMetacarpal," + "IndexMiddleJoint," + "IndexTip,"
                                     + "MiddleDistalJoint," + "MiddleKnuckle," + "MiddleMetacarpal," + "MiddleMiddleJoint," + "MiddleTip," + "Palm,"
                                     + "PinkyDistalJoint," + "PinkyKnuckle," + "PinkyMetacarpal," + "PinkyMiddleJoint," + "PinkyTip,"
                                     + "RingDistalJoint," + "RingKnuckle," + "RingMetacarpal," + "RingMiddleJoint," + "RingTip,"
                                     + "ThumbDistalJoint," + "ThumbMetacarpalJoint," + "ThumbProximalJoint," + "ThumbTip," + "Wrist,"
                                     + "IndexDistalJoint," + "IndexKnuckle," + "IndexMetacarpal," + "IndexMiddleJoint," + "IndexTip,"
                                     + "MiddleDistalJoint," + "MiddleKnuckle," + "MiddleMetacarpal," + "MiddleMiddleJoint," + "MiddleTip," + "Palm,"
                                     + "PinkyDistalJoint," + "PinkyKnuckle," + "PinkyMetacarpal," + "PinkyMiddleJoint," + "PinkyTip,"
                                     + "RingDistalJoint," + "RingKnuckle," + "RingMetacarpal," + "RingMiddleJoint," + "RingTip,"
                                     + "ThumbDistalJoint," + "ThumbMetacarpalJoint," + "ThumbProximalJoint," + "ThumbTip," + "Wrist";
    private const string SessionFolderRoot = "CSVLogger";
    #endregion

    #region private members
    private string m_sessionPath;
    private string m_filePath;
    private string m_recordingId;
    private string m_sessionId;
    private StringBuilder m_csvData;
    #endregion
    
    #region public members
    public string RecordingInstance => m_recordingId;
    #endregion

    // replace the ip address below with the Nvidia Jetson's IPv4
    private string hostname = "192.168.1.31";        

    private int csv_started, data_counter;
    private string loggerData = "";
    private int hand_logging = 1;

    // Create necessary UdpClient objects
    public bool isTxStarted = false;
    int rxPort = 8000; // port to receive data from Python on
    int txPort = 8001; // port to send data to Python on

    UdpClient client;
    IPEndPoint remoteEndPoint;
    Thread receiveThread; // Receiving Thread

    public TMP_Text logger_text;
    public static string gesture_outcome = "";


    // Start is called before the first frame update
    async void Start()
    {

        csv_started = 0;
        data_counter = 0;

        // Create remote endpoint (to Nvidia Jetson) 
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(hostname), txPort);

        // Create local client
        client = new UdpClient(rxPort);

        // local endpoint define (where messages are received)
        // Create a new thread for reception of incoming messages
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        // Initialize (seen in comments window)
        print("UDP Comms Initialised");

        StartCoroutine(SendDataCoroutine());
        await MakeNewSession();
    }

    // Update is called once per frame

    void Update()
    {
        if (csv_started == 0)
        {
            StartNewCSV();
            csv_started = 1;
            UnityEngine.Debug.Log("New CSV started");
        }
        else if (csv_started == 1)
        {
            StartCoroutine("logging_tracking");
            logger_text.text = string.Format(gesture_outcome);
        }
    }

    IEnumerator SendDataCoroutine() // Show sending data from Unity to Python via UDP
    {
        while (true)
        {
            SendData(loggerData);
            loggerData = "";            // reset the logger data being sent to Python server
            yield return new WaitForSeconds(0.0001f);
        }
    }

    public void SendData(string message) // Use to send data to Python
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

    private void ReceiveData()
    {
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);                // receive from any server
                //byte[] data = client.Receive(ref remoteEndPoint);     // can specify server as same remoteEndPoint
                string text = Encoding.UTF8.GetString(data);
                
                print("Received: " + text);
                gesture_outcome = text;
            }
            catch (Exception err)
            {
                //print(err.ToString());
            }
        }
    }


    public void logging_tracking()
    {
        List<String> rowData = new List<String>();

        // add the gesture, current time and frame number (first 3 cols)
        rowData.Add(gesture_outcome);                    // in csv column "Label"
        rowData.Add(DateTime.Now.ToString("HH:mm:ss.fff"));  // in csv column "Time"
        rowData.Add((data_counter).ToString());          // in csv column "Counter"

        bool? calibrationStatus = CoreServices.InputSystem?.EyeGazeProvider?.IsEyeCalibrationValid;

        // I think the eye calibration check was failing,
        // but maybe it is not required for hand tracking.
        calibrationStatus = false;

        if (calibrationStatus != null)
        {
            if (hand_logging == 1)
            {
                // store hand tracking data 
                add_rightHand_data(rowData);
                add_leftHand_data(rowData);
            }

            if (rowData.Count != 0)
            {
                foreach (string str in rowData)
                {
                    loggerData += str.ToString() + ", ";
                }

                print("LoggerData: " + loggerData);
            }
            
            if (csv_started == 1)
            {
                AddRow(rowData);    // add all data to CSV
                FlushData();        // flush
            }
            data_counter++;
        }
        else
        {
            logger_text.text = string.Format("Need Eye Calibration");
        }
    }


    public void StartNewCSV()
    {
        m_recordingId = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
        var filename = m_recordingId + "-" + DataSuffix + ".csv";
        m_filePath = Path.Combine(Application.persistentDataPath, filename);
        if (m_csvData != null)
        {
            EndCSV();
        }
        m_csvData = new StringBuilder();
        m_csvData.AppendLine(",,,Right Hand,,,,,,,,,,,,,,,,,,,,,,,,,,Left Hand");           // top Header of CSV, categorizes left-right hands
        m_csvData.AppendLine(CSVHeader);                                                    // joint labels
        logger_text.text = string.Format("CSV Started");
    }

    public async Task MakeNewSession()
    {
        m_sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string rootPath = "";
#if WINDOWS_UWP
        StorageFolder sessionParentFolder = await KnownFolders.PicturesLibrary
            .CreateFolderAsync(SessionFolderRoot,
            CreationCollisionOption.OpenIfExists);
        rootPath = sessionParentFolder.Path;
#else
        rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SessionFolderRoot);
        if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
#endif
        m_sessionPath = Path.Combine(rootPath, m_sessionId);
        Directory.CreateDirectory(m_sessionPath);
        UnityEngine.Debug.Log("CSVLogger logging data to " + m_sessionPath);
    }

    public void EndCSV()
    {
        if (m_csvData == null)
        {
            return;
        }
        using (var csvWriter = new StreamWriter(m_filePath, true))
        {
            csvWriter.Write(m_csvData.ToString());
        }
        m_recordingId = null;
        m_csvData = null;
    }

    public void OnDestroy()
    {
        EndCSV();
    }

    public void AddRow(List<String> rowData)
    {
        AddRow(string.Join(",", rowData.ToArray()));
    }

    public void AddRow(string row)
    {
        m_csvData.AppendLine(row);
    }

    /// <summary>
    /// Writes all current data to current file
    /// </summary>
    public void FlushData()
    {
        using (var csvWriter = new StreamWriter(m_filePath, true))
        {
            csvWriter.Write(m_csvData.ToString());
        }
        m_csvData.Clear();
    }

    public void add_rightHand_data(List<String> rowData)
    {
        MixedRealityPose pose;
        string index_disj, index_knuckle, index_mc, index_middlej, index_tip;
        string middle_disj, middle_knuckle, middle_mc, middle_middlej, middle_tip;
        string pinky_disj, pinky_knuckle, pinky_mc, pinky_middlej, pinky_tip;
        string ring_disj, ring_knuckle, ring_mc, ring_middlej, ring_tip;
        string thumb_disj, thumb_mcj, thumb_proxj, thumb_tip;
        string palm, wrist;

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexDistalJoint, Handedness.Right, out pose))
        {
            index_disj = pose.Position.ToString("F3");
            index_disj = index_disj.Replace(",", "/");
            rowData.Add(index_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexKnuckle, Handedness.Right, out pose))
        {
            index_knuckle = pose.Position.ToString("F3");
            index_knuckle = index_knuckle.Replace(",", "/");
            rowData.Add(index_knuckle);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexMetacarpal, Handedness.Right, out pose))
        {
            index_mc = pose.Position.ToString("F3");
            index_mc = index_mc.Replace(",", "/");
            rowData.Add(index_mc);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexMiddleJoint, Handedness.Right, out pose))
        {
            index_middlej = pose.Position.ToString("F3");
            index_middlej = index_middlej.Replace(",", "/");
            rowData.Add(index_middlej);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out pose))
        {
            index_tip = pose.Position.ToString("F3");
            index_tip = index_tip.Replace(",", "/");
            rowData.Add(index_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleDistalJoint, Handedness.Right, out pose))
        {
            middle_disj = pose.Position.ToString("F3");
            middle_disj = middle_disj.Replace(",", "/");
            rowData.Add(middle_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleKnuckle, Handedness.Right, out pose))
        {
            middle_knuckle = pose.Position.ToString("F3");
            middle_knuckle = middle_knuckle.Replace(",", "/");
            rowData.Add(middle_knuckle);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleMetacarpal, Handedness.Right, out pose))
        {
            middle_mc = pose.Position.ToString("F3");
            middle_mc = middle_mc.Replace(",", "/");
            rowData.Add(middle_mc);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleMiddleJoint, Handedness.Right, out pose))
        {
            middle_middlej = pose.Position.ToString("F3");
            middle_middlej = middle_middlej.Replace(",", "/");
            rowData.Add(middle_middlej);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Right, out pose))
        {
            middle_tip = pose.Position.ToString("F3");
            middle_tip = middle_tip.Replace(",", "/");
            rowData.Add(middle_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Right, out pose))
        {
            palm = pose.Position.ToString("F3");
            palm = palm.Replace(",", "/");
            rowData.Add(palm);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyDistalJoint, Handedness.Right, out pose))
        {
            pinky_disj = pose.Position.ToString("F3");
            pinky_disj = pinky_disj.Replace(",", "/");
            rowData.Add(pinky_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyKnuckle, Handedness.Right, out pose))
        {
            pinky_knuckle = pose.Position.ToString("F3");
            pinky_knuckle = pinky_knuckle.Replace(",", "/");
            rowData.Add(pinky_knuckle);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyMetacarpal, Handedness.Right, out pose))
        {
            pinky_mc = pose.Position.ToString("F3");
            pinky_mc = pinky_mc.Replace(",", "/");
            rowData.Add(pinky_mc);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyMiddleJoint, Handedness.Right, out pose))
        {
            pinky_middlej = pose.Position.ToString("F3");
            pinky_middlej = pinky_middlej.Replace(",", "/");
            rowData.Add(pinky_middlej);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyTip, Handedness.Right, out pose))
        {
            pinky_tip = pose.Position.ToString("F3");
            pinky_tip = pinky_tip.Replace(",", "/");
            rowData.Add(pinky_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingDistalJoint, Handedness.Right, out pose))
        {
            ring_disj = pose.Position.ToString("F3");
            ring_disj = ring_disj.Replace(",", "/");
            rowData.Add(ring_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingKnuckle, Handedness.Right, out pose))
        {
            ring_knuckle = pose.Position.ToString("F3");
            ring_knuckle = ring_knuckle.Replace(",", "/");
            rowData.Add(ring_knuckle);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingMetacarpal, Handedness.Right, out pose))
        {
            ring_mc = pose.Position.ToString("F3");
            ring_mc = ring_mc.Replace(",", "/");
            rowData.Add(ring_mc);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingMiddleJoint, Handedness.Right, out pose))
        {
            ring_middlej = pose.Position.ToString("F3");
            ring_middlej = ring_middlej.Replace(",", "/");
            rowData.Add(ring_middlej);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingTip, Handedness.Right, out pose))
        {
            ring_tip = pose.Position.ToString("F3");
            ring_tip = ring_tip.Replace(",", "/");
            rowData.Add(ring_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbDistalJoint, Handedness.Right, out pose))
        {
            thumb_disj = pose.Position.ToString("F3");
            thumb_disj = thumb_disj.Replace(",", "/");
            rowData.Add(thumb_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbMetacarpalJoint, Handedness.Right, out pose))
        {
            thumb_mcj = pose.Position.ToString("F3");
            thumb_mcj = thumb_mcj.Replace(",", "/");
            rowData.Add(thumb_mcj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbProximalJoint, Handedness.Right, out pose))
        {
            thumb_proxj = pose.Position.ToString("F3");
            thumb_proxj = thumb_proxj.Replace(",", "/");
            rowData.Add(thumb_proxj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out pose))
        {
            thumb_tip = pose.Position.ToString("F3");
            thumb_tip = thumb_tip.Replace(",", "/");
            rowData.Add(thumb_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Right, out pose))
        {
            wrist = pose.Position.ToString("F3");
            wrist = wrist.Replace(",", "/");
            rowData.Add(wrist);
        }
        else
            rowData.Add("0");
    }

    public void add_leftHand_data(List<String> rowData)
    {
        MixedRealityPose pose;
        string index_disj, index_knuckle, index_mc, index_middlej, index_tip;
        string middle_disj, middle_knuckle, middle_mc, middle_middlej, middle_tip;
        string pinky_disj, pinky_knuckle, pinky_mc, pinky_middlej, pinky_tip;
        string ring_disj, ring_knuckle, ring_mc, ring_middlej, ring_tip;
        string thumb_disj, thumb_mcj, thumb_proxj, thumb_tip;
        string palm, wrist;

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexDistalJoint, Handedness.Left, out pose))
        {
            index_disj = pose.Position.ToString("F3");
            index_disj = index_disj.Replace(",", "/");
            rowData.Add(index_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexKnuckle, Handedness.Left, out pose))
        {
            index_knuckle = pose.Position.ToString("F3");
            index_knuckle = index_knuckle.Replace(",", "/");
            rowData.Add(index_knuckle);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexMetacarpal, Handedness.Left, out pose))
        {
            index_mc = pose.Position.ToString("F3");
            index_mc = index_mc.Replace(",", "/");
            rowData.Add(index_mc);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexMiddleJoint, Handedness.Left, out pose))
        {
            index_middlej = pose.Position.ToString("F3");
            index_middlej = index_middlej.Replace(",", "/");
            rowData.Add(index_middlej);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Left, out pose))
        {
            index_tip = pose.Position.ToString("F3");
            index_tip = index_tip.Replace(",", "/");
            rowData.Add(index_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleDistalJoint, Handedness.Left, out pose))
        {
            middle_disj = pose.Position.ToString("F3");
            middle_disj = middle_disj.Replace(",", "/");
            rowData.Add(middle_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleKnuckle, Handedness.Left, out pose))
        {
            middle_knuckle = pose.Position.ToString("F3");
            middle_knuckle = middle_knuckle.Replace(",", "/");
            rowData.Add(middle_knuckle);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleMetacarpal, Handedness.Left, out pose))
        {
            middle_mc = pose.Position.ToString("F3");
            middle_mc = middle_mc.Replace(",", "/");
            rowData.Add(middle_mc);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleMiddleJoint, Handedness.Left, out pose))
        {
            middle_middlej = pose.Position.ToString("F3");
            middle_middlej = middle_middlej.Replace(",", "/");
            rowData.Add(middle_middlej);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Left, out pose))
        {
            middle_tip = pose.Position.ToString("F3");
            middle_tip = middle_tip.Replace(",", "/");
            rowData.Add(middle_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Left, out pose))
        {
            palm = pose.Position.ToString("F3");
            palm = palm.Replace(",", "/");
            rowData.Add(palm);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyDistalJoint, Handedness.Left, out pose))
        {
            pinky_disj = pose.Position.ToString("F3");
            pinky_disj = pinky_disj.Replace(",", "/");
            rowData.Add(pinky_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyKnuckle, Handedness.Left, out pose))
        {
            pinky_knuckle = pose.Position.ToString("F3");
            pinky_knuckle = pinky_knuckle.Replace(",", "/");
            rowData.Add(pinky_knuckle);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyMetacarpal, Handedness.Left, out pose))
        {
            pinky_mc = pose.Position.ToString("F3");
            pinky_mc = pinky_mc.Replace(",", "/");
            rowData.Add(pinky_mc);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyMiddleJoint, Handedness.Left, out pose))
        {
            pinky_middlej = pose.Position.ToString("F3");
            pinky_middlej = pinky_middlej.Replace(",", "/");
            rowData.Add(pinky_middlej);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyTip, Handedness.Left, out pose))
        {
            pinky_tip = pose.Position.ToString("F3");
            pinky_tip = pinky_tip.Replace(",", "/");
            rowData.Add(pinky_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingDistalJoint, Handedness.Left, out pose))
        {
            ring_disj = pose.Position.ToString("F3");
            ring_disj = ring_disj.Replace(",", "/");
            rowData.Add(ring_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingKnuckle, Handedness.Left, out pose))
        {
            ring_knuckle = pose.Position.ToString("F3");
            ring_knuckle = ring_knuckle.Replace(",", "/");
            rowData.Add(ring_knuckle);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingMetacarpal, Handedness.Left, out pose))
        {
            ring_mc = pose.Position.ToString("F3");
            ring_mc = ring_mc.Replace(",", "/");
            rowData.Add(ring_mc);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingMiddleJoint, Handedness.Left, out pose))
        {
            ring_middlej = pose.Position.ToString("F3");
            ring_middlej = ring_middlej.Replace(",", "/");
            rowData.Add(ring_middlej);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.RingTip, Handedness.Left, out pose))
        {
            ring_tip = pose.Position.ToString("F3");
            ring_tip = ring_tip.Replace(",", "/");
            rowData.Add(ring_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbDistalJoint, Handedness.Left, out pose))
        {
            thumb_disj = pose.Position.ToString("F3");
            thumb_disj = thumb_disj.Replace(",", "/");
            rowData.Add(thumb_disj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbMetacarpalJoint, Handedness.Left, out pose))
        {
            thumb_mcj = pose.Position.ToString("F3");
            thumb_mcj = thumb_mcj.Replace(",", "/");
            rowData.Add(thumb_mcj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbProximalJoint, Handedness.Left, out pose))
        {
            thumb_proxj = pose.Position.ToString("F3");
            thumb_proxj = thumb_proxj.Replace(",", "/");
            rowData.Add(thumb_proxj);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Left, out pose))
        {
            thumb_tip = pose.Position.ToString("F3");
            thumb_tip = thumb_tip.Replace(",", "/");
            rowData.Add(thumb_tip);
        }
        else
            rowData.Add("0");

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Left, out pose))
        {
            wrist = pose.Position.ToString("F3");
            wrist = wrist.Replace(",", "/");
            rowData.Add(wrist);
        }
        else
            rowData.Add("0");
    }

    public string string_replace(string data)
    {
        data = data.Replace("(", "");
        data = data.Replace(")", "");
        return data;
    }
}
