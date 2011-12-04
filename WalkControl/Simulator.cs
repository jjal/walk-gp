using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace WalkControl
{
	public class Simulator
	{
		/// <summary>
		/// Size of the population to shoot for
		/// </summary>
		public const int PopulationSize = 30;
		/// <summary>
		/// % of the population that will be culled off the bottom
		/// </summary>
		public const int CullPercent = 50;
		/// <summary>
		/// % of chromosomes of a population that will be mutated to produce the next generation
		/// </summary>
		public const int MutationPercent = 50;
		/// <summary>
		/// % chance each node in the chromosome is mutated when chromosome is mutated
		/// </summary>
		public const int MutationSeverity = 50;
		/// <summary>
		/// ms that a particular phenotype is allowed to run its genotype
		/// </summary>
		private const int RunLength = 1000;
		/// <summary>
		/// Select crossover partners using weighted probability (based on score) or just randomly?
		/// </summary>
		private const bool UseWeightedPairings = true;

		public Dictionary<int, Chromosome> Population { get; set; }
		public Dictionary<int, int> Scores { get; set; }
		private int NumGenerations  = 0;
		protected Random Random;
		public Simulator()
		{
			Population = new Dictionary<int, Chromosome>();
			Scores = new Dictionary<int, int>();
			Trace.WriteLine("Sim setting up, creating " + PopulationSize + " genomes.");
			Random = new Random((int)DateTime.Now.Ticks % Int32.MaxValue);
			for (int i = 0; i < PopulationSize; i++)
			{
				var c = new Chromosome(Chromosome.CreateOriginAction());
				var cond = c.CreateConditional();
				cond.Success = c.CreateAction();
				cond.Failure = c.CreateAction();
				c.Genome.AddChild(cond);
				Population.Add(c.GetHashCode(), c);
			}
			Trace.WriteLine("Sim simulator ready to go.");
		}

		/// <summary>
		/// Go through and implement all the evolutionary functions on the population 
		/// </summary>
		public Dictionary<int,int> Tick()
		{
			Trace.WriteLine("Sim Tick Generation "+NumGenerations+++" Population size: " + Population.Count);
			Scores.Clear();
			//apply fitness measure to population
			//foreach genome
			foreach (var g in Population)
			{
				//apply fitness, store score
				Scores[g.Key] = JudgeFitness(g.Value);
			}

			//sort population by scores
			Population = Population.OrderByDescending(kv => Scores[kv.Key]).ToDictionary(kv => kv.Key, kv => kv.Value);

			//select best genome to make sure it's left in the population
			var best = Population.Values.First();
			//select worst genome (for variety)
			var worst = Population.Values.Last();
			//remove bottom CullPercent for the best and worst which we'll add back later
			Population = Population.Take((Population.Count() * (100 - CullPercent) / 100) - 2).ToDictionary(kv => kv.Key, kv => kv.Value);
			//add back the worst since it would have been culled
			Population.Add(worst.GetHashCode(), worst);

			Trace.WriteLine("Sim cull done, new size: " + Population.Count);

			//create a new generation of genomes
			var NextGen = new Dictionary<int, Chromosome>();

			Trace.WriteLine("Sim mutating " + MutationPercent + "% of population");
			//mutate MutationPercent of the new generation
			//get a random MutationPercent number of genomes
			var NumGenomesToMutate = MutationPercent * Population.Count / 100;
			while (NextGen.Count < NumGenomesToMutate)
			{
				var c = Population.ElementAt(Random.Next(Population.Count)).Value;
				var mutated = c.Mutate(MutationSeverity);
				NextGen[mutated.GetHashCode()] = mutated;//add it to next generation
				Population.Remove(c.GetHashCode());//remove it from current generation
			}

			Trace.WriteLine("Sim mutation done. New generation now has " + NextGen.Count + " genomes.");

			Trace.WriteLine("Sim beginning crossovers: ");
			//crossover genomes remaining in the old population
			//After every genome has had a crack at reproducing, we need to maintain the population size, which we will do by round-robin weighted random crossovers.
			for (var i = 0; NextGen.Count < (PopulationSize - 1); i = i < Population.Count ? i++ : 0)
			{
				var popEntry = Population.ElementAt(i);
				var partnerId = UseWeightedPairings ? GetWeightedRandomPartner(Population, popEntry.Key) : GetRandomPartner(Population, popEntry.Key);
				var partner = Population[partnerId];
				Trace.Write("[" + Scores[popEntry.Key] + "," + Scores[partner.GetHashCode()] + "] ");
				//cross the genomes over and add the children to the next generation
				var children = popEntry.Value.Crossover(partner);
				foreach (var c in children)
					if (!NextGen.ContainsKey(c.GetHashCode())) //possible for this to happen on small genomes
						NextGen.Add(c.GetHashCode(), c);
			}
			Trace.WriteLine(" .. crossovers complete. NextGen now has " + NextGen.Count + " genomes.");
			//add back the top genome from the last gen (elitism)
			if (!NextGen.ContainsKey(best.GetHashCode()))
				NextGen.Add(best.GetHashCode(), best);


			//we should now have a completely mutated, crossed-over new generation. replace!
			Population.Clear();
			foreach (var kv in NextGen)
				Population.Add(kv.Key,kv.Value);
			Trace.WriteLine("Sim tick complete. Population size: " + Population.Count);
			Trace.WriteLine("");
			return Scores;
		}

		protected int GetWeightedRandomPartner(Dictionary<int,Chromosome> population, int principalId)
		{
			var partnerId = principalId;
			var partnerIndex = 0;
			do
			{
				partnerIndex = Random.Next(population.Count); // get a random partner
				partnerId = population.ElementAt(partnerIndex).Key;
				//if this random number check succeeds the partnering will go ahead
				//probability is linear, inversely proportional to distance from spot 0 
				//the bottom genome will never be selected as a second partner but will 
				//get one turn at being the primary
			} while (partnerId == principalId && Random.Next(100) < (partnerIndex * -1 * (100 / Population.Count) + 100));
			return partnerId;
		}

		protected int GetRandomPartner(Dictionary<int, Chromosome> population, int principalId)
		{
			var partnerId = principalId;
			var partnerIndex = 0;
			do
			{
				partnerIndex = Random.Next(population.Count); // get a random partner
				partnerId = population.ElementAt(partnerIndex).Key;
				//if this random number check succeeds the partnering will go ahead
				//probability is linear, inversely proportional to distance from spot 0 
				//the bottom genome will never be selected as a second partner but will 
				//get one turn at being the primary
			} while (partnerId == principalId);
			return partnerId;
		}

		protected int JudgeFitness(Chromosome g)
		{
			//run simulation
			var robot = new Robot();

			robot.DoRun(g, RunLength);

			//judge score
			var gyro = robot.GetGyroState();

			var tilt = Math.Abs(gyro[0]) + Math.Abs(gyro[1]);
			if (tilt < 1) //floor tilt at 1 so  there arn't DbZ issues
				tilt = 1;
			//we're trying to judge closeness to 0 for the x and y but height for z also encourage smalller programs
			var score = (100 / tilt) * gyro[2] / g.Enumerate().Count();
			Trace.WriteLine(String.Format("Run ended, judging fitness for " + g + " Gyro state: {0}, {1}, {2}. Score: {3}", gyro[0], gyro[1], gyro[2], score));
			return score;
		}
	}
}
