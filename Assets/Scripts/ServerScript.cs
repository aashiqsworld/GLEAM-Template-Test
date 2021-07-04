
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

// this form of networking is depreecated so i removed the following code.
#if false
public class ServerScript : MonoBehaviour
{
    public RawImage myImage;
    public bool enableLog = false;
    public Text logtext;

    Texture2D currentTexture;
    private TcpListener listner;
    private const int port = 7777;
    private bool stop = false;

    bool isConnected = false;
    TcpClient client = null;
    NetworkStream stream = null;

    private List<TcpClient> clients = new List<TcpClient>();

    //This must be the-same with SEND_COUNT on the client
    const int SEND_RECEIVE_COUNT = 15;

    private void Start()
    {
        Application.runInBackground = true;
        currentTexture = new Texture2D(300, 300);

        // Connect to the server
        listner = new TcpListener(IPAddress.Any, port);

        listner.Start();

        // Wait for client to connect in another Thread 
        Loom.RunAsync(() =>
            {
                while (!stop)
                {
                    // Wait for client connection
                    client = listner.AcceptTcpClient();
                    // We are connected
                    clients.Add(client);

                    isConnected = true;
                    stream = client.GetStream();
                }
            });

    }

    //send an image when the button is clicked
    public void sendButton(){
        //Start sending coroutine
        logtext.text = "Notsentimage";
        stop = false;
        StartCoroutine(senderCOR());
    }

    void OnGUI(){
        string ipaddress = Network.player.ipAddress;
        GUI.Box (new Rect (10, Screen.height - 50, 100, 50), ipaddress);
        GUI.Label (new Rect (20, Screen.height - 35, 100, 20), "Status" );
        GUI.Label (new Rect (20, Screen.height - 20, 100, 20), "Connected" + clients.Count);
    }

    //Converts the data size to byte array and put result to the fullBytes array
    void byteLengthToFrameByteArray(int byteLength, byte[] fullBytes)
    {
        //Clear old data
        Array.Clear(fullBytes, 0, fullBytes.Length);
        //Convert int to bytes
        byte[] bytesToSendCount = BitConverter.GetBytes(byteLength);
        //Copy result to fullBytes
        bytesToSendCount.CopyTo(fullBytes, 0);
    }

    //Converts the byte array to the data size and returns the result
    int frameByteArrayToByteLength(byte[] frameBytesLength)
    {
        int byteLength = BitConverter.ToInt32(frameBytesLength, 0);
        return byteLength;
    }

    IEnumerator senderCOR()
    {

        //Wait until client has connected
        while (!isConnected)
        {
            yield return null;
        }

        LOG("Connected!");

        bool readyToGetFrame = true;

        byte[] frameBytesLength = new byte[SEND_RECEIVE_COUNT];

        while (!stop)
        {
            currentTexture = myImage.texture as Texture2D;
            byte[] pngBytes = currentTexture.EncodeToPNG();
            //Fill total byte length to send. Result is stored in frameBytesLength
            byteLengthToFrameByteArray(pngBytes.Length, frameBytesLength);

            //Set readyToGetFrame false
            readyToGetFrame = false;

            Loom.RunAsync(() =>
                {
                    //Send total byte count first
                    stream.Write(frameBytesLength, 0, frameBytesLength.Length);
                    LOG("Sent Image byte Length: " + frameBytesLength.Length);

                    //Send the image bytes
                    stream.Write(pngBytes, 0, pngBytes.Length);
                    LOG("Sending Image byte array data : " + pngBytes.Length);
                    //logtext.text = "sentimage";

                    //Sent. Set readyToGetFrame true
                    readyToGetFrame = true;
                });

            //Wait until we are ready to get new frame(Until we are done sending data)
            while (!readyToGetFrame)
            {logtext.text = "sentimage";
                LOG("Waiting To get new frame");
                yield return null;
            }
            stop = true;
        }

    }


    void LOG(string messsage)
    {
        if (enableLog)
            Debug.Log(messsage);

    }

    private void Update()
    {

    }

    // stop everything
    public void OnApplicationQuit()
    {
        if (listner != null)
        {
            listner.Stop();
        }

        foreach (TcpClient c in clients)
            c.Close();
    }
}

#endif