using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;

namespace CXMineServer
{
	public class TimerThread
	{
		private static Task _Task;
		private static AutoResetEvent _Signal;

		private static List<Timer> _Timers;
		private static Queue _ToAdd = Queue.Synchronized(new Queue());

		public TimerThread()
		{
			_Timers = new List<Timer>();
		}

		public static void Start()
		{
			_Timers = new List<Timer>();
			_Signal = new AutoResetEvent(true);
			_Task = new Task(Run, TaskCreationOptions.LongRunning);
			_Task.Start();
		}

		public static void Stop()
		{

		}

		public static void AddTimer(Timer t)
		{
			_ToAdd.Enqueue(new TimerEntry(t, false));
			_Signal.Set();
		}

		public static void RemoveTimer(Timer t)
		{
			_ToAdd.Enqueue(new TimerEntry(t, true));
			_Signal.Set();
		}

		private static void Run()
		{
			while(true)
			{

				bool loaded = false;
				ProcessNewTimers();

				for (int i = 0; i < _Timers.Count; ++i)
				{
					if (_Timers[i].NextTick <= DateTime.Now)
					{
						loaded = true;
						Timer.Tick(_Timers[i]);

						if (_Timers[i].OneTick)
							_Timers.RemoveAt(i);
					}
				}

				if(loaded)
					CXMineServer.Server.Signal();

				_Signal.WaitOne(50, false);
			}
		}

		private static void ProcessNewTimers()
		{
			if(_ToAdd.Count > 0)
			{
				TimerEntry te = _ToAdd.Dequeue() as TimerEntry;

				if (!te.ToRemove)
					_Timers.Add(te.Timer);
				else
					_Timers.Remove(te.Timer);
			}
		}

		private class TimerEntry
		{
			private Timer _Timer;
			public Timer Timer
			{
				get { return _Timer; }
			}

			private bool _ToRemove;
			public bool ToRemove
			{
				get { return _ToRemove; }
			}

			public TimerEntry(Timer t, bool toRemove)
			{
				_Timer = t;
				_ToRemove = toRemove;
			}
		}
	}
}
