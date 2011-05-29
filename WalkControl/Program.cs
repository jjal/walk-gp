using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace WalkControl
{

	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			//Application.Run(new Form1());
			var sim = new Simulator();
			var file = File.CreateText("scores.csv");
			var maxScore = 0;
			while (maxScore < 500)
			{
				var scores = sim.Tick();
				file.WriteLine(scores.Select(s=>s.Value.ToString()).Aggregate((str,next)=>str+","+next));
				maxScore = scores.Max(s => s.Value);
			}
			file.Close();
		}
	}
}
