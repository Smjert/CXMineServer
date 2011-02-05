using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using NBT;
using CXMineServer.Utils;

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
		public int HoldingPos
		{
			get;
			set;
		}

		public Inventory()
		{
			HoldingPos = 0;
			slotList = new List<Slot>(45);
			CXMineServer.Log("Inventory Capacity: " + slotList.Capacity);
			for (int i = 0; i < slotList.Capacity; i++)
				slotList.Add(new Slot(this));
		}

		public int Add(short id)
		{
			int slot = GetFirstAvailableSlotFor(id);

			if (slot == -1)
				return -1;

			AddToPosition(slot, id, 1, 0);

			return slot;
		}

		public void AddToPosition(int position, short id, short count, short uses)
		{
			if (slotList[position].Id == -1)
			{
				CXMineServer.Log("Adding id " + id);
				slotList[position] = new Slot(this, count, id, uses, position);
			}
			else
				slotList[position].Count += 1;
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
			if (slot <= 8)
				return (short)(slot + 36);
			if (slot <= 35)
				return slot;
			if (slot <= 83)
				return (short)(slot - 79);
			return (short)(108 - slot);
		}

		public static short GameToFileSlot(short slot)
		{
			if (slot == 0)
				return (short)80; //HACK
			if (slot <= 4)
				return (short)(slot + 79);
			if (slot <= 8)
				return (short)(108 - slot);
			if (slot <= 35)
				return slot;
			return (short)(slot - 36);
		}

		private int GetFirstAvailableSlotFor(short id)
		{
			// TODO: non dovresti controllare che ci siano meno di 64 item nello slot?

			/* First we control if there's already the same block type on the quickbar or if there's a free slot.
			 * If nothing is found we check the internal inventory */
			int slot = -1;
			if ((slot = QuickBarFindIdSlot(id)) == -1)
			{
				for (int i = 9; i < 36; i++)
				{
					if (slotList[i].Count == 64)
						continue;

					if (slotList[i].Id == id)
						return i;
					else if (slot == -1 && slotList[i].Id == -1)
						slot = i;
				}
			}

			return slot;
		}

		private int QuickBarFindIdSlot(short id)
		{
			int freeSlot = -1;

			for (int i = 36; i < slotList.Count; ++i)
			{
				if (slotList[i].Count == 64)
					continue;

				if (slotList[i].Id == id)
					return i;
				else if (freeSlot == -1 && slotList[i].Id == -1)
					freeSlot = i;
			}

			return freeSlot;
		}
	}

	public class Player : Entity
	{
		private NetState _State;
		public string Username = "";
		public bool Spawned;

		

		private List<Chunk> visibleChunks = new List<Chunk>();
		public IEnumerable<Chunk> VisibleChunks
		{
			get
			{
				return visibleChunks;
			}
		}

		public NetState State
		{
			get { return _State; }
			set { _State = value; }
		}

		private List<Entity> VisibleEntities = new List<Entity>();

		public Inventory inventory;

		private const string playersPath = "players/";

		private Timer _UpdateTimer;

		public Player()
		{
			_UpdateTimer = new UpdateTimer(this);
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
			_State.SpawnPosition((int)X, (int)Y, (int)Z);
			_State.PlayerPositionLook(X, 0.0, Y, Z, Yaw, Pitch, 1);
		}

		public void Despawn()
		{
			CXMineServer.Server.Despawn(this);
			Spawned = false;
			CurrentChunk = null;
		}

		private void loadPlayerData()
		{
			string userPath = Path.Combine(CXMineServer.Server.World.WorldName, playersPath + Username + ".dat");
			if (!File.Exists(userPath))
			{
				File.Create(userPath);
				return;
			}
			using (StreamReader reader = new StreamReader(userPath))
			{
				using (GZipStream zipStream = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
				{
					BinaryTag player = NbtParser.ParseTagStream(zipStream);
					//CXMineServer.Log(player.CompoundToString("Player", ""));
					State.UpdateHealth(player["Health"].Payload);

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
						short slot = (short)((byte)_inventory[i]["Slot"].Payload);
						short realSlot = Inventory.FileToGameSlot(slot);
						inventory.AddToPosition(realSlot, (short)_inventory[i]["id"].Payload,
																		(short)(byte)_inventory[i]["Count"].Payload,
																		(short)_inventory[i]["Damage"].Payload);
						// Converting from player's .dat inventory slot to game's inventory slot
						_State.SetSlot(0, realSlot,
													 (short)_inventory[i]["id"].Payload,
													 (byte)_inventory[i]["Count"].Payload,
													 (short)_inventory[i]["Damage"].Payload);
					}
				}
			}
		}

		public void SendMessage(string message)
		{
			_State.Message(message);
		}

		public void RecvMessage(string message)
		{
			if (message == "/save")
			{
				CXMineServer.Server.World.ForceSave();
				return;
			}
			else if (message == "/quit")
			{
				CXMineServer.Server.Quit();
				return;
			}
			CXMineServer.Log("<" + Username + "> " + message);
			CXMineServer.Server.MessageAll("<" + Username + "> " + message);
		}

		public override void Update()
		{
			Chunk newChunk = CXMineServer.Server.World.GetChunkAt((int)X, (int)Z);

			if (newChunk != CurrentChunk)
			{
				List<Chunk> newVisibleChunks = new List<Chunk>();

				newVisibleChunks.AddRange(CXMineServer.Server.World.GetChunksInVisibilityRange(newChunk));

				foreach (Chunk c in VisibleChunks)
				{
					if (!newVisibleChunks.Contains(c))
					{
						_State.PreChunk(c.ChunkX, c.ChunkZ, 0);
					}
				}

				foreach (Chunk c in newVisibleChunks)
				{
					if (!visibleChunks.Contains(c))
					{
						_State.SendChunk(c);
					}
				}

				visibleChunks = newVisibleChunks;
			}

			List<Entity> newVisibleEntities = new List<Entity>();
			foreach (Chunk c in VisibleChunks)
			{
				newVisibleEntities.AddRange(c.Entities);
			}
			foreach (Entity e in VisibleEntities)
			{
				if (!newVisibleEntities.Contains(e))
				{
					DespawnEntity(e);
				}
			}
			foreach (Entity e in newVisibleEntities)
			{
				if (!VisibleEntities.Contains(e))
				{
					SpawnEntity(e);
				}
			}
			VisibleEntities = newVisibleEntities;

			FindPickupObjects(newChunk);

			CalculateTime();

			if (_UpdateTimer.Running)
				_UpdateTimer.Stop();

			_UpdateTimer.Start();

			CXMineServer.SendLogFile(DateTime.Now + " Update");

			base.Update();
		}

		public void FindPickupObjects(Chunk newChunk)
		{
			List<Item> toRemove = new List<Item>();

			foreach (Chunk c in CXMineServer.Server.World.GetChunksInRange(newChunk, 1))
			{
				foreach (Item i in c.Items)
				{
					if (Utility.IsInRange(this, i, 40) && CanPick(i))
					{
						State.CollectItem(i.EId, EntityID, (short)i.Type);
						toRemove.Add(i);
					}
				}
			}

			for (int i = 0; i < toRemove.Count; ++i)
				toRemove[i].Delete();

			toRemove.Clear();
		}

		public bool CanPick(Item i)
		{
			int pY = (int)(Y * 32.0);
			if ((pY - 51) > i.Y)
				return (pY - 51) - i.Y <= 16;
			else
				return i.Y - pY <= 16;
		}

		public bool CanPick(int itemY)
		{
			int pY = (int)(Y * 32.0);
			if ((pY - 51) > itemY)
				return (pY - 51) - itemY <= 16;
			else
				return itemY - pY <= 16;
		}

		private void CalculateTime()
		{
			TimeSpan timePassed = DateTime.Now - CXMineServer.Server.LastUpdateTime;
			CXMineServer.Server.MinecraftTime += (long)(20.0 * timePassed.TotalSeconds);

			if (CXMineServer.Server.MinecraftTime > 24000)
				CXMineServer.Server.MinecraftTime -= 24000;

			CXMineServer.Server.LastUpdateTime = DateTime.Now;
			_State.TimeUpdate(CXMineServer.Server.MinecraftTime);
		}

		private void DespawnEntity(Entity e)
		{
			// If this function doesn't work on itself... then it must be static or put out of here
			if (e == this) return;
			_State.DestroyEntity(e.EntityID);
		}

		private void SpawnEntity(Entity e)
		{
			// If this function doesn't work on itself... then it must be static or put out of here
			if (e == this) return;

			Player p = e as Player;
			if (p != null)
			{
				CXMineServer.Log("Spawning Entity " + p.Username + "(" + p.EntityID + ") on "
								 + Username + "(" + EntityID + ") client");
				_State.NamedEntitySpawn(p.EntityID,
					p.Username, (int)p.X, (int)p.Y, (int)p.Z,
					(byte)0, (byte)0, (short)p.inventory.HoldingPos);
			}
			else
			{
				SendMessage(Color.Purple + "Spawning " + e);
			}
		}

		override public string ToString()
		{
			return "[Entity.Player " + EntityID + ": " + Username + "]";
		}

		public class UpdateTimer : Timer
		{
			private Player _Player;
			public UpdateTimer(Player player) : base(TimeSpan.FromSeconds(1.0), true)
			{
				_Player = player;
			}

			protected override void OnTick()
			{
				_Player.Update();

				base.OnTick();
			}
		}

		/*public class PickObjects : Timer
		{
			private Player _Player;
			public PickObjects(Player player) : base(TimeSpan.FromSeconds(0.5), true)
			{
				_Player = player;
			}

			protected override void OnTick()
			{
				_Player.FindPickupObjects();

				base.OnTick();
			}
		}*/
	}
}
