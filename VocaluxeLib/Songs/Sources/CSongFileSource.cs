using System.IO;

namespace VocaluxeLib.Songs.Sources
{
    public sealed class CSongFileSource : ISoundSource
    {
        private readonly ISong _Song;
        private readonly string _FilePath;

        public CSongFileSource(ISong song, string filePath)
        {
            _Song = song;
            _FilePath = filePath;
        }

        public string GetUri()
        {
            return _FilePath;
        }

        public Stream GetStream()
        {
            return new FileStream(GetUri(), FileMode.Open, FileAccess.Read);
        }

        public string DisplayName => $"{_Song.Artist} - {_Song.Title}";

        public bool Equals(CSongFileSource other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(_Song, other._Song) && _FilePath == other._FilePath;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is CSongFileSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_Song != null ? _Song.GetHashCode() : 0) * 397) ^ (_FilePath != null ? _FilePath.GetHashCode() : 0);
            }
        }
    }
}
