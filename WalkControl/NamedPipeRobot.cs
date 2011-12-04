using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Text;

namespace WalkControl
{
	class NamedPipeRobot : Robot
	{
		protected int IntBytes { get { return System.BitConverter.GetBytes((Int32)1).Length; } }

		NamedPipeClientStream pipeStream;
		public NamedPipeRobot()
			: base()
		{
			//Thread ClientThread = new Thread(ThreadStartClient);
			//ClientThread.Start();
			pipeStream = new NamedPipeClientStream("\\\\.\\pipe\\robotpipe");
			// The connect function will indefinately wait for the pipe to become available
			// If that is not acceptable specify a maximum waiting time (in ms)
			pipeStream.Connect();
		}

		public override void UpdateState(Dictionary<int, int> State)
		{
			List<int> values = new List<int>();
			foreach(var value in State)
			{
				values.Add(value.Key);
				values.Add(value.Value);
			}

			//bound it with -1,-1 so that the named pipe host knows what's going on
			values.Insert(0,-1); values.Insert(0,-1);
			

			//convert to byte array and write it.
			var bytes = values.SelectMany(i => System.BitConverter.GetBytes(i)).ToArray(); //hope that preserves order alright
			using (StreamWriter sw = new StreamWriter(pipeStream))
			{
				sw.AutoFlush = true;
				sw.WriteLine("U");
				sw.Write(bytes);
			}
		}

		public override Dictionary<int, int> ReadState()
		{
			using (StreamWriter sw = new StreamWriter(pipeStream))
			{
				sw.AutoFlush = true;
				sw.WriteLine("R");
			}
			var values = new List<int>();
			using (StreamReader sr = new StreamReader(pipeStream))
			{
				byte[] val = new byte[IntBytes];
				var readBytes = 0;
				do
				{
					pipeStream.Read(val, 0, IntBytes);
					values.Add(System.BitConverter.ToInt32(val, 0));
				} while (readBytes == IntBytes);
			}
			var dic = new Dictionary<int, int>();
			for (int i = 0; i < values.Count() - 1; i += 2)
				dic[values[i]] = values[i + 1];
			return dic;
		}

		public override int[] GetGyroState()
		{
			using (StreamWriter sw = new StreamWriter(pipeStream))
			{
				sw.AutoFlush = true;
				sw.WriteLine("G");
			}
			var values = new List<int>();
			using (StreamReader sr = new StreamReader(pipeStream))
			{
				byte[] val = new byte[IntBytes];
				var readBytes = 0;
				do
				{
					pipeStream.Read(val, 0, IntBytes);
					values.Add(System.BitConverter.ToInt32(val, 0));
				} while (readBytes == IntBytes);
			}
			return values.ToArray();
		}

		protected override void ResetState()
		{
			using (StreamWriter sw = new StreamWriter(pipeStream))
			{
				sw.AutoFlush = true;
				sw.WriteLine("X");
			}
		}
	}
}
