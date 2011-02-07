using System;
using System.Text;
using NBT;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using CXMineServer.Utils;

namespace CXMineServer
{
	public enum Visibility : byte
	{
		Vertical = 0x000000,
		Horizontal = 0x000001,
		Frontal = 0x000002
	}
	public class Chunk
	{
		public int ChunkX
		{
			get;
			private set;
		}
		public int ChunkZ
		{
			get;
			private set;
		}
		public List<Entity> Entities
		{
			get;
			private set;
		}

		private List<Item> m_Items;

		public List<Item> Items
		{
			get { return m_Items; }
			set { m_Items = value; }
		}

		private BinaryTag _Structure;
		private Map _World;

		private List<BlockLine> visibleHeightLine;
		private List<BlockLine> visibleFrontLine;
		private List<BlockLine> visibleWidthLine;

		private Visibility _VisFlags;

		public Visibility VisFlags
		{
			get { return _VisFlags; }
			set { _VisFlags = value; }
		}

		public Chunk(int chunkX, int chunkZ, Map world)
		{
			ChunkX = chunkX;
			ChunkZ = chunkZ;
			_World = world;
			Entities = new List<Entity>();
			Items = new List<Item>();
			Load();
		}

		public void Generate()
		{
			byte[] blocks = new byte[32768], data = new byte[16384];
			byte[] skylight = new byte[16384], light = new byte[16384];
			byte[] height = new byte[256];
			BinaryTag[] entities = new BinaryTag[0], tileEntities = new BinaryTag[0];

			for (int i = 0; i < 16348; ++i)
			{
				blocks[i * 2] = (byte)BlockType.Rock;
				skylight[i] = 0xFF;
				light[i] = 0xFF;
			}

			BinaryTag[] structure = new BinaryTag[] {
				new BinaryTag(TagType.ByteArray, blocks, "Blocks"),
				new BinaryTag(TagType.ByteArray, data, "Data"),
				new BinaryTag(TagType.ByteArray, skylight, "SkyLight"),
				new BinaryTag(TagType.ByteArray, light, "BlockLight"),
				new BinaryTag(TagType.ByteArray, height, "HeightMap"),
				new BinaryTag(TagType.List, entities, "Entities"),
				new BinaryTag(TagType.List, tileEntities, "TileEntities"),
				new BinaryTag(TagType.Long, (long) 0, "LastUpdate"),
				new BinaryTag(TagType.Int, (int) ChunkX, "xPos"),
				new BinaryTag(TagType.Int, (int) ChunkZ, "zPos"),
				new BinaryTag(TagType.Byte, (byte) 0, "TerrainPopulated")
			};

			_Structure = new BinaryTag(TagType.Compound, new BinaryTag[] {
				new BinaryTag(TagType.Compound, structure, "Level")
			});
			Save();
		}

		public void Save()
		{
			string filename = CalculateFilename();
			int i = filename.LastIndexOfAny(new char[] { '/', '\\', ':' });
			Directory.CreateDirectory(filename.Substring(0, i));

			using (FileStream rawWriter = File.OpenWrite(filename))
			{
				using (GZipStream writer = new GZipStream(rawWriter, CompressionMode.Compress))
				{
					NbtWriter.WriteTagStream(_Structure, writer);
				}
			}
		}

		public void Load()
		{
			try
			{
				using (FileStream rawReader = File.OpenRead(CalculateFilename()))
				{
					using (GZipStream reader = new GZipStream(rawReader, CompressionMode.Decompress))
					{
						_Structure = NbtParser.ParseTagStream(reader);
					}
				}
				//CXMineServer.Log(_Structure.CompoundToString("structure", ""));
			}
			catch (FileNotFoundException)
			{
				Generate();
			}
			catch (DirectoryNotFoundException)
			{
				Generate();
			}

			//CheckVisibility();
		}

		public byte[] GetBytes()
		{
			List<Byte> builder = new List<Byte>();
			builder.AddRange((byte[])_Structure["Level"]["Blocks"].Payload);
			builder.AddRange((byte[])_Structure["Level"]["Data"].Payload);
			builder.AddRange((byte[])_Structure["Level"]["BlockLight"].Payload);
			builder.AddRange((byte[])_Structure["Level"]["SkyLight"].Payload);
			return builder.ToArray();
		}

