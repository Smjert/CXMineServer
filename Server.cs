using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CXMineServer
{
	public class Server
	{
		public int Port;
		public bool Running;
		public string WorldName;
		public string Name;
		public string Motd;
		public string ServerHash;
		
		public Map World;
		public List<Player> PlayerList;
		
		private TcpListener _Listener;

        private static int EID = 0;
		
		public Server()
		{
			Port = Configuration.GetInt("port", 25565);

			Running = false;
			WorldName = Configuration.Get("world", "world");
			Name = Configuration.Get("server-name", "Minecraft Server");
			Motd = Configuration.Get("motd", "Powered by " + Color.Green + "CXMineServer");
			ServerHash = "-";
			
			World = null;
			PlayerList = new List<Player>();
			_Listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
		}
		
		public void Run()
		{
			World = new Map(WorldName);
			if (!File.Exists(WorldName + "/level.dat")) {
				CXMineServer.Log("Generating world " + WorldName);
				World.Generate();
				World.ForceSave();
			}
			
			_Listener.Start();
			CXMineServer.Log("Listening on port " + Port);
			Running = true;
			
			while (Running) {
				// Check for new connections
				while (_Listener.Pending()) {
					AcceptConnection(_Listener.AcceptTcpClient());
				}
				
				Thread.Sleep(100);
			}
			
			World.ForceSave();
		}
		
		public void Spawn(Player player)
		{
			CXMineServer.Log(player.Username + " has joined");
			MessageAll(Color.Announce + player.Username + " has joined");
		}
		
		public void Despawn(Player player)
		{
			MessageAll(Color.Announce + player.Username + " has left");
			CXMineServer.Log(player.Username + " has left");
		}
		
		public void MessageAll(string message)
		{
			foreach(Player p in PlayerList) {
				p.SendMessage(message);
			}
		}

        public int getEID()
        {
            return EID++;
        }
		
		// ====================
		// Private helpers.
		
		private void AcceptConnection(TcpClient client)
		{
			PlayerList.Add(new Player(client));
		}
	}
}
