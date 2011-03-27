using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WalkControl
{
	public class Chromosome : ICloneable
	{
		Node Genome { get; set; }
		protected int Seed
		{
			get
			{
				return (int)(DateTime.Now.Ticks % Int32.MaxValue);
			}
		}

		public Chromosome(Node Genome)
		{
			this.Genome = Genome;
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
				if ((node as Conditional).Evaluate(State) && State != null)
					foreach (var n in Enumerate(State, (node as Conditional).Success))
						yield return n;
				if (!(node as Conditional).Evaluate(State) && State != null)
					foreach (var n in Enumerate(State, (node as Conditional).Failure))
						yield return n;
			}
		}

		public Chromosome Mutate(int PercentChance)
		{
			var c = new Chromosome(Genome.Clone());
			foreach (var n in c.Enumerate(null))
			{
				if (new Random(Seed).Next(100) > PercentChance)
				{
					//can add a child
					//can lose a child
					//can change an action (this is like add and lose)
					//can mutate a functor
				}
			}
			return c;
		}

		public Chromosome Crossover(Chromosome partner)
		{
			var c = Genome.Clone();
			return c;
		}


		#region ICloneable Members

		public Node Clone()
		{
			var c = new Chromosome();
			c.Genome = Genome.Clone();
			return c;
		}

		#endregion
	}
}
