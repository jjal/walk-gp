using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace WalkControl
{
	public class Chromosome : ICloneable
	{
		private int NewActionChance = 50;
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

		public IEnumerable<Node> Enumerate()
		{
			foreach(var n in Enumerate(null))
				yield return n;
		}

		public IEnumerable<Node> Enumerate(Dictionary<string, int> State)
		{
			foreach (var a in Enumerate(State, Genome))
				yield return a;
		}

		public IEnumerable<Node> Enumerate(Dictionary<string, int> State, Node node)
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

		/// <summary>
		/// Creates a random node
		/// </summary>
		/// <returns></returns>
		public Node CreateNode()
		{
			Node n;
			if (new Random(Seed).Next(100) > NewActionChance)
			{
				n = new Action(CreateAction());
			}
			else
			{
				n = new Conditional(CreateCondition());
			}
			return n;
		}

		private Func<Dictionary<int, int>, bool> CreateCondition()
		{
			var o = Expression.Parameter(typeof(Dictionary<string,int>), "t");
			var val = 0; //constant
			var prop = 0;
			Expression<Func<Dictionary<int, int>, bool>> expression = Expression.Lambda<Func<Dictionary<int, int>, bool>>(
				Expression.Equal(
					Expression.ArrayIndex(o, prop), Expression.Constant(val)),
				o);

			return expression.Compile();
		}

		private Dictionary<int, int> CreateAction()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// swaps a random subtree in this chromosome with a random subtree in the partner one
		/// </summary>
		/// <param name="partner"></param>
		public void Crossover(Chromosome partner)
		{
			var nodes = Enumerate().Count();
			var nodes2 = partner.Enumerate().Count();
			var selection = Enumerate().ElementAt(new Random(Seed).Next(nodes));
			var target = Enumerate().ElementAt(new Random(Seed).Next(nodes2));
			var parent1 = Enumerate().FirstOrDefault(n => n.Children.Contains(selection));
			var parent2 = Enumerate().FirstOrDefault(n => n.Children.Contains(target));
			parent1.AddChild(target,parent1.Children.IndexOf(selection));
			parent2.AddChild(selection,parent2.Children.IndexOf(target));
			parent1.RemoveChild(selection);
			parent2.RemoveChild(target);
		}


		#region ICloneable Members

		public Chromosome Clone()
		{
			var c = new Chromosome(Genome.Clone());
			return c;
		}

		#endregion
	}
}