		public void CheckVisibility()
		{
			BlockLine[] heightLine = new BlockLine[256];
			BlockLine[] frontLine = new BlockLine[2048];
			BlockLine[] widthLine = new BlockLine[2048];

			int hLineIdx = 0;
			int fLineidx = 0;
			int wlineIdx = 0;

			for (byte z = 0; z < 16; ++z)
			{
				for (byte y = 0; y < 128; ++y)
				{
					for (byte x = 0; x < 16; ++x)
					{
						hLineIdx = x + 16 * z;
						fLineidx = x + 16 * y;
						wlineIdx = z + 16 * y;

						switch (GetBlock(x, y, z))
						{
							case BlockType.Air:
							case BlockType.Fire:
							case BlockType.Glass:
							case BlockType.Ice:
							case BlockType.Leaves:
							case BlockType.BrownMushroom:
							case BlockType.Cactus:
							case BlockType.Crops:
							case BlockType.IronDoor:
							case BlockType.Fence:
							case BlockType.Gold:
							case BlockType.Ladder:
							case BlockType.Lever:
							case BlockType.MinecartTracks:
							case BlockType.MobSpawner:
							case BlockType.RedFlower:
							case BlockType.RedMushroom:
							case BlockType.RedstoneTorchOff:
							case BlockType.RedstoneTorchOn:
							case BlockType.RedstoneWire:
							case BlockType.Reed:
							case BlockType.Sapling:
							case BlockType.Signpost:
							case BlockType.SnowSurface:
							case BlockType.Torch:
							case BlockType.WallSign:
							case BlockType.Water:
							case BlockType.WoodenDoor:
							case BlockType.WoodenPressurePlate:
							case BlockType.YellowFlower:
								{
									BlockLine currentLine = heightLine[hLineIdx];
									if (currentLine == null)
										currentLine = new BlockLine(x, y, z);

									++currentLine.Counter;

									currentLine = frontLine[fLineidx];

									if (currentLine == null)
										currentLine = new BlockLine(x, y, z);

									++currentLine.Counter;

									currentLine = widthLine[wlineIdx];
									if (currentLine == null)
										currentLine = new BlockLine(x, y, z);

									++currentLine.Counter;


									break;
								}

							default:
								break;

						}
					}
				}
			}

			for (int i = 0; i < heightLine.Length; ++i)
			{
				if (heightLine[i].Counter == 16)
					visibleHeightLine.Add(heightLine[i]);
			}

			for (int i = 0; i < frontLine.Length; ++i)
			{
				if (frontLine[i].Counter == 128)
					visibleFrontLine.Add(frontLine[i]);

				if (widthLine[i].Counter == 128)
					visibleWidthLine.Add(widthLine[i]);
			}

			if (visibleHeightLine.Count > 0)
				_VisFlags |= Visibility.Vertical;

			if (visibleFrontLine.Count > 0)
				_VisFlags |= Visibility.Frontal;

			if (visibleWidthLine.Count > 0)
				_VisFlags |= Visibility.Horizontal;
		}

		// ====================
		// Tile gets/sets

		public BlockType GetBlock(int x, int y, int z)
		{
			return (BlockType)((byte[])(_Structure["Level"]["Blocks"].Payload))[BlockIndex(x, y, z)];
		}

		public void SetBlock(int x, int y, int z, BlockType block)
		{
			((byte[])(_Structure["Level"]["Blocks"].Payload))[BlockIndex(x, y, z)] = (byte)block;
		}

		public byte GetData(int x, int y, int z)
		{
			int index = BlockIndex(x, y, z) / 2;

			return ((byte[])(_Structure["Level"]["Data"].Payload))[index];
		}

		public void SetData(int x, int y, int z, byte data)
		{
			int index = BlockIndex(x, y, z) / 2;
			((byte[])(_Structure["Level"]["Data"].Payload))[index] = data;
		}

		public byte GetLight(int x, int y, int z)
		{
			int index = BlockIndex(x, y, z) / 2;
			return ((byte[])(_Structure["Level"]["BlockLight"].Payload))[index];
		}

		public void SetLight(int x, int y, int z, byte data)
		{
			int index = BlockIndex(x, y, z) / 2;
			((byte[])(_Structure["Level"]["BlockLight"].Payload))[index] = data;
		}

		public byte GetSkyLight(int x, int y, int z)
		{
			int index = BlockIndex(x, y, z) / 2;
			return ((byte[])(_Structure["Level"]["SkyLight"].Payload))[index];
		}

		public void SetSkyLight(int x, int y, int z, byte data)
		{
			int index = BlockIndex(x, y, z) / 2;
			((byte[])(_Structure["Level"]["SkyLight"].Payload))[index] = data;
		}

