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
			_ToAdd.Enqueue(t);
			_Signal.Set();
		}

		public static void RemoveTimer(Timer t)
		{
		}

		private static void Run()
		{
			while(true)
			{
				ProcessNewTimers();

				for (int i = 0; i < _Timers.Count; ++i)
				{
					if (_Timers[i].NextTick <= DateTime.Now)
					{
						Timer.Tick(_Timers[i]);

						if (_Timers[i].OneTick)
							_Timers.RemoveAt(i);
					}
				}

				_Signal.WaitOne(50, false);
			}
		}

		private static void ProcessNewTimers()
		{
			if(_ToAdd.Count > 0)
			{
				Timer t = _ToAdd.Dequeue() as Timer;
				_Timers.Add(t);
			}
		}
	}
}
