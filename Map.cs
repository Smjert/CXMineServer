using System;
using System.Collections.Generic;
using NBT;
using System.IO;
using System.IO.Compression;

namespace CXMineServer
{
	public class Map
	{
		public string WorldName;
		
		private Dictionary<long, Chunk> _Chunks;
		private BinaryTag _Structure;

        private const int visibleChunks = 6;
		
		public Map(string Name)
		{
			WorldName = Name;
			_Chunks = new Dictionary<long, Chunk>();
			
			StreamReader rawReader = new StreamReader(Name + "/level.dat");
			GZipStream reader = new GZipStream(rawReader.BaseStream, CompressionMode.Decompress);
			_Structure = NbtParser.ParseTagStream(reader);
			reader.Close();
			rawReader.Close();
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
			Builder<byte> b = new Builder<byte>();
			b.Append(BitConverter.GetBytes(chunkX));
			b.Append(BitConverter.GetBytes(chunkZ));
			long index = BitConverter.ToInt64(b.ToArray(), 0);
			if (_Chunks.ContainsKey(index)) {
				return _Chunks[index];
			} else {
				return _Chunks[index] = new Chunk(chunkX, chunkZ, this);
			}
		}
		
		public Chunk GetChunkAt(int blockX, int blockZ)
		{
			return GetChunk((int)(blockX / 16) - (blockX < 0 ? 1 : 0), (int)(blockZ / 16) - (blockZ < 0 ? 1 : 0));
		}
		
		public List<Chunk> GetChunksInRange(Chunk c)
		{
			List<Chunk> r = new List<Chunk>();
			for (int x = c.ChunkX - visibleChunks; x <= c.ChunkX + visibleChunks; ++x) {
				for (int z = c.ChunkZ - visibleChunks; z <= c.ChunkZ + visibleChunks; ++z) {
					if (Math.Abs(c.ChunkX - x) + Math.Abs(c.ChunkZ - z) < visibleChunks*2) {
						r.Add(GetChunk(x, z));
					}
				}
			}
			return r;
		}
		
		public List<Entity> EntitiesIn(Chunk c)
		{
			return c.Entities;
		}
	}
}