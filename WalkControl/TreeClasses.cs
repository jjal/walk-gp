using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WalkControl
{
	public abstract class Node : ICloneable
	{
		public List<Node> Children { get; set; }
		public Node()
		{
			Children = new List<Node>();
		}

		#region ICloneable Members

		public abstract object Clone();

		#endregion
	}

	public abstract class Conditional : Node
	{
		public Node Success { get; set; }
		public Node Failure { get; set; }
		public abstract bool Evaluate(Dictionary<string, int> State);
	}

	public abstract class Action : Node
	{
		public abstract string Serialize();
	}
}
