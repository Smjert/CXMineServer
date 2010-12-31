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
        private class Slot
        {
            private Inventory _Inventory;

            private int position;
            public int Position
            {
                get
                {
                    return position;
                }
                set
                {
                    position = value;
                }
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
                        _Inventory.SplitStackForSlot(position);
                    }
                        
                    if (count == (short)0)
                        _Inventory.ResetSlot(position);
                }
            }

            private short id;
            public short Id
            {
                get
                {
                    return id;
                }
                set
                {
                    id = value;
                }
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
                        _Inventory.ResetSlot(position);
                }
            }

            private Slot() { }
            public Slot(Inventory inv, short count = 0, short id = -1, short uses = 0, int position = -1)
            {
                _Inventory = inv;
                Count = count;
                Id = id;
                Uses = uses;
                Position = position;
            }
        }



        private List<Slot> slotList;

        public Inventory()
        {
            slotList = new List<Slot>(45);
            for (int i = 0; i < slotList.Capacity; i++)
                slotList[i] = new Slot(this);
        }

        public int Add(short id)
        {
            int slot = GetFirstAvailableSlotFor(id);
            if (slot == (short)-1)
                return -1;
            if (slotList[slot].Id == (short)-1)
                AddToPosition(slot, id, 1, 0);
            else
                slotList[slot].Count += 1;
            return slot;
        }

        public void AddToPosition(int position, short id, short count, short uses)
        {
            slotList[position] = new Slot(this, count, id, uses, position);
        }

        public void Remove(int position, short id, int quantity = 1)
        {
            slotList[position].Count -= 1;
        }

        public void ResetSlot(int position)
        {
            slotList[position] = new Slot(this);
        }

        public object GetItem(int position)
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

        public static short FileToGameSlot(int slot)
        {
            return (short)(44 - slot - 1 + (9 - ((44 - slot) % 9)) - (44 - slot) % 9);
        }

        public static short GameToFileSlot(int slot)
        {
            return 0;
            // TODO: Invertire logica dell'altro metodo
        }

        private int GetFirstAvailableSlotFor(short id)
        {
            for (int i = 0; i < slotList.Capacity; i++ )
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
		public string Username;
		public bool Spawned;
		
		public List<Chunk> VisibleChunks;
		public List<Entity> VisibleEntities;

        private short[] inventory;

        private int eid;
        public int Eid
        {
            get {
                return eid;
            }
        }

        private const string playersPath = "players/";
		
		public Player(TcpClient client)
		{
			_Conn = new Connection(client, this);
			Username = "";
			Spawned = false;
			CurrentChunk = null;
			VisibleChunks = new List<Chunk>();
			VisibleEntities = new List<Entity>();
            eid = CXMineServer.Server.getEID();
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
            inventory = new short[45];
            for (uint i = 0; i < inventory.Length; i++)
                inventory[i] = (short)-1;
            loadPlayerData();
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
            if (!File.Exists(CXMineServer.Server.World.WorldName + "/" + playersPath + Username + ".dat"))
            {
                StreamWriter writer = new StreamWriter(CXMineServer.Server.World.WorldName + "/" + playersPath + Username + ".dat");
                writer.Close();
                return;
            }
            StreamReader reader = new StreamReader(CXMineServer.Server.World.WorldName + "/" + playersPath + Username + ".dat");
            GZipStream zipStream = new GZipStream(reader.BaseStream, CompressionMode.Decompress);
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
            for (uint i = 0; i < _inventory.Length; i++)
            {
                // Send inventory item to client
                int slot = (int)((byte)_inventory[i]["Slot"].Payload);
                short realSlot =  (short)(44 - slot - 1 + (9 - ((44 - slot) % 9)) - (44 - slot) % 9);
                inventory[realSlot] = (short)_inventory[i]["id"].Payload;
                // Converting from player's .dat inventory slot to game's inventory slot
                _Conn.Transmit(PacketType.SetSlot, (byte)0, realSlot,
                                                   _inventory[i]["id"].Payload,
                                                   _inventory[i]["Count"].Payload,
                                                   (byte)((short)_inventory[i]["Damage"].Payload));
            }

            reader.Close();
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
				
				foreach (Chunk c in CXMineServer.Server.World.GetChunksInRange(newChunk)) {
					newVisibleChunks.Add(c);
				}
				foreach (Chunk c in VisibleChunks) {
					if (!newVisibleChunks.Contains(c)) {
						_Conn.Transmit(PacketType.PreChunk, c.ChunkX, c.ChunkZ, (byte) 0);
					}
				}
				foreach (Chunk c in newVisibleChunks) {
					if (!VisibleChunks.Contains(c)) {
						_Conn.SendChunk(c);
					}
				}
				
				VisibleChunks = newVisibleChunks;
			}
			
			List<Entity> newVisibleEntities = new List<Entity>();
			foreach (Chunk c in VisibleChunks) {
				foreach (Entity e in c.Entities) {
					newVisibleEntities.Add(e);
				}
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
			
			if (e is Player) {
				Player p = (Player) e;
				_Conn.Transmit(PacketType.NamedEntitySpawn, p.EntityID,
					p.Username, (int)p.X, (int)p.Y, (int)p.Z,
					(byte)0, (byte)0, (short)1);
			} else {
				SendMessage(Color.Purple + "Spawning " + e);
			}
		}
		
		override public string ToString()
		{
			return "[Entity.Player " + EntityID + ": " + Username + "]";
		}

        public void AddToInventory(int position, short ID)
        {
            if (position < 0 || position > 44)
            {
                CXMineServer.Log("Error! Invalid Inventory Position");
                return;
            }
            inventory[position] = ID;
        }

        public void RemoveFromInventoryA(short ID)
        {
            for (uint i = 0; i < inventory.Length; i++)
            {
                if (inventory[i] == ID)
                {
                    // TODO: Decrement
                    // se uguale a 0, eliminare
                }
            }
        }

        public short GetInventoryItem(int position)
        {
            return inventory[position];
        }

        public int GetSlotFor(short ID)
        {
            for (short i = 0; i < inventory.Length; i++)
            {
                if (inventory[i] == ID)
                    return i;
            }

            for (short i = 0; i < inventory.Length; i++)
            {
                if (inventory[i] == (short)-1)
                    return i;
            }

            return -1;
        }

        public short FileToGameSlot(int slot)
        {
            return (short)(44 - slot - 1 + (9 - ((44 - slot) % 9)) - (44 - slot) % 9);
        }

        public short GameToFileSlot(int slot)
        {
            return 0;
            // TODO: Invertire logica dell'altro metodo
        }
		
	}
}
