using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using SocketIOClient;
using System.Collections.Generic;
using System.Net.Sockets;
using System;
using SocketIOClient.Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Net.WebSockets;
using System.Collections;


public class SocketManager : MonoBehaviour
{
    public static SocketManager Instance { get; private set; }
    public GameObject player;

    void  Awake()
    {
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //  private  manager;
    SocketIOUnity socket;
    public string url = "localhost:3000";
    private Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    public string myid;
    public UnityEngine.UI.Image ReceivedImage;

    void Start()
    {
        var uri = new Uri(url);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
          Query = new Dictionary<string, string>
            {
                {"token", "UNITY" }
            },EIO = 4
            ,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
        });
        socket.JsonSerializer = new NewtonsoftJsonSerializer();
        socket.Connect();

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Connection Established");
           
        };

        socket.On("connection", response => { 
           
            string validatedString = response.ToString();
            validatedString = validatedString.Replace("[","");
            validatedString = validatedString.Replace("]", "");
            Debug.Log("Auth Success " + response.ToString());
            RunOnMainThread(() => {

               var obj = JsonUtility.FromJson<GenericMessage>(validatedString);
                myid = obj.data;
                SubscribeToData();
                // GenerateLocalPlayer(obj.data);

            });
        
        });

        socket.On("initialize", (data) =>
        {
            string s = data.ToString().Substring(1);
            s = s.Remove(s.Length-1);
            Debug.Log("Initialize: "+s);
            playerdata[] existingPlayers = JsonHelper.FromJsonArray<playerdata>(s.ToString());
            Debug.Log("Existing Players"+existingPlayers.Length);
            foreach (var player in existingPlayers)
            {
                if (!players.ContainsKey(player.id))
                {
                    RunOnMainThread(() => {
                        CreatePlayer(player.id, player.x, player.y, player.z);
                    });
                }
            }
        });

        socket.On("playerJoined", (data) =>
        {
            string s = data.ToString().Substring(1);
            s = s.Remove(s.Length - 1);
            Debug.Log("new joined: " + s);
            RunOnMainThread(() =>
            {
                playerdata newPlayer = JsonUtility.FromJson<playerdata>(s.ToString());
                Debug.Log("new joined: " + newPlayer.id);
                if (!players.ContainsKey(newPlayer.id))
                {
                    CreatePlayer(newPlayer.id, newPlayer.x, newPlayer.y, newPlayer.z);
                    Debug.Log("Spawn player");
                }
            });
        });

        // for image

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("<color=RED>Socket Connected</color>");
        };
    }

    public void SetImageData(Sprite s)
    {
        Debug.Log("Received Sprite");
       
            ReceivedImage.sprite = s;
            ReceivedImage.gameObject.SetActive(true);
        
       
    }

    void SubscribeToData()
    {
        var socket = getSocket();
        if (socket != null)
        {
            if (socket.Connected)
            {

                socket.On("screenData", response => {
                    Debug.Log("Got Screen Data");
                    var recieveddata = response.GetValue<byte[]>();
                    //Debug.Log(response.ToString());
                    RunOnMainThread(() =>
                    {

                        Texture2D texture = new Texture2D(1, 1);
                        texture.LoadImage(recieveddata);
                        Debug.Log("Image Loaded Into Texture");
                   
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                  
                        SetImageData(sprite);
                    });
                   
                });
            }
        }
    }


    void GenerateLocalPlayer(string id)
    {
        var pl = Instantiate(player).GetComponent<RefNetworkPlayer>();
        pl.id = id;
       // pl.isLocalPlayer = true;
    }

    void GeneratePlayer(string id)
    {
        Debug.Log("Genrate Player id: " + id);
        var pl = Instantiate(player).GetComponent<RefNetworkPlayer>();
        pl.id = id;
       // pl.isLocalPlayer = false;
    }

    public SocketIOUnity getSocket()
    {
        return socket;
    }


    private void CreatePlayer(string id, float x, float y, float z)
    {
        GameObject newPlayer = Instantiate(player, new Vector3(x, y, z), Quaternion.identity);
        players[id] = newPlayer;
        newPlayer.GetComponent<RefNetworkPlayer>().id = id;
        Debug.Log($"Player {id} instantiated at ({x}, {y}, {z}).");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(CaptureAndSendScreenshot());
        }
    }

    IEnumerator CaptureAndSendScreenshot(bool reduceSize=false)
    {
        // Wait for end of frame to capture the screen
      
        var socket = getSocket();

        while (true)
        {
            yield return new WaitForEndOfFrame();

            // Capture the screenshot
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

            byte[] pngData = null;
            if (reduceSize)
            {
                Texture2D resized = ResizeTexture(screenshot, 256, 256);

                // Encode to PNG
                pngData = resized.EncodeToJPG();
            }
            else
            {
                pngData = ImageConversion.EncodeToJPG(screenshot, 50);
            }

            Debug.Log($"Screenshot captured and encoded to PNG, size: {pngData.Length} bytes");

            // Clean up the screenshot texture (optional)
            Destroy(screenshot);

            // Send the PNG data via Socket.IO
            if (socket.Connected)
            {
                socket.Emit("screenData", pngData);
                Debug.Log("Screenshot sent to server.");
            }
            else
            {
                Debug.LogError("Socket.IO connection not open.");
            }
            yield return new WaitForSeconds(.1f);
        }
        
    }


    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        RenderTexture.active = rt;

        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        rt = null;
        return result;
    }

    void RunOnMainThread(System.Action callback)
    {
       UnityMainThreadDispatcher.Instance().Enqueue(callback);
    }
}
[Serializable]
public class GenericMessage
{
    public long date;
    public string data;
}
