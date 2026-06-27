using VocaluxeLib.Songs.UltraStar;

namespace VocaluxeLib.Songs
{
    public sealed class CMedley
    {
        public EDataSource Source { get; set; }

        public int StartBeat { get; set; }

        public int EndBeat { get; set; }

        public float FadeInTime { get; set; }

        public float FadeOutTime { get; set; }

        public CMedley(){}

        public CMedley(CMedley old)
        {
            Source = old.Source;
            StartBeat = old.StartBeat;
            EndBeat = old.EndBeat;
            FadeInTime = old.FadeInTime;
            FadeOutTime = old.FadeOutTime;
        }
    }
}
