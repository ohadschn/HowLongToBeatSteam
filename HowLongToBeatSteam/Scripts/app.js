/*global ko*/
/*global DataTable*/

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

function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)");
    var results = regex.exec(location.search);
    return results === null ? null : decodeURIComponent(results[1].replace(/\+/g, " "));
}

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

function AppViewModel(id) {
    var self = this;

    self.steamVanityUrlName = ko.observable(id);
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

    self.gameTable = new DataTable([], tableOptions);
    self.pageSizeOptions =  [10, 25, 50];

    self.partialCache = ko.observable(false);
    self.imputedTtbs = ko.observable(false);
    self.missingHltbIds = ko.observable(false);

    self.processing = ko.observable(false);
    self.error = ko.observable(null);

    self.alertHidden = ko.observable(false);
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

    Chart.defaults.global.tooltipTemplate = "<%= value %>";
    Chart.defaults.global.responsive = true;

    var dataset = {
        fillColor: "rgba(151,187,205,0.5)",
        strokeColor: "rgba(151,187,205,0.8)",
        highlightFill: "rgba(151,187,205,0.75)",
        highlightStroke: "rgba(151,187,205,1)",
        data: [0, 0, 0, 0]
    }

    var playtimeChart = new Chart($("#playtimeChart").get(0).getContext("2d"))
                        .Bar({ labels: ["Current", "Main", "Extras", "Complete"], datasets: [dataset] });

    dataset.data = [0, 0, 0];
    var remainingChart = new Chart($("#remainingChart").get(0).getContext("2d"))
                        .Bar({ labels: ["Main", "Extras", "Complete"], datasets: [dataset] });

    self.chartComputed = ko.computed(function() {
        playtimeChart.datasets[0].bars[0].value = getHours(self.total().playTime, 0);
        playtimeChart.datasets[0].bars[1].value = getHours(self.total().mainTtb, 0);
        playtimeChart.datasets[0].bars[2].value = getHours(self.total().extrasTtb, 0);
        playtimeChart.datasets[0].bars[3].value = getHours(self.total().completionistTtb, 0);
        playtimeChart.update();

        remainingChart.datasets[0].bars[0].value = getHours(self.total().mainRemaining, 0);
        remainingChart.datasets[0].bars[1].value = getHours(self.total().extrasRemaining, 0);
        remainingChart.datasets[0].bars[2].value = getHours(self.total().completionistRemaining, 0);
        remainingChart.update();
    });

    self.howlongClicked = function () {
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
        self.alertHidden(false);
        self.errorAlertHidden(false);

        $.get("api/games/library/" + self.steamVanityUrlName())
            .done(function(data) {
                self.partialCache(data.PartialCache);
                self.gameTable.rows(ko.utils.arrayMap(data.Games, function(steamGame) {
                    var game = new Game(steamGame);
                    if (!game.known) {
                        self.missingHltbIds(true);
                        self.imputedTtbs(true);
                    }
                    else if (game.hltbMainTtbImputed || game.hltbExtrasTtbImputed || game.hltbCompletionistTtbImputed) {
                        self.imputedTtbs(true);
                    }
                    return game;
                }));
                setTimeout(function() {
                    $('html, body').animate({
                        scrollTop: $("#chartContainer").offset().top
                    }, 2000);
                }, 0);
            })
            .fail(function(error) {
                console.error(error);
                self.gameTable.rows([]);
                self.error('Verify your Steam profile ID and make sure it is set to "public" in your Steam profile settings');
            })
            .always(function() {
                self.processing(false);
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

    if (!window.console) { //fix console for old browsers
        var noOp = function () { };
        window.console = { log: noOp, warn: noOp, error: noOp };
    }

    $('[data-toggle="tooltip"]').tooltip();
    $('#steamIdText').focus();

    var id = getParameterByName("id");
    ko.applyBindings(new AppViewModel(id === null ? "" : id));

    if (id !== null) {
        $('#submit').trigger('click');
    }
});