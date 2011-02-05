using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace CXMineServer
{
	public abstract class Packet
	{
		protected PacketWriter _Writer;

		public PacketType Type
		{
			get;
			private set;
		}
		public int Length { 
			get;
			private set; 
		}

		protected List<byte[]> _Strings;

		public Packet(PacketType type, int length)
		{
			Length = length;
			Type = type;
			_Writer = PacketWriter.CreateInstance(length);
			_Writer.Write((byte)Type);
		}

		public Packet(PacketType type)
		{
			Type = type;
		}

		public Packet()
		{

		}

		public void SetCapacity(int length)
		{
			Length = length;
			_Writer = PacketWriter.CreateInstance(length);
			_Writer.Write((byte)Type);
		}

		public void SetCapacity(int fixedLength, params string[] args)
		{
			byte[] bytes;

			Length = fixedLength;
			_Strings = new List<byte[]>();
			for (int i = 0; i < args.Length; ++i)
			{
				bytes = Encoding.UTF8.GetBytes(args[i]);
				Length += bytes.Length;
				_Strings.Add(bytes);
			}

			_Writer = PacketWriter.CreateInstance(Length);
			_Writer.Write((byte)Type);
		}


		public byte[] GetBuffer()
		{
			byte[] buffer = new byte[Length];
			Buffer.BlockCopy(_Writer.UnderlyingStream.GetBuffer(), 0, buffer, 0, Length);
			PacketWriter.ReleaseInstance(_Writer);

			if(_Strings != null)
				_Strings.Clear();
			return buffer;
		}
	}
}
