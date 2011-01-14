using System.Net.Sockets;
using System;
using System.IO;
using zlib;
using System.Threading;

namespace CXMineServer
{
	public class NetState
	{
		private static BufferPool _ReceiveBufferPool = new BufferPool("Receive", 2048, 2048);

		private SocketAsyncEventArgs _SocketAsyncEventOnReceive;
		private SocketAsyncEventArgs _SocketAsyncEventOnSend;

		private object _PendingLock = new object();
		private bool Pending;
		private bool _Disposing;

		public Socket Connection;
		public NetworkStream stream;

		private ByteQueue _Buffer;

		private SendQueue _SendQueue;

		private byte[] _RecvBuffer;

		public ByteQueue Buffer
		{
			get { return _Buffer; }
		}

		private Player _Owner;
		public Player Owner
		{
			get { return _Owner; }
			set { _Owner = value; }
		}

		private bool _Running;
		public bool Running
		{
			get { return _Running; }
		}

		public NetState()
		{
			_Buffer = new ByteQueue();
			_RecvBuffer = _ReceiveBufferPool.AcquireBuffer();
			_SendQueue = new SendQueue();
			//_Owner = player;
		}

		public void Login()
		{
			CXMineServer.SendLogFile("Sending: Login \r\n");
			Send(new LoginDetails(_Owner.EntityID, CXMineServer.Server.Name, CXMineServer.Server.Motd));

			_Owner.Spawn();
		}

		public void Handshake()
		{
			CXMineServer.SendLogFile("Sending: Handshake \r\n");
			Send(new Handshake(CXMineServer.Server.ServerHash));
		}

		public void DestroyEntity(int entityId)
		{
			Send(new DestroyEntity(entityId));
		}

		public void Disconnect()
		{
			_Running = false;
		}

		public void KeepAlive()
		{
			CXMineServer.SendLogFile("Sending: KeepAlive \r\n");
			Send(new KeepAlive());
		}

		public void Message(string message)
		{
			CXMineServer.SendLogFile("Sending: Message \r\n");
			Send(new Message(message));
		}
		public void NamedEntitySpawn(int entityId, string userName, int x, int y, int z, byte extra1, byte extra2, short holdingPos)
		{
			Send(new NamedEntitySpawn(entityId, userName, x, y, z, extra1, extra2, holdingPos));
		}

		public void PlayerPositionLook(double x, double y, double z, float yaw, float pitch, byte extra)
		{
			Send(new PlayerPositionLook(x, y, z, yaw, pitch, extra));
		}

		public void PreChunk(int chunkX, int chunkZ, byte extra)
		{
			CXMineServer.SendLogFile("Sending: PreChunk \r\n");
			Send(new PreChunk(chunkX, chunkZ, extra));
		}

		public void SetSlot(byte extra, short slot, object idPayload, object countPayload, short damage)
		{
			CXMineServer.SendLogFile("Sending: SetSlot" + "\r\n");
			Send(new SetSlot(extra, slot, (short)idPayload, (byte)countPayload, damage));
		}

		public void SendChunk(Chunk c)
		{
			PreChunk(c.ChunkX, c.ChunkZ, 1);
			// TODO: maybe we can cache some uncompressed chunk
			byte[] uncompressed = c.GetBytes();
			byte[] data;
			using(MemoryStream mem = new MemoryStream()) {
				using(ZOutputStream stream = new ZOutputStream(mem, zlibConst.Z_BEST_COMPRESSION)) {
					stream.Write(uncompressed, 0, uncompressed.Length);
				}
				data = mem.ToArray();
			}
			CXMineServer.SendLogFile("Sending: MapChunk dimension: " + (data.Length + 18).ToString() + "\r\n");
			Send(new MapChunk(16 * c.ChunkX, (short)0, 16 * c.ChunkZ, 15, 127, 15, data.Length, data));

			//KeepAlive();
		}

		public void SpawnPosition(int x, int y, int z)
		{
			Send(new SpawnPosition(x, y, z));
		}

		public void TimeUpdate(long time)
		{
			Send(new TimeUpdate(time));
		}

		// Something better than object?
		public void UpdateHealth(object payload)
		{
			CXMineServer.SendLogFile("Sending: UpdateHealth" + "\r\n");
			Send(new UpdateHealth((short)payload));
		}

		public void Flush()
		{
			if (Connection == null || !_SendQueue.IsFlushReady)
				return;

			SendQueue.Gram gram;

			lock (_SendQueue)
				gram = _SendQueue.CheckFlushReady();
			

			if (gram != null)
			{
				_SocketAsyncEventOnSend.SetBuffer(gram.Buffer, 0, gram.Length);
				//CXMineServer.SendLogFile("Flush Sending: " + BitConverter.ToString(gram.Buffer, 0, gram.Length) + "\r\n\r\n");
				Send_Start(gram.Length);
			}
		}

