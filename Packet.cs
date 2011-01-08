using System;
using System.Collections.Generic;
using System.Text;

namespace CXMineServer
{
	class Packet
	{
		public int Length { 
			get;
			private set; 
		}
		public object[] Values { 
			get;
			private set; 
		}

		public Packet(int index, object[] values)
		{
			Length = index;
			Values = values;
		}

		public Packet(){}
	}
}
