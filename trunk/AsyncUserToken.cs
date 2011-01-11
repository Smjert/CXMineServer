using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace CXMineServer
{
	public class AsyncUserToken
	{
		private Socket m_Connection;

		public Socket Connection
		{
			get{return m_Connection;}
			set{m_Connection = value;}
		}

		public AsyncUserToken()
		{
		}
	}
}
