/***************************************************************************
 *                             PacketHandlers.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: PacketHandlers.cs 644 2010-12-23 09:18:45Z asayre $
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace CXMineServer
{
	public static class PacketHandlers
	{
		private static PacketHandler[] m_Handlers;

		public static PacketHandler[] Handlers
		{
			get { return m_Handlers; }
		}

		static PacketHandlers()
		{
			m_Handlers = new PacketHandler[0x100];

			Register(PacketType.DestroyEntity, 0, new OnPacketReceive(ReadPlayerBlockPlace));
			Register(PacketType.PlayerInventory, 9, new OnPacketReceive(ReadPlayerInventory));
			Register(PacketType.Handshake, 0, new OnPacketReceive(ReadHandshake));
			Register(PacketType.LoginDetails, 0, new OnPacketReceive(ReadLoginDetails));
			Register(PacketType.KeepAlive, 1, new OnPacketReceive(ReadKeepAlive));
		}

		public static void Register(PacketType packetID, int length, OnPacketReceive onReceive)
		{
			m_Handlers[(byte)packetID] = new PacketHandler(packetID, length, onReceive);
		}

		public static PacketHandler GetHandler(PacketType packetID)
		{
			return m_Handlers[(byte)packetID];
		}

		public static void ReadKeepAlive(NetState ns, PacketReader packetReader)
		{
			ns.KeepAlive();
		}

		public static void ReadLoginDetails(NetState ns, PacketReader packetReader)
		{
			int protocolVersion = packetReader.ReadInt32();

			if (protocolVersion != CXMineServer.ProtocolVersion)
				ns.Disconnect();

			short userNameLength = packetReader.ReadInt16();
			string username;

			if (packetReader.Index + userNameLength < packetReader.Size)
			{
				username = packetReader.ReadString(userNameLength);
				if (username != ns.Owner.Username)
					ns.Disconnect();
			}
			else
			{
				packetReader.Failed = true;
				return;
			}

			short passwordLength = packetReader.ReadInt16();
			string password;

			if (packetReader.Index + passwordLength < packetReader.Size)
			{
				password = packetReader.ReadString(passwordLength);
			}

		}

		public static void ReadHandshake(NetState ns, PacketReader packetReader)
		{
			short length = packetReader.ReadInt16();

			if(packetReader.Index + length < packetReader.Size)
			{
				string username = packetReader.ReadString(length);
				ns.Owner.Username = username;
			}
			else
			{
				packetReader.Failed = true;
				return;
			}
		}

		public static void ReadPlayerBlockPlace(NetState ns, PacketReader packetReader)
		{
			int x = packetReader.ReadInt32();
			byte y = packetReader.ReadByte();
			int z = packetReader.ReadInt32();

			byte direction = packetReader.ReadByte();
			short id;

			byte amount;
			byte damage;

			if(packetReader.Index + 4 < packetReader.Size)
			{
				if((id = packetReader.ReadInt16()) >= 0)
				{
					amount = packetReader.ReadByte();
					damage = packetReader.ReadByte();
				}
			}
			else
			{
				packetReader.Failed = true;
				return;
			}
		}

		public static void ReadPlayerInventory(NetState ns, PacketReader packetReader)
		{
			int entityId = packetReader.ReadInt32();
			short slot = packetReader.ReadInt16();
			short itemId = packetReader.ReadInt16();

			if (slot < 0 || slot > 44)
				return;

			CXMineServer.Log("Received Player Inventory Packet");
		}
	}
}