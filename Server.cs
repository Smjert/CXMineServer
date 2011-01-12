using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System;

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

		public static SocketAsyncEventArgsPool ReadPool;
		public static SocketAsyncEventArgsPool WritePool;

		private static AutoResetEvent _Signal = new AutoResetEvent(true);
		
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

			ReadPool = new SocketAsyncEventArgsPool(50);
			WritePool = new SocketAsyncEventArgsPool(50);

			queue = new Queue<NetState>();
			currentQueue = new Queue<NetState>();
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
				ReadPool.Push(new SocketAsyncEventArgs());
				WritePool.Push(new SocketAsyncEventArgs());
			}
			
			try
			{
				_Listener.Start();
			}
			catch (System.Exception ex)
			{
				CXMineServer.Log(ex.Message);
				Console.ReadLine();
			}
			
			CXMineServer.Log("Listening on port " + Port);
			Running = true;
			
			while (_Signal.WaitOne()) {

				if(queue != null && queue.Count > 0)
				{
					lock (queue)
					{
						Queue<NetState> tmp = currentQueue;
						currentQueue = queue;
						queue = tmp;
					}

					for (int i = 0; i < currentQueue.Count; ++i)
					{
						NetState ns = currentQueue.Dequeue();
						ByteQueue buffer = ns.Buffer;

						lock (buffer)
						{
							int length = buffer.Length;

							while (length > 0)
							{
								int packetID = buffer.GetPacketID();
								PacketHandler handler = PacketHandlers.GetHandler((PacketType)packetID);

								CXMineServer.Log("arrived: " + ((PacketType)packetID).ToString());
								byte[] data;

								if (handler == null)
								{
									data = new byte[length];
									length = buffer.Dequeue(data, 0, length);

									CXMineServer.Log("Unhandled packet arrived");

									break;
								}

								if (buffer.UnderlyingBuffer.Length > 2048)
									ns.Disconnect();

								PacketReader packetReader;

								if (handler.Length == 0)
								{
									packetReader = new PacketReader(buffer.UnderlyingBuffer, buffer.UnderlyingBuffer.Length);
									handler.OnReceive(ns, packetReader);

									data = new byte[packetReader.Index + 1];

									if (!packetReader.Failed)
										buffer.Dequeue(data, 0, packetReader.Index);
								}
								else
								{
									data = new byte[handler.Length];
									int packetLength = buffer.Dequeue(data, 0, handler.Length);

									packetReader = new PacketReader(data, packetLength);
									handler.OnReceive(ns, packetReader);

									if (packetReader.Failed)
										ns.Disconnect();
								}

								length = buffer.Length;
							}
						}
					}
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

	}
}
