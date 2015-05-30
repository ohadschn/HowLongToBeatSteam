using System;
using System.Globalization;
using Common.Storage;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Entities
{
    public class GenreStatsEntity : TableEntity
    {
        public static string GetDecoratedGenre(string genre, string gameType)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} ({1})", genre, gameType);
        }

        public string Genre { get; set; }
        public string AppType { get; set; }
        public int MainAverage { get; set; }
        public int ExtrasAverage { get; set; }
        public int CompletionistAverage { get; set; }
        public double MainExtrasRatio { get; set; }
        public double ExtrasCompletionistRatio { get; set; }
        public double ExtrasPlacementRatio { get; set; }

        public GenreStatsEntity() //required by azure storage client library
        {
        }

        public GenreStatsEntity([NotNull] string genre, [NotNull] string appType)
            : base("GenreStats", StorageHelper.CleanStringForTableKey(GetDecoratedGenre(genre, appType))) 
        {
            if (genre == null) throw new ArgumentNullException("genre");
            if (appType == null) throw new ArgumentNullException("appType");

            Genre = genre;
            AppType = appType;
        }
    }
}
