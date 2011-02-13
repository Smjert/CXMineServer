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

			Register(PacketType.ArmAnimation, 6, 0, new OnPacketReceive(ReadArmAnimation));
			Register(PacketType.Disconnect, 0, 3, new OnPacketReceive(ReadDisconnect));
			Register(PacketType.PlayerInventory, 11, 0, new OnPacketReceive(ReadPlayerInventory));
			Register(PacketType.Handshake, 0, 3, new OnPacketReceive(ReadHandshake));
			Register(PacketType.LoginDetails, 0, 18, new OnPacketReceive(ReadLoginDetails));
			Register(PacketType.KeepAlive, 1, 0, new OnPacketReceive(ReadKeepAlive));
			Register(PacketType.Player, 2, 0, new OnPacketReceive(ReadPlayer));
			Register(PacketType.PlayerBlockPlace, 0, 13, new OnPacketReceive(ReadPlayerBlockPlace));
			Register(PacketType.PlayerDigging, 12, 0, new OnPacketReceive(ReadPlayerDigging));
			Register(PacketType.PlayerHolding, 3, 0, new OnPacketReceive(ReadPlayerHolding));
			Register(PacketType.PlayerPosition, 34, 0, new OnPacketReceive(ReadPlayerPosition));
			Register(PacketType.PlayerPositionLook, 42, 0, new OnPacketReceive(ReadPlayerPositionLook));
			Register(PacketType.PlayerLook, 10, 0, new OnPacketReceive(ReadPlayerLook));
			Register(PacketType.WindowClick, 0, 9, new OnPacketReceive(ReadWindowClick));
			Register(PacketType.CloseWindow, 2, 0, new OnPacketReceive(ReadCloseWindow));
		}

		public static void Register(PacketType packetID, int length, int minimumLength, OnPacketReceive onReceive)
		{
			m_Handlers[(byte)packetID] = new PacketHandler(packetID, length, minimumLength, onReceive);
		}

		public static PacketHandler GetHandler(PacketType packetID)
		{
			return m_Handlers[(byte)packetID];
		}

		public static void ReadArmAnimation(NetState ns, PacketReader packetReader)
		{
			int eid = packetReader.ReadInt32();
			byte animation = packetReader.ReadByte();

			// Send this to near players
		}

		public static void ReadCloseWindow(NetState ns, PacketReader packetReader)
		{
			byte id = packetReader.ReadByte();
		}

		public static void ReadDisconnect(NetState ns, PacketReader packetReader)
		{
			short length = packetReader.ReadInt16();

			if (packetReader.Index + length <= packetReader.Size)
			{
				string message = packetReader.ReadString(length);
			}
			else
			{
				packetReader.Failed = true;
				return;
			}

			CXMineServer.Log("Disposing " + ns.Owner.EntityID);
			ns.Dispose();
		}

		public static void ReadHandshake(NetState ns, PacketReader packetReader)
		{
			short length = packetReader.ReadInt16();

			if (packetReader.Index + length <= packetReader.Size)
			{
				string username = packetReader.ReadString(length);
				ns.Owner.Username = username;
			}
			else
			{
				packetReader.Failed = true;
				return;
			}

			ns.Handshake();
		}

		public static void ReadKeepAlive(NetState ns, PacketReader packetReader)
		{
			ns.KeepAlive();
		}

		public static void ReadLoginDetails(NetState ns, PacketReader packetReader)
		{
			int protocolVersion = packetReader.ReadInt32();

			if (protocolVersion != CXMineServer.ProtocolVersion)
			{
				ns.Disconnect();
				return;
			}

			short userNameLength = packetReader.ReadInt16();
			string username;

			if (packetReader.Index + userNameLength <= packetReader.Size)
			{
				username = packetReader.ReadString(userNameLength);
			}
			else
			{
				packetReader.Failed = true;
				return;
			}

			short passwordLength = packetReader.ReadInt16();
			string password;

			if (packetReader.Index + passwordLength <= packetReader.Size)
			{
				password = packetReader.ReadString(passwordLength);
			}

			if (username != ns.Owner.Username)
			{
				ns.Disconnect();
				return;
			}

			packetReader.ReadInt64();
			packetReader.ReadByte();

			ns.Login();
		}

		public static void ReadPlayer(NetState ns, PacketReader packetReader)
		{
			byte extra = packetReader.ReadByte();
		}

		public static void ReadPlayerBlockPlace(NetState ns, PacketReader packetReader)
		{
			int x = packetReader.ReadInt32();
			byte y = packetReader.ReadByte();
			int z = packetReader.ReadInt32();

			byte direction = packetReader.ReadByte();
			int id;

			byte amount;
			short damage;

			if((id = packetReader.ReadInt16()) >= 0)
			{
				if(packetReader.Index + 3 <= packetReader.Size)
				{
					amount = packetReader.ReadByte();
					damage = packetReader.ReadInt16();
				}
				else
				{
					packetReader.Failed = true;
					return;
				}
			}

			if(id < 0)
			{
				// Check interaction with objects
			}
			else if(id < 100)
			{
				// The player placed a block
				Chunk.PlaceBlock(ns, id, x, y, z, direction);
			}
			
		}

		public static void ReadPlayerDigging(NetState ns, PacketReader packetReader)
		{
			int status = packetReader.ReadByte();

			int x = packetReader.ReadInt32();
			byte y = packetReader.ReadByte();
			int z = packetReader.ReadInt32();

			byte face = packetReader.ReadByte();

			if(status == 3)
				Chunk.DestroyBlock(ns, x, y, z, face);
			
		}

		public static void ReadPlayerHolding(NetState ns, PacketReader packetReader)
		{
			int slotId = packetReader.ReadInt16();

			ns.Owner.inventory.HoldingPos = slotId;
		}

		public static void ReadPlayerInventory(NetState ns, PacketReader packetReader)
		{
			int entityId = packetReader.ReadInt32();
			short slot = packetReader.ReadInt16();
			short itemId = packetReader.ReadInt16();
			short damage = packetReader.ReadInt16();

			if (slot < 0 || slot > 44)
				return;

			CXMineServer.Log("Received Player Inventory Packet");
		}

		public static void ReadPlayerPosition(NetState ns, PacketReader packetReader)
		{
			double x = packetReader.ReadDouble();
			double stance = packetReader.ReadDouble();
			double y = packetReader.ReadDouble();
			double z = packetReader.ReadDouble();

			ns.Owner.X = x;
			ns.Owner.Y = y;
			ns.Owner.Z = z;

			bool onGround = packetReader.ReadBool();

			ns.Owner.Moved = true;
		}

		public static void ReadPlayerLook(NetState ns, PacketReader packetReader)
		{
			float yaw = packetReader.ReadFloat();
			float pitch = packetReader.ReadFloat();

			ns.Owner.Yaw = yaw;
			ns.Owner.Pitch = pitch;

			bool onGround = packetReader.ReadBool();
		}
		public static void ReadPlayerPositionLook(NetState ns, PacketReader packetReader)
		{
			double x = packetReader.ReadDouble();
			double stance = packetReader.ReadDouble();
			double y = packetReader.ReadDouble();
			double z = packetReader.ReadDouble();

			float yaw = packetReader.ReadFloat();
			float pitch = packetReader.ReadFloat();

			ns.Owner.Yaw = yaw;
			ns.Owner.Pitch = pitch;

			ns.Owner.X = x;
			ns.Owner.Y = y;
			ns.Owner.Z = z;

			bool onGround = packetReader.ReadBool();

			ns.Owner.Moved = true;
		}

		public static void ReadWindowClick(NetState ns, PacketReader packetReader)
		{
			byte windowId = packetReader.ReadByte();
			short slot = packetReader.ReadInt16();
			bool rightClick = packetReader.ReadBool();
			short actionNumber = packetReader.ReadInt16();
			short itemId = packetReader.ReadInt16();

			if(itemId != -1)
			{
				byte itemCount = 0;
				short itemUses = 0;
				if (packetReader.Index + 3 <= packetReader.Size)
				{
					itemCount = packetReader.ReadByte();
					itemUses = packetReader.ReadInt16();
				}
				else
				{
					packetReader.Failed = true;
					return;
				}

				// We need security controls, like controlling if we own the item, if the count and uses is correct or not, for now we say it's always ok
				ns.Transaction(windowId, actionNumber, true);

				if(windowId == 0)
				{
					if(rightClick)
					{
						if(ns.Owner.MouseHoldingItem == null)
						{
							Inventory.Slot item = ns.Owner.inventory.GetItem(slot);
							int prevCount = ns.Owner.inventory.GetItem(slot).Count;
							int newCount = ns.Owner.inventory.GetItem(slot).Count = (short)(ns.Owner.inventory.GetItem(slot).Count > 1 ? ns.Owner.inventory.GetItem(slot).Count / 2 : 1);

							ns.Owner.MouseHoldingItem = new Item();
							ns.Owner.MouseHoldingItem.Count = prevCount - newCount;
							ns.Owner.MouseHoldingItem.Uses = item.Uses;
						}
						else
						{
							Inventory.Slot item = ns.Owner.inventory.GetItem(slot);
							if (item.Id == ns.Owner.MouseHoldingItem.Type && item.Count < 64)
							{
								++item.Count;
								--ns.Owner.MouseHoldingItem.Count;
							}
						}
					}
					else
					{
						Inventory.Slot item = ns.Owner.inventory.GetItem(slot);

						if(item == null)
							ns.Owner.inventory.AddToPosition(slot, itemId, itemCount, itemUses, false);

						else if(ns.Owner.MouseHoldingItem == null)
						{
							ns.Owner.MouseHoldingItem = new Item();
							ns.Owner.MouseHoldingItem.Count = item.Count;
							ns.Owner.MouseHoldingItem.Uses = item.Uses;

							item.Count = 0;
						}
						else if (item.Count < 64)
						{
							int stillHolding = ns.Owner.MouseHoldingItem.Count - (64 - item.Count);

							int added = ns.Owner.MouseHoldingItem.Count - stillHolding;

							if (stillHolding == 0)
								ns.Owner.MouseHoldingItem = null;

							item.Count += (short)added;
						}
					}
				}
			}
			else if(ns.Owner.MouseHoldingItem != null)
			{
				ns.Owner.inventory.AddToPosition(slot, (short)ns.Owner.MouseHoldingItem.Type, (short)ns.Owner.MouseHoldingItem.Count, (short)ns.Owner.MouseHoldingItem.Uses, false);
				ns.Owner.MouseHoldingItem = null;
			}
		}
	}
}