using System;
using System.Collections.Generic;
using NBT;
using System.IO;
using System.IO.Compression;

namespace CXMineServer
{
	public class Map
	{
		public string WorldName{
			get;
			private set;
		}

		private Dictionary<long, Chunk> _Chunks = new Dictionary<long, Chunk>();
		private BinaryTag _Structure;

		public const int visibleChunks = 8;
		
		public Map(string name)
		{
			WorldName = name;
		}
		
		#region Properties
		
		public int SpawnX { get { return (int)(_Structure["Data"]["SpawnX"].Payload); }
							set { _Structure["Data"]["SpawnX"].Payload = value; } }
		public int SpawnZ { get { return (int)(_Structure["Data"]["SpawnZ"].Payload); }
							set { _Structure["Data"]["SpawnZ"].Payload = value; } }
		public int SpawnY { get { return (int)(_Structure["Data"]["SpawnY"].Payload); }
							set { _Structure["Data"]["SpawnY"].Payload = value; } }
		public long Time  { get { return (long)(_Structure["Data"]["Time"].Payload); }
							set { _Structure["Data"]["Time"].Payload = value; } }
		
		#endregion

		public bool LoadLevel()
		{
			string path = Path.Combine(WorldName, "level.dat");
			if(File.Exists(path))
			{
				using(FileStream rawReader = File.OpenRead(path)) {
					using(GZipStream reader = new GZipStream(rawReader, CompressionMode.Decompress)) {
					_Structure = NbtParser.ParseTagStream(reader);
					}
				}
			}
			else
			{
				CXMineServer.Log("Missing level.dat file, the world cannot start");
				return false;
			}

			return true;
		}
		
		public void Generate()
		{
			// ...
		}
		
		public void ForceSave()
		{
			foreach(KeyValuePair<long, Chunk> kvp in _Chunks) {
				kvp.Value.Save();
			}
		}
		
		public Chunk GetChunk(int chunkX, int chunkZ)
		{			
			List<byte> b = new List<byte>();
			b.AddRange(BitConverter.GetBytes(chunkX));
			b.AddRange(BitConverter.GetBytes(chunkZ));
			long index = BitConverter.ToInt64(b.ToArray(), 0);
			if (_Chunks.ContainsKey(index)) {
				return _Chunks[index];
			} else {
				return _Chunks[index] = new Chunk(chunkX, chunkZ, this);
			}
		}
		
		public Chunk GetChunkAt(int blockX, int blockZ)
		{
			//return GetChunk((int)(blockX / 16) - (blockX < 0 ? 1 : 0), (int)(blockZ / 16) - (blockZ < 0 ? 1 : 0));
			return GetChunk(blockX >> 4, blockZ >> 4);
		}

		public IEnumerable<Chunk> GetChunksInVisibilityRange(Chunk c)
		{
			for (int x = c.ChunkX - visibleChunks; x <= c.ChunkX + visibleChunks; ++x)
			{
				for (int z = c.ChunkZ - visibleChunks; z <= c.ChunkZ + visibleChunks; ++z)
				{
					yield return GetChunk(x, z);
				}
			}
		}

		public IEnumerable<Chunk> GetChunksInRange(Chunk c, int range)
		{
			for (int x = c.ChunkX - range; x <= c.ChunkX + range; ++x)
			{
				for (int z = c.ChunkZ - range; z <= c.ChunkZ + range; ++z)
				{
					yield return GetChunk(x, z);
				}
			}
		}
		
		public static IEnumerable<Entity> EntitiesIn(Chunk c)
		{
			return c.Entities;
		}
	}
}
