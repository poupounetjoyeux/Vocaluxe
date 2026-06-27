#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;
using VocaluxeLib.Songs.Sources;

namespace VocaluxeLib.Songs.UltraStar
{
    public class CSongPointer
    {
        public readonly int SongId;
        public string SortString;
        public bool IsSung;

        public CSongPointer(int id, string sortString)
        {
            SongId = id;
            SortString = sortString;
        }
    }

    [Flags]
    enum EHeaderFlags
    {
        Title = 1,
        Artist = 2,
        Audio = 4,
        Instrumental = 5,
        Vocals = 6,
        Bpm = 8,
        MedleyStartBeat = 16,
        MedleyEndBeat = 32
    }

    public enum EDataSource
    {
        Calculated,
        Tag
    }

    public partial class CUltraStarSong : ISong
    {
        private readonly object _CoverLock = new();
        private CTextureRef _CoverTexture;

        private bool _CalculateMedley { get; set; } = true;
        public CMedley Medley { get; private set; }
        public CPreview Preview { get; private set; }
        public CShortEnd ShortEnd { get; private set; }

        public Encoding Encoding { get; private set; } = new UTF8Encoding();
        public bool ManualEncoding { get; private set; }
        public string Folder { get; private set; } = string.Empty;
        public string FolderName { get; private set; } = string.Empty;
        public string FileName { get; private set; } = string.Empty;
        public bool Relative { get; private set; }

        public string Audio { get; private set; } = string.Empty;
        public string Instrumental { get; private set; } = string.Empty;
        public string Vocals { get; private set; } = string.Empty;
        public string Cover { get; private set; } = string.Empty;
        public List<string> BackgroundFileNames { get; private set; } = new();
        public string Video { get; private set; } = string.Empty;

        public EAspect VideoAspect { get; set; } = EAspect.Automatic;

        public string Title { get; private set; } = string.Empty;
        public string Artist { get; private set; } = string.Empty;

        public CTextureRef CoverTexture
        {
            get
            {
                LoadAndCacheCoverIfNeeded();
                return _CoverTexture;
            }
        }

        public string TitleSorting { get; private set; } = string.Empty;
        public string ArtistSorting { get; private set; } = string.Empty;

        public string Version { get; set; } = string.Empty;
        public string Length { get; set; } = string.Empty; //Length set in song file, SHOULD match actual song length but is more a hint
        public string Source { get; set; } = string.Empty;
        public List<string> UnknownTags  { get; private set; } = new();

        /// <summary>
        ///     Start of the song in s (s in txt)
        /// </summary>
        public float Start { get; set; }
        /// <summary>
        ///     End of the song in s (ms in txt)
        /// </summary>
        public float End { get; set; }

        public float Bpm { get; private set; } = 1f;
        /// <summary>
        ///     Gap of the mp3 in s (ms in txt)
        /// </summary>
        public float Gap { get; private set; }
        /// <summary>
        ///     Gap of the video in s (s in txt)
        /// </summary>
        public float VideoGap { get; private set; }

        private string _Comment = "";

        // Sorting
        public int Id { get; set; }
        public bool IsDuet => Notes.VoiceCount > 1;
        public bool IsRap { get; private set; }
        public string Album { get; private set; } = string.Empty;
        public string Year { get; private set; } = string.Empty;

        public bool HasCover => _FileExist(Cover);
        public bool HasVideo => _FileExist(Video);
        public bool HasVocals => _FileExist(Vocals);
        public bool HasInstrumental => _FileExist(Instrumental);

        public List<string> Creators { get; private set; } = new();
        public List<string> Editions { get; private set; } = new();
        public List<string> Genres { get; private set; } = new();
        public List<string> Tags { get; private set; } = new();
        public List<string> Languages { get; private set; } = new();

        // Notes
        public bool NotesLoaded { get; private set; }
        public CNotes Notes { get; private set; } = new();

        public CSongInfos Infos { get; private set; }

        //No point creating a song without a text file --> Use factory method LoadSong
        private CUltraStarSong() { }

