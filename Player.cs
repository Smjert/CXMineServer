using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using NBT;

namespace CXMineServer
{

    public class Inventory
    {


        // Private Nested Slot class
        public class Slot
        {
            private Inventory _Inventory;

            public int Position
            {
                get;
                set;
            }

            private short count;
            public short Count
            {
                get
                {
                    return count;
                }
                set
                {
                    count = value;
                    if (count > (short)64)
                    {
                        _Inventory.SplitStackForSlot(Position);
                    }
                    if (Id != (short)-1 && count == (short)0)
                    {
                        _Inventory.ResetSlot(Position);
                    }
                }
            }

            public short Id
            {
                get;
                set;
            }

            private short uses;
            public short Uses
            {
                get
                {
                    return uses;
                }
                set
                {
                    uses = value;
                    if (uses < 0)
                        uses = 0;
                    if (uses > 38)
                        _Inventory.ResetSlot(Position);
                }
            }

            private Slot() { }
            public Slot(Inventory inv, short count = 0, short id = -1, short uses = 0, int position = -1)
            {
                _Inventory = inv;
                this.count = count;
                Id = id;
                this.uses = uses;
                Position = position;
            }
        }



        private List<Slot> slotList;
        public int HoldingPos{
			get;
			set;
		}

        public Inventory()
        {
			HoldingPos=0;
            slotList = new List<Slot>(45);
            CXMineServer.Log("Inventory Capacity: " + slotList.Capacity);
            for (int i = 0; i < slotList.Capacity; i++)
                slotList.Add(new Slot(this));
        }

        public short Add(short id)
        {
            short slot = GetFirstAvailableSlotFor(id);
            if (slot == (short)-1)
                return -1;
            if (slotList[slot].Id == (short)-1)
            {
                CXMineServer.Log("Adding id " + id);
                AddToPosition(slot, id, 1, 0);
            }
            else
                slotList[slot].Count += 1;
            return slot;
        }

        public void AddToPosition(int position, short id, short count, short uses)
        {
            slotList[position] = new Slot(this, count, id, uses, position);
        }

        public void Remove(int position, int quantity = 1)
        {
            slotList[position].Count -= (short)quantity;
        }

        public void ResetSlot(int position)
        {
            slotList[position] = new Slot(this);
        }

        public Slot GetItem(int position)
        {
            return slotList[position];
        }

        public void UseItem(short position)
        {
            slotList[position].Uses += 1;
        }

        public void SplitStackForSlot(int position)
        {
            int slot = GetFirstAvailableSlotFor(slotList[position].Id);
            if (slot != -1)
                slotList[slot] = new Slot(this, (short)(slotList[slot].Count + slotList[position].Count - 64), slotList[position].Id, slotList[slot].Uses, slot);
            slotList[position].Count = 64;
        }

        public static short FileToGameSlot(short slot)
        {
			if(slot<=8)
				return (short)(slot+36);
			if(slot<=35)
				return slot;
			if(slot<=83)
				return (short)(slot-79);
			return (short)(108-slot);
        }

        public static short GameToFileSlot(short slot)
        {
			if(slot==0)
				return (short)80; //HACK
            if(slot<=4)
				return (short)(slot+79);
			if(slot<=8)
				return (short)(108-slot);
			if(slot<=35)
				return slot;
			return (short)(slot-36);
        }

        private short GetFirstAvailableSlotFor(short id)
        {
			// TODO: non dovresti controllare che ci siano meno di 64 item nello slot?
            for (short i = 0; i < slotList.Capacity; i++ )
            {
                if (slotList[i].Id == id)
                    return i;
            }

            for (short i = 0; i < slotList.Capacity; i++)
            {
                if (slotList[i].Id == (short)-1)
                    return i;
            }

            return -1;
        }
    }

	public class Player : Entity
	{
		private Connection _Conn;
		public string Username = "";
		public bool Spawned;
		
		private List<Chunk> visibleChunks = new List<Chunk>();
		public IEnumerable<Chunk> VisibleChunks{
			get{
				return visibleChunks;
			}
		}

		private List<Entity> VisibleEntities = new List<Entity>();

        public Inventory inventory;

        private const string playersPath = "players/";
		
		public Player(TcpClient client)
		{
			_Conn = new Connection(client, this);
		}
		
		public void Spawn()
		{
			CXMineServer.Server.Spawn(this);
			Spawned = true;
			CurrentChunk = null;
			X = CXMineServer.Server.World.SpawnX + 0.5;
			Y = CXMineServer.Server.World.SpawnY + 5;
			Z = CXMineServer.Server.World.SpawnZ + 0.5;
            Yaw = 0.0F;
            Pitch = 0.0F;
            CXMineServer.Log("Creating Inventory");
            inventory = new Inventory();
            CXMineServer.Log("Inventory Created");
            loadPlayerData();
            CXMineServer.Log("Inventory Loaded");
			Update();
			_Conn.Transmit(PacketType.SpawnPosition, (int)X, (int)Y, (int)Z);
			_Conn.Transmit(PacketType.PlayerPositionLook, X, Y, Y, Z, Yaw, Pitch, (byte)1);
		}
		
