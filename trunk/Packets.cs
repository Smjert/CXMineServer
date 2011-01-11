using System;
using System.Collections.Generic;
using System.Text;

namespace CXMineServer
{
	public class DestroyEntity : Packet
	{
		public DestroyEntity(int entityId) : base(PacketType.DestroyEntity, 5)
		{
			_Writer.Write(entityId);
		}
	}

	public class MapChunk : Packet
	{
		public MapChunk(int x, short boh, int z, byte extra1, byte extra2, byte extra3, int dataLength, byte[] data)
			: base(PacketType.MapChunk)
		{
			SetCapacity(18 + dataLength);

			_Writer.Write(x);
			_Writer.Write(boh);
			_Writer.Write(z);
			_Writer.Write(extra1);
			_Writer.Write(extra2);
			_Writer.Write(extra3);
			_Writer.Write(dataLength);
			_Writer.Write(data);
		}
	}

	public class Message : Packet
	{
		public Message(string message) : base(PacketType.Message)
		{
			SetCapacity(3 + message.Length);

			_Writer.Write(message);
		}
	}

	public class NamedEntitySpawn : Packet
	{
		public NamedEntitySpawn(int entityId, string userName, int x, int y, int z, byte extra1, byte extra2, short holdingPos) : base(PacketType.NamedEntitySpawn)
		{
			SetCapacity(23 + userName.Length);

			_Writer.Write(entityId);
			_Writer.Write(userName);
			_Writer.Write(x);
			_Writer.Write(y);
			_Writer.Write(z);
			_Writer.Write(extra1);
			_Writer.Write(extra2);
			_Writer.Write(holdingPos);
		}
	}

	public class PlayerPositionLook : Packet
	{
		public PlayerPositionLook(double x, double y, double z, float yaw, float pitch, byte extra) : base(PacketType.PlayerPositionLook, 34)
		{
			_Writer.Write(x);
			_Writer.Write(y);
			_Writer.Write(z);
			_Writer.Write(yaw);
			_Writer.Write(pitch);
			_Writer.Write(extra);
		}
	}

	public class PreChunk : Packet
	{
		public PreChunk(int x, int z, byte extra) : base(PacketType.PreChunk, 10)
		{
			_Writer.Write(x);
			_Writer.Write(z);
			_Writer.Write(extra);
		}
	}

	public class SetSlot : Packet
	{
		public SetSlot(byte extra, short slot, short idPayload, byte countPayload, byte damage) : base(PacketType.SetSlot, 8)
		{
			_Writer.Write(extra);
			_Writer.Write(slot);
			_Writer.Write(idPayload);
			_Writer.Write(countPayload);
			_Writer.Write(damage);
		}
	}

	public class SpawnPosition : Packet
	{
		public SpawnPosition(int x, int y, int z) : base(PacketType.SpawnPosition, 13)
		{
			_Writer.Write(x);
			_Writer.Write(y);
			_Writer.Write(z);
		}
	}

	public class TimeUpdate : Packet
	{
		public TimeUpdate(long time) : base(PacketType.TimeUpdate, 9)
		{
			_Writer.Write(time);
		}
	}

	public class UpdateHealth : Packet
	{
		public UpdateHealth(short health) : base(PacketType.UpdateHealth, 3)
		{
			_Writer.Write(health);
		}
	}
}
