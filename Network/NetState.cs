using System.Net.Sockets;
using System;

namespace CXMineServer
{
	public class NetState
	{
		private static BufferPool _ReceiveBufferPool = new BufferPool("Receive", 2048, 2048);

		private SocketAsyncEventArgs _SocketAsyncEventOnReceive;
		private SocketAsyncEventArgs _SocketAsyncEventOnSend;

		public Socket Connection;

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

		public NetState()
		{
			_Buffer = new ByteQueue();
			_RecvBuffer = _ReceiveBufferPool.AcquireBuffer();
			//_Owner = player;
		}

		public void DestroyEntity(int entityId)
		{
		}

		public void Disconnect()
		{
		}

		public void KeepAlive()
		{
			
		}

		public void Message(string message)
		{

		}
		public void NamedEntitySpawn(int entityId, string userName, int x, int y, int z, byte extra1, byte extra2, short holdingPos)
		{
		}

		public void PlayerPositionLook(double x, double y, double z, float yaw, float pitch, byte extra)
		{
			
		}

		public void PreChunk(int chunkX, int chunkZ, byte extra)
		{
			
		}

		public void SetSlot(byte extra, short slot, object idPayload, object countPayload, byte damage)
		{
			
		}

		public void SendChunk(Chunk c)
		{
			
		}

		public void SpawnPosition(int x, int y, int z)
		{
			
		}

		public void TimeUpdate(long time)
		{
			
		}

		// Something better than object?
		public void UpdateHealth(object payload)
		{

		}

		public void Send(Packet p, SocketAsyncEventArgs e)
		{
			SendQueue.Gram gram;
			byte[] buffer = p.GetBuffer();
			lock (_SendQueue)
			{
				gram = _SendQueue.Enqueue(buffer, buffer.Length);
			}

			if(gram != null)
			{
				_SocketAsyncEventOnSend.SetBuffer(gram.Buffer, 0, gram.Length);
				Send_Start();
			}
		}

		public void Send_Start()
		{
			try
			{
				bool result = false;

				do
				{
					result = !Connection.SendAsync(_SocketAsyncEventOnSend);

				} while (result);
			}
			catch (Exception ex)
			{
				CXMineServer.Log(ex.Message);
			}
		}

		public void Start()
		{
			_SocketAsyncEventOnReceive = Server.ReadPool.Pop();
			_SocketAsyncEventOnReceive.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);
			_SocketAsyncEventOnReceive.SetBuffer(_RecvBuffer, 0, _RecvBuffer.Length);

			_SocketAsyncEventOnSend = Server.WritePool.Pop();
			_SocketAsyncEventOnSend.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);

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
			bool willRaiseEvent = Connection.ReceiveAsync(_SocketAsyncEventOnReceive);
			if (!willRaiseEvent)
				Process_Receive(_SocketAsyncEventOnReceive);
		}

		public void Process_Receive(SocketAsyncEventArgs e)
		{
			try
			{
				if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
				{
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
		}

		public void OnReceive(object sender, SocketAsyncEventArgs e)
		{
			Process_Receive(e);

			Receive_Start();
		}

		public void OnSend(object sender, SocketAsyncEventArgs e)
		{
			SendQueue.Gram gram;

			lock (_SendQueue)
			{
				gram = _SendQueue.Dequeue();
			}

			if (gram != null)
			{
				_SocketAsyncEventOnSend.SetBuffer(gram.Buffer, 0, gram.Length);
				Send_Start();
			}
		}
		
	}
}
