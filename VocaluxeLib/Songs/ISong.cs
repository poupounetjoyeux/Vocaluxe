using System.Collections.Generic;
using System.Drawing;
using System.IO;
using VocaluxeLib.Draw;
using VocaluxeLib.Songs.Sources;

namespace VocaluxeLib.Songs
{
    public interface ISong
    {
        int Id { get; set; }
        CSongInfos Infos { get; }
        public CTextureRef CoverTexture { get; }
        string Title { get; }
        string Artist { get; }
        string Album { get; }
        float Bpm { get; }
        float Gap { get; }
        float VideoGap { get; }
        float Start { get; set; }
        float End { get; set; }
        string Year { get; }
        List<string> Creators { get; }
        List<string> Genres { get; }
        List<string> Languages { get; }
        List<string> Editions { get; }
        List<string> Tags { get; }
        bool IsDuet { get; }
        bool IsRap { get; }
        bool HasCover { get; }
        bool HasVideo { get; }
        bool HasVocals { get; }
        bool HasInstrumental { get; }
        bool NotesLoaded { get; }
        CMedley Medley { get; }
        CShortEnd ShortEnd { get; }
        CPreview Preview { get; }
        CNotes Notes { get; }
        EAspect VideoAspect { get; }
        ISoundSource GetAudioSource();
        ISoundSource GetInstrumentalSource();
        ISoundSource GetVocalsSource();
        string[] GetBackgroundUris();
        Stream GetVideoStream();
        ISong Clone();
        void LoadAndCacheCoverIfNeeded();
        string GetTitleSorting(bool ignoreArticles);
        string GetArtistSorting(bool ignoreArticles);
        Bitmap GetCoverBitmap();
    }
}
