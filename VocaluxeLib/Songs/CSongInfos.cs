using System;

namespace VocaluxeLib.Songs
{
    public sealed class CSongInfos
    {
        public int DataBaseSongId { get; }

        public DateTime DateAdded { get; set; } = DateTime.Today;

        public int NumPlayed { get; set; }

        public int NumPlayedSession { get; set; }

        public CSongInfos(int dataBaseSongId)
        {
            DataBaseSongId = dataBaseSongId;
        }

        public CSongInfos(CSongInfos old)
        {
            DataBaseSongId = old.DataBaseSongId;
            DateAdded = old.DateAdded;
            NumPlayed = old.NumPlayed;
            NumPlayedSession = old.NumPlayedSession;
        }
    }
}
