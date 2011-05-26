using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace WalkControl
{
	public class Chromosome : ICloneable
	{
		/// <summary>
		/// number of servos on the walker
		/// </summary>
		private const int NumServos = 6;
		/// <summary>
		/// chance of creating a new action when mutating the chromosome
		/// </summary>
		private const int NewActionChance = 50;
		/// <summary>
		/// Size of the state dictionary. 
		/// </summary>
		private const int StateSize = 10;
		/// <summary>
		/// Max new servos to add in a new action node
		/// </summary>
		private const int NewActionMaxServos = 2;
		/// <summary>
		/// Max value for a servo
		/// </summary>
		private int ServoMax = 1024;
		/// <summary>
		/// chance to lose a node
		/// </summary>
		private int MutateChanceLose = 10;
		/// <summary>
		/// chance to mutate a node
		/// </summary>
		private int MutateChanceMutate = 10;
		/// <summary>
		/// chance to add a node
		/// </summary>
		private int MutateChanceAdd = 15; //add > lose so they'll grow over time
		/// <summary>
		/// chance to mutate an angle (if already mutating the action)
		/// </summary>
		private int MutateActionAngle = 50; 
		/// <summary>
		/// Root node of the genome
		/// </summary>
		protected Node Genome { get; set; }
		/// <summary>
		/// Seed for random operations
		/// </summary>
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

		public Chromosome()
		{
			// TODO: Complete member initialization
		}

		public string Serialize(Dictionary<int, int> State)
		{
			return String.Join(";", Enumerate(State).Select(a => a.Serialize()));
		}

		public IEnumerable<Node> Enumerate()
		{
			foreach (var n in Enumerate(null))
				yield return n;
		}

		public IEnumerable<Node> Enumerate(Dictionary<int, int> State)
		{
			foreach (var a in Enumerate(State, Genome))
				yield return a;
		}

		public IEnumerable<Node> Enumerate(Dictionary<int, int> State, Node node)
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

		public Chromosome Mutate(int MutatePercentChance)
		{
			var c = new Chromosome(Genome.Clone() as Node);
			foreach (var n in c.Enumerate(null))
			{
				if (new Random(Seed).Next(100) > MutatePercentChance)
				{
					//can add a child
					if (new Random(Seed).Next(100) > MutateChanceAdd)
					{
						n.AddChild(CreateNode());
					}
					//can lose a child
					if (new Random(Seed).Next(100) > MutateChanceLose)
					{
						if (n.Children.Count > 0)
							n.RemoveChild(n.Children[new Random(Seed).Next(n.Children.Count)]);
					}
					//can change an action (this is like add and lose) or a functor
					if (new Random(Seed).Next(100) > MutateChanceMutate)
					{
						n.GetType().GetMethod("Mutate").Invoke(n,null);
					}
				}
			}
			return c;
		}

		public static Node CreateOriginAction()
		{
			var d = new Dictionary<int, int>();
			for (int i = 0; i < NumServos; i++)
			{
				d.Add(i, 0);
			}
			return new Action(d);
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

		private Expression<Func<Dictionary<int, int>, bool>> CreateCondition()
		{
			//holy shit i'm using expression builders!
			var state = Expression.Parameter(typeof(Dictionary<string, int>), "state");
			var result = Expression.Parameter(typeof(int), "result");

			var index = new Random(Seed).Next(StateSize);
			var max = new Random(Seed).Next(ServoMax);
			//Expression.Property(valueBag, "Item", key)
			var stateAccess = Expression.Block(
				new[] { result },               //make the result a variable in scope for the block           
				Expression.Assign(result, Expression.Property(state, "Item", Expression.Constant(index))),
				result                          //last value Expression becomes the return of the block 
			);

			var comparison = Expression.GreaterThanOrEqual(stateAccess, Expression.Constant(max));
			var expression = Expression.Lambda<Func<Dictionary<int, int>, bool>>(comparison, state, result);
			return expression;
		}

		private Dictionary<int, int> CreateAction()
		{
			var a = new Dictionary<int, int>();
			for (int i = 0; i < new Random(Seed).Next(NewActionMaxServos); i++)
				a[new Random(Seed).Next(NumServos)] = new Random(Seed).Next(ServoMax);
			return a;
		}

		private Node Mutate(Action a)
		{
			foreach (var i in a.Angles)
			{
				if (new Random(Seed).Next(100) > MutateActionAngle)
					a.Angles[i.Key] = new Random(Seed).Next(ServoMax);
			}
			return a;
		}

		private Node Mutate(Conditional c)
		{
			var e = c.Condition;
			//TODO: actually mutate
			return c;
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
			parent1.AddChild(target, parent1.Children.IndexOf(selection));
			parent2.AddChild(selection, parent2.Children.IndexOf(target));
			parent1.RemoveChild(selection);
			parent2.RemoveChild(target);
		}


		#region ICloneable Members

		public object Clone()
		{
			var c = new Chromosome(Genome.Clone() as Node);
			return c;
		}

		#endregion
	}
}
