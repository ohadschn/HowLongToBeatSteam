﻿/*global ko*/
/*global DataTable*/
/*global AmCharts*/
/*global countdown*/
/*global appInsights*/

"use strict"; // jshint ignore:line

// https://stackoverflow.com/questions/610406/javascript-equivalent-to-printf-string-format
// ReSharper disable once NativeTypePrototypeExtending
if (!String.prototype.format) {
    String.prototype.format = function () {
        var args = arguments;
        return this.replace(/{(\d+)}/g, function (match, number) {
            return typeof args[number] != "undefined"
                    ? args[number]
                    : match;
        });
    };
}

var getOwnProperties = function(object) {
    var propArr = [];
    for (var prop in object) {
        if (object.hasOwnProperty(prop)) {
            propArr.push(prop);
        }
    }
    return propArr;
};

var getOrderedOwnProperties = function(object) {
    var propArr = getOwnProperties(object);
    propArr.sort(function (a, b) {
        var aLower = a.toLowerCase();
        var bLower = b.toLowerCase();
        if (aLower < bLower) //sort string ascending
            return -1;
        if (aLower > bLower)
            return 1;
        return 0; //default return value (no sorting)
    });
    return propArr;
};

var capitalizeFirstLetter = function (str) { // jshint ignore:line
    return str.charAt(0).toUpperCase() + str.slice(1);
};

var getHours = function (minutes, digits) { // jshint ignore:line
    if (digits === undefined) {
        digits = 2;
    }
    var hours = Math.max(minutes, 0) / 60;
    return +hours.toFixed(digits);
};

var numberWithCommas = function (x) { // jshint ignore:line
    return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
};

var timeWithCommas = function(x, unit) {
    return numberWithCommas(x) + " " + ((x === 1.0) ? unit : (unit + "s"));
};

var hoursWithCommas = function(x) { // jshint ignore:line
    var hours = getHours(x, 0);
    return timeWithCommas(hours, "hour");
};

var getYearsFromMinutes = function (minutes) { // jshint ignore:line
    /*jshint bitwise: false*/
    return countdown(null, { minutes: minutes }, countdown.YEARS | countdown.MONTHS | countdown.WEEKS | countdown.DAYS | countdown.HOURS).toString("0 hours");
    /*jshint bitwise: true*/
};

var getYearsFromDateDiff = function (start, end) { // jshint ignore:line
    return countdown(start, end, countdown.YEARS, 0, 2, "0 years");
};

var getPercent = function (portion, total) { // jshint ignore:line
    return (100 * portion / total).toFixed(2) + "%";
};

var GameUpdatePhase = {
    None: "None",
    InProgress: "InProgress",
    Success: "Success",
    Failure: "Failure"
};

var PlaytimeType = {
    Current: "Current",
    Main: "Main",
    Extras: "Extras",
    Completionist: "Completionist"
};

var AuthenticationStatus = {
    None: "None",
    Success: "Success",
    Failure: "Failure"
};

var alternatingPalette = [];
var progressivePalette = ["#75c7f0", "#6CC3EF", "#6CC3EF", "#5ABCED", "#47B4EB", "#35ADE9", "#23A5E7", "#189BDC", "#168ECA", "#1481B8"];
var unknownColor = "#ABB5BA";
var unknownTitle = "Unknown";

var slashRegex = new RegExp("/", "g");
var genreSeparatorReplacer = function(genre) { //avoid creating function objects
    return genre.replace(slashRegex, "-");
};

var isBetween = function(x, min, max) {
    return x >= min && x <= max;
};

var getTimeFromNow = function(hoursFromNow) {
    var date = new Date();
    date.setUTCHours(date.getUTCHours() + hoursFromNow);
    return date;
};

//static observables used in initialization for increased performance
var includedObservable = ko.observable(true);
var updatePhaseObservable = ko.observable(GameUpdatePhase.None);
var emptyObservable = ko.observable("");

function Game(steamGame) {

    var self = this;
    self.included = includedObservable;
    self.updatePhase = updatePhaseObservable;

    self.steamPlaytime = steamGame.Playtime;

    var steamAppData = steamGame.SteamAppData;
    self.steamAppId = steamAppData.SteamAppId;
    self.steamName = steamAppData.SteamName;
    self.verifiedGame = steamAppData.VerifiedGame;
    self.appType = steamAppData.AppType;
    self.genres = ko.utils.arrayMap(steamAppData.Genres, genreSeparatorReplacer);
    self.releaseYear = steamAppData.ReleaseYear;
    self.metacriticScore = steamAppData.MetacriticScore;

    var hltbInfo = steamAppData.HltbInfo;
    self.known = hltbInfo.Id !== -1;
    self.hltbId = self.known ? hltbInfo.Id : "";
    self.suggestedHltbId = emptyObservable;
    self.hltbName = self.known ? hltbInfo.Name : "";
    self.hltbMainTtb = hltbInfo.MainTtb;
    self.hltbMainTtbImputed = hltbInfo.MainTtbImputed;
    self.hltbExtrasTtb = hltbInfo.ExtrasTtb;
    self.hltbExtrasTtbImputed = hltbInfo.ExtrasTtbImputed;
    self.hltbCompletionistTtb = hltbInfo.CompletionistTtb;
    self.hltbCompletionistTtbImputed = hltbInfo.CompletionistTtbImputed;
}

Game.prototype.match = function (filter) { //define in prototype to prevent memory footprint on each instance

    //first create dependency on observables
    var metacriticFilterApplied = this.viewModel.metacriticFilterApplied();
    var metacriticFromApplied = this.viewModel.metacriticFromApplied();
    var metacriticToApplied = this.viewModel.metacriticToApplied();

    var releaseYearFilterApplied = this.viewModel.releaseYearFilterApplied();
    var releaseYearFromApplied = this.viewModel.releaseYearFromApplied();
    var releaseYearToApplied = this.viewModel.releaseYearToApplied();

    var genreFilterApplied = this.viewModel.genreFilterApplied();
    var genreAppliedFilter = this.viewModel.genreAppliedFilter();

    //we need to check whether the filters were applied in order to allow unknown values when they are not
    return  (this.steamName.toLowerCase().indexOf(filter.toLowerCase()) !== -1) &&
            (!metacriticFilterApplied || isBetween(this.metacriticScore, metacriticFromApplied, metacriticToApplied)) &&
            (!releaseYearFilterApplied || isBetween(this.releaseYear, releaseYearFromApplied, releaseYearToApplied)) &&
            (!genreFilterApplied || genreAppliedFilter[this.getCombinedGenre()]);
};

Game.prototype.getCombinedGenre = function() {
    if (!this.combinedGenre) {
        var realGenres = ko.utils.arrayFilter(this.genres, function(g) { return g !== "Indie" && g !== "Casual"; });
        this.combinedGenre = (realGenres.length === 0 ? this.genres : realGenres).join("/");
    }
    return this.combinedGenre;
};

