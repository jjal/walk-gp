using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WalkControl
{
	public class Move : Action
	{
		public int Joint { get; set; }
		public int Angle { get; set; }

		#region IAction Members

		public string Serialize()
		{
			return Joint + ":" + Angle;
		}

		#endregion
	}

	public class About : Conditional
	{
		public string Key { get; set; }
		public int Target { get; set; }
		public int Error { get; set; }

		public bool Evaluate(Dictionary<string, int> State)
		{
			if (State.ContainsKey(Key))
			{
				return Math.Abs(State[Key] - Target) <= Error;
			}
			throw new InvalidOperationException("State does not contain target key: " + Key);
		}
	}
}
