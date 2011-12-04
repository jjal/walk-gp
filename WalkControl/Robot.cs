using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace WalkControl
{
	public abstract class Robot
	{
		protected Dictionary<int, int> State;
		protected const int NumServos = 4;
		protected const int ActionCompletionTimeout = 100;

		public Robot()
		{
			ResetState();
		}

		public void DoRun(Chromosome DNA, int RunLength)
		{
			ResetState();
			var runStart = DateTime.Now.Ticks;
			while ( DateTime.Now.Ticks - runStart < RunLength)
			{
				var actions = DNA.Enumerate(State).ToList();
				Trace.Write("Emu got "+actions.Count()+" actions for this tick. Applying...");
				foreach (var action in actions.OfType<Action>())
				{
					//write action's angles to state
					foreach (var kv in action.Angles)
						State[kv.Key] = kv.Value;
					
					//upload the new state to the device
					UpdateState(State);

					//wait for the actions to complete
					var start = DateTime.Now.Ticks;
					while (!ReadState().SequenceEqual(State) && DateTime.Now.Ticks- start < ActionCompletionTimeout) ;
				}
				Trace.WriteLine(" done");
			}
		}

		abstract public void UpdateState(Dictionary<int, int> State);

		abstract public Dictionary<int, int> ReadState();

		abstract public int[] GetGyroState();

		abstract protected void ResetState();
	}
}