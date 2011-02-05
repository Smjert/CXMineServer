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

		private bool _Running;

		public bool Running
		{
			get { return _Running; }
		}

		public Timer(TimeSpan nextTick, bool oneTick)
		{
			_Delay = nextTick;
			OneTick = false;
			_Running = false;
		}

		public void Start()
		{
			_Running = true;
			_NextTick = DateTime.Now + _Delay;
			TimerThread.AddTimer(this);
		}

		public void Stop()
		{
			_Running = false;
			TimerThread.RemoveTimer(this);
		}

		protected virtual void OnTick()
		{
			if(_NextTick != null && !OneTick)
				_NextTick = DateTime.Now + _Delay;
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
		}
	}
}
