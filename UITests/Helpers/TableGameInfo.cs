using System;
using static System.FormattableString;

namespace UITests.Helpers
{
    public enum UpdateState
    {
        None,
        InProgress,
        Submitted,
        Failure
    }

    public sealed class TableGameInfo : IEquatable<TableGameInfo>
    {
        public bool Included { get; }
        public string SteamName { get; }
        public bool VerifiedFinite { get; }
        public double SteamPlaytime { get; }
        public double MainPlaytime { get; }
        public double ExtrasPlaytime { get; }
        public double CompletionistPlaytime { get; }
        public string HltbName { get; }
        public bool MissingCorrelation { get; }
        public bool VerifiedCorrelation { get; }
        public UpdateState UpdateState { get; }

        public TableGameInfo(bool included, string steamName, bool verifiedFinite, double steamPlaytime,
            double mainPlaytime, double extrasPlaytime, double completionistPlaytime, bool missingCorrelation, string hltbName, bool verifiedCorrelation, UpdateState updateState)
        {
            Included = included;
            SteamName = steamName;
            VerifiedFinite = verifiedFinite;
            SteamPlaytime = steamPlaytime;
            MainPlaytime = mainPlaytime;
            ExtrasPlaytime = extrasPlaytime;
            CompletionistPlaytime = completionistPlaytime;
            VerifiedCorrelation = verifiedCorrelation;
            HltbName = hltbName;
            MissingCorrelation = missingCorrelation;
            UpdateState = updateState;
        }

        public bool Equals(TableGameInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Included == other.Included && string.Equals(SteamName, other.SteamName) && VerifiedFinite == other.VerifiedFinite && SteamPlaytime.Equals(other.SteamPlaytime) &&
                   MainPlaytime.Equals(other.MainPlaytime) && ExtrasPlaytime.Equals(other.ExtrasPlaytime) && CompletionistPlaytime.Equals(other.CompletionistPlaytime) &&
                   string.Equals(HltbName, other.HltbName) && VerifiedCorrelation == other.VerifiedCorrelation;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((TableGameInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Included.GetHashCode();
                hashCode = (hashCode * 397) ^ (SteamName?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ VerifiedFinite.GetHashCode();
                hashCode = (hashCode * 397) ^ SteamPlaytime.GetHashCode();
                hashCode = (hashCode * 397) ^ MainPlaytime.GetHashCode();
                hashCode = (hashCode * 397) ^ ExtrasPlaytime.GetHashCode();
                hashCode = (hashCode * 397) ^ CompletionistPlaytime.GetHashCode();
                hashCode = (hashCode * 397) ^ (HltbName?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ VerifiedCorrelation.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return Invariant($"Included: {Included}, SteamName: {SteamName}, VerifiedFinite: {VerifiedFinite}, SteamPlaytime: {SteamPlaytime}, MainPlaytime: {MainPlaytime}, ExtrasPlaytime: {ExtrasPlaytime}, CompletionistPlaytime: {CompletionistPlaytime}, HltbName: {HltbName}, VerifiedCorrelation: {VerifiedCorrelation}");
        }
    }
}