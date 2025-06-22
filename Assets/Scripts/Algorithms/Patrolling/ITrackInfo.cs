namespace Maes.Algorithms.Patrolling
{
    public interface ITrackInfo
    {

    }

    public struct MeetingHeldTrackInfo : ITrackInfo
    {
        public MeetingHeldTrackInfo(int meetingId)
        {
            MeetingId = meetingId;
        }

        public int MeetingId { get; }
    }

    public delegate void OnTrackInfo(ITrackInfo objectToLog);
}