		public void Despawn()
		{
			CXMineServer.Server.Despawn(this);
			Spawned = false;
			CurrentChunk = null;
		}

        private void loadPlayerData() {
			string userPath=Path.Combine(CXMineServer.Server.World.WorldName,playersPath + Username + ".dat");
            if (!File.Exists(userPath))
			{
                File.Create(userPath);
                return;
            }
            using(StreamReader reader = new StreamReader(userPath)) {
				using(GZipStream zipStream = new GZipStream(reader.BaseStream, CompressionMode.Decompress)) {
					BinaryTag player = NbtParser.ParseTagStream(zipStream);
					//CXMineServer.Log(player.CompoundToString("Player", ""));
					_Conn.Transmit(PacketType.UpdateHealth, player["Health"].Payload);
					X = (double)player["Pos"][0].Payload;
					Y = (double)player["Pos"][1].Payload + 2;
					Z = (double)player["Pos"][2].Payload;
					Yaw = (float)player["Rotation"][0].Payload;
					Pitch = (float)player["Rotation"][1].Payload;

					BinaryTag[] _inventory = (BinaryTag[])player["Inventory"].Payload;
					//CXMineServer.Log(player["Inventory"].CompoundToString("Inventory", ""));
					for (uint i = 0; i < _inventory.Length; i++) {
						// Send inventory item to client
						int slot = (int)((byte)_inventory[i]["Slot"].Payload);
						short realSlot =  Inventory.FileToGameSlot(slot);
						inventory.AddToPosition(realSlot, (short)_inventory[i]["id"].Payload,
						                        (short)(byte)_inventory[i]["Count"].Payload,
						                        (short)_inventory[i]["Damage"].Payload);
						// Converting from player's .dat inventory slot to game's inventory slot
						_Conn.Transmit(PacketType.SetSlot, (byte)0, realSlot,
						               _inventory[i]["id"].Payload,
						               _inventory[i]["Count"].Payload,
						               (byte)((short)_inventory[i]["Damage"].Payload));
					}
				}
            }
        }

		public void SendMessage(string message)
		{
			_Conn.Transmit(PacketType.Message, message);
		}
		
		public void RecvMessage(string message)
		{
			CXMineServer.Log("<" + Username + "> " + message);
			CXMineServer.Server.MessageAll("<" + Username + "> " + message);
		}
		
		public override void Update()
		{
			Chunk newChunk = CXMineServer.Server.World.GetChunkAt((int)X, (int)Z);
			
			if (newChunk != CurrentChunk) {
				List<Chunk> newVisibleChunks = new List<Chunk>();

				newVisibleChunks.AddRange(CXMineServer.Server.World.GetChunksInRange(newChunk));

				foreach (Chunk c in VisibleChunks) {
					if (!newVisibleChunks.Contains(c)) {
						_Conn.Transmit(PacketType.PreChunk, c.ChunkX, c.ChunkZ, (byte) 0);
					}
				}

				foreach (Chunk c in newVisibleChunks) {
					if (!visibleChunks.Contains(c)) {
						_Conn.SendChunk(c);
					}
				}
				
				visibleChunks = newVisibleChunks;
			}
			
			List<Entity> newVisibleEntities = new List<Entity>();
			foreach (Chunk c in VisibleChunks) {
				newVisibleEntities.AddRange(c.Entities);
			}
			foreach (Entity e in VisibleEntities) {
				if (!newVisibleEntities.Contains(e)) {
					DespawnEntity(e);
				}
			}
			foreach (Entity e in newVisibleEntities) {
				if (!VisibleEntities.Contains(e)) {
					SpawnEntity(e);
				}
			}
			VisibleEntities = newVisibleEntities;
			
			_Conn.Transmit(PacketType.TimeUpdate, CXMineServer.Server.World.Time);
			base.Update();
		}
		
		private void DespawnEntity(Entity e)
		{
			if (e == this) return;
			_Conn.Transmit(PacketType.DestroyEntity, e.EntityID);
		}
		
		private void SpawnEntity(Entity e)
		{
			if (e == this) return;

			Player p = e as Player;
			if (p != null) {
                CXMineServer.Log("Spawning Entity " + p.Username + "(" + p.EntityID + ") on "
				                 + Username + "(" + EntityID + ") client");
				_Conn.Transmit(PacketType.NamedEntitySpawn, p.EntityID,
					p.Username, (int)p.X, (int)p.Y, (int)p.Z,
					(byte)0, (byte)0, (short)p.inventory.HoldingPos);
			} else {
				SendMessage(Color.Purple + "Spawning " + e);
			}
		}
		
		override public string ToString()
		{
			return "[Entity.Player " + EntityID + ": " + Username + "]";
		}
	}
}
