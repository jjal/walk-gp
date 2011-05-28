using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WalkControl
{
	public class Simulator
	{
		public SortedDictionary<Chromosome,int> Population { get; set; }
		public int MutatePercentChance { get; set; }		
		public int PopulationSize = 20;
		public int CullPercent = 10;
		public int MutationPercent = 10;
		public int CrossoverPercent = 90;
		protected Random Random;
		public Simulator()
		{
			Population = new List<Chromosome>();
			Scores = new SortedDictionary<int, int>();
			for(int i=0;i<PopulationSize;i++)
				Population.Add(new Chromosome(Chromosome.CreateOriginAction()));
			Random = new Random(int)(DateTime.Now.Ticks % Int32.MaxValue);
		}
		
		/// <summary>
		/// Go through and implement all the evolutionary functions on the population 
		/// </summary>
		public void Tick()
		{
			//apply fitness measure to population
			//foreach genome
			foreach(var g in Population.Keys)
			{
				//apply fitness, store score
				Population[g] = JudgeFitness(g);
			}
			//sort scores
			Population = Population.OrderByDescending(kv=>kv.Value);
			
			//select best genome to make sure it's left in the population
			var best = Population.Keys.First();
			//select worst genome (for variety)
			var worst = Population.Keys.Last();
			
			//remove bottom CullPercent + 2 for the best and worst which we'll add back later
			Population = Population.Take((Population.Count()*CullPercent/100)+2);
			//add back the worst since it would have been culled
			Population.Add(worst,0); 
			
			//create a new generation of genomes
			var NextGen = new SortedDictionary<Chromosome,int>();
			
			//mutate MutationPercent of the new generation
			//get a random MutationPercent number of genomes
			while (NextGen.Count < MutationPercent*Population.Count/100)
			{
				var c = Population.ElementAt(Random.Next(Population.Count));
				NextGen[c]=0;//add it to next generation
				Population.Remove(c);//remove it from current generation
			}
			
			//mutate these genomes
			foreach(var g in NextGen)
				g.Key.Mutate();
			
			//crossover genomes remaining in the old population
			foreach(var g in Population)
			{
				var partner = g.Value;
				var partnerIdex = 0;
				do 
				{
					partnerIndex = Random.Next(Population.Count); // get a random partner
					partner = Population[partnerIndex];
					//if this random number check succeeds the partnering will go ahead
					//probability is linear, inversely proportional to distance from spot 0 
					//the bottom genome will never be selected as a second partner but will 
					//get one turn at being the primary
				} while(partner == g && Random.Next(100) < (partnerIndex*-1*(100/Population.Count) + 100));
				//cross the genomes over and add the child to the next generation
				NextGen.Add(g.Value.Crossover(partner));
			}
			//add back the top genome from the last gen (elitism)
			NextGen.Add(best,0);
			
			//we should now have a completely mutated, crossed-over new generation. replace!
			Population = NextGen;
		}
		
		protected int JudgeFitness (Chromosome g)
		{
			//run simulation for N ticks
			//judge score
		}
	}
}
