/***************************************************************************
 *                              PacketHandler.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: PacketHandler.cs 4 2006-06-15 04:28:39Z mark $
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

namespace CXMineServer
{
	public delegate void OnPacketReceive( NetState state, PacketReader pvSrc );
	public delegate Result OnPacketReceiveResult(NetState state, PacketReader pvSrc);
	public delegate bool ThrottlePacketCallback( NetState state );

	public class Result
	{
		public bool Success;
		public int Length;
	}

	public class PacketHandler
	{
		private PacketType _PacketID;
		private int _Length;
		private OnPacketReceive _OnReceive;

		private ThrottlePacketCallback m_ThrottleCallback;

		public PacketHandler( PacketType packetID, int length, OnPacketReceive onReceive )
		{
			_PacketID = packetID;
			_Length = length;
			_OnReceive = onReceive;
		}

		public PacketType PacketID
		{
			get
			{
				return _PacketID;
			}
		}

		public OnPacketReceive OnReceive
		{
			get
			{
				return _OnReceive;
			}
		}

		public int Length
		{
			get
			{
				return _Length;
			}
		}

		public ThrottlePacketCallback ThrottleCallback
		{
			get{ return m_ThrottleCallback; }
			set{ m_ThrottleCallback = value; }
		}
	}
}