using shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/**
 * The main ChatLobbyClient where you will have to do most of your work.
 * 
 * @author J.C. Wichman
 */
public class ChatLobbyClient : MonoBehaviour
{
    //reference to the helper class that hides all the avatar management behind a blackbox
    private AvatarAreaManager _avatarAreaManager;
    //reference to the helper class that wraps the chat interface
    private PanelWrapper _panelWrapper;


    // > new id for clients
    private int _myAvatarId = -1;

    [SerializeField] private string _server = "localhost";
    [SerializeField] private int _port = 55555;

    private TcpClient _client;


    // > allow for waiting for server to start
    private float _reconnectDelay = 3f;
    private bool _connected = false;
    private Coroutine _connectionCoroutine;

    private void Start()
    {
        // connectToServer();
        StartConnectionLoop();

        //register for the important events
        _avatarAreaManager = FindFirstObjectByType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;

        _panelWrapper = FindFirstObjectByType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += onChatTextEntered;
    }

    private void StartConnectionLoop()
    {
        if (_connectionCoroutine == null)
        {
            _connectionCoroutine = StartCoroutine(ConnectionLoop());
        }
    }

     private IEnumerator ConnectionLoop()
    {
        while (!_connected)
        {
            Debug.Log("Attempting to connect to server...");
            connectToServer();
            yield return new WaitForSeconds(_reconnectDelay);
        }
    }

    private void connectToServer()
    {
        if (_connected) return;

        try
        {
            if (_client != null) _client.Close();
            
            _client = new TcpClient();
            _client.ConnectAsync(_server, _port).Wait(1000); // Wait with timeout
            
            if (_client.Connected)
            {
                _connected = true;
                Debug.Log("Connected to server!");
                sendObject(new ClientJoinRequest());
                if (_connectionCoroutine != null)
                {
                    StopCoroutine(_connectionCoroutine);
                    _connectionCoroutine = null;
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Connection failed: {e.Message}");
            _client?.Close();
            _connected = false;
        }
    }

    private void onAvatarAreaClicked(Vector3 pClickPosition)
    {
        Debug.Log("ChatLobbyClient: you clicked on " + pClickPosition);
        //TODO pass data to the server so that the server can send a position update to all clients (if the position is valid!!)

        AvatarMoveRequest moveRequest = new AvatarMoveRequest
        {
            X = pClickPosition.x,
            Y = pClickPosition.y,
            Z = pClickPosition.z
        };
        sendObject(moveRequest);
    }

    private void onChatTextEntered(string pText)
    {
        _panelWrapper.ClearInput();
        sendChatMessage(pText);
    }

    // > replaced SendString, this uses objs
    private void sendChatMessage(string pText) 
    {
        ChatMessage message = new ChatMessage { SenderId = _myAvatarId, Text = pText };
        sendObject(message);
    }

    private void sendObject(ISerializable pObject) 
    {
        try 
        {
            Packet packet = new Packet();
            packet.Write(pObject);
            byte[] bytes = packet.GetBytes();
            StreamUtil.Write(_client.GetStream(), bytes);
        } 
        catch (Exception e) 
        {
            /* error handling code */  
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    // > modified() update to use the new packet system
    private void Update()
    {
        try
        {
            if (_client.Available > 0) 
            {
                byte[] bytes = StreamUtil.Read(_client.GetStream());
                Packet packet = new Packet(bytes);
                ISerializable received = packet.ReadObject();
                
                if (received is AvatarUpdateMessage update) 
                {
                    List<int> existingIds = _avatarAreaManager.GetAllAvatarIds();
                    List<int> receivedIds = new List<int>();

                    foreach (var avatar in update.Avatars) 
                    {
                        receivedIds.Add(avatar.Id);
                        
                        if (_avatarAreaManager.HasAvatarView(avatar.Id))
                        {
                            // Update existing avatar
                            AvatarView view = _avatarAreaManager.GetAvatarView(avatar.Id);
                            view.SetSkin(avatar.Skin);
                            view.Move(new Vector3(avatar.X, avatar.Y, avatar.Z));
                        }
                        else 
                        {
                            // Add new avatar
                            AvatarView view = _avatarAreaManager.AddAvatarView(avatar.Id);
                            view.SetSkin(avatar.Skin);
                            view.Move(new Vector3(avatar.X, avatar.Y, avatar.Z));
                        }
                    }

                    // Remove avatars that are no longer present
                    foreach (int existingId in existingIds)
                    {
                        if (!receivedIds.Contains(existingId))
                        {
                            _avatarAreaManager.RemoveAvatarView(existingId);
                        }
                    }
                }
                else if (received is ChatMessage chat) 
                {
                    AvatarView avatar = _avatarAreaManager.GetAvatarView(chat.SenderId);
                    if (avatar != null) avatar.Say(chat.Text);
                }
                else if (received is AvatarInfoMessage info) 
                {
                    _myAvatarId = info.Id; // Store our own ID
                }
            }
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    private void showMessage(string pText)
    {
        //This is a stub for what should actually happen
        //What should actually happen is use an ID that you got from the server, to get the correct avatar
        //and show the text message through that
        List<int> allAvatarIds = _avatarAreaManager.GetAllAvatarIds();
        
        if (allAvatarIds.Count == 0)
        {
            Debug.Log("No avatars available to show text through:" + pText);
            return;
        }

        int randomAvatarId = allAvatarIds[UnityEngine.Random.Range(0, allAvatarIds.Count)];
        AvatarView avatarView = _avatarAreaManager.GetAvatarView(randomAvatarId);
        avatarView.Say(pText);
    }

    private void OnDestroy()
    {
        if (_client != null)
        {
            _client.Close();
        }
    }
}
