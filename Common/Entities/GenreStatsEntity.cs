﻿using System;
using System.Globalization;
using Common.Storage;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Entities
{
    public class GenreStatsEntity : TableEntity
    {
        private const string GenreStatsPartitionKey = "GenreStats";

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

        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)] //by azure storage client library
        public GenreStatsEntity()
        {
        }

        public GenreStatsEntity([NotNull] string genre, [NotNull] string appType)
            : base(GenreStatsPartitionKey, StorageHelper.CleanStringForTableKey(GetDecoratedGenre(genre, appType))) 
        {
            Genre = genre ?? throw new ArgumentNullException(nameof(genre));
            AppType = appType ?? throw new ArgumentNullException(nameof(appType));
        }
    }
}
