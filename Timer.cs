using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace CXMineServer
{
	public class Timer
	{
		private DateTime _NextTick;
		private TimeSpan _Delay;

		public static Queue ToTick = Queue.Synchronized(new Queue());

		public DateTime NextTick
		{
			get { return _NextTick; } 
		}

		public bool OneTick;

		public Timer(TimeSpan nextTick, bool oneTick)
		{
			_NextTick = DateTime.Now + nextTick;
			_Delay = nextTick;
			OneTick = false;
		}

		public void Start()
		{
			TimerThread.AddTimer(this);
		}

		public void Stop()
		{
			TimerThread.RemoveTimer(this);
		}

		protected virtual void OnTick()
		{
			if(_NextTick != null)
				_NextTick += _Delay;
		}

		public static void Slice()
		{
			while(ToTick.Count > 0)
			{
				Timer t = ToTick.Dequeue() as Timer;
				t.OnTick();
			}
		}

		public static void Tick(Timer t)
		{
			lock(ToTick)
				ToTick.Enqueue(t);

			CXMineServer.Server.Signal();
		}
	}
}
