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

		public void AddChild(Node n)
		{
			Children.Add(n);
		}

		public void AddChild(Node n, int index)
		{
			Children.Insert(index, n);
		}

		public void RemoveChild(Node n)
		{
			Children.Remove(n);
		}

		#region ICloneable Members

		public abstract Node Clone();

		#endregion
	}

	public class Conditional : Node
	{
		public Node Success { get; set; }
		public Node Failure { get; set; }
		public abstract bool Evaluate(Dictionary<string, int> State);
		public Func<Dictionary<string, int>, bool> Condition
		{
			get;
			protected set;
		}

		public Conditional(Func<Dictionary<string, int>, bool> condition)
		{
			Condition = condition;
		}

		public override object Clone()
		{
			var n = new Conditional(Condition);
			foreach (var c in Children)
			{
				n.AddChild(c.Clone());
			}
			return n;
		}
	}

	public class Action : Node
	{
		Dictionary<int, int> Angles
		{
			get;
			protected set;
		}

		public Action(Dictionary<int, int> Angles)
		{
			this.Angles = Angles;
		}

		public override object Clone()
		{
			var n = new Action(Angles);
			foreach (var c in Children)
			{
				c.AddChild(c.Clone());
			}
			return n;
		}

		public string Serialize()
		{
			var s = new StringBuilder();
			foreach (var p in Angles)
			{
				if (s.Length > 0)
					s.Append(";");
				s.Append(p.Key);
				s.Append(":");
				s.Append(p.Value);
			}
			return s;
		}

		internal void Mutate()
		{
			throw new NotImplementedException();
		}
	}
}
