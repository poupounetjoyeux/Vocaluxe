using VocaluxeLib.Songs.UltraStar;

namespace VocaluxeLib.Songs
{
    public sealed class CShortEnd
    {
        public EDataSource Source { get; set; }

        public int EndBeat { get; set; }

        public CShortEnd(){}

        public CShortEnd(CShortEnd old)
        {
            Source = old.Source;
            EndBeat = old.EndBeat;
        }
    }
}
