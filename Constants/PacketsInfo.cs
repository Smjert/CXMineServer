using System;

namespace CXMineServer
{
	public enum PacketType : byte
	{
		KeepAlive = 0x00,           // c <-> s
		LoginDetails = 0x01,        //   <->
		Handshake = 0x02,           //   <->
		Message = 0x03,             //   <->
		TimeUpdate = 0x04,          //   <--
		PlayerInventory = 0x05,     //   -->
		SpawnPosition = 0x06,       //   <--
		InteractEntity = 0x07,		//   <->
		UpdateHealth = 0x08,		//   <--
		Respawn = 0x09,				//   <->
		Player = 0x0A,              //   -->
		PlayerPosition = 0x0B,      //   -->
		PlayerLook = 0x0C,          //   -->
		PlayerPositionLook = 0x0D,  //   <->
		PlayerDigging = 0x0E,       //   -->
		PlayerBlockPlace = 0x0F,    //   -->
		PlayerHolding = 0x10,       //   <->
		ArmAnimation = 0x12,        //   <->
		// Unused space
		NamedEntitySpawn = 0x14,    //   <--
		PickupSpawn = 0x15,         //   <->
		CollectItem = 0x16,         //   <--
		AddObjectVehicle = 0x17,    //   <--
		MobSpawn = 0x18,            //   <--
		// Unused space
		EntityVelocity = 0x1C,		//   <--
		DestroyEntity = 0x1D,       //   <--
		Entity = 0x1E,              //   <--
		EntityRelativeMove = 0x1F,  //   <--
		EntityLook = 0x20,          //   <--
		EntityLookAndMove = 0x21,   //   <--
		EntityTeleport = 0x22,      //   <--
		// Unused space
		EntityDamage = 0x26,		//   <--
		AttachEntity = 0x27,		//   <--
		// Unused space
		PreChunk = 0x32,            //   <--
		MapChunk = 0x33,            //   <--
		MultiBlockChange = 0x34,    //   <--
		BlockChange = 0x35,         //   <--
		// Unused space
		Explosion = 0x3C,			//   <--
		// Unused space
        OpenWindow = 0x64,          //   <--
        CloseWindow = 0x65,         //   -->
        WindowClick = 0x66,         //   -->
        SetSlot = 0x67,             //   <--
        WindowItem = 0x68,          //   <--
        UpdateProgressBar = 0x69,   //   <--
        Transaction = 0x6A,         //   <--
        UpdateSign = 0x82,          //   <--
        Disconnect = 0xFF           //   <->

	}
	
	public static class PacketStructure
	{
		// b - byte(1)
		// s - short(2)
		// i - int(4)
		// l - long(8)
		// f - float(4)
		// d - double(8)
		// t - string - short-prefixed
		// x - bytearray - special handling
		
		public static string[] Data = {
			"b",				// keep alive - 0x00
			"bittlb",			// login request
			"bt",				// handshake
			"bt",				// chat message
			"bl",				// time update
			"bisss",				// player inventory
			"biii",				// spawn position
			"biib",				// interact entity
			"bs",				// update health
			"b",				// respawn
			"bb",				// player base
			"bddddb",			// player position
			"bffb",				// player look
			"bddddffb",			// player position+look
			"bbibib",			// player digging
            "bibibsbb",         // player block place
			"bs",				// player holding
            "",
			"bib",				// arm animation
			"",					// unused space
			"bitiiibbs",		// named entity spawn
			"bisbiiibbb",		// pickup spawn
			"bii",				// collect item
			"bibiii",			// vehicle spawn
			"bibiiibb",			// mob spawn
			"", "", "",			// unused space
			"bisss",			// entity velocity
			"bi",				// destroy entity
			"bi",				// entity base
			"bibbb",			// entity relative move
			"bibb",				// entity look
			"bibbbbb",			// entity look+move
			"biiiibb",			// entity teleport
			"", "", "", 		// unused space
			"bssb",				// entity damage
			"bii",				// attach entity
			"", "", "", "", "",	// unused space
			"", "", "", "", "", // unused space
			"biib",				// prechunk
			"bisibbbix",		// mapchunk
			"biisxxx",			// multi-block change
			"bibibb",			// block change
			"", "", "", "", "",	// unused space
            "",
			"bdddfib",			// explosion
            "", "", "", "", "",	// unused space
            "", "", "", "", "",	// unused space
            "", "", "", "", "",	// unused space
            "", "", "", "", "",	// unused space
            "", "", "", "", "",	// unused space
            "", "", "", "", "",	// unused space
            "", "", "", "", "",	// unused space
            "", "", "", "", 	// unused space
            "bbbtb",            // open window
            "bb",               // close window
            "bbsbssbb",         // window click
            "bbssbs",           // set slot
            "bbsx",             // window item
            "bbss",             // update progress bar
            "bbsb",             // transaction
            "", "", "", "", "",	// unused space
            "", "", "", "", "",	// unused space
            "", "", "", "", "",	// unused space
            "", "", "", "", "",	// unused space
            "", "", "",       	// unused space
            "bisitttt"          // update sign

			// special handling for disconnect
		};
	}
}