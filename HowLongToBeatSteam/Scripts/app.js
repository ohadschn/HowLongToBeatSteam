/*global ko*/
/*global DataTable*/
/*global AmCharts*/

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

function addMinutes(date, minutes) {
    return new Date(date.getTime() + minutes * 60000);
}

var timeWithCommas = function (x, unit) {
    return numberWithCommas(x) + " " + ((x === 1.0) ? unit : (unit+"s"));
}

var hoursWithCommas = function(x) { // jshint ignore:line
    var hours = getHours(x, 0);
    return timeWithCommas(hours, "hour");
};

var getYears = function (minutes) {
    return countdown(null, addMinutes(new Date(), minutes), countdown.YEARS | countdown.MONTHS | countdown.WEEKS | countdown.DAYS | countdown.HOURS).toString();
}

var GameUpdatePhase = {
    None: "None",
    InProgress: "InProgress",
    Success: "Success",
    Failure: "Failure"
};

var palette = ["97BBCD", "85C2E0", "A3B8C2", "75C7F0", "ABB5BA", "6EC9F7", "7DC4E8", "9CBAC9", "8CBFD9", "66CCFF"];

function Game(steamGame) {

    var self = this;
    self.included = ko.observable(true);
    self.updatePhase = ko.observable(GameUpdatePhase.None);

    self.steamPlaytime = steamGame.Playtime;

    var steamAppData = steamGame.SteamAppData;
    self.steamAppId = steamAppData.SteamAppId;
    self.steamName = steamAppData.SteamName;
    self.steamUrl = "http://store.steampowered.com/app/" + self.SteamAppId;
    self.appType = steamAppData.AppType;
    self.platforms = steamAppData.Platforms;
    self.categories = steamAppData.Categories;
    self.genres = steamAppData.Genres;
    self.developers = steamAppData.Developers;
    self.publishers = steamAppData.Publishers;
    self.releaseDate = steamAppData.ReleaseDate;
    self.metaCriticScore = steamAppData.MetaCriticScore;

    var hltbInfo = steamAppData.HltbInfo;
    self.known = hltbInfo.Id !== -1;
    self.hltbOriginalId= self.known ? hltbInfo.Id : "";
    self.hltbId = ko.observable(self.known ? hltbInfo.Id : "");
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
        hltbId: ko.observable("")
    });
    self.gameToUpdateSuggestedHltbId = ko.observable("");

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

    self.previousIds = [];
    self.prevTotal = {
        count: 0, playTime: 0, mainTtb: 0, extrasTtb: 0, completionistTtb: 0,
        mainRemaining: 0, extrasRemaining: 0, completionistRemaining: 0,
        totalByGenre: {}
    };
    self.total = ko.pureComputed(function () {

        var count = 0;
        var playTime = 0;
        var mainTtb = 0;
        var extrasTtb = 0;
        var completionistTtb = 0;
        var mainRemaining = 0;
        var extrasRemaining = 0;
        var completionistRemaining = 0;
        var totalByGenre = {};

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
            playTime += game.steamPlaytime;
            mainTtb += game.hltbMainTtb;
            extrasTtb += game.hltbExtrasTtb;
            completionistTtb += game.hltbCompletionistTtb;
            mainRemaining += Math.max(0, game.hltbMainTtb - game.steamPlaytime);
            extrasRemaining += Math.max(0, game.hltbExtrasTtb - game.steamPlaytime);
            completionistRemaining += Math.max(0, game.hltbCompletionistTtb - game.steamPlaytime);

            var genre = game.genres[0];
            if (!totalByGenre.hasOwnProperty(genre)) {
                totalByGenre[genre] = [0,0,0,0];
            }
            totalByGenre[genre][0] += game.steamPlaytime;
            totalByGenre[genre][1] += game.hltbMainTtb;
            totalByGenre[genre][2] += game.hltbExtrasTtb;
            totalByGenre[genre][3] += game.hltbCompletionistTtb;
        }

        if (count === totalLength) {
            self.toggleAllChecked(true);
        } else if (count === 0) {
            self.toggleAllChecked(false);
        }

        var total = {
            count: count,
            playTime: playTime,
            mainTtb: mainTtb,
            extrasTtb: extrasTtb,
            completionistTtb: completionistTtb,
            mainRemaining: mainRemaining,
            extrasRemaining: extrasRemaining,
            completionistRemaining: completionistRemaining,
            totalByGenre: totalByGenre
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
        self.playtimeChart.dataProvider[0].hours = getHours(total.playTime, 0);
        self.playtimeChart.dataProvider[1].hours = getHours(total.mainTtb, 0);
        self.playtimeChart.dataProvider[2].hours = getHours(total.extrasTtb, 0);
        self.playtimeChart.dataProvider[3].hours = getHours(total.completionistTtb, 0);
        self.playtimeChart.validateData();

        self.remainingChart.dataProvider[0].hours = getHours(total.mainRemaining, 0);
        self.remainingChart.dataProvider[1].hours = getHours(total.extrasRemaining, 0);
        self.remainingChart.dataProvider[2].hours = getHours(total.completionistRemaining, 0);
        self.remainingChart.validateData();
    };

    var getOrderedOwnProperties = function (object) {
        var propArr = [];
        for (var prop in object) {
            if (object.hasOwnProperty(prop)) {
                propArr.push(prop);
            }
        }
        propArr.sort(); //we don't care that it's not alphabetical as long as it's consistent
        return propArr;
    }

    var updateSliceCharts = function (total) {
        var genres = getOrderedOwnProperties(total.totalByGenre);
        self.genreChart.dataProvider = [];
        var paletteIndex = 0;
        for (var i = 0; i < genres.length; i++) {
            var genre = genres[i];
            self.genreChart.dataProvider.push({
                genre: genre,
                hours: getHours(total.totalByGenre[genre][0], 0),
                color: palette[paletteIndex % palette.length]
            });
            paletteIndex++;
        }
        self.genreChart.validateData();
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

    var firstInit = true;
    var initCharts = function() {

        if (!firstInit) {
            self.playtimeChart.animateAgain();
            self.remainingChart.animateAgain();
            self.genreChart.animateAgain();
            return;
        }
        firstInit = false;

        var hourAxis =
        {
            axisAlpha: 0,
            position: "left",
            title: "Hours"
        };
        var hourBalloon = {
            type: "column",
            valueField: "hours",
            colorField: "color",
            alphaField: "alpha",
            balloonText: "<b>[[value]] hours</b>",
            fillAlphas: 0.9,
            lineAlpha: 0.2
        };
        initChart("playtimeChart");
        var barColor = "#97BBCD";
        self.playtimeChart = AmCharts.makeChart("playtimeChart", {
            type: "serial",
            dataProvider: [
                { playtime: "Current", hours: 0, color: barColor, alpha: 0.7 },
                { playtime: "Main", hours: 0, color: barColor, alpha: 0.7 },
                { playtime: "Extras", hours: 0, color: barColor, alpha: 0.7 },
                { playtime: "Complete", hours: 0, color: barColor, alpha: 0.7 }
            ],
            categoryField: "playtime",
            valueAxes: [hourAxis],
            graphs: [hourBalloon],
            startDuration: 1
        });

        initChart("remainingChart");
        self.remainingChart = AmCharts.makeChart("remainingChart", {
            type: "serial",
            dataProvider: [
                { playtime: "Main", hours: 0, color: barColor, alpha: 0.7 },
                { playtime: "Extras", hours: 0, color: barColor, alpha: 0.7 },
                { playtime: "Complete", hours: 0, color: barColor, alpha: 0.7 }
            ],
            categoryField: "playtime",
            valueAxes: [hourAxis],
            graphs: [hourBalloon],
            startDuration: 1
        });

        initChart("genreChart");
        self.genreChart = AmCharts.makeChart("genreChart", {
            type: "pie",
            valueField: "hours",
            titleField: "genre",
            labelRadius: 10,
            labelText: "[[title]]",
            balloonText: "[[title]]: [[percents]]% ([[value]] hours)\n[[description]]",
            colorField: "color"
        });

        self.total.subscribe(updateCharts);
        updateCharts(self.total());
    };

    self.loadGames = function () {

        if (self.currentRequest !== undefined) {
            self.currentRequest.abort(); //in case of hash tag navigation while we're loading
        }

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
                initCharts();
                scrollToAlerts();
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
        self.gameToUpdateSuggestedHltbId(game.hltbId());
        $('#HltbUpdateModal').modal('show');
    };

    self.updateHltb = function(gameToUpdate) {

        gameToUpdate.updatePhase(GameUpdatePhase.InProgress);
        gameToUpdate.hltbId(self.gameToUpdateSuggestedHltbId());

        $.post("api/games/update/" + gameToUpdate.steamAppId + "/" + gameToUpdate.hltbId())
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
    $('#steamIdText').focus();

    //Init knockout
    var viewModel = new AppViewModel();
    ko.applyBindings(viewModel);

    //Init sammy
    var sammyApp = $.sammy(function () {

        this.get('#/', function () {
            viewModel.introPage(true);
        });

        this.get('#/:vanityUrlName', function () {
            viewModel.introPage(false);
            viewModel.steamVanityUrlName(this.params.vanityUrlName);
            viewModel.loadGames();
        });
    });

    sammyApp.run("#/");
});