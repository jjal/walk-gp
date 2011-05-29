using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Diagnostics;

namespace WalkControl
{
	public class Chromosome : ICloneable
	{
		/// <summary>
		/// number of servos on the walker
		/// </summary>
		private const int NumServos = 4;
		/// <summary>
		/// chance of creating a new action when mutating the chromosome
		/// </summary>
		private const int NewActionChance = 50;
		/// <summary>
		/// Size of the state dictionary. 
		/// </summary>
		private const int StateSize = 4;
		/// <summary>
		/// Max new servos to add in a new action node
		/// </summary>
		private const int NewActionMaxServos = 3;
		/// <summary>
		/// Max value for a servo
		/// </summary>
		private int ServoMax = 1024;
		/// <summary>
		/// chance to lose a node
		/// </summary>
		private int MutateChanceLose = 30;
		/// <summary>
		/// chance to mutate a node
		/// </summary>
		private int MutateChanceMutate = 60;
		/// <summary>
		/// chance to add a node
		/// </summary>
		private int MutateChanceAdd = 60; //add > lose so they'll grow over time
		/// <summary>
		/// chance to mutate an angle (if already mutating the action)
		/// </summary>
		private int MutateActionAngle = 60;
		/// <summary>
		/// Genomes will not mutate larger than this. 
		/// </summary>
		private int MutateMaxSize = 50;
		/// <summary>
		/// Genomes will not mutate smaller than this.
		/// </summary>
		private int MutateMinSize = 4;
		/// <summary>
		/// When mutation an Action, chance that an angle definition will be added to it
		/// </summary>
		private int MutateActionAddAngle = 50;
		/// <summary>
		/// When mutating an Action, chance that an angle definition will be removed from it
		/// </summary>
		private int MutateActionRemAngle = 50;
		/// <summary>
		/// Root node of the genome
		/// </summary>
		public Node Genome { get; set; }
		/// <summary>
		/// Seed for random operations
		/// </summary>
		protected Random Random
		{
			get;
			set;
		}