function AppViewModel() {
    var self = this;

    Game.prototype.viewModel = self; //define in prototype to prevent memory footprint on each instance

    self.width = $(window).width();
    self.height = $(window).height();

    self.superSmall = self.width < 380;
    self.extraExtraSmall = self.width < 480;
    self.extraSmall = self.width < 768;
    self.small = self.width < 992;
    self.medium = self.width < 1200;

    $(window).on("orientationchange", function() {
        setTimeout(function() { location.reload(); }, 0); //can't reload inside event handler in FireFox
    });
    $(window).resize(function() {
        if ($(window).height() === self.width && $(window).width() === self.height) {
            setTimeout(function() { location.reload(); }, 0); //can't reload inside event handler in FireFox
        }
    });

    $("#hltb-update-modal")
        .on("shown.bs.modal",
            function () {
                $("#hltb-update-intput").focus();
            });

    self.steamVanityUrlName = ko.observable("");

    var tableOptions = {
        recordWord: "game",
        sortDir: "asc",
        perPage: 10,
        paginationLimit: self.superSmall ? 3 : (self.small ? 4 : (self.medium ? 6 : 10)),
        unsortedClass: "glyphicon glyphicon-sort",
        ascSortClass: "glyphicon glyphicon-sort-by-attributes",
        descSortClass: "glyphicon glyphicon-sort-by-attributes-alt",
        alwaysMatch: true
    };

    self.introPage = ko.observable(true);
    self.externalPageTitle = ko.observable("");
    self.externalPageUrl = ko.observable("");
    self.authenticated = ko.observable(AuthenticationStatus.None);

    self.gameTable = new DataTable([], tableOptions);
    self.pageSizeOptions = [10, 25, 50, 100];
    self.gameTable.perPage.subscribe(function(perPage) {
        appInsights.trackEvent("PerPageChanged", {}, { perPage: perPage });
    });

    self.sliceTotal = ko.observable();
    self.sliceCompletionLevel = ko.observable();

    self.partialCache = ko.observable(false);
    self.excludedGameCount = ko.observable(0);
    self.imputedTtbs = ko.observable(false);
    self.missingHltbIds = ko.observable(false);
    self.libraryIsEmpty = ko.observable(false);
    self.personaName = ko.observable("");
    self.avatarUrl = ko.observable("");

    self.processing = ko.observable(false);
    self.status = ko.observable("");
    self.error = ko.observable(false);
    self.cached = ko.observable(false);
    self.missing = ko.observable(false);
    self.realUser = ko.observable(true);

    self.alertHidden = ko.observable(false);
    self.missingAlertHidden = ko.observable(false);
    self.errorAlertHidden = ko.observable(false);

    self.gameToUpdate = ko.observable({
        steamAppId: 0,
        steamName: "",
        hltbId: "",
        suggestedHltbId: ko.observable("")
    });

    var stopProcessing = function() {
        self.status("");
        self.processing(false);
    };

    self.gameTable.filter.subscribe(function(val) {
        appInsights.trackEvent("FilterApplied", {}, { length: val.length });
    });

    self.metacriticPossibleValues = ko.observableArray();
    for (var score = 0; score <= 100; score++) {
        self.metacriticPossibleValues.push(score);
    }
    self.metacriticFrom = ko.observable(0);
    self.metacriticFromApplied = ko.observable(0);
    self.metacriticTo = ko.observable(100);
    self.metacriticToApplied = ko.observable(100);
    self.metacriticRangeValid = ko.pureComputed(function() {
        return self.metacriticFrom() <= self.metacriticTo();
    });
    self.metacriticFilterApplied = ko.pureComputed(function() {
        return self.metacriticFromApplied() > 0 || self.metacriticToApplied() < 100;
    });

    var earliestYear = 1980;
    var latestYear = new Date().getFullYear();
    self.releaseYearPossibleValues = ko.observableArray();
    for (var year = earliestYear; year <= latestYear; year++) {
        self.releaseYearPossibleValues.push(year);
    }
    self.releaseYearFrom = ko.observable(earliestYear);
    self.releaseYearFromApplied = ko.observable(earliestYear);
    self.releaseYearTo = ko.observable(latestYear);
    self.releaseYearToApplied = ko.observable(latestYear);
    self.releaseYearRangeValid = ko.pureComputed(function() {
        return self.releaseYearFrom() <= self.releaseYearTo();
    });
    self.releaseYearFilterApplied = ko.pureComputed(function() {
        return self.releaseYearFromApplied() > earliestYear || self.releaseYearToApplied() < latestYear;
    });

    self.genrePossibleFilters = ko.observableArray();
    self.genreSelectedFilters = ko.observableArray();
    self.genreAppliedFilter = ko.observable({});
    self.genreSelectionValid = ko.pureComputed(function() {
        return self.genrePossibleFilters().length === 0 || self.genreSelectedFilters().length > 0;
    });
    self.genreFilterApplied = ko.pureComputed(function () {
        var firstExcludedGenre = ko.utils.arrayFirst(self.genrePossibleFilters(), function(genre) {
            return !self.genreAppliedFilter()[genre];
        });
        return firstExcludedGenre !== null;
    });

    self.advancedFilterApplied = ko.pureComputed(function () {
        return self.metacriticFilterApplied() || self.releaseYearFilterApplied() || self.genreFilterApplied();
    });

    var setGenreAppliedFilter = function (genres, include, suppressMutationNotification) {
        for (var i = 0; i < genres.length; i++) {
            self.genreAppliedFilter()[genres[i]] = include;
        }

        if (!suppressMutationNotification) {
            self.genreAppliedFilter.valueHasMutated();
        }
    };

    var resetAdvancedFilters = function () {
        self.metacriticFromApplied(0);
        self.metacriticToApplied(100);
        self.releaseYearFromApplied(earliestYear);
        self.releaseYearToApplied(latestYear);
        setGenreAppliedFilter(self.genrePossibleFilters(), true);
    };

    var calculateGenrePossibleFilters = function() {
        var games = self.gameTable.rows();
        var possibleFilterHash = {};
        for (var i = 0; i < games.length; i++) {
            possibleFilterHash[games[i].getCombinedGenre()] = true;
        }

        self.genrePossibleFilters([]);
        var possibleFilters = getOrderedOwnProperties(possibleFilterHash);
        for (var j = 0; j < possibleFilters.length; j++) {
            if (possibleFilters[j] !== unknownTitle) {
                self.genreAppliedFilter()[possibleFilters[j]] = true; //should come first due to observable dependency order
                self.genrePossibleFilters.push(possibleFilters[j]);
            }
        }
    };

    var possibleFiltersCalculated = false;
    self.advancedFilterClicked = function () {
        appInsights.trackEvent("advancedFilterClicked");

        if (!possibleFiltersCalculated) {
            calculateGenrePossibleFilters();
            possibleFiltersCalculated = true;
        }

        self.genreSelectedFilters(ko.utils.arrayFilter(self.genrePossibleFilters(), function (genre) { return self.genreAppliedFilter()[genre]; }));
        self.metacriticFrom(self.metacriticFromApplied());
        self.metacriticTo(self.metacriticToApplied());
        self.releaseYearFrom(self.releaseYearFromApplied());
        self.releaseYearTo(self.releaseYearToApplied());

        $("#advanced-filter-modal").modal("show");
    };

    self.applyAdvancedFilter = function () {
        self.gameTable.currentPageNumber(1);
        
        setGenreAppliedFilter(self.genrePossibleFilters(), false, true); //suppress valueHasMutated since we're going to change the filter again below
        setGenreAppliedFilter(self.genreSelectedFilters(), true);
        
        self.metacriticFromApplied(self.metacriticFrom());
        self.metacriticToApplied(self.metacriticTo());
        self.releaseYearFromApplied(self.releaseYearFrom());
        self.releaseYearToApplied(self.releaseYearTo());
        
        appInsights.trackEvent("AdvancedFilterApplied", {}, {
            MetacriticFrom: self.metacriticFrom(),
            MetacriticTo: self.metacriticTo(),
            ReleaseYearFrom: self.releaseYearFrom(),
            ReleaseYearTo: self.releaseYearTo(),
            GenresIncluded: self.genreSelectedFilters().length,
            GenresPossible: self.genrePossibleFilters().length
        });

        $("#advanced-filter-modal").modal("hide");
    };

    self.clearAdvancedFilter = function() {
        self.gameTable.currentPageNumber(1);

        resetAdvancedFilters();

        appInsights.trackEvent("ClearAdvancedFilter");

        $("#advanced-filter-modal").modal("hide");
    };

    self.userGender = ko.observable("male");

    self.userBirthYear = ko.observable(latestYear - 30);
    self.birthYearPossibleValues = ko.observableArray();
    for (var birthYear = latestYear - 120; birthYear <= latestYear; birthYear++) {
        self.birthYearPossibleValues.push(birthYear);
    }

    self.weeklyPlayHours = ko.observable(10);
    self.weeklyPlayHoursPossibleValues = ko.observableArray();
    for (var weeklyPlaytime = 1; weeklyPlaytime <= 150; weeklyPlaytime++) {
        self.weeklyPlayHoursPossibleValues.push(weeklyPlaytime);
    }

    self.userPlayStyle = ko.observable(PlaytimeType.Main);
    self.playStylePossibleValues = ko.observableArray([PlaytimeType.Main, PlaytimeType.Extras, PlaytimeType.Completionist]);

    var survivalPositiveVerdicts = [
        { prefix: "You will die happy, having completed your backlog with", suffix: "left to live." },
        { prefix: "Not only will you complete your backlog, you'll have", suffix: "to relax after." },
        { prefix: "Now is the time to go buy more games for your barren", suffix: "of post-backlog." }
    ];
    var survivalNegativeVerdicts = [
        { prefix: "You will die a horrible death,", suffix: "before beating your backlog." },
        { prefix: "You'd need", suffix: "more to live in order to complete your backlog." },
        { prefix: "Looks like the Grim Reaper is going to beat you by", suffix: ":(" }
    ];

    self.survivalCalculated = ko.observable(false);
    self.survivalCalculationError = ko.observable(false);
    self.calculatingSurvival = ko.observable(false);
    self.remainingWeeksToLiveAfterBacklogCompletion = ko.observable(0);
    self.timeOfDeath = ko.observable(new Date());
    self.timeOfBacklogCompletion = ko.observable(new Date());
    self.survivalPrefix = ko.observable("");
    self.survivalSuffix = ko.observable("");
    self.survivalYears = ko.observable("");

    self.survivalCalculatorClicked = function() {
        appInsights.trackEvent("survivalCalculatorClicked");

        self.survivalCalculated(false);
        self.survivalCalculationError(false);
        $("#steam-survival-modal").modal("show");
    };

    self.calculateSurvival = function () {
        appInsights.trackEvent("SurvivalCalculated", { gender: self.userGender() }, { birthYear: self.userBirthYear() });

        self.calculatingSurvival(true);
        $.get("api/survival/life-expectancy/remaining/{0}/{1}/{2}"
            .format("World", self.userGender(), new Date().getFullYear() - self.userBirthYear()))
        .done(function (data) {
            self.survivalCalculationError(false);
            self.timeOfDeath(getTimeFromNow(data.RemainingHours));

            var playstyleRemainingMinutes = (self.userPlayStyle() === PlaytimeType.Completionist) ? self.total().completionistRemaining :
                ((self.userPlayStyle() === PlaytimeType.Extras) ? self.total().extrasRemaining : self.total().mainRemaining);

            var weeksLeftForBacklogCompletion = (playstyleRemainingMinutes / 60) / self.weeklyPlayHours();
            self.timeOfBacklogCompletion(getTimeFromNow(weeksLeftForBacklogCompletion * 7 * 24));

            var verdict, years;
            if (self.timeOfDeath() < self.timeOfBacklogCompletion()) {
                verdict = survivalNegativeVerdicts[Math.floor(Math.random() * survivalNegativeVerdicts.length)];
                years = getYearsFromDateDiff(self.timeOfDeath(), self.timeOfBacklogCompletion());
            } else {
                verdict = survivalPositiveVerdicts[Math.floor(Math.random() * survivalPositiveVerdicts.length)];
                years = getYearsFromDateDiff(self.timeOfBacklogCompletion(), self.timeOfDeath());
            }

            self.survivalPrefix(verdict.prefix);
            self.survivalSuffix(verdict.suffix);
            self.survivalYears(years);

        }).fail(function (error) {
            appInsights.trackException(error);
            self.survivalCalculationError(true);
        }).always(function () {
            self.calculatingSurvival(false);
            self.survivalCalculated(true);
        });
    };

    self.toggleAllChecked = ko.observable(true);

    self.toggleAllCore = function (include) {
        var games = self.gameTable.rows();
        for (var i = 0; i < games.length; i++) {
            games[i].included(include);
        }
    };

    var togglingAll = false;
    self.toggleAll = function () {
        appInsights.trackEvent("ToggleAll");
        togglingAll = true;
        self.toggleAllCore(!self.toggleAllChecked()); //binding is one way (workaround KO issue) so toggleAllChecked still has its old value
        togglingAll = false;
        return true;
    };

    var gotoPage = function(eventName, page) {
        self.gameTable.gotoPage(page)();
        appInsights.trackEvent(eventName, {}, { page: self.gameTable.currentPageNumber(), totalPages: self.gameTable.pages().length });
    };

    self.gotoPage = function (page) {
        if (self.gameTable.currentPageNumber() !== page.number) {
            gotoPage("NavigatedToPage", page.number);
        }
    };

    self.gotoFirstPage = function () {
        if (self.gameTable.currentPageNumber() > 1) {
            gotoPage("NavigatedToFirstPage", 1);
        }
    };

    self.gotoLastPage = function () {
        if (self.gameTable.currentPageNumber() < self.gameTable.pages().length) {
            gotoPage("NavigatedToLastPage", self.gameTable.pages().length);
        }
    };

    self.gotoPrevPage = function() {
        if (self.gameTable.currentPageNumber() > 1) {
            gotoPage("NavigatedToPreviousPage", self.gameTable.currentPageNumber() - 1);
        }
    };

    self.gotoNextPage = function() {
        if (self.gameTable.currentPageNumber() < self.gameTable.pages().length) {
            gotoPage("NavigatedToNextPage", self.gameTable.currentPageNumber() + 1);
        }
    };

    var toggleSort = function(field) {
        self.gameTable.toggleSort(field)();
        appInsights.trackEvent("Sort", { field: field, direction: self.gameTable.sortDir() });
    };

    self.toggleNameSort = function() {
        toggleSort("steamName");
    };

    self.togglePlaytimeSort = function() {
        toggleSort("steamPlaytime");
    };

    self.toggleMainTtbSort = function () {
        toggleSort("hltbMainTtb");
    };

    self.toggleExtrasTtbSort = function() {
        toggleSort("hltbExtrasTtb");
    };

    self.toggleCompletionistTtbSort = function() {
        toggleSort("hltbCompletionistTtb");
    };

    self.toggleHltbNameSort = function() {
        toggleSort("hltbName");
    };

    var sliceByTotal = function (total) {
        self.sliceTotal(total);
        appInsights.trackEvent("Slice", { by: total ? "Total" : "Remaining" });
    };

    self.sliceTotalClicked = function() {
        sliceByTotal(true);
    };

    self.sliceRemainingClicked = function() {
        sliceByTotal(false);
    };

    var sliceByCompletionLevel = function(playtimeType) {
        self.sliceCompletionLevel(playtimeType);
        appInsights.trackEvent("Slice", { by: playtimeType });
    };

    self.sliceCurrentClicked = function () {
        sliceByCompletionLevel(PlaytimeType.Current);
    };

    self.sliceMainClicked = function() {
        sliceByCompletionLevel(PlaytimeType.Main);
    };

    self.sliceExtrasClicked = function() {
        sliceByCompletionLevel(PlaytimeType.Extras);
    };

    self.sliceCompletionistClicked = function() {
        sliceByCompletionLevel(PlaytimeType.Completionist);
    };

    var getSlicedPlaytime = function (sliceCompletionLevel, sliceTotal, game, gameMainRemaining, gameExtrasRemaining, gameCompletionistRemaining) {
        switch (sliceCompletionLevel) {
            case PlaytimeType.Current:
                return sliceTotal ? game.steamPlaytime : gameMainRemaining;
            case PlaytimeType.Main:
                return sliceTotal ? game.hltbMainTtb : gameMainRemaining;
            case PlaytimeType.Extras:
                return sliceTotal ? game.hltbExtrasTtb : gameExtrasRemaining;
            case PlaytimeType.Completionist:
                return sliceTotal ? game.hltbCompletionistTtb : gameCompletionistRemaining;
            default:
                appInsights.trackException("Invalid playtime slice type: " + sliceCompletionLevel);
                return 0;
        }
    };

    var updateSlicedPlaytime = function (playtimes, key, playtime) {
        if (!playtimes.hasOwnProperty(key)) {
            playtimes[key] = playtime;
        } else {
            playtimes[key] += playtime;
        }
    };

    self.total = ko.pureComputed(function () {

        //visit dependencies first so that knockout subscribes to them regardless of initialTotal
        var filteredRows = self.gameTable.filteredRows();
        var sliceCompletionLevel = self.sliceCompletionLevel();
        var sliceTotal = self.sliceTotal();

        if (self.initialTotal !== undefined) {

            //subscribe to initial included observables
            includedObservable();
            var perPage = Math.min(filteredRows.length, self.gameTable.perPage());
            for (var j = 0; j < perPage; j++) {
                filteredRows[j].included();
            }

            self.toggleAllChecked(true);
            var ret = self.initialTotal;
            self.initialTotal = undefined;
            return ret;
        }

        var count = 0;
        var playtime = 0;
        var mainTtb = 0;
        var extrasTtb = 0;
        var completionistTtb = 0;
        var mainRemaining = 0;
        var extrasRemaining = 0;
        var completionistRemaining = 0;
        var mainCompleted = 0;
        var extrasCompleted = 0;
        var completionistCompleted = 0;
        var playtimesByGenre = {};
        var playtimesByMetacritic = {};
        var playtimesByAppType = {};
        var playtimesByReleaseYear = {};

        for (var i = 0; i < filteredRows.length; ++i) {
            var game = filteredRows[i];
            if (!game.included()) {
                continue;
            }

            count++;
            playtime += game.steamPlaytime;
            mainTtb += game.hltbMainTtb;
            extrasTtb += game.hltbExtrasTtb;
            completionistTtb += game.hltbCompletionistTtb;
            mainCompleted += Math.min(game.steamPlaytime, game.hltbMainTtb);
            extrasCompleted += Math.min(game.steamPlaytime, game.hltbExtrasTtb);
            completionistCompleted += Math.min(game.steamPlaytime, game.hltbCompletionistTtb);

            var gameMainRemaining = Math.max(0, game.hltbMainTtb - game.steamPlaytime);
            var gameExtrasRemaining = Math.max(0, game.hltbExtrasTtb - game.steamPlaytime);
            var gameCompletionistRemaining = Math.max(0, game.hltbCompletionistTtb - game.steamPlaytime);

            mainRemaining += gameMainRemaining;
            extrasRemaining += gameExtrasRemaining;
            completionistRemaining += gameCompletionistRemaining;

            var slicedPlaytime = getSlicedPlaytime(sliceCompletionLevel, sliceTotal, game, gameMainRemaining, gameExtrasRemaining, gameCompletionistRemaining);
            updateSlicedPlaytime(playtimesByGenre, game.getCombinedGenre(), slicedPlaytime);
            updateSlicedPlaytime(playtimesByMetacritic, game.metacriticScore, slicedPlaytime);
            updateSlicedPlaytime(playtimesByAppType, game.appType, slicedPlaytime);
            updateSlicedPlaytime(playtimesByReleaseYear, game.releaseYear, slicedPlaytime);
        }

        if (count === filteredRows.length) {
            self.toggleAllChecked(true);
        } else if (count === 0) {
            self.toggleAllChecked(false);
        }

        return {
            count: count,
            playtime: playtime,
            mainTtb: mainTtb,
            extrasTtb: extrasTtb,
            completionistTtb: completionistTtb,
            mainRemaining: mainRemaining,
            extrasRemaining: extrasRemaining,
            completionistRemaining: completionistRemaining,
            mainCompleted: mainCompleted,
            extrasCompleted: extrasCompleted,
            completionistCompleted: completionistCompleted,
            playtimesByGenre: playtimesByGenre,
            playtimesByMetacritic: playtimesByMetacritic,
            playtimesByAppType: playtimesByAppType,
            playtimesByReleaseYear: playtimesByReleaseYear
        };
    }).extend({ rateLimit: 0 });

    var trackGameIncluded = function (included) {
        if (!togglingAll) {
            appInsights.trackEvent("GameIncludeToggled", { included: included });
        }
    };

    self.pagedGames = ko.pureComputed(function () {
        var games = self.gameTable.pagedRows();
        var replaced = false;
        for (var i = 0; i < games.length; i++) {
            var game = games[i];
            if (game.included === includedObservable) {
                game.included = ko.observable(includedObservable.peek());
                game.included.subscribe(trackGameIncluded);
                game.updatePhase = ko.observable(updatePhaseObservable());
                game.suggestedHltbId = ko.observable("");
                replaced = true;
            }
        }
        if (replaced) {
            includedObservable.valueHasMutated(); //will trigger re-evaluation of total
        }
        return games;
    });

    var setSteamIdFromUrl = function(prefix) {
        //handle profile URL instead of ID
        var parts = self.steamVanityUrlName().split(prefix);
        if (parts.length > 1) {
            //steamVanityUrlName is something like "[http(s)://](prefix)ID[/home]"
            var suffix = parts[1];
            //suffix is something like "(ID)[/home]"
            self.steamVanityUrlName(suffix.split("/")[0]);
        }
    };

    self.vanityUrlSubmitted = function () {

        setSteamIdFromUrl("steamcommunity.com/id/");
        setSteamIdFromUrl("steamcommunity.com/profiles/");

        if (window.location.hash === "#/" + self.steamVanityUrlName()) {
            //no submission will take place since it's the same URL, so we need to trigger Sammy manually
            self.sammyApp.runRoute("get", window.location.hash);
        }
    };

    var updateMainCharts = function (total) {
        self.playtimeChart.dataProvider[0].current = getHours(total.playtime);

        self.playtimeChart.dataProvider[1].completed = getHours(total.mainCompleted);
        self.playtimeChart.dataProvider[1].remaining = getHours(total.mainRemaining);

        self.playtimeChart.dataProvider[2].completed = getHours(total.extrasCompleted);
        self.playtimeChart.dataProvider[2].remaining = getHours(total.extrasRemaining);

        self.playtimeChart.dataProvider[3].completed = getHours(total.completionistCompleted);
        self.playtimeChart.dataProvider[3].remaining = getHours(total.completionistRemaining);

        self.playtimeChart.invalidateSize();
        self.playtimeChart.validateData();
    };

    var getPlaytimeTotalHours = function () {
        switch (self.sliceCompletionLevel()) {
            case PlaytimeType.Current:
                return getHours(self.sliceTotal() ? self.total().playtime : self.total().mainRemaining);
            case PlaytimeType.Main:
                return getHours(self.sliceTotal() ? self.total().mainTtb : self.total().mainRemaining);
            case PlaytimeType.Extras:
                return getHours(self.sliceTotal() ? self.total().extrasTtb : self.total().extrasRemaining);
            case PlaytimeType.Completionist:
                return getHours(self.sliceTotal() ? self.total().completionistTtb : self.total().completionistRemaining);
            default:
                appInsights.trackException("Invalid playtime slice type: " + self.sliceCompletionLevel());
                return 0;
        }
    };

    var updateDiscreteSliceChart = function (chart, slicedPlaytime) {
        var groupingThreshold = 0.01;
        var titles = getOrderedOwnProperties(slicedPlaytime);
        var playtimeTotal = getPlaytimeTotalHours();
        chart.dataProvider = [];
        var others = [];
        var othersTotal = 0;
        for (var i = 0; i < titles.length; i++) {
            var title = titles[i];
            var hours = getHours(slicedPlaytime[title]);
            if (title === unknownTitle || (hours / playtimeTotal) < groupingThreshold) {
                others.push(title);
                othersTotal += hours;
                continue;
            }
            chart.dataProvider.push({
                title: title,
                hours: hours
            });
        }
        if (others.length >= 1) {
            chart.dataProvider.push({
                title: (others.length === 1 && others[0] !== unknownTitle) ? others[0] : "Other",
                hours: othersTotal,
                color: unknownColor
            });
        }
        chart.invalidateSize();
        chart.validateData();
    };

    var getCategoryRangePredicate = function(sliceKey) { //avoids creating functions in a loop
        return function(category) {
            return (sliceKey <= category.max) && (sliceKey >= category.min);
        };
    };

    var getShardTitle = function (min, max) {
        return min === max ? min : min + "-" + max;
    };

    var populateCategories = function (slicedPlaytime, categories, groupHandler) {

        for (var j = 0; j < categories.length; j++) {
            categories[j].observedMin = Number.POSITIVE_INFINITY;
            categories[j].observedMax = Number.NEGATIVE_INFINITY;
        }

        var sliceGroups = getOrderedOwnProperties(slicedPlaytime);
        for (var i = 0; i < sliceGroups.length; i++) {
            var sliceGroupKey = sliceGroups[i];
            var sliceGroupMinutes = slicedPlaytime[sliceGroupKey];

            var sliceGroupKeyTyped = Number(sliceGroupKey);
            var matchingCategory = ko.utils.arrayFirst(categories, getCategoryRangePredicate(sliceGroupKeyTyped));

            matchingCategory.hours += getHours(sliceGroupMinutes);
            matchingCategory.observedMin = Math.min(matchingCategory.observedMin, sliceGroupKeyTyped);
            matchingCategory.observedMax = Math.max(matchingCategory.observedMax, sliceGroupKeyTyped);

            if (typeof groupHandler !== "undefined") {
                groupHandler(matchingCategory, sliceGroupKey, sliceGroupMinutes);
            }
        }

        for (var k = 0; k < categories.length; k++) {
            if (categories[k].observedMin !== Number.POSITIVE_INFINITY) {
                categories[k].min = categories[k].observedMin;
                categories[k].max = categories[k].observedMax;

                if (categories[k].index === -1) { //it's a shard, so update its title
                    categories[k].title = getShardTitle(categories[k].min, categories[k].max);
                }
            }
        }
    };

    var mergeShards = function (firstShard, lastShard, hours) {
        if (firstShard === lastShard) {
            return firstShard;
        }

        return {
            title: getShardTitle(firstShard.min, lastShard.max),
            min: firstShard.min,
            max: lastShard.max,
            hours: hours,
            color: lastShard.color,
            pulled: true,
            index: -1 //we treat all shard clicks the same
        };
    };

    var unifySmallShards = function (shards, categoryHours) {
        var mergedShards = [];
        var minPercentage = 0.5 * (1 / shards.length);
        var firstShardToMerge = 0;
        var mergeHours = 0;
        for (var j = 0; j < shards.length; j++) {

            if ((shards[j].hours / categoryHours) < minPercentage) {
                mergeHours += shards[j].hours;
                continue;
            }

            if (j > firstShardToMerge) {
                mergedShards.push(mergeShards(shards[firstShardToMerge], shards[j - 1], mergeHours));
            }
            mergedShards.push(shards[j]);
            firstShardToMerge = j + 1;
            mergeHours = 0;
        }

        if (firstShardToMerge < shards.length) {
            mergedShards.push(mergeShards(shards[firstShardToMerge], shards[shards.length - 1], mergeHours));
        }

        return mergedShards;
    };

    var breakdown = function (slicedPlaytime, category, shardCount) {
        var shards = [];
        var breakdownInterval = Math.ceil((category.max - category.min + 1) / shardCount);
        for (var i = category.min; i <= category.max; i = i + breakdownInterval) {
            var boundMax = Math.min(category.max, i + breakdownInterval - 1);
            shards.push({
                title: getShardTitle(i, boundMax),
                min: i,
                max: boundMax,
                hours: 0,
                color: category.color,
                pulled: true,
                index: -1 //we treat all shard clicks the same
            });
        }
        var lastShard = shards[shards.length - 1];
        if (shards.length > 1 && (lastShard.max - lastShard.min) < breakdownInterval / 2) {
            var secondToLastShard = shards[shards.length - 2];
            shards.splice(shards.length - 2, 2, mergeShards(secondToLastShard, lastShard, 0));
        }

        populateCategories(slicedPlaytime, shards);
        return unifySmallShards(shards, category.hours);
    };

    var enrichCategoriesForChart = function (categories, pullUnknown) {
        for (var j = 0; j < categories.length; j++) {
            categories[j].hours = 0;
            categories[j].color = progressivePalette[j % progressivePalette.length]; //modulo just in case, progressivePalette should be big enough
            categories[j].pulled = false;
            categories[j].description = " (" + categories[j].min + "-" + categories[j].max + ")";
            categories[j].index = j;
        }

        categories.push({
            title: unknownTitle,
            min: Number.NEGATIVE_INFINITY,
            max: Number.POSITIVE_INFINITY,
            hours: 0,
            color: unknownColor,
            pulled: pullUnknown,
            index: categories.length
        });
    };

    var updateContinuousSliceChart = function (chart, slicedPlaytime, categories, clickedSlice) {
        var sliceClicked = typeof clickedSlice !== "undefined";

        var clickedCategory = null;
        if (sliceClicked) {
            if (self.extraSmall) { //don't break down on extra small devices
                return;
            }
            // ReSharper disable once QualifiedExpressionMaybeNull
            clickedCategory = clickedSlice.dataContext;
        }

        // ReSharper disable QualifiedExpressionMaybeNull
        enrichCategoriesForChart(categories, sliceClicked && clickedCategory.title === unknownTitle && clickedSlice.pulled);
        // ReSharper restore QualifiedExpressionMaybeNull

        var slicedPlaytimeToBreakDown = {};

        // ReSharper disable once QualifiedExpressionMaybeNull
        var categoryClicked = sliceClicked && clickedCategory.index !== -1 && clickedCategory.title !== unknownTitle;
        populateCategories(slicedPlaytime, categories, !categoryClicked ? undefined : function (matchingCategory, sliceGroupKey, sliceGroupMinutes) {
            if (matchingCategory.index === clickedCategory.index) {
                slicedPlaytimeToBreakDown[sliceGroupKey] = sliceGroupMinutes;
            }
        });

        if (categoryClicked) {
            // ReSharper disable once QualifiedExpressionMaybeNull
            var shardCount = Math.round((clickedCategory.hours / getPlaytimeTotalHours()) * 10) + 1;
            categories.splice.apply(categories, [clickedCategory.index, 1].concat(breakdown(slicedPlaytimeToBreakDown, clickedCategory, shardCount)));
        }

        chart.dataProvider = categories;
        chart.invalidateSize();
        chart.validateData();
    };

    var updateMetacriticChart = function(total, clickedSlice) {
        updateContinuousSliceChart(self.metacriticChart, total.playtimesByMetacritic, [
            { title: "Overwhelming Dislike", min: 0, max: 19 },
            { title: "Generally Unfavorable", min: 20, max: 49 },
            { title: "Mixed or Average", min: 50, max: 74 },
            { title: "Generally Favorable", min: 75, max: 89 },
            { title: "Universal Acclaim", min: 90, max: 100 }
        ], clickedSlice);
    };

    var updateGenreChart = function(total) {
        updateDiscreteSliceChart(self.genreChart, total.playtimesByGenre);
    };

    var updateAppTypeChart = function (total) {
        updateDiscreteSliceChart(self.appTypeChart, total.playtimesByAppType);
    };

    var updateReleaseDateChart = function (total, clickedSlice) {
        var decades = [];
        for (var firstDecadeYear = earliestYear; firstDecadeYear <= latestYear; firstDecadeYear += 10) {
            decades.push({ title: firstDecadeYear.toString().substring(2) + "s", min: firstDecadeYear, max: firstDecadeYear + 9 });
        }
        updateContinuousSliceChart(self.releaseDateChart, total.playtimesByReleaseYear, decades, clickedSlice);
    };

    var updateSliceCharts = function (total) {
        //we need to use jQuery as knockout is not immediate and thus trips chart rendering
        var sliceCharts = $(".slice-chart");
        var noDataIndicators = $(".no-data-indicator");

        if (getPlaytimeTotalHours() === 0) {
            sliceCharts.hide();
            noDataIndicators.show();
            return;
        }

        sliceCharts.show();
        noDataIndicators.hide();

        updateGenreChart(total);
        updateMetacriticChart(total);
        updateAppTypeChart(total);
        updateReleaseDateChart(total);
    };

    var updateCharts = function (total) {
        updateMainCharts(total);
        updateSliceCharts(total);
    };

    var initChart = function (chartId, factor) {
        if (typeof factor === "undefined") {
            factor = 1;
        }
        var chart = $("#" + chartId);
        var chartHeight = chart.width() * factor * (self.superSmall ? 1.3 : 2 / 3);
        chart.height(chartHeight);

        var noDataIndicator = $("#" + chartId + "NoData");
        noDataIndicator.height(chartHeight); //prevent jumps
        noDataIndicator.css("line-height", chartHeight + "px"); //needed for vertical centering
    };

    var initSerialChart = function(chartId, dataProvider) {
        initChart(chartId, Math.max(Math.min(768 / self.width, 0.9), 0.6));
        var chart = AmCharts.makeChart(chartId, {
            panEventsEnabled: false,
            type: "serial",
            theme: "light",
            pathToImages: "https://www.amcharts.com/lib/3/images/",
            rotate: !self.extraSmall,
            colors: alternatingPalette,
            dataProvider: dataProvider,
            categoryField: "playtimeType",
            valueAxes: [
                {
                    axisAlpha: 0,
                    stackType: "regular",
                    position: "left"
                }
            ],
            categoryAxis: {
                gridPosition: "start",
                labelRotation: self.superSmall ? 45 : 0
            },
            graphs: [
                {
                    type: "column",
                    valueField: "completed",
                    lineThickness: 1.5,
                    balloonText: "<b>[[value]] hours completed</b> ([[percents]]%)",
                    fillAlphas: 0.5,
                    lineAlpha: 0.8
                },
                {
                    type: "column",
                    valueField: "remaining",
                    lineThickness: 1.5,
                    balloonText: "<b>[[value]] hours remaining</b> ([[percents]]%)",
                    fillAlphas: 0.5,
                    lineAlpha: 0.8
                },
                {
                    type: "column",
                    valueField: "current",
                    lineThickness: 1.5,
                    balloonText: "<b>[[value]] total hours played</b>",
                    fillAlphas: 0.5,
                    lineAlpha: 0.8
                }
            ],
            precision: 0,
            startDuration: (typeof window.orientation === "undefined") ? 1.5 : 0,
            responsive: {
                enabled: true,
                rules: [{
                    minWidth: 1,
                    overrides: {
                        categoryAxis: {
                            labelsEnabled: true,
                            ignoreAxisWidth: false,
                            inside: false,
                            showFirstLabel: true,
                            showLastLabel: true
                        }
                    }
                }]
            }
        });
        chart.balloon.maxWidth *= 0.9;
        return chart;
    };

    var initPieChart = function(chartId, marginTop, updater) {
        initChart(chartId);
        var chart = AmCharts.makeChart(chartId, {
            panEventsEnabled: false,
            type: "pie",
            theme: "light",
            pathToImages: "https://www.amcharts.com/lib/3/images/",
            colors: alternatingPalette,
            marginLeft: 0,
            marginRight: 0,
            marginBottom: 15,
            marginTop: marginTop,
            pullOutRadius: "5%",
            pullOutDuration: 0,
            pullOutOnlyOne: (typeof updater === "undefined"), //for non-breakdown charts allow only one for consistent experience
            valueField: "hours",
            colorField: "color",
            descriptionField: "description",
            pulledField: "pulled",
            precision: 0,
            titleField: "title",
            labelRadius: 10,
            labelText: "[[title]]",
            balloonText: "[[title]][[description]]: [[percents]]% ([[value]] hours)",
            balloon: {
                maxWidth: $("#" + chartId).width() * 2 / 3
            },
            startDuration: 0,
            responsive: {
                enabled: true
            }
        });

        chart.addListener("clickSlice", function () { appInsights.trackEvent("SliceClicked", { chart: chartId }); });

        if (typeof updater !== "undefined") {
            chart.addListener("clickSlice", function (event) { updater(self.total(), event.dataItem); });
        }

        return chart;
    };

    var animate = false;
    var chratsInitialized = false;
    var initCharts = function() {

        if (chratsInitialized) {
            return;
        }
        chratsInitialized = true;

        self.playtimeChart = initSerialChart("playtimeChart",
        [
            { playtimeType: "Current", hours: 0 },
            { playtimeType: "Main", hours: 0 },
            { playtimeType: "Extras", hours: 0 },
            { playtimeType: "Complete", hours: 0 }
        ]);

        self.playtimeChart.addListener("dataUpdated", function () {
            if (animate && self.total().count > 0) {
                self.playtimeChart.animateAgain();
                animate = false;
            }
        });

        self.genreChart = initPieChart("genreChart", 25);
        self.metacriticChart = initPieChart("metacriticChart", 25, updateMetacriticChart);
        self.appTypeChart = initPieChart("appTypeChart", 40);
        self.releaseDateChart = initPieChart("releaseDateChart", 40, updateReleaseDateChart);

        self.total.subscribe(updateCharts);
        self.sliceTotal.subscribe(function(slicetotal) {
            if (!slicetotal && self.sliceCompletionLevel() === PlaytimeType.Current) {
                self.sliceCompletionLevel(PlaytimeType.Main); //will trigger slice chart update per above
            }
        });
    };

    var chartsSizeInvalidated = false;
    var invalidateChartSize = function () {
        if (chartsSizeInvalidated) {
            return;
        }

        setTimeout(function () {
            self.genreChart.invalidateSize();
            self.metacriticChart.invalidateSize();
            self.releaseDateChart.invalidateSize();
            self.appTypeChart.invalidateSize();
        }, 0);

        chartsSizeInvalidated = true;
    };

    var scrollEvents = "scroll mousedown DOMMouseScroll mousewheel keyup touchstart";
    var scrollToAlerts = function () {
        if (window.pageYOffset > 0) {
            return; //don't override user position
        }

        var $viewport = $("html, body");

        //scroll viewport
        $viewport.animate({
            scrollTop: $("#alerts").offset().top - 10
        }, 1500);

        //stop scrolling on user interruption
        $viewport.bind(scrollEvents, function (e) {
            if (e.which > 0 || e.type === "mousedown" || e.type === "mousewheel" || e.type === "touchstart") {
                $viewport.stop().unbind(scrollEvents); // Identify the scroll as a user action, stop the animation, and unbind the event
            }
        });
    };

    var renderedRows = 0;
    var afterRequest = false;
    var firstTableRender = true;

    self.rowRendered = function (elements) {
        
        if (self.extraSmall) {
            var row = ko.utils.arrayFirst(elements, function (elem) { return elem.tagName === "TR"; });
            $(row).find("span[data-toggle='tooltip']:first").tooltip();
        }

        if (!afterRequest) {
            return;
        }

        renderedRows++;
        if (renderedRows < Math.min(self.gameTable.perPage(), self.gameTable.filteredRows().length)) {
            return;
        }

        afterRequest = false;

        stopProcessing();
        invalidateChartSize(); //needed for initial responsive rules application
        scrollToAlerts();

        if (firstTableRender) {
            $.each($("table th.compressed"), function () {
                $(this).css("width", $(this).width() + 10 + "px");
            });

            $("#game-table").css("table-layout", "fixed");

            firstTableRender = false;
        }
    };

    var getGamesArray = function(gameData) {
        var games = new Array(gameData.length);
        var containsUnknown = false;
        var containsImputed = false;
        for (var i = 0; i < gameData.length; i++) {
            var game = new Game(gameData[i]);
            if (!containsUnknown && !game.known) {
                self.missingHltbIds(true);
                self.imputedTtbs(true);
                containsUnknown = true;
            } else if (!containsImputed && (game.hltbMainTtbImputed || game.hltbExtrasTtbImputed || game.hltbCompletionistTtbImputed)) {
                self.imputedTtbs(true);
                containsImputed = true;
            }
            games[i] = game;
        }
        return games;
    };

    self.loadGames = function() {

        if (self.currentRequest !== undefined) {
            self.currentRequest.abort(); //in case of hash tag navigation while we're loading
        }

        self.processing(true);
        self.status("Retrieving Data...");

        $("html, body").animate({ scrollTop: 0 }, 500);

        self.error(false);
        self.partialCache(false);
        self.excludedGameCount(0);
        self.imputedTtbs(false);
        self.missingHltbIds(false);
        self.libraryIsEmpty(false);
        self.sliceTotal(true);
        self.sliceCompletionLevel(PlaytimeType.Main);
        self.gameTable.filter("");
        resetAdvancedFilters();
        possibleFiltersCalculated = false;
        self.gameTable.toggleSort("");
        self.cached(self.steamVanityUrlName().indexOf("cached/") !== -1);
        self.missing(self.steamVanityUrlName().indexOf("missing/") !== -1);
        self.realUser(!self.cached() && !self.missing());

        self.currentRequest = $.get("api/games/library/" + self.steamVanityUrlName())
            .done(function(data) {
                afterRequest = true;
                includedObservable(true);
                self.partialCache(data.PartialCache);
                self.excludedGameCount(data.ExcludedCount);
                self.personaName(data.PersonaInfo.PersonaName);
                self.avatarUrl(data.PersonaInfo.Avatar);
                self.initialTotal = {
                    count: data.Games.length,
                    playtime: data.Totals.Playtime,
                    mainTtb: data.Totals.MainTtb,
                    extrasTtb: data.Totals.ExtrasTtb,
                    completionistTtb: data.Totals.CompletionistTtb,
                    mainRemaining: data.Totals.MainRemaining,
                    extrasRemaining: data.Totals.ExtrasRemaining,
                    completionistRemaining: data.Totals.CompletionistRemaining,
                    mainCompleted: data.Totals.MainCompleted,
                    extrasCompleted: data.Totals.ExtrasCompleted,
                    completionistCompleted: data.Totals.CompletionistCompleted,
                    playtimesByGenre: data.Totals.PlaytimesByGenre,
                    playtimesByMetacritic: data.Totals.PlaytimesByMetacritic,
                    playtimesByAppType: data.Totals.PlaytimesByAppType,
                    playtimesByReleaseYear: data.Totals.PlaytimesByReleaseYear
                };
                self.gameTable.rows(getGamesArray(data.Games));

                self.originalMainRemaining = data.Totals.MainRemaining;

                if (self.gameTable.rows().length > 0) {
                    self.gameTable.currentPageNumber(1);
                    renderedRows = 0;
                } else {
                    self.libraryIsEmpty(true);
                    self.introPage(true);
                    stopProcessing();
                    return;
                }

                if (!firstTableRender) {
                    stopProcessing();
                } else {
                    self.status("Processing Data...");
                }
            })
            .fail(function(error) {
                appInsights.trackException(error);
                self.gameTable.rows([]);
                self.error(true);
                self.introPage(true);
                stopProcessing();
            })
            .always(function() {
                self.alertHidden(false);
                self.missingAlertHidden(false);
                self.errorAlertHidden(false);
            });

        initCharts(); //init charts while waiting for response
        animate = true;
    };

    self.displayNonGameDialog = function(game) {
        appInsights.trackEvent("UpdateNonGameClicked");
        self.gameToUpdate(game);
        $("#non-game-update-modal").modal("show");
    };

    self.displayUpdateDialog = function (game) {
        appInsights.trackEvent("UpdateClicked", {known: game.known});
        self.gameToUpdate(game);
        $("#hltb-update-modal").modal("show");
    };

    var displayExternalPage = function(url, title) {
        self.externalPageUrl(url);
        self.externalPageTitle(title);
        var height = $(window).height();
        var heightFactor = (height <= 320) ? 0.6 : ((height <= 480) ? 0.7 : 0.8);
        $("#external-modal-body").height(heightFactor * height);
        $("#external-modal").modal("show");
    };

    self.displayPrivacyPolicy = function() {
        appInsights.trackEvent("PrivacyPolicyClicked");
        displayExternalPage("Pages/Privacy.html", "Privacy Policy");
    };

    self.displayFaq = function() {
        appInsights.trackEvent("FaqClicked");
        displayExternalPage("Pages/FAQ.html", "Frequently Asked Questions");
    };

    var updateGameCore = function(gameToUpdate) {
        gameToUpdate.updatePhase(GameUpdatePhase.InProgress);
        $.post("api/games/update/" + gameToUpdate.steamAppId + "/" + gameToUpdate.suggestedHltbId())
            .done(function() {
                gameToUpdate.updatePhase(GameUpdatePhase.Success);
            })
            .fail(function(error) {
                appInsights.trackException(error);
                gameToUpdate.updatePhase(GameUpdatePhase.Failure);
            });
    };

    self.updateNonGame = function() {
        self.gameToUpdate().suggestedHltbId(-1);
        self.gameToUpdate().included(false);

        updateGameCore(self.gameToUpdate());
        $("#non-game-update-modal").modal("hide");
        appInsights.trackEvent("NonGameUpdateSubmitted");
    };

    self.updateHltb = function() {
        updateGameCore(self.gameToUpdate());
        $("#hltb-update-modal").modal("hide");
        appInsights.trackEvent("UpdateSubmitted", {known: self.gameToUpdate().known });
    };

    var getHostname = function() {
        //check against localhost for easy local debugging
        return (window.location.hostname === "localhost") ? "www.howlongtobeatsteam.com" : window.location.hostname;
    };

    var getOrigin = function() {
        return window.location.protocol + "//" + getHostname();
    };

    var getCurrentAddress = function () {
        return getOrigin() + window.location.pathname + window.location.hash;
    };

    var getShortShareText = function (hours) {
        return "I just found out I have over " + (hours ? hoursWithCommas(self.originalMainRemaining) : (getYearsFromMinutes(self.originalMainRemaining) + " of consecutive gameplay")) + " left to beat my entire Steam library!";
    };

    var getSurvivalText = function() {
        return (self.timeOfDeath() < self.timeOfBacklogCompletion()) ?
            "Looks like I'm missing " + getYearsFromDateDiff(self.timeOfDeath(), self.timeOfBacklogCompletion()) + " from my life to be able to complete my Steam backlog :)" :
            "Looks like I'll be able to complete my Steam backlog in this lifetime with " + getYearsFromDateDiff(self.timeOfBacklogCompletion(), self.timeOfDeath()) + " to spare:)";
    };

    var openShareWindow = function (url, wide) {
        window.open(
            url,
            "share",
            "toolbar=no, location=no, status=no, menubar=no, scrollbars=yes, resizable=yes, height=600, width=" + (wide ? "900" : "600"));
    };

    var shareOnFacebookCore = function(shareEvent, text) {
        appInsights.trackEvent(shareEvent, { site: "Facebook" });
        openShareWindow("https://www.facebook.com/dialog/feed?" + $.param({
            app_id: "445487558932250",
            display: "popup",
            caption: "HowLongToBeatSteam",
            description: text + " facebook.com/howlongtobeatsteam",
            link: getCurrentAddress(),
            redirect_uri: getOrigin() + "/CloseWindow.html",
            picture: getOrigin() + "/Resources/sk5_0.jpg"
        }));
    };

    self.shareOnFacebook = function () {
        shareOnFacebookCore("Shared", getShortShareText(false));
    };

    self.shareSurvivalOnFacebook = function() {
        shareOnFacebookCore("SharedSurvival", getSurvivalText());
    };

    var shareOnTwitterCore = function(shareEvent, text) {
        appInsights.trackEvent(shareEvent, { site: "Twitter" });
        openShareWindow("https://twitter.com/share?" + $.param({ url: getCurrentAddress(), text: text, hashtags: "hltbsteam" }));
    };

    self.shareOnTwitter = function () {
        shareOnTwitterCore("Shared", getShortShareText(true));
    };

    self.shareSurvivalOnTwitter = function() {
        shareOnTwitterCore("SharedSurvival", getSurvivalText());
    };

    var shareOnRedditCore = function(shareEvent, text) {
        appInsights.trackEvent(shareEvent, { site: "Reddit" });
        openShareWindow("https://www.reddit.com/submit?" + $.param({ url: getCurrentAddress(), title: text, resubmit: true }), true);
    };

    self.shareOnReddit = function () {
        shareOnRedditCore("Shared", getShortShareText(false));
    };

    self.shareSurvivalOnReddit = function() {
        shareOnRedditCore("SharedSurvival", getSurvivalText());
    };

    self.authenticate = function() {
        appInsights.trackEvent("SteamLogin");
        window.location.href = "/Authentication";
    };
}

