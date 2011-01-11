using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CXMineServer
{
	public class Server
	{
		public int Port{
			get;
			private set;
		}

		public bool Running{
			get;
			private set;
		}

		public string WorldName{
			get;
			private set;
		}

		public string Name{
			get;
			private set;
		}

		public string Motd{
			get;
			private set;
		}

		public string ServerHash{
			get;
			private set;
		}
		
		public Map World{
			get;
			private set;
		}

		private List<Player> playerList;
		public IEnumerable<Player> PlayerList{
			get{
				return playerList;
			}
		}

		private Queue<NetState> queue;
		public Queue<NetState> Queue
		{
			get{
				return queue;
			}
		}

		private Queue<NetState> currentQueue;
		public Queue<NetState> CurrentQueue
		{
			get{
				return currentQueue;
			}
		}

		public static SocketAsyncEventArgsPool ReadWritePool;
		private AutoResetEvent _Signal;
		
		private Listener _Listener;

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
			playerList = new List<Player>();
			_Listener = new Listener(new IPEndPoint(IPAddress.Any, Port));

			ReadWritePool = new SocketAsyncEventArgsPool(50);
		}

		public void Signal()
		{
			_Signal.Set();
		}
		
		public void Run()
		{
			World = new Map(WorldName);

			if (!World.LoadLevel()) {
				// The generation is missing
				/*CXMineServer.Log("Generating world " + WorldName);
				World.Generate();
				World.ForceSave();*/
				return;
			}

			for (int i = 0; i < 50; ++i)
			{
				SocketAsyncEventArgs socketEvent = new SocketAsyncEventArgs();
				socketEvent.Completed += NetState.OnCompleted;
				socketEvent.UserToken = new AsyncUserToken();
				socketEvent.SetBuffer(new byte[1024], 0, 1024);

				ReadWritePool.Push(socketEvent);
			}
			
			_Listener.Start();
			CXMineServer.Log("Listening on port " + Port);
			Running = true;
			
			while (_Signal.WaitOne()) {

				lock(this)
				{
					Queue<byte[]> tmp = currentQueue;
					currentQueue = queue;
					queue = tmp;
				}

				for (int i = 0; i < currentQueue.Count; ++i )
				{
					byte[] buffer = currentQueue.Dequeue();
					
				}
				/*// Check for new connections
				while (_Listener.Pending()) {
					AcceptConnection(_Listener.AcceptTcpClient());
				}*/
				
				//Thread.Sleep(100);
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
            playerList.Remove(player);
		}
		
		public void MessageAll(string message)
		{
			foreach(Player p in PlayerList) {
				p.SendMessage(message);
			}
		}

		public void Quit()
		{
			Running = false;

			for(int i = playerList.Count - 1; i >= 0; --i)
				playerList[i].State.Disconnect();

			playerList.Clear();
		}

        public static int getEID()
        {
            return EID++;
        }
		
		// ====================
		// Private helpers.
		
		private void AcceptConnection(Socket client)
		{
			Player newPlayer = new Player();
			NetState state = new NetState(client, newPlayer);
			newPlayer.State = state;
			playerList.Add(newPlayer);
		}
	}
}
