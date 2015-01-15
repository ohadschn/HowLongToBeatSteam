/*global ko*/
/*global DataTable*/
/*global Chart*/

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

var GameUpdatePhase = {
    None: "None",
    InProgress: "InProgress",
    Success: "Success",
    Failure: "Failure",
};

function Game(steamGame) {

    var self = this;

    self.known = steamGame.HltbInfo.Id !== -1;
    self.included = ko.observable(true);
    self.updatePhase = ko.observable(GameUpdatePhase.None);

    self.steamAppId = steamGame.SteamAppId;
    self.steamName = steamGame.SteamName;
    
    self.steamPlaytime = steamGame.Playtime;
    self.steamUrl = "http://store.steampowered.com/app/" + steamGame.SteamAppId;

    self.hltbOriginalId= self.known ? steamGame.HltbInfo.Id : "";
    self.hltbId = ko.observable(self.known ? steamGame.HltbInfo.Id : "");
    self.hltbName = ko.observable(self.known ? steamGame.HltbInfo.Name : "Unknown; please update"); //we'll abuse this for update status
    self.hltbMainTtb = steamGame.HltbInfo.MainTtb;
    self.hltbMainTtbImputed = steamGame.HltbInfo.MainTtbImputed;
    self.hltbExtrasTtb = steamGame.HltbInfo.ExtrasTtb;
    self.hltbExtrasTtbImputed = steamGame.HltbInfo.ExtrasTtbImputed;
    self.hltbCompletionistTtb = steamGame.HltbInfo.CompletionistTtb;
    self.hltbCompletionistTtbImputed = steamGame.HltbInfo.CompletionistTtbImputed;
    self.hltbUrl = self.known
        ? "http://www.howlongtobeat.com/game.php?id=" + steamGame.HltbInfo.Id
        : "http://www.howlongtobeat.com";
}

Game.prototype.match = function(filter) { //define in prototype to prevent memory footprint on each instance
    return this.steamName.toLowerCase().indexOf(filter.toLowerCase()) !== -1;
};

