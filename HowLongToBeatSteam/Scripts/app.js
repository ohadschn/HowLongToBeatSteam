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

var alternatingPalette = [];
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
    self.genres = ko.utils.arrayMap(steamAppData.Genres, genreSeparatorReplacer);
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

    self.width = $(window).width();
    self.height = $(window).height();

    self.superSmall = self.width < 380;
    self.extraSmall = self.width < 768;
    self.small = self.width < 992;
    self.medium = self.width < 1200;

    $(window).on("orientationchange", function () {
        setTimeout(function () { location.reload(); }, 0); //can't reload inside event handler in FireFox
    });
    $(window).resize(function () {
        if ($(window).height() === self.width && $(window).width() === self.height) {
            setTimeout(function () { location.reload(); }, 0); //can't reload inside event handler in FireFox
        }
    });

    self.steamVanityUrlName = ko.observable("");

    var tableOptions = {
        recordWord: 'game',
        sortDir: 'asc',
        perPage: 10,
        paginationLimit: self.superSmall ? 3 : (self.small ? 4 : (self.medium ? 6 : 10)),
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

    self.personaName = ko.observable("");
    self.avatarUrl = ko.observable("");

    self.processing = ko.observable(false);
    self.error = ko.observable(false);
    self.bonusLinkVisible = ko.observable(true);

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
        var playtimesByGenre = {};
        var playtimesByMetacritic = {};

        var arr = ko.utils.arrayFilter(filteredRows, function (game) { return game.included(); });
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
            playtimesByGenre: playtimesByGenre,
            playtimesByMetacritic: playtimesByMetacritic
        };
    }).extend({ rateLimit: 0 });

    self.pagedGames = ko.pureComputed(function () {
        var games = self.gameTable.pagedRows();
        var replaced = false;
        for (var i = 0; i < games.length; i++) {
            var game = games[i];
            if (game.included === includedObservable) {
                game.included = ko.observable(includedObservable.peek());
                game.updatePhase = ko.observable(updatePhaseObservable());
                game.suggestedHltbId = ko.observable(game.hltbId);
                replaced = true;
            }
        }
        if (replaced) {
            includedObservable.valueHasMutated(); //will trigger re-evaluation of total
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
            var shardCount = Math.round((clickedCategory.hours / getPlaytimeTotalHours()) * 10) + 2;
            categories.splice.apply(categories, [clickedCategory.index, 1].concat(breakdown(slicedPlaytimeToBreakDown, clickedCategory, shardCount)));
        }

        chart.dataProvider = categories;
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

    var updateSliceCharts = function (total) {
        //we need to use jQuery as knockout is not immediate and thus the chart rendering will fail
        var sliceCharts = $("#genreChart, #metacriticChart");
        var noDataIndicators = $("#genreChartNoData, #metacriticChartNoData");

        if (getPlaytimeTotalHours() === 0) {
            sliceCharts.hide();
            noDataIndicators.show();
            return;
        }

        sliceCharts.show();
        noDataIndicators.hide();

        updateGenreChart(total);
        updateMetacriticChart(total);
    };

    var updateCharts = function (total) {
        updateMainCharts(total);
        updateSliceCharts(total);
    };

    var initChart = function (chartId) {
        var chart = $("#" + chartId);
        var chartHeight = chart.width() * (self.superSmall ? 1 : 2/3);
        chart.height(chartHeight);

        var noDataIndicator = $("#" + chartId + "NoData");
        noDataIndicator.height(chartHeight); //prevent jumps
        noDataIndicator.css("line-height", chartHeight + "px"); //needed for vertical centering
    };

    var initSerialChart = function(chartId, dataProvider) {
        initChart(chartId);
        return AmCharts.makeChart(chartId, {
            panEventsEnabled: false,
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
            startDuration: (typeof window.orientation === 'undefined') ? 1.5 : 0,
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
    };

    var initPieChart = function(chartId, marginTop, updater) {
        initChart(chartId);
        var chart = AmCharts.makeChart(chartId, {
            panEventsEnabled: false,
            type: "pie",
            theme: "light",
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

        if (typeof updater !== "undefined") {
            chart.addListener("clickSlice", function (event) { updater(self.total(), event.dataItem); });
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

        self.genreChart = initPieChart("genreChart", 25);
        self.metacriticChart = initPieChart("metacriticChart", 25, updateMetacriticChart);

        self.total.subscribe(updateCharts);
        self.sliceTotal.subscribe(function (slicetotal) {
            if (!slicetotal && self.sliceCompletionLevel() === PlaytimeType.Current) {
                self.sliceCompletionLevel(PlaytimeType.Main); //will trigger slice chart update per above
            }
        });
        updateCharts(self.total());
    };

    var scrollToAlerts = function () {
        if (window.pageYOffset > 0) {
            return; //don't override user position
        }
        setTimeout(function() {
            $('html, body').animate({
                scrollTop: $("#alerts").offset().top - 10
            }, 800);
        }, 200);
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

        initCharts();

        if (!firstTableRender) {
            scrollToAlerts();
            return;
        }

        $.each($("table th.compressed"), function () {
            $(this).css("width", $(this).width() + 10 + "px");
        });

        $("#gameTable").css('table-layout', "fixed");

        firstTableRender = false;
        self.processing(false);
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

        self.processing(true);

        self.error(false);
        self.partialCache(false);
        self.imputedTtbs(false);
        self.missingHltbIds(false);
        self.sliceTotal(false);
        self.sliceCompletionLevel(PlaytimeType.Main);
        self.gameTable.filter("");
        self.gameTable.toggleSort("");
        self.bonusLinkVisible(self.steamVanityUrlName().indexOf("cached/") === -1);

        self.currentRequest = $.get("api/games/library/" + self.steamVanityUrlName())
            .done(function(data) {
                afterRequest = true;
                includedObservable(true);
                self.partialCache(data.PartialCache);
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
                    playtimesByGenre: data.Totals.PlaytimesByGenre,
                    playtimesByMetacritic: data.Totals.PlaytimesByMetacritic
                };
                self.gameTable.rows(getGamesArray(data.Games));

                self.originalMainRemaining = data.Totals.MainRemaining;

                if (self.gameTable.rows().length > 0) {
                    $("#content").show(); //IE + FF fix
                    self.gameTable.currentPageNumber(1);
                    renderedRows = 0;
                }

                if(!firstTableRender || self.gameTable.rows().length === 0) {
                    self.processing(false);
                }
            })
            .fail(function(error) {
                console.error(error);
                self.gameTable.rows([]);
                self.error(true);
                self.processing(false);
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

    var getCanonicalAddress = function () {
        return "http://howlongtobeatsteam.com" + window.location.pathname + window.location.hash;
    };

    self.getShortShareText = function (hours) {
        return "I just found out I have over " + (hours ? hoursWithCommas(self.originalMainRemaining) : (getYears(self.originalMainRemaining) + " of consecutive gameplay")) + " left to beat my entire Steam library!";
    };

    self.getShareText = function() {
        return self.getShortShareText(false) + " Click to check it out and find out how long you have too :)";
    };

    self.shareOnFacebook = function() {
        self.openShareWindow("https://www.facebook.com/dialog/feed?app_id=445487558932250&display=popup&caption=HowLongToBeatSteam.com&description=" + encodeURIComponent(self.getShareText()) + "&link=" + encodeURIComponent(getCanonicalAddress()) + "&redirect_uri=" + encodeURIComponent("http://howlongtobeatsteam.com/CloseWindow.html") + "&picture=" + encodeURIComponent("http://howlongtobeatsteam.com/Resources/sk5_0.jpg"));
    };

    self.shareOnTwitter = function () {
        self.openShareWindow("https://twitter.com/share?url=" + encodeURIComponent(getCanonicalAddress()) + "&text=" + self.getShortShareText(true) + "&hashtags=hltbs,steam");
    };

    self.shareOnGooglePlus = function() {
        self.openShareWindow("https://plus.google.com/share?url=" + encodeURIComponent(getCanonicalAddress()));
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

    for (var i = 0; i < 50; i++) {
        alternatingPalette.push("#97BBCD", "#75C7F0");
    }

    //Fix up layout
    $('span[data-toggle="tooltip"]').tooltip();

    //Init knockout
    var viewModel = new AppViewModel();
    ko.applyBindings(viewModel);

    //Init sammy
    var sammyApp = $.sammy(function () {

        this.get("#/", function () {
            viewModel.steamVanityUrlName("");
            viewModel.introPage(true);
            setTimeout(function () {
                $("#steamIdText").focus(); //workaround for FF
            }, 0);
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
            loadGames("cached/" + this.params.count);
        });
    });

    sammyApp.run("#/");
});