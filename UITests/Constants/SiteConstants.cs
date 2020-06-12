using System;
using System.Collections.ObjectModel;

namespace UITests.Constants
{
    public static class SiteConstants
    {
        public const string LocalDeploymentUrl = "http://localhost:26500";
        public const string ProductionDeploymentUrl = "http://www.howlongtobeatsteam.com";
        
        public const string CachedGamesSuffix = "#/cached/all";
        internal static string LocalCachedGamesPage = new Uri(new Uri(LocalDeploymentUrl), CachedGamesSuffix).ToString();
        internal static string ProductionCachedGamesPage = new Uri(new Uri(ProductionDeploymentUrl), CachedGamesSuffix).ToString();

        public const string MissingGamesSuffix = "#/missing/all";
        internal static string LocalMissingGamesPage = new Uri(new Uri(LocalDeploymentUrl), MissingGamesSuffix).ToString();
        internal static string ProductionMissingGamesPage = new Uri(new Uri(ProductionDeploymentUrl), MissingGamesSuffix).ToString();

        public const string SteamIdTextId = "steam-id-text";
        public const string SteamSignInButtonId = "steam-sign-in";

        public const string LoginErrorDivId = "login-error";
        public const string EmptyLibraryDivId = "empty-library";

        public const string PersonaNameSpanId = "persona-name";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Img")]
        public const string PersonaAvatarImgId = "persona-avatar";

        public const string GamesFoundTitleId = "games-found-title";
        public const string ExcludedGameCountSpanId = "excluded-game-count";

        public const string MissingHltbGamesAlertDivId = "missing-hltb-games-alert";
        public const string ImputedValuesNotificationDivId = "imputed-values-notification";

        public const string CurrentPlaytimeSpanId = "current-playtime";
        public const string MainPlaytimeRemainingSpan = "main-playtime-remaining";
        public const string MainPlaytimeRemainingPercentSpan = "main-playtime-remaining-percent";
        public const string ExtrasPlaytimeRemainingSpan = "extras-playtime-remaining";
        public const string ExtrasPlaytimeRemainingPercentSpan = "extras-playtime-remaining-percent";
        public const string CompletionistPlaytimeRemainingSpan = "completionist-playtime-remaining";
        public const string CompletionistPlaytimeRemainingPercentSpan = "completionist-playtime-remaining-percent";

        public const string SocialSharingHeaderId = "social-sharing";
        public const string FacebookShareAnchorId = "facebook-share";
        public const string TwitterShareAnchorId = "twitter-share";
        public const string RedditShareAnchorId = "reddit-share";

        public const string SurvivalCalculatorAnchorId = "survival-calculator";
        public const string SurvivalModalId = "steam-survival-modal";
        public const string SurvivalGenderSelectId = "gender-select";
        public const string SurvivalBirthYearSelectId = "birth-year-select";
        public const string SurvivalWeeklyPlaytimeSelectId = "weekly-playtime-select";
        public const string SurvivalPlayStyleSelectId = "play-style-select";
        public const string SurvivalBacklogCompletionLabelId = "backlog-completion-label";
        public const string SurvivalTimeOfDeathLabelId = "time-of-death-label";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Img")]
        public const string SurvivalFailureImgId = "survival-failure";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Img")]
        public const string SurvivalSuccessImgId = "survival-success";
        public const string SurvivalCalculatorButtonId = "survival-calculator-button";
        public const string SurvivalFacebookShareAnchorId = "survival-facebook-share";
        public const string SurvivalTwitterShareAnchorId = "survival-twitter-share";
        public const string SurvivalRedditShareAnchorId = "survival-reddit-share";
        public const string SurvivalNotCalculatedText = "???";
        public const string SurvivalSocialSharingHeaderId = "survival-social-sharing";

        public const string GameTableId = "game-table";

        public const string SteamNameTitle = "steam-name-title";
        public const string SteamPlaytimeTitle = "steam-playtime-title";
        public const string MainPlaytimeTitle = "main-playtime-title";
        public const string ExtrasPlaytimeTitle = "extras-playtime-title";
        public const string CompletionistPlaytimeTitle = "completionist-playtime-title";
        public const string HltbNameTitle = "hltb-name-title";

        public const string RowIncludedCheckboxClass = "inclusion-checkbox";
        public const string RowSteamNameSpanClass = "steam-name";
        public const string RowVerifyGameAnchorId = "verify-game-link";
        public const string RowMobilePlaytimeIndicatorSpanClass = "mobile-playtime-indicator";
        public const string RowSteamPlaytimeCellClass = "steam-playtime";
        public const string RowMainPlaytimeCellClass = "main-playtime";
        public const string RowExtrasPlaytimeCellClass = "extras-playtime";
        public const string RowCompletionistPlaytimeCellClass = "completionist-playtime";
        public const string RowMissingCorrelationSpanClass = "missing-correlation";
        public const string RowHltbNameAnchorClass = "hltb-name";
        public const string RowWrongGameAnchorClass = "wrong-game-link";
        public const string RowCorrelationUpdatingSpanClass = "updating-correlation";
        public const string RowCorrelationUpdateSubmittedClass = "correlation-update-submitted";
        public const string RowCorrelationUpdateFailedClass = "correlation-update-failed";
        public const string RowBlankClass = "blank-row";

