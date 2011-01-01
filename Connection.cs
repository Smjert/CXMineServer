using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO.Compression;
using System.IO;
using zlib;
using System.Diagnostics;

namespace CXMineServer
{
	public class Connection
	{
		public string IPString;
		
		private Thread _Thread;
		private TcpClient _Client;
		private Queue<byte[]> _TransmitQueue;
		private bool _Running;
		private byte[] _Buffer;
		private Player _Player;
		
		public Connection(TcpClient client, Player player)
		{
			_Client = client;
			IPString = _Client.Client.RemoteEndPoint.ToString();
			
			_Running = true;
			_TransmitQueue = new Queue<byte[]>();
			_Buffer = new byte[0];
			_Player = player;
			
			_Thread = new Thread(ConnectionThread);
			_Thread.Name = "SC-Player " + _Client.GetHashCode();
			_Thread.Start();
		}
		
		public void Stop()
		{
			_Running = false;
		}
		
		#region Network code
		
		public void Transmit(PacketType type, params object[] args)
		{
			// CXMineServer.Log("Transmitting: " + type + "(" + (byte)type + ")");
			string structure = (type == PacketType.Disconnect ? "bt" : PacketStructure.Data[(byte) type]);
			
			Builder<Byte> packet = new Builder<Byte>();
			packet.Append((byte) type);
			
			byte[] bytes;
			int current = 1;
			try {
				for (int i = 1; i < structure.Length; ++i) {
					current = i;
					switch (structure[i]) {
						case 'b':		// byte(1)
							packet.Append((byte) args[i-1]);
							break;
							
						case 's':		// short(2)
							packet.Append(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) args[i-1])));
							break;
							
						case 'f':		// float(4)
							bytes = BitConverter.GetBytes((float) args[i-1]);
							for (int j = 3; j >= 0; --j) {
								packet.Append(bytes[j]);
							}
							//packet.Append(bytes);
							break;
							
						case 'i':		// int(4)
							packet.Append(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int) args[i-1])));
							break;
							
						case 'd':		// double(8)
							bytes = BitConverter.GetBytes((double) args[i-1]);
							for (int j = 7; j >= 0; --j) {
								packet.Append(bytes[j]);
							}
							//packet.Append(bytes);
							break;
							
						case 'l':		// long(8)
							packet.Append(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long) args[i-1])));
							break;
						
						case 't':		// string
							bytes = Encoding.UTF8.GetBytes((string) args[i-1]);
							packet.Append(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) bytes.Length)));
							packet.Append(bytes);
							break;
						
						case 'x':		// byte array
							packet.Append((byte[]) args[i-1]);
							break;
					}
				}
			}
			catch (InvalidCastException) {
				CXMineServer.Log("Error transmitting " + type + ": expected '" + structure[current] +
					"', got " + args[current - 1].GetType().ToString() + " for argument " + current + " (format: " + structure + ")");
				throw;
			}
			_TransmitQueue.Enqueue(packet.ToArray());
		}
		
		private void ConnectionThread()
		{
			CXMineServer.Log("Connection thread " + _Client.GetHashCode() + " running.");
			
			Stopwatch clock = new Stopwatch();
			clock.Start();
			double lastKeepAlive = 0;
			double lastUpdateChunks = 0;
			
			while (_Running) {
				while (_TransmitQueue.Count > 0) {
					byte[] next = _TransmitQueue.Dequeue();
					TransmitRaw(next);
					if (next[0] == (byte) PacketType.Disconnect) {
						_Client.GetStream().Flush();
						_Client.Close();
					}
				}
				
				if (!_Client.Connected) {
					_Client.Close();
					if (_Player.Spawned) {
						_Player.Despawn();
					} else {
						CXMineServer.Log("Anonymous connection thread stopped.");
					}
					_Running = false;
					break;
				}
				
				if (_Client.GetStream().DataAvailable) {
					IncomingData();
				}
				
				if (lastKeepAlive + 10 < clock.Elapsed.TotalSeconds) {
					Transmit(PacketType.KeepAlive);
					lastKeepAlive = clock.Elapsed.TotalSeconds;
				}
				
				if (lastUpdateChunks + 2 < clock.Elapsed.TotalSeconds) {
					_Player.Update();
					lastUpdateChunks = clock.Elapsed.TotalSeconds;
				}
				
				Thread.Sleep(10);
			}
		}
		
		private void TransmitRaw(byte[] packet)
		{
			try {
				_Client.GetStream().Write(packet, 0, packet.Length);
			}
			catch (IOException) {
				_Client.Close();
				if (_Player.Spawned) {
					_Player.Despawn();
				} else {
					CXMineServer.Log("Anonymous connection thread stopped.");
				}
				_Running = false;
			}
		}
		
		public void Disconnect(string message)
		{
			Transmit(PacketType.Disconnect, message);
		}
		
		private void IncomingData()
		{
			NetworkStream stream = _Client.GetStream();
			Builder<byte> buffer = new Builder<byte>();
			buffer.Append(_Buffer);
			
			while (stream.DataAvailable) {
				buffer.Append((byte) stream.ReadByte());
			}
			
			_Buffer = buffer.ToArray();
			buffer = null;
			
			while (_Buffer.Length > 0) {
				Pair<int, object[]> pair = CheckCompletePacket();
				int length = pair.First;
				if (length > 0) {
					//byte[] packet = new byte[length];
					//Array.Copy(_Buffer, packet, length);
					
					byte[] newBuffer = new byte[_Buffer.Length - length];
					Array.Copy(_Buffer, length, newBuffer, 0, _Buffer.Length - length);
					_Buffer = newBuffer;
					
					ProcessPacket(pair.Second);
				} else {
					break;
				}
			}
		}
		
		private Pair<int, object[]> CheckCompletePacket()
		{
			Pair<int, object[]> nPair = new Pair<int, object[]>(0, null);
			
			PacketType type = (PacketType) _Buffer[0];
			if (type == PacketType.PlayerInventory) {
				CXMineServer.Log("Someone sent an inventory! :V");
			}
			if (_Buffer[0] >= PacketStructure.Data.Length && _Buffer[0] != 0xFF) {
				CXMineServer.Log("Got invalid packet: " + (byte)_Buffer[0]);
				return nPair;
			} 
			
            // special handling for the Disconnect case
			string structure = (type == PacketType.Disconnect ? "bt" : PacketStructure.Data[_Buffer[0]]);

            // special handling for the Player Block Placement case, coz of the double nature of the packet
            structure = ((type == PacketType.PlayerBlockPlace && BitConverter.ToInt16(_Buffer, 11) == (short)-1) ? "bibibs" : structure);
			int bufPos = 0;
			Builder<object> data = new Builder<object>();
			byte[] bytes = new byte[8];
			
			for (int i = 0; i < structure.Length; ++i) {
				switch (structure[i]) {
					case 'b':		// byte(1)
						if ((bufPos + 1) > _Buffer.Length) return nPair;
						data.Append((byte) _Buffer[bufPos]);
						bufPos += 1;
						break;
					
					case 's':		// short(2)
						if ((bufPos + 2) > _Buffer.Length) return nPair;
						data.Append((short) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(_Buffer, bufPos)));
						bufPos += 2;
						break;
					
					case 'f':		// float(4)
						if ((bufPos + 4) > _Buffer.Length) return nPair;
						for (int j = 0; j < 4; ++j) {
							bytes[i] = _Buffer[bufPos + 3 - j];
						}
						data.Append((float) BitConverter.ToSingle(bytes, 0));
						bufPos += 4;
						break;
					case 'i':		// int(4)
						if ((bufPos + 4) > _Buffer.Length) return nPair;
						data.Append((int) IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_Buffer, bufPos)));
						bufPos += 4;
						break;
					
					case 'd':		// double(8)
						if ((bufPos + 8) > _Buffer.Length) return nPair;
						for (int j = 0; j < 8; ++j) {
							bytes[j] = _Buffer[bufPos + 7 - j];
						}
						data.Append((double) BitConverter.ToDouble(bytes, 0));
						bufPos += 8;
						break;
					case 'l':		// long(8)
						if ((bufPos + 8) > _Buffer.Length) return nPair;
						data.Append((long) IPAddress.NetworkToHostOrder(BitConverter.ToInt64(_Buffer, bufPos)));
						bufPos += 8;
						break;
					
					case 't':		// string
                        if ((bufPos + 2) > _Buffer.Length) return nPair;
                        int len = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(_Buffer, bufPos));
                        if ((bufPos + 2 + len) > _Buffer.Length) return nPair;
                        data.Append((string)Encoding.UTF8.GetString(_Buffer, bufPos + 2, len));
                        bufPos += (2 + len);
						break;
					
					case 'x':		// onos!
						// TODO
						return nPair;
						//break;
				}
			}
			
			return new Pair<int, object[]>(bufPos, data.ToArray());
		}
		
		public void SendChunk(Chunk chunk)
		{
			Transmit(PacketType.PreChunk, chunk.ChunkX, chunk.ChunkZ, (byte) 1);
			
			byte[] uncompressed = chunk.GetBytes();
			MemoryStream mem = new MemoryStream();
			ZOutputStream stream = new ZOutputStream(mem, zlibConst.Z_BEST_COMPRESSION);
			stream.Write(uncompressed, 0, uncompressed.Length);
			stream.Close();
			byte[] data = mem.ToArray();
			
			Transmit(PacketType.MapChunk, 16 * chunk.ChunkX, (short) 0, 16 * chunk.ChunkZ,
				(byte) 15, (byte) 127, (byte) 15, data.Length, data);
		}
		
		#endregion
		
		private void ProcessPacket(object[] packet)
		{
			PacketType type = (PacketType) (byte) packet[0];
			
			// CXMineServer.Log("Packet received: " + type);
			
			switch(type) {
                // Keep Alive packet's here just for coherence, simply sending back another Keep Alive packet
                // packet[0]: 0x00
                case PacketType.KeepAlive: {
                    CXMineServer.Log("Received Keep Alive Packet");
                    Transmit(PacketType.KeepAlive);
                    break;
                }

                // packet[0]: 0x02
                // packet[1]: Username : string
				case PacketType.Handshake: {
                    CXMineServer.Log("Received Handshake Packet");
					_Player.Username = (string) packet[1];
					Transmit(PacketType.Handshake, CXMineServer.Server.ServerHash);
					break;
				}

                // packet[0]: 0x01
                // packet[1]: Protocol Version : int
                // packet[2]: Username : string
                // packet[3]: Password : string
				case PacketType.LoginDetails: {
                    CXMineServer.Log("Received Login Details Packet");
					if ((int) packet[1] != CXMineServer.ProtocolVersion) {
						CXMineServer.Log("Expecting protocol v" + CXMineServer.ProtocolVersion + ", got v" + (int) packet[1]);
						Disconnect("Invalid protocol version");
						break;
					}
					if ((string) packet[2] != _Player.Username) {
						Disconnect("Sent invalid username");
						break;
					}
					
					Transmit(PacketType.LoginDetails, _Player.EntityID,
						CXMineServer.Server.Name, CXMineServer.Server.Motd,
						/* World.Seed */ (long) 0, /* World.Dimension */ (byte) 0);
					_Player.Spawn();
					
					break;
				}
				
                // packet[0]: 0x03
                // packet[1]: Message : string
				case PacketType.Message: {
                    CXMineServer.Log("Received Message Packet");
					_Player.RecvMessage((string) packet[1]);
					break;
				}

                // packet[0]: 0x05
                // packet[1]: EID : int
                // packet[2]: Slot : short
                // packet[3]: Item ID : short (-1 for unequipped/hands)
                case PacketType.PlayerInventory: {
                        CXMineServer.Log("Received Player Inventory Packet");
                        break;
                }

                // packet[0]: 0x07
                // packet[1]: User : int
                // packet[2]: Target : int
                // packet[3]: Left-Click? : bool (1 for left click, 0 for right click)
				case PacketType.InteractEntity: {
                    CXMineServer.Log("Received Interact Entity Packet");
					// TODO: Handle InteractEntity
					break;
				}

                // packet[0]: 0x09
				case PacketType.Respawn: {
                    CXMineServer.Log("Received Respawn Packet");
					// TODO: Handle Respawn
					break;
				}
				
                // packet[0]: 0x0A
                // packet[1]: OnGround : bool
				case PacketType.Player: {
                    //CXMineServer.Log("Received Player Packet");
					// Ignore.
					break;
				}

                // packet[0]: 0x0B
                // packet[1]: X : double
                // packet[2]: Y : double
                // packet[3]: Stance : double
                // packet[4]: Z : double
                // packet[5]: OnGround : bool
				case PacketType.PlayerPosition: {
                    //CXMineServer.Log("Received Player Position Packet");
					_Player.X = (double)packet[1];
					_Player.Y = (double)packet[2];
					_Player.Z = (double)packet[4];
					break;
				}

                // packet[0]: 0x0C
                // packet[1]: Yaw : float
                // packet[2]: Pitch : float
                // packet[3]: OnGround : bool
				case PacketType.PlayerLook: {
                    //CXMineServer.Log("Received Player Look Packet");
                    _Player.Yaw = (float)packet[1];
                    _Player.Pitch = (float)packet[2];
					break;
				}

                // packet[0]: 0x0D
                // packet[1]: X : double
                // packet[2]: Stance : double
                // packet[3]: Y : double
                // packet[4]: Z : double
                // packet[5]: Yaw : float
                // packet[6]: Pitch : float
                // packet[7]: OnGround : bool
				case PacketType.PlayerPositionLook: {
                    //CXMineServer.Log("Received Player Position Look Packet");
					// TODO: Handle PlayerPositionLook
					_Player.X = (double) packet[1];
					_Player.Y = (double) packet[2];
					_Player.Z = (double) packet[4];
                    _Player.Yaw = (float)packet[5];
                    _Player.Pitch = (float)packet[6];
					break;
				}

                // packet[0]: 0x0E
                // packet[1]: Status : byte (0 == Started Digging, 1 == Digging, 2 == Stopped Digging, 3 == Block broken, 4 == Drop item)
                // packet[2]: X : int
                // packet[3]: Y : byte
                // packet[4]: Z : int
                // packet[5]: Face : byte (0 == -Y, 1 == +Y, 2 == -Z, 3 == +Z, 4 == -X, 5 == +X)
                case PacketType.PlayerDigging: {
                    CXMineServer.Log("Received Player Digging Packet");
                    if((byte)packet[1] == (byte)3) {
                        // Block destroyed
                        CXMineServer.Log("Received Block Destroyed Packet");
                        Transmit(PacketType.BlockChange, packet[2], packet[3], packet[4], (byte)Block.Air, (byte)0x00);
                        int eid = CXMineServer.Server.getEID();
                        Transmit(PacketType.PickupSpawn, eid, (short)4, (byte)1, (int)packet[2] * 32 + 16, (int)((byte)packet[3]) * 32, (int)packet[4] * 32 + 16, (byte)0, (byte)0, (byte)0);
                        Transmit(PacketType.CollectItem, eid, _Player.Eid);
                        Transmit(PacketType.DestroyEntity, eid);
                        //int slot = _Player.GetSlotFor((short)4);
                        //_Player.AddToInventory(slot, (short)4);
                        int slot = _Player.inventory.Add((short)4);
                        Transmit(PacketType.SetSlot, (byte)0, Inventory.FileToGameSlot(slot), (short)4, (byte)1, (byte)1);
                    }
                    else if ((byte)packet[1] == (byte)4)
                    {
                        // Block dropped
                        CXMineServer.Log("Received Block Dropped Packet");
                    }
                    break;
                }

                // packet[0]: 0x0F
                // packet[1]: X : int
                // packet[2]: Y : byte
                // packet[3]: Z : int
                // packet[4]: Direction : byte (0 == -Y, 1 == +Y, 2 == -Z, 3 == +Z, 4 == -X, 5 == +X)
                // packet[5]: Block/Item ID : short
                // packet[6]: Amount : byte (of items in players hand)
                // packet[7]: Damage : byte (uses of the item)
                case PacketType.PlayerBlockPlace: {
                    CXMineServer.Log("Received Player Block Placement Packet");                

                    if ((short)packet[5] < (short)0) // Using hands to interact
                    {
                        CXMineServer.Log("Received Hand Interact Packet");
                        Transmit(PacketType.OpenWindow, (byte)0, (byte)0, "Large chest", (byte)54);
                    }
                    else if ((short)packet[5] < (short)100) // using blocks
                    {
                        CXMineServer.Log("Received Block Placement Packet");
                        int x = (int)packet[1], z = (int)packet[3];
                        byte y = (byte)packet[2];
                        switch((byte)packet[4]) { // Direction
                            case (byte)0: { // -Y
                                y -= 1;
                                break;
                            }
                            case (byte)1: { // +Y
                                y += 1;
                                break;
                            }
                            case (byte)2: { // -Z
                                z -= 1;
                                break;
                            }
                            case (byte)3: { // +Z
                                z += 1;
                                break;
                            }
                            case (byte)4: { // -X
                                x -= 1;
                                break;
                            }
                            case (byte)5: { // +X
                                x += 1;
                                break;
                            }
                        }
                        Transmit(PacketType.BlockChange, x, y, z, (byte)((short)packet[5]), (byte)0x00);
                        // TODO: Decrementa il contatore degli oggetti
                    }
                    else // using items
                    {
                        Transmit(PacketType.OpenWindow, (byte)0, (byte)0, "Large chest", (byte)54);
                    }
                    
                    break;
                }

                // packet[0]: 0x10
                // packet[1]: Slot ID : short
                case PacketType.PlayerHolding: {
                        CXMineServer.Log("Received Player Hold Changed Packet");
                        break;
                }

                // packet[0]: 0x12
                // packet[1]: EID : int
                // packet[2]: Animate : byte (0 == No Animation, 1 == Swing Arm, 2 == Damage Animation, + Composition like 102)
                case PacketType.ArmAnimation: {
                        CXMineServer.Log("Received Arm Animation Packet");
                        break;
                }

                // packet[0]: 0x15
                // packet[1]: EID : int
                // packet[2]: Item ID : short
                // packet[3]: Count : short
                // packet[4]: X : int
                // packet[5]: Y : int
                // packet[6]: Z : int
                // packet[7]: Rotation : byte
                // packet[8]: Pitch : byte
                // packet[9]: Roll : byte
                case PacketType.PickupSpawn: {
                        CXMineServer.Log("Received Pickup Spawn Packet");
                        break;
                }

                // packet[0]: 0x65
                // packet[1]: Window ID : byte
                case PacketType.CloseWindow: {
                        CXMineServer.Log("Received Close Window Packet");
                        break;
                }

                // packet[0]: 0x66
                // packet[1]: Window ID : byte
                // packet[2]: Slot : short
                // packet[3]: Right-Click? : byte (1 for Right Click, 0 for left Click)
                // packet[4]: Action Number : byte (An unique transaction number for the action)
                // packet[5]: Item ID : short (-1 if unequipped/empty)
                // if Item ID != -1 there are two more data
                // packet[6]: Item Count : byte
                // packet[7]: Item uses : byte
                case PacketType.WindowClick: {
                        CXMineServer.Log("Received Window Click Packet");
                        Transmit(PacketType.Transaction, (byte)0, (short)12, (byte)0);
                        break;
                }

                // packet[0]: 0x6A
                case PacketType.Transaction: {
                    CXMineServer.Log("Received Transaction Packet");
                    break;
                }

                // packet[0]: 0xFF
                // packet[1]: Reason : string
                case PacketType.Disconnect: {
                    CXMineServer.Log("Received Disconnect Packet");
					Disconnect("Quitting");
					break;
				}
			}
		}
	}
}
