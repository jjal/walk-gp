using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WalkControl
{
	public class InCodeEmulator : Robot
	{
		private double RADIANS = 0.0174532925;
		private double LimbLength = 10;
		private double ServoMax = 1024;

		public InCodeEmulator()
			: base()
		{
			State = new Dictionary<int, int>();
		}

		public override void UpdateState(Dictionary<int, int> State)
		{
			//load state to device
			this.State = State;
		}

		public override Dictionary<int, int> ReadState()
		{
			return State;
		}

		protected override void ResetState()
		{
			for (int i = 0; i < NumServos; i++)
			{
				State[i] = 0;
			}
		}

		public override int[] GetGyroState()
		{
			//pretend opposable joints
			var left = JointDegrees(0) - JointDegrees(1);
			//pretend opposable joints
			var right = JointDegrees(2) - JointDegrees(3);
			//we'll roughly approximate the pitch as the average of two legs' pitches
			var x = (int)(left + right / 2);
			//we'll count this as the height. the maths are a bit hazy.. 
			var firstStepLeft = Math.Asin(JointRadians(0)) * LimbLength;
			var theta = (90 - (JointDegrees(0) + JointDegrees(1))) * RADIANS;
			var hypotenuse = cosine(firstStepLeft, LimbLength, theta);
			var heightBase = Math.Acos(JointRadians(0)) * LimbLength;
			var totalHeightLeft = Math.Sqrt(Math.Pow(hypotenuse, 2) - Math.Pow(heightBase, 2)); //pythagorean theorum

			var firstStepRight = Math.Asin(JointRadians(2)) * LimbLength;
			theta = (90 - (JointDegrees(2) + JointDegrees(3))) * RADIANS;
			hypotenuse = cosine(firstStepLeft, LimbLength, theta);
			heightBase = Math.Acos(JointRadians(2)) * LimbLength;
			var totalHeightRight = Math.Sqrt(Math.Pow(hypotenuse, 2) - Math.Pow(heightBase, 2)); //pythagorean theorum

			if (Double.IsNaN(totalHeightLeft)) totalHeightLeft = 0;
			if (Double.IsNaN(totalHeightRight)) totalHeightRight = 0;

			//roughly approximate the yaw as the difference between legs
			var y = (int)(totalHeightLeft - totalHeightRight);

			var z = (int)(totalHeightLeft + totalHeightRight / 2);
			return new int[] { x, y, z }; ;
		}

		protected double JointDegrees(int joint)
		{
			return (double)State[joint] / ServoMax * 360;
		}

		protected double JointRadians(int joint)
		{
			return JointDegrees(joint) * RADIANS;
		}

		protected double cosine(double a, double b, double C)
		{
			return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2) + (2 * a * b * Math.Cos(C)));
		}
	}
}