		public static void PlaceBlock(NetState from, int id, int x, int y, int z, int direction)
		{
			int meta = MetaHtN(direction);

			switch (direction)
			{ // Direction
				case 0:
					{ // -Y
						CXMineServer.Log("-Y");
						y -= 1;
						break;
					}
				case 1:
					{ // +Y
						CXMineServer.Log("+Y");
						y += 1;
						break;
					}
				case 2:
					{ // -Z
						CXMineServer.Log("-Z");
						z -= 1;
						break;
					}
				case 3:
					{ // +Z
						CXMineServer.Log("+Z");
						z += 1;
						break;
					}
				case 4:
					{ // -X
						CXMineServer.Log("-X");
						x -= 1;
						break;
					}
				case 5:
					{ // +X
						CXMineServer.Log("+X");
						x += 1;
						break;
					}
			}

			int _x = x & 15, _z = z & 15;

			if (!Utility.IsInRange(from.Owner, x*32+16, z*32+16, 128) || !from.Owner.CanPlace(y) || (id == (int)BlockType.Torch && direction == 0))
			{
				// No need to rollback, no block exist here
				if(y > 127 || y < -128)
					return;
				// Rollback to the precedent situation if the placement is invalid
				BlockType block = CXMineServer.Server.World.GetChunkAt(x, z).GetBlock(_x, y, _z);
				byte data = (byte)MetaHtN((int)CXMineServer.Server.World.GetChunkAt(x, z).GetData(_x, y, _z));
				from.BlockChange(x, (byte)y, z, (byte)block, data);

				Inventory.Slot slot = from.Owner.inventory.GetItem(from.Owner.inventory.HoldingPos + 36);
				from.SetSlot(0, (short)slot.Position, slot.Id, (byte)slot.Count, slot.Uses);
				return;
			}

			// Get the current chunk
			Chunk chunk = CXMineServer.Server.World.GetChunkAt(x, z);
			from.BlockChange(x, (byte)y, z, (byte)id, (byte)meta);
			// For each player using that chunk, update the block data
			foreach (Player p in CXMineServer.Server.PlayerList)
			{
				if (p == from.Owner)
					continue;

				int chunkX = PlayerToChunkPosition(p.X);
				int chunkZ = PlayerToChunkPosition(p.Z);

				if (DistanceBetweenChunks(chunk, CXMineServer.Server.World.GetChunkAt(chunkX, chunkZ)) <= Map.visibleChunks)
					from.BlockChange(x, (byte)y, z, (byte)id, (byte)meta);
			}

			// Update the chunk's data on the server
			try
			{
				chunk.SetBlock(_x, y, _z, (BlockType)id);
			}
			catch (System.Exception ex)
			{
				CXMineServer.Log("Exception: " + ex.Message + "\n\nXYZ: " + _x + " " + y + " " + _z);
			}
			
			// Decrement the inventory counter
			int pos = from.Owner.inventory.HoldingPos;
			from.Owner.inventory.Remove(pos + 36);

			// Handle Count == 0 and so ID == -1 (Needed by the different packet format)
			if (from.Owner.inventory.GetItem(pos).Id == -1)
				from.SetSlot(0, (short)pos, (short)-1);
			else
			{
				Inventory.Slot slot = from.Owner.inventory.GetItem(pos);
				from.SetSlot(0, (short)pos, slot.Id, (byte)slot.Count, slot.Uses);
			}
		}

