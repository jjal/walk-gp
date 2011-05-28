using System;
namespace WalkControl
{
	public class Emulator
	{
		protected Dictionary<int,int> State;
		protected Chromosome DNA;
		protected const int NumServos = 4;
		protected const int ActionCompletionTimeout = 1000;
		
		public Emulator ()
		{
			State = new Dictionary<int, int>();
			for(int i=0;i<NumServos;i++)
			{
				State[i] = 0;
			}
		}
		
		public void LoadDNA(Chromosome dna)
		{
			DNA = dna;
		}
		
		public void DoRun()
		{
			var actions = DNA.Enumerate(State).ToList();
			foreach(var action in actions.OfType<Action>())
			{
				//write action's angles to state
				foreach(kv in action.Angles)
					State[kv.Key] = kv.Value;
				//upload the actions
				UpdateState(State);
				var start = DateTime.Now;
				
				//wait for the actions to complete
				while (ReadState() != State && start - DateTime.Now < ActionCompletionTimeout);
			}
		}
		
		public void UpdateState(Dictionary<int,int> State)
		{
			//load state to device
			this.State = State;
		}
		
		public Dictionary<int,int> ReadState()
		{
			return State;
		}
		
		public Triplet<int,int,int> GetGyroState()
		{
			var left = State[0] - State[1];
			var right = State[2] - State[3];
			x = left + right / 2;
			y = left - right;
			z = 0;
			var gyroState = new Triplet<int,int,int>(x,y,z);			
		}
		
	}
}