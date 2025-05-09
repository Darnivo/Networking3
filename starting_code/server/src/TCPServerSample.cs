using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Linq;

class TCPServerSample
{

    // new client stuff
    class ClientState
    {
        public TcpClient Client;
        public int AvatarId;
        public float X, Y, Z;  // Changed from Position
        public int Skin;
    }

    private List<ClientState> _clientStates = new List<ClientState>();
    private int _nextAvatarId = 0;
    private TcpListener _listener;
    private List<TcpClient> _clients = new List<TcpClient>(); // Original client list

    public static void Main(string[] args)
    {
        new TCPServerSample().run();
    }

    private void run()
    {
        Console.WriteLine("Server started on port 55555");
        _listener = new TcpListener(IPAddress.Any, 55555);
        _listener.Start();

        while (true)
        {
            processNewClients();
            processExistingClients();
            Thread.Sleep(100);
        }
    }

    private void processNewClients()
    {
        while (_listener.Pending())
        {
            TcpClient newClient = _listener.AcceptTcpClient();
            _clients.Add(newClient);
            _clientStates.Add(new ClientState { Client = newClient }); // Initialize ClientState
            Console.WriteLine("Accepted new client.");
        }
    }


    //modified processExistingClients to use ClientState
    private void processExistingClients()
    {
        foreach (ClientState clientState in _clientStates.ToList())
        {
            TcpClient client = clientState.Client;
            // Check if client disconnected
            if (!IsClientConnected(client))
            {
                handleClientDisconnect(clientState);
                continue;
            }


            NetworkStream stream = clientState.Client.GetStream();
            if (stream.DataAvailable)
            {
                try
                {
                    byte[] bytes = StreamUtil.Read(stream);
                    Packet packet = new Packet(bytes);
                    ISerializable received = packet.ReadObject();

                    if (received is ClientJoinRequest)
                    {
                        clientState.AvatarId = _nextAvatarId++;
                        clientState.Skin = new Random().Next(0, 1000);
                        var position = GetValidPosition();
                        clientState.X = position.X;
                        clientState.Y = position.Y;
                        clientState.Z = position.Z;

                        Send(clientState.Client, new AvatarInfoMessage
                        {
                            Id = clientState.AvatarId,
                            Skin = clientState.Skin,
                            X = clientState.X,
                            Y = clientState.Y,
                            Z = clientState.Z
                        });

                        BroadcastAvatarUpdate();
                    }
                    else if (received is ChatMessage chat)
                    {
                        chat.SenderId = clientState.AvatarId;
                        Broadcast(chat);
                    }
                    else if (received is AvatarMoveRequest moveRequest)
                    {
                        // Validate position (radius 20)
                        float distance = MathF.Sqrt(moveRequest.X * moveRequest.X + moveRequest.Z * moveRequest.Z);
                        if (distance <= 20)
                        {
                            clientState.X = moveRequest.X;
                            clientState.Y = moveRequest.Y;
                            clientState.Z = moveRequest.Z;
                            BroadcastAvatarUpdate();
                        }
                        else
                        {
                            Console.WriteLine($"Invalid position {moveRequest.X}, {moveRequest.Z}");
                        }
                    }
                }
                catch (IOException)
                {
                    handleClientDisconnect(clientState);
                }
            }
        }
    }

    // > send packet
    private void Send(TcpClient pClient, ISerializable pMessage)
    {
        try
        {
            Packet packet = new Packet();
            packet.Write(pMessage);
            byte[] bytes = packet.GetBytes();
            StreamUtil.Write(pClient.GetStream(), bytes);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending message: {e.Message}");
        }
    }

    // > new broadcast helpers
    private void Broadcast(ISerializable message)
    {
        Packet packet = new Packet();
        packet.Write(message);
        byte[] bytes = packet.GetBytes();
        foreach (var clientState in _clientStates)
        {
            StreamUtil.Write(clientState.Client.GetStream(), bytes);
        }
    }

    private void BroadcastAvatarUpdate()
    {
        var update = new AvatarUpdateMessage();
        foreach (var clientState in _clientStates)
        {
            update.Avatars.Add(new AvatarInfoMessage
            {
                Id = clientState.AvatarId,
                Skin = clientState.Skin,
                X = clientState.X,  // Fixed variable name
                Y = clientState.Y,
                Z = clientState.Z
            });
        }
        Broadcast(update);
    }

    // > convert loc info
    private (float X, float Y, float Z) GetValidPosition()
    {
        float angle = new Random().Next(0, 360) * MathF.PI / 180f;
        float distance = new Random().Next(0, 10);
        return (MathF.Cos(angle) * distance, 0, MathF.Sin(angle) * distance);
    }

    private bool IsClientConnected(TcpClient client)
    {
        return client.Connected && (client.Client.Poll(1000, SelectMode.SelectRead) ? client.Client.Available > 0 : true);
    }

    private void handleClientDisconnect(ClientState clientState)
    {
        _clientStates.Remove(clientState);
        clientState.Client.Close();
        Console.WriteLine($"Client {clientState.AvatarId} disconnected.");
        BroadcastAvatarUpdate(); // Notify all clients to remove the avatar
    }
}