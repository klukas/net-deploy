using System;

namespace System {
	public struct TimeDiff {
		public string Units;
		public double HowMany;

		public TimeDiff(double howMany, string units) {
			HowMany = howMany;
			Units = units;
		}

		public TimeDiff Abs() {
			HowMany = Math.Abs(HowMany);
			return this;
		}

		public override string ToString() {
			int howMany = (int)HowMany;

			return howMany + " " + (howMany == 1 ? Units.TrimEnd('s') : Units);
		}
	}

	public static class TimeDiffExtensions {

		public static TimeDiff Diff(this DateTime date) {
			return Diff(date, DateTime.Now);
		}

		public static TimeDiff Diff(this DateTime from, DateTime to) {
			var diff = to - from;

			if(Math.Abs(diff.TotalMinutes) < 1) return new TimeDiff(diff.TotalSeconds, "seconds");
			if(Math.Abs(diff.TotalHours) < 1) return new TimeDiff(diff.TotalMinutes, "minutes");
			if(Math.Abs(diff.TotalDays) < 1) return new TimeDiff(diff.TotalHours, "hours");
			if(Math.Abs(diff.TotalDays) < 7) return new TimeDiff(diff.TotalDays, "days");
			if(Math.Abs(diff.TotalDays) < 30 * 2) return new TimeDiff(diff.TotalDays / 7, "weeks");
			if(Math.Abs(diff.Days) < 365 * 2) return new TimeDiff(diff.TotalDays / (365 / 12), "months");
			return new TimeDiff(diff.TotalDays / 365, "years");
		}


	}
}