        public const string GamesPerPageSelectId = "games-per-page";
        internal static ReadOnlyCollection<int> GamesPerPageOptions = new ReadOnlyCollection<int>(new[] {10, 25, 50, 100});

        public const string FirstPageAnchorId = "first-page";
        public const string PreviousPageAnchorId = "previous-page";
        public const string FixedPageAnchorIdPrefix = "fixed-page-";
        public const string NextPageAnchorId = "next-page";
        public const string LastPageAnchorId = "last-page";

        public const string NonGameUpdateModalId = "non-game-update-modal";
        public const string NonGameUpdateButtonId = "update-non-game";

        public const string HltbUpdateModalId = "hltb-update-modal";
        public const string HltbUpdateInputId = "hltb-update-intput";
        public const string HltbUpdateSubmitButtonId = "submit-hltb-suggestion";

        public const string FilterGameCountSpanId = "filter-game-count";
        public const string FilterInputId = "filter-input";
        public const string SummaryFilterActive = "summary-filter-active";
        public const string SlicingFilterActive = "slicing-filter-active";

        public const string AdvancedFilterModalId = "advanced-filter-modal";
        public const string AdvancedFilterAnchorId = "advanced-filter";
        public const string AdvancedFilterReleaseYearFromOptionsId = "release-year-from";
        public const string AdvancedFilterReleaseYearToOptionsId = "release-year-to";
        public const string AdvancedFilterMetacriticFromOptionsId = "metacritic-from";
        public const string AdvancedFilterMetacriticToOptionsId = "metacritic-to";
        public const string AdvancedFilterGenreOptionsId = "genre-filter";
        public const string AdvancedFilterClearButtonId = "clear-advanced-filter";
        public const string AdvancedFilterApplyButtonId = "apply-advanced-filter";
        public const string AdvancedFilterClearExternalSpanId = "clear-advanced-filter-external";
        public const string AdvancedFilterNoGenresSelectedSpanId = "no-genres-selected";

        public const string CachedGamesPanelId = "cached-games-panel";
        public const string MissingGamesLinkId = "missing-games-page-link";

        public const string FooterFacebookLinkId = "footer-facebook-link";
        public const string FooterTwitterLinkId = "footer-twitter-link";
        public const string FooterSteamGroupLinkId = "footer-steam-group-link";
        public const string FooterGithubLinkId = "footer-github-link";
        public const string MobileFooterPrefix = "mobile-";

        public const string ExternalModalId = "external-modal";

        public const string ContactAnchorId = "contact-link";
        public const string PrivacyAnchorId = "privacy-link";
        public const string FaqAnchorId = "faq-link";
        public const string SteamAnchorId = "steam-link";
        public const string HltbAnchorId = "hltb-link";
        public const string OhadSoftAnchorId = "ohadsoft-link";

        public const string FaqTitle = "Frequently Asked Questions";
        public const string FaqContainerId = "faq-container";

        public const string PrivacyTitle = "Privacy Policy";
        public const string PrivacyContainerId = "privacy-container";

        public const string ExternalPageTitleHeaderId = "external-title";
        public const string ExternalPageFrameId = "external-page-frame";

        public const string ValveSteamLoginButtonId = "imageLogin";
        public const string ValveSteamUsername = "steamAccountName";
        public const string ValveSteamPassword = "steamPassword";

        public static readonly ReadOnlyCollection<string> AmchartDivs = new ReadOnlyCollection<string>(new [] { "playtimeChart", "genreChart", "metacriticChart", "releaseDateChart", "appTypeChart" });
        public const string TotalPlaytimeSlicerId = "total-playtime-slicer";
        public const string RemainingPlaytimeSlicerId = "remaining-playtime-slicer";
        public const string CurrentPlaytimeSlicerId = "current-playtime-slicer";
        public const string MainPlaytimeSlicerId = "main-playtime-slicer";
        public const string ExtrasPlaytimeSlicerId = "extras-playtime-slicer";
        public const string CompletionistPlaytimeSlicerId = "completionist-playtime-slicer";

        public const string NoDataIndicatorId = "no-data-indicator";

        public const string TooltipClass = "tooltip-inner";

        public const string ProfileIdTooltip = "profile-id-tooltip";
        public const string PlaytimeTooltip = "playtime-tooltip";
        public const string ExcludedGamesTooltip = "excluded-games-tooltip";
    }
}
