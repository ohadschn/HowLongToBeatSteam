/*global ko*/
/*global DataTable*/
/*global AmCharts*/
/*global countdown*/

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
//var progressivePalette = ["#97BBCD", "#85C2E0", "#75C7F0", "#97BBCD", "#97BBCD", "#97BBCD"];
var progressivePalette = ["#97BBCD", "#85C2E0", "#75C7F0", "#66CCFF", "#66d7ff"];
var unknownColor = "#ABB5BA";

var genreSeparatorReplacer = function(genre) { //avoid creating function objects
    return genre.replace(new RegExp("/", "g"), "-");
};

function Game(steamGame) {

    var self = this;
    self.included = ko.observable(true);
    self.updatePhase = ko.observable(GameUpdatePhase.None);

    self.steamPlaytime = steamGame.Playtime;

    var steamAppData = steamGame.SteamAppData;
    self.steamAppId = steamAppData.SteamAppId;
    self.steamName = steamAppData.SteamName;
    self.steamUrl = "http://store.steampowered.com/app/" + steamAppData.SteamAppId;
    //self.appType = steamAppData.AppType;
    //self.platforms = steamAppData.Platforms;
    //self.categories = steamAppData.Categories;
    self.genres = ko.utils.arrayMap(steamAppData.Genres, genreSeparatorReplacer);
    //self.developers = steamAppData.Developers;
    //self.publishers = steamAppData.Publishers;
    //self.releaseDate = steamAppData.ReleaseDate;
    self.metacriticScore = steamAppData.MetacriticScore;

    var hltbInfo = steamAppData.HltbInfo;
    self.known = hltbInfo.Id !== -1;
    self.hltbOriginalId= self.known ? hltbInfo.Id : "";
    self.hltbId = ko.observable(self.known ? hltbInfo.Id : "");
    self.suggestedHltbId = ko.observable(self.hltbId());
    self.hltbName = self.known ? hltbInfo.Name : "";
    self.hltbMainTtb = hltbInfo.MainTtb;
    self.hltbMainTtbImputed = hltbInfo.MainTtbImputed;
    self.hltbExtrasTtb = hltbInfo.ExtrasTtb;
    self.hltbExtrasTtbImputed = hltbInfo.ExtrasTtbImputed;
    self.hltbCompletionistTtb = hltbInfo.CompletionistTtb;
    self.hltbCompletionistTtbImputed = hltbInfo.CompletionistTtbImputed;
    self.hltbUrl = self.known
        ? "http://www.howlongtobeat.com/game.php?id=" + hltbInfo.Id
        : "http://www.howlongtobeat.com";
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
        sortField: 'steamName',
        perPage: 10,
        unsortedClass: "glyphicon glyphicon-sort",
        ascSortClass: "glyphicon glyphicon-sort-by-attributes",
        descSortClass: "glyphicon glyphicon-sort-by-attributes-alt"
    };

    self.introPage = ko.observable(true);

    self.gameTable = new DataTable([], tableOptions);
    self.pageSizeOptions =  [10, 25, 50];

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
        hltbId: ko.observable(""),
        suggestedHltbId: ko.observable("")
    });

    self.toggleAllChecked = ko.observable(true);

    self.toggleAllCore = function(include) {
        ko.utils.arrayForEach(self.gameTable.rows(), function(game) {
            game.included(include);
        });
    };

    self.toggleAll = function () {
        self.toggleAllCore(!self.toggleAllChecked()); //binding is one way (workaround KO issue) so toggleAllChecked still has its old value
        return true;
    };

    var updatePlaytimes = function (playtimes, key, game, gameMainRemaining, gameExtrasRemaining, gameCompletionistRemaining) {
        if (!playtimes.hasOwnProperty(key)) {
            playtimes[key] = [0, 0, 0, 0, 0, 0, 0];
        }

        var arr = playtimes[key];
        arr[0] += game.steamPlaytime;
        arr[1] += game.hltbMainTtb;
        arr[2] += game.hltbExtrasTtb;
        arr[3] += game.hltbCompletionistTtb;
        arr[4] += gameMainRemaining;
        arr[5] += gameExtrasRemaining;
        arr[6] += gameCompletionistRemaining;
    };

    self.previousIds = [];
    self.prevTotal = {
        count: 0, playtime: 0, mainTtb: 0, extrasTtb: 0, completionistTtb: 0,
        mainRemaining: 0, extrasRemaining: 0, completionistRemaining: 0,
        playtimesByGenre: {},
        playtimesByMetacritic: {}
    };
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

        var totalLength = self.gameTable.filteredRows().length;
        var arr = ko.utils.arrayFilter(self.gameTable.filteredRows(), function (game) { return game.included(); });
        var ids = ko.utils.arrayMap(arr, function(game) { return game.steamAppId; });

        if ($(ids).not(self.previousIds).length === 0 && $(self.previousIds).not(ids).length === 0) {
            self.prevTotal.orderChange = true;
            return self.prevTotal;
        }

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

            var realGenres = $(game.genres).not(['Indie', 'Casual']).get();
            var genre = (realGenres.length === 0 ? game.genres : realGenres).join('/');
           
            updatePlaytimes(playtimesByGenre, genre, game, gameMainRemaining, gameExtrasRemaining, gameCompletionistRemaining);
            updatePlaytimes(playtimesByMetacritic, game.metacriticScore, game, gameMainRemaining, gameExtrasRemaining, gameCompletionistRemaining);
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
            playtimesByMetacritic: playtimesByMetacritic
        };

        self.previousIds = ids;
        self.prevTotal = total;

        return total;
    }).extend({ rateLimit: 0 });

    var scrollDuration = 1000;
    var scrollToAlerts = function() {
        $('html, body').animate({
            scrollTop: $("#content").offset().top - 10
        }, 0.75 * scrollDuration);
    };

    var firstTableRender = true;
    self.tableRendered = function() {
        if (!firstTableRender || $("#gameTable tbody").children().length !== self.gameTable.perPage()) {
            return;
        }
        firstTableRender = false;

        setTimeout(function() {
            var tableWidth = $("#gameTable").width();

            var compressedColumnWidth = 0;
            $.each($("table th.compressed"), function() {
                var widthWithMargin = $(this).width() + 10;
                compressedColumnWidth += widthWithMargin;
                $(this).css("width", widthWithMargin + "px");
            });

            var expandedCount = $("table th.expanded").size();
            var expandedColumnWidth = (tableWidth - compressedColumnWidth) / expandedCount;
            $("table th.expanded").css("width", expandedColumnWidth + "px");

            $("#gameTable").css('table-layout', "fixed");
        }, scrollDuration / 2);
    };

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

    var getPlaytimeInfo = function() {
        var playtimeIndex;
        var playtimeTotal;
        switch (self.sliceCompletionLevel()) {
        case PlaytimeType.Current:
            playtimeIndex = self.sliceTotal() ? 0 : 4;
            playtimeTotal = self.sliceTotal() ? self.total().playtime : self.total().mainRemaining;
            break;
        case PlaytimeType.Main:
            playtimeIndex = self.sliceTotal() ? 1 : 4;
            playtimeTotal = self.sliceTotal() ? self.total().mainTtb : self.total().mainRemaining;
            break;
        case PlaytimeType.Extras:
            playtimeIndex = self.sliceTotal() ? 2 : 5;
            playtimeTotal = self.sliceTotal() ? self.total().extrasTtb : self.total().extrasRemaining;
            break;
        case PlaytimeType.Completionist:
            playtimeIndex = self.sliceTotal() ? 3 : 6;
            playtimeTotal = self.sliceTotal() ? self.total().completionistTtb : self.total().completionistRemaining;
            break;
        default:
            console.error("Invalid playtime slice type: " + self.sliceCompletionLevel());
            playtimeIndex = self.sliceTotal() ? 1 : 4;
            playtimeTotal = self.sliceTotal() ? self.total().mainTtb : self.total().mainRemaining;
            break;
        }

        return { index: playtimeIndex, total: getHours(playtimeTotal, 3) };
    };

    var unknownTitle = "Unknown";

    var updateDiscreteSliceChart = function (chart, slicedPlaytime) {
        var groupingThreshold = 0.01;
        var titles = getOrderedOwnProperties(slicedPlaytime);
        var playtimeInfo = getPlaytimeInfo();
        chart.dataProvider = [];
        var others = [];
        var othersTotal = 0;
        for (var i = 0; i < titles.length; i++) {
            var title = titles[i];
            var hours = getHours(slicedPlaytime[title][playtimeInfo.index]);
            if (title === unknownTitle || hours / playtimeInfo.total < groupingThreshold) {
                others.push( title) ;
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

    var getCategoryRangePredicate = function(value) { //avoids creating functions in a loop
        return function(category) {
            return (value <= category.max) && (value >= category.min);
        };
    };

    var updateContinuousSliceChart = function(chart, slicedPlaytime, categories) {
        var sliceKeys = getOrderedOwnProperties(slicedPlaytime);
        var playtimeInfo = getPlaytimeInfo();

        for (var j = 0; j < categories.length; j++) {
            categories[j].hours = 0;
        }
        var slicesData = categories.concat([
            {
                title: unknownTitle,
                min: Number.NEGATIVE_INFINITY,
                max: Number.POSITIVE_INFINITY,
                hours: 0,
                color: unknownColor
            }
        ]);

        for (var i = 0; i < sliceKeys.length; i++) {
            var sliceKey = sliceKeys[i];
            var sliceData = ko.utils.arrayFirst(slicesData, getCategoryRangePredicate(Number(sliceKey)));
            sliceData.hours += getHours(slicedPlaytime[sliceKey][playtimeInfo.index], 3);
        }

        chart.dataProvider = slicesData;
        chart.validateData();
    };

    var updateSliceCharts = function(total) {
        updateDiscreteSliceChart(self.genreChart, total.playtimesByGenre);
        updateContinuousSliceChart(self.metacriticChart, total.playtimesByMetacritic, [
            { title: "Overwhelming Dislike", min: 0, max: 19 },
            { title: "Generally Unfavorable", min: 20, max: 49 },
            { title: "Mixed or Average", min: 50, max: 74 },
            { title: "Generally Favorable", min: 75, max: 89 },
            { title: "Universal Acclaim", min: 90, max: 100 }
        ]);
    };

    var updateCharts = function(total) {
        if (total.orderChange === true) {
            return;
        }

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

    var initPieChart = function(chartId, palette) {
        initChart(chartId);
        return AmCharts.makeChart(chartId, {
            type: "pie",
            theme: "light",
            colors: palette,
            marginLeft: 0,
            marginRight: 0,
            marginBottom: 0,
            marginTop: 0,
            valueField: "hours",
            colorField: "color",
            precision: 0,
            titleField: "title",
            labelRadius: 10,
            labelText: "[[title]]",
            balloonText: "[[title]]: [[percents]]% ([[value]] hours)",
            balloon: {
                maxWidth: $("#genreChart").width() * 2 / 3
            },
            responsive: {
                enabled: true
            }
        });
    };

    var initDiscretePieChart = function(chartId) {
        return initPieChart(chartId, alternatingPalette);
    };

    var initContinuousPieChart = function(chartId) {
        return initPieChart(chartId, progressivePalette);
    };

    var firstInit = true;
    var initCharts = function() {

        if (!firstInit) {
            self.playtimeChart.animateAgain();
            self.remainingChart.animateAgain();
            self.genreChart.animateAgain();
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

        self.genreChart = initDiscretePieChart("genreChart");
        self.metacriticChart = initContinuousPieChart("metacriticChart");

        self.total.subscribe(updateCharts);
        self.sliceCompletionLevel.subscribe(function sliceCompletionLevel() {
            updateSliceCharts(self.total());
        });
        self.sliceTotal.subscribe(function (slicetotal) {
            if (!slicetotal && self.sliceCompletionLevel() === PlaytimeType.Current) {
                self.sliceCompletionLevel(PlaytimeType.Main); //will trigger slice chart update per above
            } else {
                updateSliceCharts(self.total());
            }
        });
        updateCharts(self.total());
    };

    self.loadGames = function () {

        if (self.currentRequest !== undefined) {
            self.currentRequest.abort(); //in case of hash tag navigation while we're loading
        }

        $("#content").hide();   //IE + FF fix

        var height = $(window).height();
        $('.loader').spin({
            lines: 14,
            length: Math.ceil(height / 25),
            width: Math.ceil(height / 90),
            radius: Math.ceil(height / 15)
        });

        self.error(false);

        self.processing(true);
        self.partialCache(false);
        self.imputedTtbs(false);
        self.missingHltbIds(false);
        self.sliceTotal(false);
        self.sliceCompletionLevel(PlaytimeType.Main);
        self.gameTable.filter('');

        self.currentRequest = $.get("api/games/library/" + self.steamVanityUrlName())
            .done(function(data) {
                self.partialCache(data.PartialCache);
                self.gameTable.rows(ko.utils.arrayMap(data.Games, function(steamGame) {
                    var game = new Game(steamGame);
                    if (!game.known) {
                        self.missingHltbIds(true);
                        self.imputedTtbs(true);
                    } else if (game.hltbMainTtbImputed || game.hltbExtrasTtbImputed || game.hltbCompletionistTtbImputed) {
                        self.imputedTtbs(true);
                    }
                    return game;
                }));

                self.originalMainRemaining = self.total().mainRemaining;

                self.alertHidden(false);
                if (self.gameTable.rows().length > 0) {
                    $("#content").show(); //IE + FF fix
                    initCharts();
                    scrollToAlerts();
                    self.gameTable.currentPage(1);
                }
            })
            .fail(function(error) {
                console.error(error);
                self.gameTable.rows([]);
                self.error(true);
            })
            .always(function () {
                self.processing(false);
                self.alertHidden(false);
                self.missingAlertHidden(false);
                self.errorAlertHidden(false);
                $('.loader').spin(false);
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

    self.allowUpdate = function(game) { //defined on view model to avoid multiple definitions (one for each game in array)
        return (game.updatePhase() === GameUpdatePhase.None) || (game.updatePhase() === GameUpdatePhase.Failure);
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

        this.get("#/:vanityUrlName", function () {
            viewModel.introPage(false);
            viewModel.steamVanityUrlName(this.params.vanityUrlName);
            viewModel.loadGames();
        });
    });

    sammyApp.run("#/");
});