		public Chromosome(Node Genome)
		{
			this.Genome = Genome;
			Random = new Random((int)(DateTime.Now.Ticks % Int32.MaxValue));
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

		/// <summary>
		/// Will enumerate a list of actions based on the state, calculating based
		/// on the conditionals in the tree
		/// </summary>
		/// <param name="State">
		/// A <see cref="Dictionary<System.Int32, System.Int32>"/>
		/// </param>
		/// <returns>
		/// A <see cref="IEnumerable<Action>"/>
		/// </returns>
		public IEnumerable<Node> Enumerate(Dictionary<int, int> State)
		{
			foreach (var a in Enumerate(State, Genome))
				yield return a;
		}

		/// <summary>
		/// Recursive enumeration of actions using conditions considering current state
		/// </summary>
		/// <param name="State">
		/// A <see cref="Dictionary<System.Int32, System.Int32>"/>
		/// </param>
		/// <param name="node">
		/// A <see cref="Node"/>
		/// </param>
		/// <returns>
		/// A <see cref="IEnumerable<Node>"/>
		/// </returns>
		public IEnumerable<Node> Enumerate(Dictionary<int, int> State, Node node)
		{

			if (node as Action != null)
				yield return node as Action;

			if (node as Conditional != null && State == null) //only enumerate conditionals if this is a non-state-dependent enumeration
				yield return node as Conditional;

			foreach (var c in node.Children)
				foreach (var n in Enumerate(State, c))
					yield return n;

			var cond = node as Conditional;
			if (cond != null)
			{
				if (cond.Success != null && ((State != null && cond.Evaluate(State)) || State == null))
					foreach (var n in Enumerate(State, cond.Success))
						yield return n;
				if (cond.Failure != null && ((State != null && !cond.Evaluate(State)) || State == null))
					foreach (var n in Enumerate(State, cond.Failure))
						yield return n;
			}

		}

		/// <summary>
		/// Creates and returns a mutated version of this chromosome
		/// </summary>
		/// <param name="MutatePercentChance">The percentage chance that any given node in the chromosome will be modified</param>
		/// <returns></returns>
		public Chromosome Mutate(int MutatePercentChance)
		{
			var c = new Chromosome(Genome.Clone() as Node);
			var nodes = c.Enumerate().ToList();
			Trace.Write("Mutating: ");
			foreach (var n in nodes)
			{
				if (Random.Next(100) < MutatePercentChance)
				{
					//can add a child
					if (Random.Next(100) < MutateChanceAdd && nodes.Count < MutateMaxSize)
					{
						Trace.Write(" add ");
						n.AddChild(CreateNode());
					}
					//can lose a child
					if ((Random.Next(100) < MutateChanceLose && nodes.Count > MutateMinSize) || nodes.Count > MutateMaxSize)
					{
						Trace.Write(" rem ");
						if (n.Children.Count > 0)
							n.RemoveChild(n.Children[Random.Next(n.Children.Count)]);
					}
					//can change an action (this is like add and lose) or a functor
					if (Random.Next(100) < MutateChanceMutate)
					{
						Trace.Write(" mut ");
						var a = n as Action;
						if (a != null)
							Mutate(a);
						else
						{
							var co = n as Conditional;
							if (co != null)
								Mutate(co);
						}
						//n.GetType().GetMethod("Mutate").Invoke(n, null);
					}
				}
			}
			Trace.WriteLine("");
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

		public Conditional CreateConditional()
		{
			return new Conditional(CreateConditionParameters());
		}

		public Action CreateAction()
		{
			return new Action(CreateActionParameters());
		}
		/// <summary>
		/// Creates a random node
		/// </summary>
		/// <returns></returns>
		public Node CreateNode()
		{
			Node n;
			if (Random.Next(100) < NewActionChance)
			{
				n = new Action(CreateActionParameters());
			}
			else
			{
				n = new Conditional(CreateConditionParameters());
			}
			return n;
		}

		private Expression<Func<Dictionary<int, int>, bool>> CreateConditionParameters()
		{
			//holy shit i'm using expression builders!
			var state = Expression.Parameter(typeof(Dictionary<int, int>), "state");
			var result = Expression.Parameter(typeof(int), "result");

			var index = Random.Next(StateSize);
			var max = Random.Next(ServoMax);
			//Expression.Property(valueBag, "Item", key)
			var stateAccess = Expression.Block(
				new[] { result },               //make the result a variable in scope for the block           
				Expression.Assign(result, Expression.MakeIndex(state, typeof(Dictionary<int, int>).GetProperty("Item"), new Expression[] { Expression.Constant(index) })),
				result                          //last value Expression becomes the return of the block 
			);
			BinaryExpression comparison;
			if (Random.Next(100) < 50)
				comparison = Expression.GreaterThanOrEqual(stateAccess, Expression.Constant(max));
			else
				comparison = Expression.LessThanOrEqual(stateAccess, Expression.Constant(max));
			var expression = Expression.Lambda<Func<Dictionary<int, int>, bool>>(comparison, state);
			return expression;
		}

		private Dictionary<int, int> CreateActionParameters()
		{
			var a = new Dictionary<int, int>();
			var servosToAdd = Random.Next(NewActionMaxServos);
			for (int i = 0; i < servosToAdd; i++)
				a[Random.Next(NumServos)] = Random.Next(ServoMax);
			return a;
		}

		/// <summary>
		/// Mutate an action by changing (MutateActionAngle), adding (MutateActionAddAngle) or removing (MutateActionRemAngle) an angle definition
		/// </summary>
		/// <remarks>TODO: move this to the action node itself? unclear. right now node classes have no knowledge of genetic shit.</remarks>
		/// <param name="a"></param>
		/// <returns></returns>
		public Node Mutate(Action a)
		{
			for (var i = 0; i < a.Angles.Count; i++)
			{
				if (Random.Next(100) < MutateActionAngle)
					a.Angles[i] = Random.Next(ServoMax);
			}
			if (Random.Next(100) < MutateActionAddAngle && a.Angles.Count < NumServos)
			{
				int angleIndex = 0;
				do
				{
					angleIndex = Random.Next(NumServos);
				} while (a.Angles.ContainsKey(angleIndex));
				a.Angles[angleIndex] = Random.Next(ServoMax);
			}
			if (Random.Next(100) < MutateActionRemAngle && a.Angles.Count > 1)
			{
				int angleIndex = 0;
				do
				{
					angleIndex = Random.Next(NumServos);
				} while (!a.Angles.ContainsKey(angleIndex));
				a.Angles.Remove(angleIndex);
			}
			return a;
		}

		/// <summary>
		/// Mutate a conditional by changing its functor. TODO: right now the condition expression is simply replaced. in the future, actually modify it.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public Node Mutate(Conditional c)
		{
			var e = c.Condition;
			//TODO: actually mutate
			var newC = CreateConditional();
			newC.Success = c.Success;
			newC.Failure = c.Failure;
			return newC;
		}

		/// <summary>
		/// Swaps a random subtree in this chromosome with a random subtree in the partner one. won't modify the chromosome if nodes <= 1
		/// </summary>
		/// <param name="partner"></param>
		public Chromosome[] Crossover(Chromosome partner)
		{
			var child1 = this.Clone() as Chromosome;
			var child2 = partner.Clone() as Chromosome;
			var nodes1 = child1.Enumerate().Count();
			var nodes2 = child2.Enumerate().Count();
			Node parent1 = null, parent2 = null;
			while (parent1 == null && parent2 == null && nodes1 > 1 && nodes2 > 1)
			{
				var child1Site = child1.Enumerate().ElementAt(Random.Next(nodes1));
				var child2Site = child2.Enumerate().ElementAt(Random.Next(nodes2));
				parent1 = child1.Enumerate().FirstOrDefault(n => n.Children.Contains(child1Site));
				parent2 = child2.Enumerate().FirstOrDefault(n => n.Children.Contains(child2Site));
				if (parent1 != null && parent2 != null) //can't be crossing over root nodes!
				{
					parent1.AddChild(child2Site, parent1.Children.IndexOf(child1Site));
					parent2.AddChild(child1Site, parent2.Children.IndexOf(child2Site));
					parent1.RemoveChild(child1Site);
					parent2.RemoveChild(child2Site);
				}
			}
			return new Chromosome[] { child1, child2 };
		}

		public override string ToString()
		{
			//TODO: finish this
			return String.Format("Chromosome {0} [ {1} ]", this.GetHashCode(), Enumerate().Select(n => n.GetType().Name.Substring(0, 1)).Aggregate((str, next) => str += next));
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
