/*global ko*/
/*global DataTable*/
/*global AmCharts*/
/*global countdown*/

"use strict"; // jshint ignore:line

var getOrderedOwnProperties = function(object) {
    var propArr = [];
    for (var prop in object) {
        if (object.hasOwnProperty(prop)) {
            propArr.push(prop);
        }
    }
    propArr.sort(); //we don't care that it's not alphabetical as long as it's consistent
    return propArr;
};

var duplicate = function(arr, times) {
    var ret = arr;
    for (var i = 0; i < times - 1; i++) {
        ret = ret.concat(arr);
    }
    return ret;
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

var getYears = function (minutes) { // jshint ignore:line
    /*jshint bitwise: false*/
    return countdown(null, { minutes: minutes }, countdown.YEARS | countdown.MONTHS | countdown.WEEKS | countdown.DAYS | countdown.HOURS).toString("0 hours");
    /*jshint bitwise: true*/
};

var GameUpdatePhase = {
    None: "None",
    InProgress: "InProgress",
    Success: "Success",
    Failure: "Failure"
};

var PlaytimeType = {
    Current: "current",
    Main: "main",
    Extras: "extras",
    Completionist: "completionist"
};

var alternatingPalette = duplicate(["#97BBCD", "#75C7F0"], 50); //2*50=100 colors (won't need more due to grouping)
var progressivePalette = ["#75c7f0", "#6CC3EF", "#6CC3EF", "#5ABCED", "#47B4EB", "#35ADE9", "#23A5E7", "#189BDC", "#168ECA", "#1481B8"];
var unknownColor = "#ABB5BA";

var slashRegex = new RegExp("/", "g");
var genreSeparatorReplacer = function(genre) { //avoid creating function objects
    return genre.replace(slashRegex, "-");
};

//static observables used in initialization for increased performance
var includedObservable = ko.observable(true);
var updatePhaseObservable = ko.observable(GameUpdatePhase.None);
var zeroObservable = ko.observable(0);

function Game(steamGame) {

    var self = this;
    self.included = includedObservable;
    self.updatePhase = updatePhaseObservable;

    self.steamPlaytime = steamGame.Playtime;

    var steamAppData = steamGame.SteamAppData;
    self.steamAppId = steamAppData.SteamAppId;
    self.steamName = steamAppData.SteamName;
    self.appType = steamAppData.AppType;
    self.platforms = steamAppData.Platforms;
    self.genres = ko.utils.arrayMap(steamAppData.Genres, genreSeparatorReplacer);
    self.releaseYear = steamAppData.ReleaseYear;
    self.metacriticScore = steamAppData.MetacriticScore;

    var hltbInfo = steamAppData.HltbInfo;
    self.known = hltbInfo.Id !== -1;
    self.hltbId = self.known ? hltbInfo.Id : "";
    self.suggestedHltbId = zeroObservable;
    self.hltbName = self.known ? hltbInfo.Name : "";
    self.hltbMainTtb = hltbInfo.MainTtb;
    self.hltbMainTtbImputed = hltbInfo.MainTtbImputed;
    self.hltbExtrasTtb = hltbInfo.ExtrasTtb;
    self.hltbExtrasTtbImputed = hltbInfo.ExtrasTtbImputed;
    self.hltbCompletionistTtb = hltbInfo.CompletionistTtb;
    self.hltbCompletionistTtbImputed = hltbInfo.CompletionistTtbImputed;
}

Game.prototype.match = function(filter) { //define in prototype to prevent memory footprint on each instance
    return this.steamName.toLowerCase().indexOf(filter.toLowerCase()) !== -1;
};

function AppViewModel() {
    var self = this;

    self.steamVanityUrlName = ko.observable("");

    var tableOptions = {
        recordWord: 'game',
        sortDir: 'asc',
        perPage: 10,
        unsortedClass: "glyphicon glyphicon-sort",
        ascSortClass: "glyphicon glyphicon-sort-by-attributes",
        descSortClass: "glyphicon glyphicon-sort-by-attributes-alt"
    };

    self.introPage = ko.observable(true);

    self.gameTable = new DataTable([], tableOptions);
    self.pageSizeOptions = [10, 25, 50];

    self.sliceTotal = ko.observable();
    self.sliceCompletionLevel = ko.observable();

    self.partialCache = ko.observable(false);
    self.imputedTtbs = ko.observable(false);
    self.missingHltbIds = ko.observable(false);

    self.processing = ko.observable(false);
    self.error = ko.observable(false);

    self.alertHidden = ko.observable(false);
    self.missingAlertHidden = ko.observable(false);
    self.errorAlertHidden = ko.observable(false);

    self.gameToUpdate = ko.observable({
        steamAppId: 0,
        steamName: "",
        hltbId: "",
        suggestedHltbId: ko.observable("")
    });

    self.toggleAllChecked = ko.observable(true);

    self.toggleAllCore = function (include) {
        var games = self.gameTable.rows();
        for (var i = 0; i < games.length; i++) {
            games[i].included(include);
        }
    };

    self.toggleAll = function () {
        self.toggleAllCore(!self.toggleAllChecked()); //binding is one way (workaround KO issue) so toggleAllChecked still has its old value
        return true;
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
                console.error("Invalid playtime slice type: " + sliceCompletionLevel);
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

    var platformLookup = ["Unknown", "Windows", "Mac", "Windows / Mac", "Linux", "Windows / Linux", "Mac / Linux", "Windows / Mac / Linux"];

    self.total = ko.pureComputed(function () {

        var count = 0;
        var playtime = 0;
        var mainTtb = 0;
        var extrasTtb = 0;
        var completionistTtb = 0;
        var mainRemaining = 0;
        var extrasRemaining = 0;
        var completionistRemaining = 0;
        var playtimesByGenre = {};
        var playtimesByMetacritic = {};
        var playtimesByAppType = {};
        var playtimesByReleaseYear = {};
        var playtimesByPlatform = {};
        var totalLength = self.gameTable.filteredRows().length;
        var arr = ko.utils.arrayFilter(self.gameTable.filteredRows(), function (game) { return game.included(); });
        var sliceCompletionLevel = self.sliceCompletionLevel();
        var sliceTotal = self.sliceTotal();

        for (var i = 0; i < arr.length; ++i) {
            var game = arr[i];

            count++;
            playtime += game.steamPlaytime;
            mainTtb += game.hltbMainTtb;
            extrasTtb += game.hltbExtrasTtb;
            completionistTtb += game.hltbCompletionistTtb;

            var gameMainRemaining = Math.max(0, game.hltbMainTtb - game.steamPlaytime);
            var gameExtrasRemaining = Math.max(0, game.hltbExtrasTtb - game.steamPlaytime);
            var gameCompletionistRemaining = Math.max(0, game.hltbCompletionistTtb - game.steamPlaytime);

            mainRemaining += gameMainRemaining;
            extrasRemaining += gameExtrasRemaining;
            completionistRemaining += gameCompletionistRemaining;

            var realGenres = ko.utils.arrayFilter(game.genres, function (subGenre) { return subGenre !== "Indie" && subGenre !== "Casual"; });
            var genre = (realGenres.length === 0 ? game.genres : realGenres).join('/');

            var slicedPlaytime = getSlicedPlaytime(sliceCompletionLevel, sliceTotal, game, gameMainRemaining, gameExtrasRemaining, gameCompletionistRemaining);
            updateSlicedPlaytime(playtimesByGenre, genre, slicedPlaytime);
            updateSlicedPlaytime(playtimesByMetacritic, game.metacriticScore, slicedPlaytime);
            updateSlicedPlaytime(playtimesByAppType, game.appType, slicedPlaytime);
            updateSlicedPlaytime(playtimesByPlatform, platformLookup[game.platforms], slicedPlaytime);
            updateSlicedPlaytime(playtimesByReleaseYear, game.releaseYear, slicedPlaytime);
        }

        if (count === totalLength) {
            self.toggleAllChecked(true);
        } else if (count === 0) {
            self.toggleAllChecked(false);
        }

        var total = {
            count: count,
            playtime: playtime,
            mainTtb: mainTtb,
            extrasTtb: extrasTtb,
            completionistTtb: completionistTtb,
            mainRemaining: mainRemaining,
            extrasRemaining: extrasRemaining,
            completionistRemaining: completionistRemaining,
            playtimesByGenre: playtimesByGenre,
            playtimesByMetacritic: playtimesByMetacritic,
            playtimesByAppType: playtimesByAppType,
            playtimesByPlatform: playtimesByPlatform,
            playtimesByReleaseYear: playtimesByReleaseYear
        };

        return total;
    }).extend({ rateLimit: 0 });

    self.pagedGames = ko.pureComputed(function () {
        var games = self.gameTable.pagedRows();
        var replaced = false;
        for (var i = 0; i < games.length; i++) {
            var game = games[i];
            if (game.included === includedObservable) {
                game.included = ko.observable(includedObservable());
                game.updatePhase = ko.observable(updatePhaseObservable());
                game.suggestedHltbId = ko.observable(game.hltbId);
                replaced = true;
            }
        }
        if (replaced) {
                includedObservable.valueHasMutated(); //will trigger re-evaluation of dependent computables
        }
        return games;
    });

    self.vanityUrlSubmitted = function() {
        if (window.location.hash === "#/" + self.steamVanityUrlName()) {
            self.loadGames(); //no submission will take place since it's the same URL, so just load again
        }
    };

    var updateMainCharts = function (total) {
        self.playtimeChart.dataProvider[0].hours = getHours(total.playtime);
        self.playtimeChart.dataProvider[1].hours = getHours(total.mainTtb);
        self.playtimeChart.dataProvider[2].hours = getHours(total.extrasTtb);
        self.playtimeChart.dataProvider[3].hours = getHours(total.completionistTtb);
        self.playtimeChart.validateData();

        self.remainingChart.dataProvider[0].hours = getHours(total.mainRemaining);
        self.remainingChart.dataProvider[1].hours = getHours(total.extrasRemaining);
        self.remainingChart.dataProvider[2].hours = getHours(total.completionistRemaining);
        self.remainingChart.validateData();
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
                console.error("Invalid playtime slice type: " + self.sliceCompletionLevel());
                return 0;
        }
    };

    var unknownTitle = "Unknown";

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
                others.push(title) ;
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
        chart.validateData();
    };

    var getCategoryRangePredicate = function(sliceKey) { //avoids creating functions in a loop
        return function(category) {
            return (sliceKey <= category.max) && (sliceKey >= category.min);
        };
    };

    var breakdown = function (min, max, breakdownCount) {
        var sliceShards = [];
        var breakdownInterval = Math.round((max - min + 1) / breakdownCount);
        var i = 0;
        for (var k = min; k <= max; k = k + breakdownInterval) {
            var boundMax = Math.min(max, k + breakdownInterval - 1);
            sliceShards.push({ //we don't set the index so that we get it as undefined when the user clicks on one of the shards
                title: k + "-" + boundMax,
                min: k,
                max: boundMax,
                hours: 0,
                color: progressivePalette[i++ % progressivePalette.length],
                pulled: true
            });
        }
        var lastShard = sliceShards[sliceShards.length - 1];
        if (sliceShards.length > 1 && (lastShard.max - lastShard.min) < breakdownInterval / 2) {
            var secondToLastShard = sliceShards[sliceShards.length - 2];
            secondToLastShard.max = lastShard.max;
            secondToLastShard.title = secondToLastShard.min + "-" + lastShard.max;
        }
        return sliceShards;
    };

    var updateContinuousSliceChart = function (chart, slicedPlaytime, categories, sliceClicked) {

        var sliceKeys = getOrderedOwnProperties(slicedPlaytime);
        
        // ReSharper disable once QualifiedExpressionMaybeNull
        if (typeof sliceClicked === "undefined" || sliceClicked.pulled === true) {
            //either this is the initial render with this dataset, or we clicked on a pulled slice so we want to revert
            for (var j = 0; j < categories.length; j++) {
                var category = categories[j];
                category.hours = 0;
                category.color = progressivePalette[j % progressivePalette.length];
                category.pulled = false;
                category.description = " (" + category.min + "-" + category.max + ")";
            }
        } else {
            var unknownHours = 0;
            var minKnownKey = Number.POSITIVE_INFINITY;
            var maxKnownKey = Number.NEGATIVE_INFINITY;
            for (var k = 0; k < sliceKeys.length; k++) {
                var sliceKeyTyped = Number(sliceKeys[k]);
                if (sliceKeyTyped < categories[0].min || sliceKeyTyped > categories[categories.length - 1].max) {
                    unknownHours += getHours(slicedPlaytime[sliceKeys[k]]);
                } else {
                    minKnownKey = Math.min(minKnownKey, sliceKeyTyped);
                    maxKnownKey = Math.max(maxKnownKey, sliceKeyTyped);
                }
            }
            if (minKnownKey === Number.POSITIVE_INFINITY) {
                categories = [];
            } else {
                var playtimeTotal = getPlaytimeTotalHours();
                var knownPercent = (playtimeTotal - unknownHours) / playtimeTotal;
                categories = breakdown(minKnownKey, maxKnownKey, Math.round(knownPercent * 10));
            }
        }

        var slicesData = categories.concat([
            {
                title: unknownTitle,
                min: Number.NEGATIVE_INFINITY,
                max: Number.POSITIVE_INFINITY,
                hours: 0,
                color: unknownColor,
                index: categories.length,
                pulled: categories[categories.length-1].pulled
            }
        ]);

        for (var i = 0; i < sliceKeys.length; i++) {
            var sliceKey = sliceKeys[i];
            var sliceData = ko.utils.arrayFirst(slicesData, getCategoryRangePredicate(Number(sliceKey)));
            sliceData.hours += getHours(slicedPlaytime[sliceKey]);
        }

        chart.dataProvider = slicesData;
        chart.validateData();
    };

    var updateMetacriticChart = function(total, sliceBreakIndex) {
        updateContinuousSliceChart(self.metacriticChart, total.playtimesByMetacritic, [
            { title: "Overwhelming Dislike", min: 0, max: 19 },
            { title: "Generally Unfavorable", min: 20, max: 49 },
            { title: "Mixed or Average", min: 50, max: 74 },
            { title: "Generally Favorable", min: 75, max: 89 },
            { title: "Universal Acclaim", min: 90, max: 100 }
        ], sliceBreakIndex);
    };

    var updateGenreChart = function(total) {
        updateDiscreteSliceChart(self.genreChart, total.playtimesByGenre);
    };

    var updateAppTypeChart = function (total) {
        updateDiscreteSliceChart(self.appTypeChart, total.playtimesByAppType);
    };

    var updatePlatformChart = function (total) {
        updateDiscreteSliceChart(self.platformChart, total.playtimesByPlatform);
    };

    var updateReleaseDateChart = function (total, sliceBreakIndex) {
        var decades = [];
        var currentYear = new Date().getFullYear();
        for (var year = 1980; year <= currentYear; year+=10) {
            decades.push({ title: year.toString().substring(2) + "s", min: year, max: year + 9 });
        }
        updateContinuousSliceChart(self.releaseDateChart, total.playtimesByReleaseYear, decades, sliceBreakIndex);
    };

    var updateSliceCharts = function (total) {
        updateGenreChart(total);
        updateMetacriticChart(total);
        updateAppTypeChart(total);
        updatePlatformChart(total);
        updateReleaseDateChart(total);
    };

    var updateCharts = function (total) {
        updateMainCharts(total);
        updateSliceCharts(total);
    };

    var initChart = function (chartId) {
        var chart = $("#" + chartId);
        chart.height(chart.width() * 2 / 3);
    };

    var initSerialChart = function(chartId, dataProvider) {
        initChart(chartId);
        return AmCharts.makeChart(chartId, {
            type: "serial",
            theme: "light",
            colors: alternatingPalette,
            dataProvider: dataProvider,
            categoryField: "playtime",
            valueAxes: [
                {
                    axisAlpha: 0,
                    position: "left",
                    title: "Hours"
                }
            ],
            categoryAxis: { gridPosition: "start" },
            graphs: [
                {
                    type: "column",
                    valueField: "hours",
                    colorField: "color",
                    lineThickness: 1.5,
                    balloonText: "<b>[[value]] hours</b>",
                    fillAlphas: 0.5,
                    lineAlpha: 0.8
                }
            ],
            precision: 0,
            startDuration: 1,
            responsive: {
                enabled: true
            }
        });
    };

    var initPieChart = function(chartId, title, marginTop, updater) {
        initChart(chartId);
        var chart = AmCharts.makeChart(chartId, {
            titles: [{ text: title }],
            type: "pie",
            theme: "light",
            colors: alternatingPalette,
            marginLeft: 0,
            marginRight: 0,
            marginBottom: 15,
            marginTop: marginTop,
            pullOutRadius: "5%",
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

        if (typeof updater !== "undefined") {
            chart.addListener("clickSlice", function (event) { updater(self.total(), event.dataItem.dataContext); });
        }

        return chart;
    };

    var firstInit = true;
    var initCharts = function() {

        if (!firstInit) {
            self.playtimeChart.animateAgain();
            self.remainingChart.animateAgain();
            return;
        }
        firstInit = false;

        self.playtimeChart = initSerialChart("playtimeChart",
        [
            { playtime: "Current", hours: 0 },
            { playtime: "Main", hours: 0 },
            { playtime: "Extras", hours: 0 },
            { playtime: "Complete", hours: 0 }
        ]);

        self.remainingChart = initSerialChart("remainingChart", [
            { playtime: "Main", hours: 0 },
            { playtime: "Extras", hours: 0 },
            { playtime: "Complete", hours: 0 }
        ]);

        self.genreChart = initPieChart("genreChart", "Playtime by Genre", 25);
        self.metacriticChart = initPieChart("metacriticChart", "Playtime by Metacritic score", 25, updateMetacriticChart);
        self.appTypeChart = initPieChart("appTypeChart", "Playtime by type", 45);
        self.platformChart = initPieChart("platformChart", "Playtime by platform", 45);
        self.releaseDateChart = initPieChart("releaseDateChart", "Playtime by release date", 45, updateReleaseDateChart);

        self.total.subscribe(updateCharts);
        self.sliceTotal.subscribe(function (slicetotal) {
            if (!slicetotal && self.sliceCompletionLevel() === PlaytimeType.Current) {
                self.sliceCompletionLevel(PlaytimeType.Main); //will trigger slice chart update per above
            }
        });
        updateCharts(self.total());
    };

    var scrollToAlerts = function() {
        setTimeout(function() {
            $('html, body').animate({
                scrollTop: $("#alerts").offset().top - 10
            }, 800);
        }, 200);
    };

    var startProcessing = function () {
        self.processing(true);
        var height = $(window).height();
        $('.loader').spin({
            lines: 14,
            length: Math.ceil(height / 25),
            width: Math.ceil(height / 90),
            radius: Math.ceil(height / 15)
        });
    };

    var stopProcessing = function () {
        self.processing(false);
        $('.loader').spin(false);
    };

    var renderedRows = 0;
    var afterRequest = false;
    var firstTableRender = true;

    self.rowRendered = function () {
        if (!afterRequest) {
            return;
        }

        renderedRows++;
        if (renderedRows < Math.min(self.gameTable.perPage(), self.gameTable.filteredRows().length)) {
            return;
        }

        afterRequest = false;

        initCharts();

        if (!firstTableRender) {
            scrollToAlerts();
            return;
        }

        var tableWidth = $("#gameTable").width();

        var compressedColumnWidth = 0;
        $.each($("table th.compressed"), function () {
            var widthWithMargin = $(this).width() + 10;
            compressedColumnWidth += widthWithMargin;
            $(this).css("width", widthWithMargin + "px");
        });

        var expandedCount = $("table th.expanded").size();
        var expandedColumnWidth = (tableWidth - compressedColumnWidth) / expandedCount;
        $("table th.expanded").css("width", expandedColumnWidth + "px");

        $("#gameTable").css('table-layout', "fixed");

        firstTableRender = false;
        stopProcessing();
        scrollToAlerts();
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

        $("#content").hide(); //IE + FF fix
        startProcessing();

        self.error(false);
        self.partialCache(false);
        self.imputedTtbs(false);
        self.missingHltbIds(false);
        self.sliceTotal(false);
        self.sliceCompletionLevel(PlaytimeType.Main);
        self.gameTable.filter("");
        self.gameTable.toggleSort("");

        self.currentRequest = $.get("api/games/library/" + self.steamVanityUrlName())
            .done(function(data) {
                self.partialCache(data.PartialCache);
                includedObservable(true);
                self.gameTable.rows(getGamesArray(data.Games));

                self.originalMainRemaining = self.total().mainRemaining;

                if (self.gameTable.rows().length > 0) {
                    $("#content").show(); //IE + FF fix
                    self.gameTable.currentPageNumber(1);
                    afterRequest = true;
                    renderedRows = 0;
                }

                if(!firstTableRender || self.gameTable.rows().length === 0) {
                    stopProcessing();
                }
            })
            .fail(function(error) {
                console.error(error);
                self.gameTable.rows([]);
                self.error(true);
                stopProcessing();
            })
            .always(function() {
                self.alertHidden(false);
                self.missingAlertHidden(false);
                self.errorAlertHidden(false);
            });
    };

    self.displayUpdateDialog = function (game) {

        self.gameToUpdate(game);
        $('#HltbUpdateModal').modal('show');
    };

    self.updateHltb = function(gameToUpdate) {

        gameToUpdate.updatePhase(GameUpdatePhase.InProgress);
        $.post("api/games/update/" + gameToUpdate.steamAppId + "/" + gameToUpdate.suggestedHltbId())
            .done(function() {
                gameToUpdate.updatePhase(GameUpdatePhase.Success);
            })
            .fail(function(error) {
                console.error(error);
                gameToUpdate.updatePhase(GameUpdatePhase.Failure);
            });

        $('#HltbUpdateModal').modal('hide');
    };

    self.getShortShareText = function() {
        return "I just found out I have over " + hoursWithCommas(self.originalMainRemaining) + " left to beat my entire Steam library!";
    };

    self.getShareText = function() {
        return self.getShortShareText() + " Click to check it out and find out how long you have too...";
    };

    self.shareOnFacebook = function() {
        self.openShareWindow("https://www.facebook.com/dialog/feed?app_id=445487558932250&display=popup&caption=HowLongToBeatSteam.com&description=" + encodeURIComponent(self.getShareText()) + "&link=" + encodeURIComponent(window.location.href) + "&redirect_uri=" + encodeURIComponent("http://howlongtobeatsteam.com/CloseWindow.html") + "&picture=" + encodeURIComponent("http://howlongtobeatsteam.com/Resources/sk5_0.jpg"));
    };

    self.shareOnTwitter = function() {
        self.openShareWindow("https://twitter.com/share?url=" + encodeURIComponent(window.location.href) + "&text=" + self.getShortShareText() + "&hashtags=hltbs,steam");
    };

    self.shareOnGooglePlus = function() {
        self.openShareWindow("https://plus.google.com/share?url=" + encodeURIComponent(window.location.href));
    };

    self.openShareWindow = function(url) {
        window.open(
            url,
            "share",
            "toolbar=no, location=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=600,height=600");
    };
}

$(document).ready(function () {

    //Fix console for old browsers
    if (!window.console) { 
        var noOp = function () { };
        window.console = { log: noOp, warn: noOp, error: noOp };
    }

    //Fix up layout
    $('[data-toggle="tooltip"]').tooltip();
    $("#steamIdText").focus();

    //Init knockout
    var viewModel = new AppViewModel();
    ko.applyBindings(viewModel);

    //Init sammy
    var sammyApp = $.sammy(function () {

        this.get("#/", function () {
            viewModel.introPage(true);
        });

        var loadGames = function (vanityUrl) {
            viewModel.introPage(false);
            viewModel.steamVanityUrlName(vanityUrl);
            viewModel.loadGames();
        };

        this.get("#/:vanityUrlName", function () {
            loadGames(this.params.vanityUrlName);
        });

        this.get("#/cached/:count", function () {
            loadGames("cached/"+this.params.count);
        });
    });

    sammyApp.run("#/");
});