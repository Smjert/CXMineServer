using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

namespace CXMineServer
{
	public class PacketWriter
	{
		private static Stack<PacketWriter> _Pool = new Stack<PacketWriter>();

		private int _Capacity;
		public int Capacity
		{
			get{ return _Capacity; }
			set{ _Capacity = value; }
		}

		private MemoryStream _Stream;
		public MemoryStream UnderlyingStream
		{
			get{ return _Stream; }
			set{ _Stream = value; }
		}

		public PacketWriter(int capacity)
		{
			_Stream = new MemoryStream(capacity);
			_Capacity = capacity;
		}

		public static PacketWriter CreateInstance()
		{
			return CreateInstance(32);
		}

		public static PacketWriter CreateInstance(int capacity)
		{
			PacketWriter pw = null;

			if (_Pool.Count > 0)
			{
				pw = _Pool.Pop();

				if (pw != null)
				{
					pw._Capacity = capacity;
					pw._Stream.SetLength(0);
				}
			}

			if (pw == null)
				pw = new PacketWriter(capacity);

			return pw;
		}

		public static void ReleaseInstance(PacketWriter pw)
		{
			if (!_Pool.Contains(pw))
				_Pool.Push(pw);
			else
			{
				try
				{
					using (StreamWriter op = new StreamWriter("neterr.log"))
					{
						op.WriteLine("{0}\tInstance pool contains writer", DateTime.Now);
					}
				}
				catch
				{
					Console.WriteLine("net error");
				}
			}
		}

		public void Write(byte value)
		{
			_Stream.WriteByte(value);
		}

		public void Write(short value)
		{
			_Stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)), 0, 2);
		}

		public void Write(int value)
		{
			_Stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)), 0, 4);
		}

		public void Write(long value)
		{
			_Stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)), 0, 8);
		}

		public void Write(float value)
		{
			_Stream.Write(BitConverter.GetBytes(value), 0, 4);
		}

		public void Write(double value)
		{
			_Stream.Write(BitConverter.GetBytes(value), 0, 8);
		}

		public void WriteString(byte[] value)
		{
			_Stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)value.Length)), 0, 2);
			_Stream.Write(value, 0, value.Length);
		}

		public void Write(byte[] value)
		{
			_Stream.Write(value, 0, value.Length);
		}
	}
}
