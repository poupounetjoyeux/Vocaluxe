using VocaluxeLib.Songs.UltraStar;

namespace VocaluxeLib.Songs
{
    public sealed class CPreview
    {
        public EDataSource Source { get; set; }

        public float StartTime { get; set; }

        public CPreview(){}

        public CPreview(CPreview old)
        {
            Source = old.Source;
            StartTime = old.StartTime;
        }
    }
}
