using System.Net.Sockets;
using System;

namespace CXMineServer
{
	public class NetState
	{
		private Connection _Conn;

		public static EventHandler<SocketAsyncEventArgs> OnCompleted = new EventHandler<SocketAsyncEventArgs>(SocketOnCompleted);

		private byte[] _Buffer = new byte[4096];

		public byte[] Buffer
		{
			get { return _Buffer; }
		}

		private int _BufferLength;

		public int BufferLength
		{
			get { return _BufferLength; }
		}

		public NetState(Socket client, Player player)
		{
			_Conn = new Connection(client, player);
		}

		public void DestroyEntity(int entityId)
		{
			_Conn.Transmit(PacketType.DestroyEntity, entityId);
		}

		public void Disconnect()
		{
			_Conn.Disconnect("Bye");
		}

		public void Message(string message)
		{
			_Conn.Transmit(PacketType.Message, message);
		}

		public void NamedEntitySpawn(int entityId, string userName, int x, int y, int z, byte extra1, byte extra2, short holdingPos)
		{
			_Conn.Transmit(PacketType.NamedEntitySpawn, entityId,
					userName, x, y, z,
					extra1, extra2, holdingPos);
		}

		public void PlayerPositionLook(double x, double y, double z, float yaw, float pitch, byte extra)
		{
			_Conn.Transmit(PacketType.PlayerPositionLook, x, y, y, z, yaw, pitch, extra);
		}

		public void PreChunk(int chunkX, int chunkZ, byte extra)
		{
			_Conn.Transmit(PacketType.PreChunk, chunkX, chunkZ, extra);
		}

		public void SetSlot(byte extra, short slot, object idPayload, object countPayload, byte damage)
		{
			_Conn.Transmit(PacketType.SetSlot, extra, slot, idPayload, countPayload, damage);
		}

		public void SendChunk(Chunk c)
		{
			_Conn.SendChunk(c);
		}

		public void SpawnPosition(int x, int y, int z)
		{
			_Conn.Transmit(PacketType.SpawnPosition, x, y, z);
		}

		public void TimeUpdate(long time)
		{
			_Conn.Transmit(PacketType.TimeUpdate, time);
		}

		// Something better than object?
		public void UpdateHealth(object payload)
		{
			_Conn.Transmit(PacketType.UpdateHealth, payload);
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

			Array.Copy(e.Buffer, _Buffer, e.BytesTransferred);

			if(e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
			{
				lock (CXMineServer.Server)
				{
					CXMineServer.Server.Queue.Enqueue(this);
				}
			}
		}

		public void OnSend(SocketAsyncEventArgs e)
		{

		}
		
	}
}