$(document).ready(function () {

    $("#copyright-year").text(new Date().getFullYear());

    //init palette
    for (var i = 0; i < 50; i++) {
        alternatingPalette.push("#75C7F0", "#97BBCD");
    }

    //enable tooltips
    $('span[data-toggle="tooltip"]').tooltip();

    //Init knockout
    var viewModel = new AppViewModel();
    ko.applyBindings(viewModel);

    //Init sammy
    var sammyApp = $.sammy(function () {

        var setTitle = function(suffix) {
            document.title = "HowLongToBeatSteam - " + suffix;
        };

        this.get("#/", function () {
            appInsights.trackEvent("NavigatedToHomepage");
            setTitle("How long would it take to complete your entire Steam game collection?");
            viewModel.steamVanityUrlName("");
            viewModel.introPage(true);
            setTimeout(function () {
                $("#steam-id-text").focus(); //workaround for FF
            }, 0);
        });

        var loadGames = function (vanityUrl) {
            viewModel.introPage(false);
            viewModel.steamVanityUrlName(vanityUrl);
            viewModel.loadGames();
        };

        this.get("#/:vanityUrlName", function () {
            viewModel.authenticated(AuthenticationStatus.None);
            appInsights.trackEvent("LoadGames", { authenticated: false });
            setTitle(this.params.vanityUrlName);
            loadGames(this.params.vanityUrlName);
        });

        this.get("#/auth/:steamid", function () {
            if (this.params.steamid === "failed") {
                viewModel.authenticated(AuthenticationStatus.Failure);
                viewModel.introPage(false);
                viewModel.error(true);
                return;
            }

            viewModel.authenticated(AuthenticationStatus.Success);
            appInsights.trackEvent("LoadGames", { authenticated: true });
            setTitle(this.params.steamid);
            loadGames(this.params.steamid);
        });

        this.get("#/cached/:count", function () {
            appInsights.trackEvent("LoadCachedGames", {}, { count: parseInt(this.params.count) });
            setTitle("All cached");
            loadGames("cached/" + this.params.count);
        });

        this.get("#/missing/:count", function () {
            appInsights.trackEvent("LoadMissingGames", {}, { count: parseInt(this.params.count) });
            setTitle("All missing");
            loadGames("missing/" + this.params.count);
        });
    });

    viewModel.sammyApp = sammyApp;
    sammyApp.run("#/");
});