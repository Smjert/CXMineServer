/***************************************************************************
 *                              PacketReader.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: PacketReader.cs 4 2006-06-15 04:28:39Z mark $
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Text;
using System.IO;
using System.Net;

namespace CXMineServer
{
	public class PacketReader
	{
		private byte[] m_Data;
		private int m_Size;
		private int m_Index;
		private bool _Failed;



		public PacketReader( byte[] data, int size )
		{
			m_Data = data;
			m_Size = size;
			m_Index = 1;
		}

		public byte[] Buffer
		{
			get
			{
				return m_Data;
			}
		}

		public int Size
		{
			get
			{
				return m_Size;
			}
		}

		public int Index
		{
			get
			{
				return m_Index;
			}
		}

		public bool Failed
		{
			get { return _Failed; }
			set { _Failed = value; }
		}

		public int Seek( int offset, SeekOrigin origin )
		{
			switch ( origin )
			{
				case SeekOrigin.Begin: m_Index = offset; break;
				case SeekOrigin.Current: m_Index += offset; break;
				case SeekOrigin.End: m_Index = m_Size - offset; break;
			}

			return m_Index;
		}

		public int ReadInt32()
		{
			if ( (m_Index + 4) > m_Size )
				return 0;

			return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(m_Data, m_Index));
		}

		public short ReadInt16()
		{
			if ( (m_Index + 2) > m_Size )
				return 0;

			return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(m_Data, m_Index));
		}

		public byte ReadByte()
		{
			if ( (m_Index + 1) > m_Size )
				return 0;

			return m_Data[m_Index];
		}

		public string ReadString(int length)
		{
			if ((m_Index + length) > m_Size)
				return "";

			return Encoding.UTF8.GetString(m_Data, m_Index, length);
		}
	}
}