		public static void DestroyBlock(NetState from, int x, int y, int z, int face)
		{
			// Get the chunk the player is digging in
			Chunk chunk = CXMineServer.Server.World.GetChunkAt(x, z);

			// Get a new EID for the spawn
			int eid = Server.getEID();
			// Get relative X and Z coordinate in the chunk
			int _x = x & 15, _z = z & 15;

			// Get the block data in the chunk
			BlockType block = chunk.GetBlock(_x, y, _z);
			// Update the chunk with the new block
			chunk.SetBlock(_x, y, _z, BlockType.Air);
			// Manage special spawn case where the destroyed block isn't the one to spawn
			if (block == BlockType.Grass)
				block = BlockType.Dirt;
			if (block == BlockType.Rock)
				block = BlockType.Cobblestone;

			int itemX = x * 32 + 16;
			int itemZ = z * 32 + 16;
			int itemY = y * 32;

			int prevDistance = Utility.DistanceBetweenEntities(from.Owner, itemX, itemZ); // It's the power of 2 distance
			Player picksUp = null;

			from.BlockChange(x, (byte)y, z, (byte)BlockType.Air, 0);
			from.PickupSpawn(eid, (short)block, 1, 0, itemX, itemY, itemZ, 0, 0, 0);

			foreach (Player p in CXMineServer.Server.PlayerList)
			{
				if (p == from.Owner)
					continue;

				int chunkX = PlayerToChunkPosition(p.X);
				int chunkZ = PlayerToChunkPosition(p.Z);

				if (DistanceBetweenChunks(chunk, CXMineServer.Server.World.GetChunkAt(chunkX, chunkZ)) <= (Map.visibleChunks * Map.visibleChunks))
					p.State.BlockChange(x, (byte)y, z, (byte)BlockType.Air, 0);

				if (DistanceBetweenChunks(chunk, CXMineServer.Server.World.GetChunkAt(chunkX, chunkZ)) <= 1)
					p.State.PickupSpawn(eid, (short)block, 1, 0, itemX, itemY, itemZ, 0, 0, 0);

				int distance;
				if (Utility.IsInRange(p, itemX, itemZ, 40, out distance) && p.CanPick(itemY))
				{
					if (distance < prevDistance)
					{
						prevDistance = distance;
						picksUp = p;
					}
				}
			}

			Item newItem = null;

			if (picksUp != null)
				picksUp.State.CollectItem(eid, picksUp.EntityID, (short)block, 0);
			else if (prevDistance <= 1600 && from.Owner.CanPick(itemY))
				from.CollectItem(eid, from.Owner.EntityID, (short)block, 0);
			else
			{
				newItem = new Item(chunk);
				newItem.Type = (int)block;
				newItem.X = itemX;
				newItem.Y = itemY;
				newItem.Z = itemZ;
				newItem.Yaw = 0.0f;
				newItem.Pitch = 0.0f;
				newItem.EId = eid;
				newItem.Uses = 1;

				chunk.Items.Add(newItem);
			}

			// Spawn a new object to collect
			/*Transmit(PacketType.PickupSpawn, eid, (short)block, (byte)1, (int)packet[2] * 32 + 16, (int)((byte)packet[3]) * 32, (int)packet[4] * 32 + 16, (byte)0, (byte)0, (byte)0);
			// Collect the block instantly (TODO: Collect the block if near the player)
			Transmit(PacketType.CollectItem, eid, _Player.EntityID);
			// Destroy the entity beacuse it's collected
			Transmit(PacketType.DestroyEntity, eid);
			// Update the inventory coherently
			int slot = _Player.inventory.Add((short)block);
			CXMineServer.Log("Sent to slot " + slot.ToString());
			Transmit(PacketType.SetSlot, (byte)0, slot, (short)block, (byte)_Player.inventory.GetItem(slot).Count, (byte)0);*/
		}

		public void DeleteItem(Item i)
		{
			Items.Remove(i);
		}

		private static int MetaHtN(int meta)
		{
			switch (meta)
			{
				case 0:
					return 5;

				case 1:
					return 0;

				case 2:
					return 4;

				case 3:
					return meta;

				case 4:
					return 2;

				case 5:
					return 1;

				default:
					return 0;
			}
		}

		// ====================
		// Helper functions

		private static int BlockIndex(int x, int y, int z)
		{
			return y + (z * 128 + (x * 128 * 16));
		}

		private string CalculateFilename()
		{
			int modX = (ChunkX >= 0 ? ChunkX % 64 : 64 - Math.Abs(ChunkX) % 64);
			int modZ = (ChunkZ >= 0 ? ChunkZ % 64 : 64 - Math.Abs(ChunkZ) % 64);
			StringBuilder sb = new StringBuilder();
			return (sb.Append(_World.WorldName).Append("/")
						.Append(CXMineServer.Base36Encode(modX)).Append("/")
						.Append(CXMineServer.Base36Encode(modZ)).Append("/")
						.Append("c.").Append(CXMineServer.Base36Encode(ChunkX))
						.Append(".").Append(CXMineServer.Base36Encode(ChunkZ))
						.Append(".dat").ToString());
		}

		public override string ToString()
		{
			return "[Chunk at " + ChunkX + ", " + ChunkZ + "]";
		}


		public static int DistanceBetweenChunks(Chunk from, Chunk to)
		{
			int distanceX = Math.Abs(to.ChunkX - from.ChunkX);
			int distanceZ = Math.Abs(to.ChunkZ - from.ChunkZ);

			return distanceX * distanceX + distanceZ * distanceZ;
		}

		public static int PlayerToChunkPosition(double pos)
		{
			return ((int)pos) >> 4;
		}
	}
}
