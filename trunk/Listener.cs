using System;
using System.Net;
using System.Net.Sockets;

namespace CXMineServer
{
	public class Listener
	{
		private Socket _Listener;
		private IPEndPoint _IPEndPoint;

		public Listener(IPEndPoint ipEndPoint)
		{
			_Listener = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		}

		public void Start()
		{
			_Listener.Bind(_IPEndPoint);
			_Listener.Listen(10);

			StartAccept(null);
		}

		public void StartAccept(SocketAsyncEventArgs acceptEventArg)
		{
			if (acceptEventArg == null)
			{
				acceptEventArg = new SocketAsyncEventArgs();
				acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
			}
			else
				acceptEventArg.AcceptSocket = null;

			bool willRaiseEvent = _Listener.AcceptAsync(acceptEventArg);
			if (!willRaiseEvent)
			{
				ProcessAccept(acceptEventArg);
			}
		}

		public void ProcessAccept(SocketAsyncEventArgs e)
		{
			SocketAsyncEventArgs readEventArgs = Server.ReadWritePool.Pop();
			readEventArgs.UserToken = e.AcceptSocket;

			e.AcceptSocket.ReceiveAsync(readEventArgs);

			StartAccept(e);
		}

		public void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
		{
			ProcessAccept(e);
		}

	}
}
