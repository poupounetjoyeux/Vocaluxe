using System.Collections.Generic;
using System.Linq;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Utils
{
    public static class CSongUtil
    {
        public static IList<EGameMode> GetAvailableGameModes(this ISong song)
        {
            var gms = new List<EGameMode> { song.IsDuet ? EGameMode.TR_GAMEMODE_DUET : EGameMode.TR_GAMEMODE_NORMAL };
            if (song.Medley != null)
            {
                gms.Add(EGameMode.TR_GAMEMODE_MEDLEY);
            }

            if (song.ShortEnd != null)
            {
                gms.Add(EGameMode.TR_GAMEMODE_SHORTSONG);
            }

            return gms;
        }

        /// <summary>
        ///     Returns true if the requested game mode is available
        /// </summary>
        /// <param name="song"></param>
        /// <param name="gameMode"></param>
        /// <returns>true if the requested game mode is available</returns>
        public static bool IsGameModeAvailable(this ISong song, EGameMode gameMode)
        {
            return song.GetAvailableGameModes().Any(gm => gm == gameMode);
        }
    }
}