        public static CUltraStarSong LoadSong(string filePath)
        {
            var song = new CUltraStarSong();
            var loader = new CUltraStarSongLoader(song);
            return loader.InitPaths(filePath) && loader.ReadHeader() ? song : null;
        }

        public bool LoadNotes()
        {
            var loader = new CUltraStarSongLoader(this);
            return loader.ReadNotes();
        }

        public ISong Clone()
        {
            return new CUltraStarSong
            {
                _CoverTexture = _CoverTexture,
                Medley = Medley != null ? new CMedley(Medley) : null,
                _CalculateMedley = _CalculateMedley,
                Preview = Preview != null ? new CPreview(Preview) : null,
                ShortEnd = ShortEnd != null ? new CShortEnd(ShortEnd) : null,
                Encoding = Encoding,
                ManualEncoding = ManualEncoding,
                Folder = Folder,
                FolderName = FolderName,
                FileName = FileName,
                Relative = Relative,
                Audio = Audio,
                Instrumental = Instrumental,
                Vocals = Vocals,
                Cover = Cover,
                BackgroundFileNames = BackgroundFileNames,
                Video = Video,
                VideoAspect = VideoAspect,
                NotesLoaded = NotesLoaded,
                Artist = Artist,
                Title = Title,
                ArtistSorting = ArtistSorting,
                TitleSorting = TitleSorting,
                Version = Version,
                Length = Length,
                Source = Source,
                UnknownTags = UnknownTags.ToList(),
                Start = Start,
                End = End,
                Bpm = Bpm,
                Gap = Gap,
                VideoGap = VideoGap,
                _Comment = _Comment,
                Id = Id,
                Creators = Creators.ToList(),
                Editions = Editions.ToList(),
                Genres = Genres.ToList(),
                Tags = Tags.ToList(),
                Languages = Languages.ToList(),
                Album = Album,
                Year = Year,
                Infos = Infos != null ? new CSongInfos(Infos) : null,
                Notes = new CNotes(Notes)
            };
        }

        public bool Save()
        {
            return Save(Path.Combine(Folder, FileName));
        }

        public bool Save(string filePath)
        {
            var writer = new CUltraStarSongWriter(this);
            return writer.SaveFile(filePath);
        }

        public ISoundSource GetAudioSource()
        {
            var audioPath = _GetFilePathIfExist(Audio);
            return audioPath == null ? null : new CSongFileSource(this, audioPath);
        }

        public ISoundSource GetInstrumentalSource()
        {
            var instrumentalPath = _GetFilePathIfExist(Instrumental);
            return instrumentalPath == null ? null : new CSongFileSource(this, instrumentalPath);
        }

        public ISoundSource GetVocalsSource()
        {
            var vocalsPath = _GetFilePathIfExist(Vocals);
            return vocalsPath == null ? null : new CSongFileSource(this, vocalsPath);
        }

        public string[] GetBackgroundUris()
        {
            return BackgroundFileNames.Select(_GetFilePathIfExist).Where(p => !string.IsNullOrEmpty(p)).ToArray();
        }

        private bool _FileExist(string fileName)
        {
            return !string.IsNullOrEmpty(_GetFilePathIfExist(fileName));
        }

