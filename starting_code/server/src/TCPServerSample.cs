using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;

/**
 * This class implements a simple tcp echo server.
 * Read carefully through the comments below.
 * Note that the server does not contain any sort of error handling.
 */
class TCPServerSample
{

	// > new stuff for clients
	class ClientState 
	{
		public TcpClient Client;
		public int AvatarId;
		public float X, Y, Z;
		public int Skin;
	}

	private List<ClientState> _clients = new List<ClientState>();
	private int _nextAvatarId = 0;

	public static void Main(string[] args)
	{
		TCPServerSample server = new TCPServerSample();
		server.run();
	}

	private TcpListener _listener;
	private List<TcpClient> _clients = new List<TcpClient>();

	private void run()
	{
		Console.WriteLine("Server started on port 55555");

		_listener = new TcpListener(IPAddress.Any, 55555);
		_listener.Start();

		while (true)
		{
			processNewClients();
			processExistingClients();

			//Although technically not required, now that we are no longer blocking, 
			//it is good to cut your CPU some slack
			Thread.Sleep(100);
		}
	}

	private void processNewClients()
	{
		while (_listener.Pending())
		{
			_clients.Add(_listener.AcceptTcpClient());
			Console.WriteLine("Accepted new client.");
		}
	}


	// > new processExistingClients()
	private void processExistingClients() 
	{
		foreach (ClientState clientState in _clients.ToList()) 
		{
			NetworkStream stream = clientState.Client.GetStream();
			if (stream.DataAvailable) 
			{
				byte[] bytes = StreamUtil.Read(stream);
				Packet packet = new Packet(bytes);
				ISerializable received = packet.ReadObject();
				
				if (received is ClientJoinRequest) 
				{
					// Create new avatar
					clientState.AvatarId = _nextAvatarId++;
					clientState.Skin = new Random().Next(0, 1000);

					var position = GetValidPosition();
					clientState.X = position.X;
					clientState.Y = position.Y;
					clientState.Z = position.Z;
					
					// Send initial info back
					Send(clientState.Client, new AvatarInfoMessage { 
						Id = clientState.AvatarId, 
						Skin = clientState.Skin,
						Position = clientState.Position
					});
					
					// Broadcast update
					BroadcastAvatarUpdate();
				}
				else if (received is ChatMessage chat) 
				{
					chat.SenderId = clientState.AvatarId;
					Broadcast(chat);
				}
			}
		}
	}

	// > new broadcast helpers
	private void Broadcast(ISerializable message) 
	{
		Packet packet = new Packet();
		packet.Write(message);
		byte[] bytes = packet.GetBytes();
		foreach (var client in _clients) {
			StreamUtil.Write(client.Client.GetStream(), bytes);
		}
	}

	private void BroadcastAvatarUpdate() 
	{
		var update = new AvatarUpdateMessage();
		foreach (var client in _clients) {
			update.Avatars.Add(new AvatarInfoMessage {
				Id = client.AvatarId,
				Skin = client.Skin,
				X = clientState.X,
				Y = clientState.Y,
				Z = clientState.Z
			});
		}
		Broadcast(update);
	}


	// > convert loc info for avatarinfoupdate
	private (float X, float Y, float Z) GetValidPosition() 
	{
		float angle = new Random().Next(0, 360) * MathF.PI / 180f;
		float distance = new Random().Next(0, 10);
		return (
			X: MathF.Cos(angle) * distance,
			Y: 0,
			Z: MathF.Sin(angle) * distance
		);
	}

}

