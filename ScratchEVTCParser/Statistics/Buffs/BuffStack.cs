using ScratchEVTCParser.Model.Agents;
using ScratchEVTCParser.Model.Skills;

namespace ScratchEVTCParser.Statistics.Buffs
{
	public class BuffStack
	{
		public BuffStack(long timeStart, long timeEnd, Skill buff, Agent source)
		{
			TimeStart = timeStart;
			TimeEnd = timeEnd;
			Buff = buff;
			Source = source;
		}

		public long TimeStart { get; }
		public long TimeEnd { get; }
		public Skill Buff { get; }
		public Agent Source { get; }
	}
}