using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WalkControl
{
	public class Simulator
	{
		public Chromosome Genome { get; set; }
		public int MutatePercentChance { get; set; }		
		public int PopulationSize = 20;
		public int CullPercent = 10;
		public int MutationPercent = 10;
		
		public Simulator()
		{
			Genome = new Chromosome();
		}

		public void Tick()
		{
			//apply fitness measure
				//foreach genome
					//apply fitness, store score
			//sort scores
			//select best genome
			//select worst genome (for variety)
			//remove bottom CullPercent
			//breed remaining genomes with a random other genome based on fitness ranking
			//mutate MutationPErcent of the new generation
			Genome.Mutate(MutatePercentChance);
		}
	}
}
