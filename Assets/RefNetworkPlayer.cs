
using PimDeWitte.UnityMainThreadDispatcher;
using System.Net.Sockets;
using System.Net.WebSockets;
using UnityEngine;
using System.Collections;


public class RefNetworkPlayer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string id;
    Vector3 lastposition;
    CharacterController characterController;
    float speed = 5;
    Vector3 remotepos;
     bool isLocalPlayer;
    
    void Start()
    {
        if(id == SocketManager.Instance.myid)
        {
            isLocalPlayer = true;
        }
        characterController = gameObject.AddComponent<CharacterController>();

      
        if (!isLocalPlayer)
        {
            SubscribeToPos();
           
            // sendSpawnEvent(this.transform.position);
        }
       
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
            {
                characterController.Move(new Vector3(Input.GetAxis("Horizontal") * speed * Time.deltaTime, 0, Input.GetAxis("Vertical") * speed * Time.deltaTime));
            }
            if (Vector3.Distance(lastposition, this.transform.position) > .2f)
            {
                lastposition = this.transform.position;
                SendPlayerMove(lastposition);
            }

           
        }
        else
        {
            this.transform.position = remotepos;
        }
      
    }

    void SubscribeToPos()
    {
        var socket = SocketManager.Instance.getSocket();
        if (socket != null)
        {
            if (socket.Connected)
            {
               
                socket.On("playerMove", response => {
                    //Debug.Log(response.ToString());
                    string validatedString = response.ToString();
                    validatedString = validatedString.Replace("[", "");
                    validatedString = validatedString.Replace("]", "");
                    
                    var d = JsonUtility.FromJson<playerdata>(validatedString);
                    if (d.id == id)
                    {
                        var pos = JsonUtility.FromJson<PlayerMovementData>(d.pos);
                        Debug.Log(pos.x + " " + pos.z);
                        Vector3 towardspos = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
                        remotepos = towardspos;
                    }
                    

                });
            }
        }
    }

   

    


    public  void SendPlayerMove(Vector3 pos)
    {
        var socket = SocketManager.Instance.getSocket();
        if (socket.Connected)
        {
            var data = new PlayerMovementData();
            data.x = pos.x;
            data.y = pos.y; 
            data.z = pos.z;
            var message = JsonUtility.ToJson(data);
            socket.Emit("playerMove", message);
            //Debug.Log(message);
        }
    }

    void sendSpawnEvent(Vector3 pos)
    {
        var socket = SocketManager.Instance.getSocket();
        if (socket.Connected)
        {
            var data = new PlayerMovementData();
            data.x = pos.x;
            data.y = pos.y;
            data.z = pos.z;
            var message = JsonUtility.ToJson(data);
            socket.Emit("playerJoined", message);
            //Debug.Log(message);
        }
    }

    void RunOnMainThread(System.Action callback)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(callback);
    }
}
[System.Serializable]
public class PlayerMovementData
{
    public float x;
    public float y;
    public float z;
}
[System.Serializable]
public class playerdata
{
    public string id;
    public float x,y,z;
    public string pos;
}
