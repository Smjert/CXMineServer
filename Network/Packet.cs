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

		public Packet(PacketType type, int length)
		{
			Length = length;
			Type = type;
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
		}

		public byte[] GetBuffer()
		{
			byte[] buffer = _Writer.UnderlyingStream.GetBuffer();
			PacketWriter.ReleaseInstance(_Writer);
			return buffer;
		}
	}
}
