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
			_IPEndPoint = ipEndPoint;
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
			try
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
			catch (System.Exception ex)
			{
				CXMineServer.Log(ex.Message);
				Console.ReadLine();
			}
		}

		public void ProcessAccept(SocketAsyncEventArgs e)
		{
			try
			{
				NetState ns = new NetState();
				ns.Connection = e.AcceptSocket;
				ns.Owner = new Player();
				ns.Start();

				StartAccept(e);
			}
			catch (System.Exception ex)
			{
				CXMineServer.Log(ex.Message);
				Console.ReadLine();
			}
		}

		public void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
		{
			ProcessAccept(e);
		}

	}
}