		public void Send(Packet p)
		{
			if (_Disposing)
				return;

			SendQueue.Gram gram;
			byte[] buffer = p.GetBuffer();
			lock (_SendQueue)
				gram = _SendQueue.Enqueue(buffer, p.Length);

			if(gram != null)
			{
				_SocketAsyncEventOnSend.SetBuffer(gram.Buffer, 0, gram.Length);
				CXMineServer.SendLogFile("Sending: " + BitConverter.ToString(buffer, 0, p.Length) + "\r\n\r\n");
				
				Send_Start(p.Length);
			}

			CXMineServer.Server.Signal();
		}

		public void Send(Packet p, bool sync)
		{
			if (_Disposing && !Connection.Connected)
				return;

			byte[] buffer = p.GetBuffer();
			CXMineServer.SendLogFile(BitConverter.ToString(buffer, 0, p.Length) + "\r\n");
			//Connection.Send(buffer, p.Length, SocketFlags.None);
			stream.Write(buffer, 0, p.Length);
			CXMineServer.SendLogFile("Sent\r\n\r\n");

			//Thread.Sleep(1000);
		}

		public void Send_Start(int length)
		{
			try
			{
				if (!Connection.Connected)
					return;

				//CXMineServer.SendLogFile(BitConverter.ToString(_SocketAsyncEventOnSend.Buffer, 0, length > 1024 ? 1024 : length) + "\r\n");
				bool result = Connection.SendAsync(_SocketAsyncEventOnSend);
				if(!result)
					Process_Send(_SocketAsyncEventOnSend);
			}
			catch (Exception ex)
			{
				CXMineServer.Log(ex.Message + " " + ex.Source + "\n" + ex.StackTrace);
			}
		}

		public void Start()
		{
			_SocketAsyncEventOnReceive = Server.ReadPool.Pop();
			_SocketAsyncEventOnReceive.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);
			_SocketAsyncEventOnReceive.SetBuffer(_RecvBuffer, 0, _RecvBuffer.Length);

			_SocketAsyncEventOnSend = Server.WritePool.Pop();
			_SocketAsyncEventOnSend.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);

			_Running = true;
			Receive_Start();
		}

		/*public static void SocketOnCompleted(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Receive:
					(e.UserToken as NetState).OnReceive(e);
					break;
				case SocketAsyncOperation.Send:
					(e.UserToken as NetState).OnSend(e);
					break;
				default:
					throw new ArgumentException("The last operation completed on the socket was not a receive or send");
			} 
		}*/

		public void Receive_Start()
		{
			lock(_PendingLock)
			{
				if(!Pending)
				{
					Pending = true;
					bool willRaiseEvent = Connection.ReceiveAsync(_SocketAsyncEventOnReceive);
					if (!willRaiseEvent)
						Process_Receive(_SocketAsyncEventOnReceive);
				}
			}
		}

		public void Process_Receive(SocketAsyncEventArgs e)
		{
			try
			{
				if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
				{
					CXMineServer.Log("Received packet size: " + e.BytesTransferred.ToString());
					lock (Buffer)
						_Buffer.Enqueue(e.Buffer, 0, e.BytesTransferred);

					lock (CXMineServer.Server.Queue)
						CXMineServer.Server.Queue.Enqueue(this);

					CXMineServer.Server.Signal();
				}
			}
			catch (System.Exception ex)
			{
				CXMineServer.Log(ex.Message + ex.Source + "\n" + ex.StackTrace);
				Console.ReadLine();
			}

			lock(_PendingLock)
				Pending = false;
		}

		public void OnReceive(object sender, SocketAsyncEventArgs e)
		{
			Process_Receive(e);

			if (_Disposing)
				return;

			Receive_Start();
		}

		public void OnSend(object sender, SocketAsyncEventArgs e)
		{
			if (_Disposing)
				return;
			Process_Send(e);
		}

		public void Process_Send(SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.ConnectionAborted)
				Dispose();

			if (_Disposing)
				return;

			SendQueue.Gram gram;

			lock (_SendQueue)
			{
				gram = _SendQueue.Dequeue();
				//CXMineServer.Log("Remaining: " + _SendQueue.Count);
			}

			if (gram != null)
			{
				_SocketAsyncEventOnSend.SetBuffer(gram.Buffer, 0, gram.Length);
				Send_Start(gram.Length);
			}

			//CXMineServer.Log(e.SocketError.ToString());
		}

		public void Dispose()
		{
			if (_Disposing)
				return;

			_Disposing = true;
			Connection.Shutdown(SocketShutdown.Both);
			Connection.Close();

			_Running = false;

			_ReceiveBufferPool.ReleaseBuffer(_RecvBuffer);

			lock (_SendQueue)
				_SendQueue.Clear();
		}
	}
}
