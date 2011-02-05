using System;
using System.Collections.Generic;
using System.Text;

namespace CXMineServer
{

	public class BlockChange : Packet
	{
		public BlockChange(int x, byte y, int z, byte id, byte metadata) : base(PacketType.BlockChange, 12)
		{
			_Writer.Write(x);
			_Writer.Write(y);
			_Writer.Write(z);
			_Writer.Write(id); 
			_Writer.Write(metadata);
		}
	}

	public class CollectItem : Packet
	{
		public CollectItem(int itemEId, int playerEId)
			: base(PacketType.CollectItem, 9)
		{
			_Writer.Write(itemEId);
			_Writer.Write(playerEId);
		}
	}

	public class DestroyEntity : Packet
	{
		public DestroyEntity(int entityId) : base(PacketType.DestroyEntity, 5)
		{
			_Writer.Write(entityId);
		}
	}

	public class Handshake : Packet
	{
		public Handshake(string hash) : base(PacketType.Handshake)
		{
			SetCapacity(3, hash);

			_Writer.WriteString(_Strings[0]);
		}
	}

	public class KeepAlive : Packet
	{
		public KeepAlive()
			: base(PacketType.KeepAlive, 1)
		{
		}
	}

	public class LoginDetails : Packet
	{
		public LoginDetails(int entityId, string name, string motd) : base(PacketType.LoginDetails)
		{
			SetCapacity(18, name, motd);

			_Writer.Write(entityId);
			
			_Writer.WriteString(_Strings[0]);
			_Writer.WriteString(_Strings[1]);
			_Writer.Write((long)0);
			_Writer.Write((byte)0);
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
			SetCapacity(3, message);

			_Writer.WriteString(_Strings[0]);
		}
	}

	public class NamedEntitySpawn : Packet
	{
		public NamedEntitySpawn(int entityId, string userName, int x, int y, int z, byte rotation, byte pitch, short holdingPos) : base(PacketType.NamedEntitySpawn)
		{
			SetCapacity(23, userName);

			_Writer.Write(entityId);
			_Writer.WriteString(_Strings[0]);
			_Writer.Write(x);
			_Writer.Write(y);
			_Writer.Write(z);
			_Writer.Write(rotation);
			_Writer.Write(pitch);
			_Writer.Write(holdingPos);
		}
	}

	public class PickupSpawn : Packet
	{
		public PickupSpawn(int eid, short block, byte count, short damage, int x, int y, int z, byte rotation, byte pitch, byte roll) : base(PacketType.PickupSpawn, 25)
		{
			_Writer.Write(eid);
			_Writer.Write(block);
			_Writer.Write(count);
			_Writer.Write(damage);
			_Writer.Write(x);
			_Writer.Write(y);
			_Writer.Write(z);
			_Writer.Write(rotation);
			_Writer.Write(pitch);
			_Writer.Write(roll);
		}
	}

	public class PlayerPositionLook : Packet
	{
		public PlayerPositionLook(double x, double y, double stance, double z, float yaw, float pitch, byte extra) : base(PacketType.PlayerPositionLook, 42)
		{
			_Writer.Write(x);
			_Writer.Write(stance);
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

	public class SetSlotAdd : Packet
	{
		public SetSlotAdd(byte extra, short slot, short idPayload, byte countPayload, short damage) : base(PacketType.SetSlot, 9)
		{
			_Writer.Write(extra);
			_Writer.Write(slot);
			_Writer.Write(idPayload);
			_Writer.Write(countPayload);
			_Writer.Write(damage);
		}
	}

	public class SetSlotRemove : Packet
	{
		public SetSlotRemove(byte extra, short slot, short idPayload): base(PacketType.SetSlot, 6)
		{
			_Writer.Write(extra);
			_Writer.Write(slot);
			_Writer.Write(idPayload);
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
