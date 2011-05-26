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

		public Simulator()
		{
			Genome = new Chromosome();
		}

		public void Tick()
		{
			Genome.Mutate(MutatePercentChance);
		}
	}
}
