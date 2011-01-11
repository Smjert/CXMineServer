using System.Net.Sockets;
using System;

namespace CXMineServer
{
	public class NetState
	{
		public static EventHandler<SocketAsyncEventArgs> OnCompleted = new EventHandler<SocketAsyncEventArgs>(SocketOnCompleted);

		private ByteQueue _Buffer;

		public ByteQueue Buffer
		{
			get { return _Buffer; }
		}

		private Player _Owner;
		public Player Owner
		{
			get { return _Owner; }
		}

		public NetState(Socket client, Player player)
		{
			_Buffer = new ByteQueue();
			_Owner = player;
		}

		public void DestroyEntity(int entityId)
		{
		}

		public void Disconnect()
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

		public static void SocketOnCompleted(object sender, SocketAsyncEventArgs e)
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
		}

		public void OnReceive(SocketAsyncEventArgs e)
		{
			AsyncUserToken asyncToken = (AsyncUserToken)e.UserToken;

			if(e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
			{
				lock(Buffer)
					_Buffer.Enqueue(e.Buffer, 0, e.BytesTransferred);

				lock (CXMineServer.Server.Queue)
					CXMineServer.Server.Queue.Enqueue(this);
			}
		}

		public void OnSend(SocketAsyncEventArgs e)
		{

		}
		
	}
}