        private string _GetFilePathIfExist(string fileName)
        {
            if (string.IsNullOrEmpty(Folder) || string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var filePath = Path.Combine(Folder, fileName);
            return File.Exists(filePath) ? filePath : null;
        }

        public Stream GetVideoStream()
        {
            var videoPath = _GetFilePathIfExist(Video);
            if (!string.IsNullOrEmpty(videoPath))
            {
                return new FileStream(videoPath, FileMode.Open, FileAccess.Read);
            }

            CLog.Error($"Video file {videoPath} doesn't exist");
            return null;
        }

        public void LoadAndCacheCoverIfNeeded()
        {
            lock (_CoverLock)
            {
                if (_CoverTexture != null)
                {
                    // Already loaded
                    return;
                }

                var coverPath = _GetFilePathIfExist(Cover);
                if (!string.IsNullOrEmpty(coverPath))
                {
                    _CoverTexture = CBase.DataBase.GetCover(coverPath);
                    if (_CoverTexture != null)
                    {
                        // Cover loaded from DB cache
                        return;
                    }
                    // Generate the cover then enqueue it in the CoverDB transaction
                    using var bitmap = GetCoverBitmap();
                    var coverData = CBase.Cover.GenerateCoverData(bitmap, out var finalSize);
                    CBase.DataBase.EnqueueCoverToTransaction(coverPath, finalSize, coverData);
                }

                // Fallback on the default cover
                _CoverTexture ??= CBase.Cover.GenerateCover(Title, ECoverGeneratorType.Song, null);
            }
        }

        public string GetTitleSorting(bool ignoreArticles)
        {
            if (ignoreArticles && !string.IsNullOrEmpty(TitleSorting))
            {
                return TitleSorting;
            }

            return Title;
        }
        public string GetArtistSorting(bool ignoreArticles)
        {
            if (ignoreArticles && !string.IsNullOrEmpty(ArtistSorting))
            {
                return ArtistSorting;
            }

            return Artist;
        }

        public Bitmap GetCoverBitmap()
        {
            var coverPath = _GetFilePathIfExist(Cover);
            return CHelper.LoadBitmap(coverPath);
        }

        private void _CheckFiles()
        {
            if (Cover == "")
            {
                var files = CHelper.ListImageFiles(Folder);
                foreach (var file in files)
                {
                    if (file.ContainsIgnoreCase("[CO]") &&
                        (file.ContainsIgnoreCase(Title) || file.ContainsIgnoreCase(Artist)))
                    {
                        Cover = file;
                    }
                }
            }

            if (BackgroundFileNames.Count == 0)
            {
                var files = CHelper.ListImageFiles(Folder);
                foreach (var file in files)
                {
                    if (file.ContainsIgnoreCase("[BG]") &&
                        (file.ContainsIgnoreCase(Title) || file.ContainsIgnoreCase(Artist)))
                    {
                        BackgroundFileNames.Add(file);
                    }
                }
            }
        }

        private void _CheckDuet()
        {
            for (var i = 0; i < Notes.VoiceCount; i++)
            {
                if (!Notes.VoiceNames.IsSet(i))
                {
                    CLog.Error("Warning: Can't find #P" + (i + 1) + "-tag for duets in \"" + Artist + " - " + Title + "\".");
                }
            }
        }

        private struct SSeries
        {
            public int Start;
            public int End;
            public int Length;
        }

        private List<SSeries> _GetSeries()
        {
            var voice = Notes.GetVoice(0);

            if (voice.NumLines == 0)
            {
                return null;
            }

            // build sentences list
            var sentences = voice.Lines.Select(line => line.Points != 0 ? line.Lyrics : string.Empty).ToList();

            // find equal sentences series
            var series = new List<SSeries>();
            for (var i = 0; i < voice.NumLines - 1; i++)
            {
                for (var j = i + 1; j < voice.NumLines; j++)
                {
                    if (sentences[i] != sentences[j] || sentences[i] == "")
                    {
                        continue;
                    }

                    var tempSeries = new SSeries { Start = i, End = i };

                    int max;
                    if (j + j - i > voice.NumLines)
                    {
                        max = voice.NumLines - 1 - j;
                    }
                    else
                    {
                        max = j - i - 1;
                    }

                    for (var k = 1; k <= max; k++)
                    {
                        if (sentences[i + k] == sentences[j + k] && sentences[i + k] != "")
                        {
                            tempSeries.End = i + k;
                        }
                        else
                        {
                            break;
                        }
                    }

                    tempSeries.Length = tempSeries.End - tempSeries.Start + 1;
                    series.Add(tempSeries);
                }
            }

            return series;
        }

        private void _CalcMedley()
        {
            if (IsDuet)
            {
                Medley = null;
                return;
            }

            if (!_CalculateMedley || Medley != null)
            {
                return;
            }

            var series = _GetSeries();
            if (series == null)
            {
                return;
            }

            // search for longest series
            var longest = 0;
            for (var i = 0; i < series.Count; i++)
            {
                if (series[i].Length > series[longest].Length)
                {
                    longest = i;
                }
            }

            var voice = Notes.GetVoice(0);

            Medley = new CMedley();
            // set medley vars
            if (series.Count > 0 && series[longest].Length > CBase.Settings.GetMedleyMinSeriesLength())
            {
                Medley.StartBeat = voice.Lines[series[longest].Start].FirstNoteBeat;
                Medley.EndBeat = voice.Lines[series[longest].End].LastNoteBeat;

                var foundEnd = CBase.Game.GetTimeFromBeats(Medley.EndBeat, Bpm) - CBase.Game.GetTimeFromBeats(Medley.StartBeat, Bpm) < CBase.Settings.GetMedleyMinDuration();

                // set end if duration < MedleyMinDuration

                if (!foundEnd)
                {
                    for (var i = series[longest].End + 1; i < voice.NumLines - 1; i++)
                    {
                        if (CBase.Game.GetTimeFromBeats(voice.Lines[i].LastNoteBeat, Bpm) - CBase.Game.GetTimeFromBeats(Medley.StartBeat, Bpm) <
                            CBase.Settings.GetMedleyMinDuration())
                        {
                            foundEnd = true;
                            Medley.EndBeat = voice.Lines[i].LastNoteBeat;
                            break;
                        }
                    }
                }

                if (foundEnd)
                {
                    Medley.Source = EDataSource.Calculated;
                    Medley.FadeInTime = CBase.Settings.GetDefaultMedleyFadeInTime();
                    Medley.FadeOutTime = CBase.Settings.GetDefaultMedleyFadeOutTime();
                }
            }
        }

        private void _CheckPreview()
        {
            if (Preview != null)
            {
                return;
            }

            if (Medley != null)
            {
                Preview = new CPreview
                {
                    StartTime = CBase.Game.GetTimeFromBeats(Medley.StartBeat, Bpm),
                    Source = EDataSource.Calculated
                };
            }
        }

        private void _FindShortEnd()
        {
            if (ShortEnd != null)
            {
                return;
            }

            var series = _GetSeries();
            if (series == null)
            {
                return;
            }

            var voice = Notes.GetVoice(0);

            //Calculate length of singing
            var stop = (voice.Lines[voice.Lines.Length - 1].LastNoteBeat - voice.Lines[0].FirstNote.StartBeat) / 2 + voice.Lines[0].FirstNote.StartBeat;

            ShortEnd = new CShortEnd();
            //Check if stop is in series
            for (var i = 0; i < series.Count; i++)
            {
                if (voice.Lines[series[i].Start].FirstNoteBeat < stop && voice.Lines[series[i].End].LastNoteBeat > stop)
                {
                    if (stop < voice.Lines[series[i].Start].FirstNoteBeat + (voice.Lines[series[i].End].LastNoteBeat - voice.Lines[series[i].Start].FirstNoteBeat) / 2)
                    {
                        ShortEnd.EndBeat = voice.Lines[series[i].Start - 1].LastNote.EndBeat;
                        ShortEnd.Source = EDataSource.Calculated;
                        return;
                    }

                    ShortEnd.EndBeat = voice.Lines[series[i].End].LastNote.EndBeat;
                    ShortEnd.Source = EDataSource.Calculated;
                    return;
                }
            }

            //Check if stop is in line
            foreach (var line in voice.Lines)
            {
                if (line.FirstNoteBeat < stop && line.LastNoteBeat > stop)
                {
                    ShortEnd.EndBeat = line.LastNoteBeat;
                    ShortEnd.Source = EDataSource.Calculated;
                    return;
                }
            }

            ShortEnd.EndBeat = stop;
            ShortEnd.Source = EDataSource.Calculated;
        }
    }
}