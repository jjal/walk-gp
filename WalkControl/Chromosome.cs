using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WalkControl
{
	public class Chromosome : ICloneable
	{
		Node Genome { get; set; }

		public Chromosome()
		{
			
		}

		public string Serialize(Dictionary<string, int> State)
		{
			return String.Join(";", Enumerate(State).Select(a => a.Serialize()));
		}

		public IEnumerable<Action> Enumerate(Dictionary<string, int> State)
		{
			foreach (var a in Enumerate(State, Genome))
				yield return a;
		}

		public IEnumerable<Action> Enumerate(Dictionary<string, int> State, Node node)
		{
			if (node as Action != null)
				yield return node as Action;

			foreach (var c in node.Children)
				foreach (var n in Enumerate(State, c))
					yield return n;
			
			if (node as Conditional != null)
			{
				if ((node as Conditional).Evaluate(State))
					foreach (var n in Enumerate(State, (node as Conditional).Success))
						yield return n;
				else
					foreach (var n in Enumerate(State, (node as Conditional).Failure))
						yield return n;
			}
		}

		public Chromosome Mutate()
		{
			return new Chromosome();
		}

		public Chromosome Crossover(Chromosome partner)
		{
			return new Chromosome();
		}


		#region ICloneable Members

		public object Clone()
		{
			var c = new Chromosome();
			return c;
		}

		#endregion
	}
}