function AppViewModel() {
    var self = this;

    self.steamVanityUrlName = ko.observable();
    self.badSteamVanityUrlName = ko.observable(false);

    $("#steamIdText").keydown(function() {
        self.badSteamVanityUrlName(false);
    });

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
    self.error = ko.observable(null);

    self.alertHidden = ko.observable(true);
    self.errorAlertHidden = ko.observable(false);

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

    self.total = ko.pureComputed(function () {

        var count = 0;
        var playTime = 0;
        var mainTtb = 0;
        var extrasTtb = 0;
        var completionistTtb = 0;
        var mainRemaining = 0;
        var extrasRemaining = 0;
        var completionistRemaining = 0;
        var length = self.gameTable.filteredRows().length;
        var arr = self.gameTable.filteredRows();

        for (var i = 0; i < length; ++i) {
            var game = arr[i];

            if (!game.included()) {
                continue;
            }

            count++;
            playTime += game.steamPlaytime;
            mainTtb += game.hltbMainTtb;
            extrasTtb += game.hltbExtrasTtb;
            completionistTtb += game.hltbCompletionistTtb;
            mainRemaining += Math.max(0, game.hltbMainTtb - game.steamPlaytime);
            extrasRemaining += Math.max(0, game.hltbExtrasTtb - game.steamPlaytime);
            completionistRemaining += Math.max(0, game.hltbCompletionistTtb - game.steamPlaytime);
        }

        if (count === length) {
            self.toggleAllChecked(true);
        } else if (count === 0) {
            self.toggleAllChecked(false);
        }

        return {
            count: count,
            playTime: playTime,
            mainTtb: mainTtb,
            extrasTtb: extrasTtb,
            completionistTtb: completionistTtb,
            mainRemaining: mainRemaining,
            extrasRemaining: extrasRemaining,
            completionistRemaining: completionistRemaining,
        };
    }).extend({ rateLimit: 0 });

    Chart.defaults.global.tooltipTemplate = "<%= value %> hours";
    Chart.defaults.global.responsive = true;

    var dataset = {
        fillColor: "rgba(151,187,205,0.5)",
        strokeColor: "rgba(151,187,205,0.8)",
        highlightFill: "rgba(151,187,205,0.75)",
        highlightStroke: "rgba(151,187,205,1)",
        data: [0, 0, 0, 0]
    };

    var playtimeChart = new Chart($("#playtimeChart").get(0).getContext("2d"))
                        .Bar({ labels: ["Current", "Main", "Extras", "Complete"], datasets: [dataset] });

    dataset.data = [0, 0, 0];
    var remainingChart = new Chart($("#remainingChart").get(0).getContext("2d"))
                        .Bar({ labels: ["Main", "Extras", "Complete"], datasets: [dataset] });

    self.total.subscribe(function(total) {
        playtimeChart.datasets[0].bars[0].value = getHours(total.playTime, 0);
        playtimeChart.datasets[0].bars[1].value = getHours(total.mainTtb, 0);
        playtimeChart.datasets[0].bars[2].value = getHours(total.extrasTtb, 0);
        playtimeChart.datasets[0].bars[3].value = getHours(total.completionistTtb, 0);
        playtimeChart.update();

        remainingChart.datasets[0].bars[0].value = getHours(total.mainRemaining, 0);
        remainingChart.datasets[0].bars[1].value = getHours(total.extrasRemaining, 0);
        remainingChart.datasets[0].bars[2].value = getHours(total.completionistRemaining, 0);
        remainingChart.update();
    });

    var scrollDuration = 1000;
    var scrollToAlerts = function() {
        $('html, body').animate({
            scrollTop: $("#alertContainer").offset().top - 10
        }, scrollDuration);
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

    self.loadGames = function () {

        if (self.currentRequest !== undefined) {
            self.currentRequest.abort(); //in case of hash tag navigation while we're loading
        }

        $('.loader').spin({ lines: 12, length: 35, width: 8, radius: 50 });

        if (self.steamVanityUrlName().length === 0) {
            self.badSteamVanityUrlName(true);
            self.error("Please specify your Steam Profile ID");
            return;
        } else {
            self.error(null);
        }

        self.processing(true);
        self.partialCache(false);
        self.imputedTtbs(false);
        self.missingHltbIds(false);
        self.alertHidden(true);
        self.errorAlertHidden(false);
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

                self.alertHidden(false);
                scrollToAlerts();
            })
            .fail(function(error) {
                console.error(error); //TODO replace console print with user error display
                self.gameTable.rows([]);
                self.error('verify your Steam profile ID as it appears in your Steam profile page URL (<i>steamcommunity.com/id/<strong>ID</strong></i>) and make sure it is set to public in your Steam profile settings');
            })
            .always(function () {
                self.processing(false);
                $('.loader').spin(false);
            });
    };

    self.updateHltb = function (game) {
        game.hltbName("Updating...");
        game.updatePhase(GameUpdatePhase.InProgress);
        $.post("api/games/update/" + game.steamAppId + "/" + game.hltbId())
            .done(function() {
                game.hltbName("Update submitted for approval, please check back later");
                game.updatePhase(GameUpdatePhase.Success);
            })
            .fail(function(error) {
                console.error(error);
                game.hltbName("Update failed, please verify the HLTB ID");
                game.updatePhase(GameUpdatePhase.Failure);
            });
    };

    self.allowUpdate = function(game) { //defined on view model to avoid multiple definitions (one for each game in array)
        return (game.updatePhase() === GameUpdatePhase.None) || (game.updatePhase() === GameUpdatePhase.Failure);
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
            $.backstretch("http://res2.windows.microsoft.com/resbox/en/windows%207/main/b1697ff2-4fef-4125-a4c4-f3dcaa68a0aa_12.jpg");
        });

        this.get('#/:vanityUrlName', function () {
            viewModel.introPage(false);
            if ($(document.body).data('backstretch') !== undefined) {
                $.backstretch("destroy", false);
            }

            viewModel.steamVanityUrlName(this.params.vanityUrlName);
            viewModel.loadGames();
        });
    });

    sammyApp.run("#/");
});