using System;
using System.Text;
using NBT;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace CXMineServer
{
	public class Chunk
	{
		public int ChunkX{
			get;
			private set;
		}
		public int ChunkZ{
			get;
			private set;
		}
		public List<Entity> Entities{
			get;
			private set;
		}

		private BinaryTag _Structure;
		private Map _World;
		
		public Chunk(int chunkX, int chunkZ, Map world)
		{
			ChunkX = chunkX;
			ChunkZ = chunkZ;
			_World = world;
			Entities=new List<Entity>();
			Load();
		}
		
		public void Generate()
		{
			byte[] blocks = new byte[32768], data = new byte[16384];
			byte[] skylight = new byte[16384], light = new byte[16384];
			byte[] height = new byte[256];
			BinaryTag[] entities = new BinaryTag[0], tileEntities = new BinaryTag[0];
			
			for (int i = 0; i < 16348; ++i) {
				blocks[i*2] = (byte) Block.Rock;
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

			using (StreamWriter rawWriter = new StreamWriter(filename)) {
				using (GZipStream writer = new GZipStream(rawWriter.BaseStream, CompressionMode.Compress)) {
					NbtWriter.WriteTagStream(_Structure, writer);
				}
			}
		}
		
		public void Load()
		{
			try {
				using (StreamReader rawReader = new StreamReader(CalculateFilename())) {
					using (GZipStream reader = new GZipStream(rawReader.BaseStream, CompressionMode.Decompress)) {
						_Structure = NbtParser.ParseTagStream(reader);
					}
				}
                //CXMineServer.Log(_Structure.CompoundToString("structure", ""));
			}
			catch (FileNotFoundException) {
				Generate();
			}
			catch (DirectoryNotFoundException) {
				Generate();
			}
		}
		
		public byte[] GetBytes()
		{
			List<Byte> builder = new List<Byte>();
			builder.AddRange((byte[]) _Structure["Level"]["Blocks"].Payload);
			builder.AddRange((byte[]) _Structure["Level"]["Data"].Payload);
			builder.AddRange((byte[]) _Structure["Level"]["BlockLight"].Payload);
			builder.AddRange((byte[]) _Structure["Level"]["SkyLight"].Payload);
			return builder.ToArray();
		}
		
		// ====================
		// Tile gets/sets
		
		public Block GetBlock(int x, int y, int z)
		{
			return (Block) ((byte[])(_Structure["Level"]["Blocks"].Payload))[BlockIndex(x, y, z)];
		}
		
		public void SetBlock(int x, int y, int z, Block block)
		{
			((byte[])(_Structure["Level"]["Blocks"].Payload))[BlockIndex(x, y, z)] = (byte)block;
		}
		
		public byte GetData(int x, int y, int z)
		{
            return ((byte[])(_Structure["Level"]["Data"].Payload))[BlockIndex(x, y, z)];
		}
		
		public void SetData(int x, int y, int z, byte data)
		{
            ((byte[])(_Structure["Level"]["Data"].Payload))[BlockIndex(x, y, z)] = data;
		}
		
		public byte GetLight(int x, int y, int z)
		{
            return ((byte[])(_Structure["Level"]["BlockLight"].Payload))[BlockIndex(x, y, z)];
		}
		
		public void SetLight(int x, int y, int z, byte data)
		{
            ((byte[])(_Structure["Level"]["BlockLight"].Payload))[BlockIndex(x, y, z)] = data;
		}
		
		public byte GetSkyLight(int x, int y, int z)
		{
            return ((byte[])(_Structure["Level"]["SkyLight"].Payload))[BlockIndex(x, y, z)];
		}
		
		public void SetSkyLight(int x, int y, int z, byte data)
		{
            ((byte[])(_Structure["Level"]["SkyLight"].Payload))[BlockIndex(x, y, z)] = data;
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
		
		override public string ToString()
		{
			return "[Chunk at " + ChunkX + ", " + ChunkZ + "]";
		}
		
	}
}
