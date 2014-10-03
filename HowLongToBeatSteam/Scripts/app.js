/*global ko*/

function Game(steamGame) {

    var self = this;

    self.known = steamGame.HltbInfo !== null;
    self.included = ko.observable(true);

    self.steamAppId = steamGame.SteamAppId;
    self.steamName = steamGame.SteamName;
    self.steamPlaytime = steamGame.Playtime;

    self.hltbInfo = {
        id: self.known ? steamGame.HltbInfo.Id : "",
        name: self.known ? steamGame.HltbInfo.Name : "Unknown, please update",
        mainTtb: self.known ? steamGame.HltbInfo.MainTtb : 0,
        extrasTtb: self.known ? steamGame.HltbInfo.ExtrasTtb : 0,
        completionistTtb: self.known ? steamGame.HltbInfo.CompletionistTtb : 0,
        combinedTtb: self.known ? steamGame.HltbInfo.CombinedTtb : 0,
        url: self.known 
            ? "http://www.howlongtobeat.com/game.php?id=" + steamGame.HltbInfo.Id
            : "http://www.howlongtobeat.com/search.php?t=games&s=" + self.steamName,
        linkText: self.known ? "Browse on HLTB" : "Find on HLTB",
    };
}

function AppViewModel() {
    var self = this;

    self.steamId64 = ko.observable("");
    self.badSteamId64 = ko.observable(false);

    $("#steamIdText").keydown(function() {
        self.badSteamId64(false);
    });

    self.games = ko.observableArray([
    new Game({
        "SteamAppId": 3830,
        "SteamName": "Psychonauts",
        "Playtime": 0,
        "HltbInfo": {
            "Id": 7372,
            "Name": "Psychonauts",
            "MainTtb": 750,
            "ExtrasTtb": 960,
            "CompletionistTtb": 1320,
            "CombinedTtb": 930
        }
    }),
    new Game({
        "SteamAppId": 228200,
        "SteamName": "Company of Heroes (New Steam Version)",
        "Playtime": 64,
        "HltbInfo": null
    })]);

    self.partialCache = ko.observable(false);
    self.processing = ko.observable(false);
    self.toggling = ko.observable("");
    self.error = ko.observable(null);

    self.missingIdsAlertHidden = ko.observable(false);
    self.partialCacheAlertHidden = ko.observable(false);
    self.errorAlertHidden = ko.observable(false);

    self.toggleAllChecked = ko.observable(true);
    self.toggleAllCore = function(include) {
        ko.utils.arrayForEach(self.games(), function(game) {
            if (game.inCache) {
                game.included(include);
            }
        });
    };
    self.toggleAll = function () {
        var include = !self.toggleAllChecked(); //binding is one way (workaround KO issue) so toggleAllChecked still has its old value
        if (self.games().length < 100) {
            self.toggleAllCore(include);
            return true;
        }

        $('#togglingModal').modal();
        setTimeout(function () {
            self.toggleAllCore(include);
            $('#togglingModal').modal("hide");
        }, 0);
        return false;
    };

    self.total = ko.computed(function () {

        var count = 0;
        var missingIdsCount = 0;
        var totalPlaytime = 0;
        var totalMain = 0;
        var totalExtras = 0;
        var totalCompletionist = 0;
        var totalCombined = 0;
        var mainRemaining = 0;
        var extrasRemaining = 0;
        var completionistRemaining = 0;
        var combinedRemaining = 0;
        var length = self.games().length;
        var arr = self.games();

        for (var i = 0; i < length; ++i) {
            var game = arr[i];
            missingIdsCount += !game.known;

            if (!game.included()) {
                continue;
            }

            count++;
            totalPlaytime += game.steamPlaytime;
            totalMain += game.hltbInfo.mainTtb;
            totalExtras += game.hltbInfo.extrasTtb;
            totalCompletionist += game.hltbInfo.completionistTtb;
            totalCombined += game.hltbInfo.combinedTtb;
            mainRemaining += Math.max(0, game.hltbInfo.mainTtb - game.steamPlaytime);
            extrasRemaining += Math.max(0, game.hltbInfo.extrasTtb - game.steamPlaytime);
            completionistRemaining += Math.max(0, game.hltbInfo.completionistTtb - game.steamPlaytime);
            combinedRemaining += Math.max(0, game.hltbInfo.combinedTtb - game.steamPlaytime);
        }

        if (count === length) {
            self.toggleAllChecked(true);
        } else if (count === 0) {
            self.toggleAllChecked(false);
        }

        return {
            count: count,
            missingIds: missingIdsCount > 0,
            totalPlaytime: totalPlaytime,
            totalMain: totalMain,
            totalExtras: totalExtras,
            totalCompletionist: totalCompletionist,
            totalCombined: totalCombined,
            mainRemaining: mainRemaining,
            extrasRemaining: extrasRemaining,
            completionistRemaining: completionistRemaining,
            combinedRemaining: combinedRemaining
        };
    });

    self.howlongClicked = function () {
        if (self.steamId64().length === 0 || /\D/.test(self.steamId64())) {
            self.badSteamId64(true);
            self.error("Your Steam64ID must be a 64-bit integer");
            return;
        } else {
            self.error(null);
        }

        self.processing(true);
        self.games([]);
        self.partialCache(false);
        self.missingIdsAlertHidden(false);
        self.partialCacheAlertHidden(false);
        self.errorAlertHidden(false);

        $.get("api/games/library/" + self.steamId64())
            .done(function (data) {
                self.partialCache(data.PartialCache);
                data.Games.sort(function (a, b) {
                    var aName = a.SteamName.toLowerCase();
                    var bName = b.SteamName.toLowerCase();
                    return ((aName < bName) ? -1 : ((aName > bName) ? 1 : 0));
                });
                self.games(ko.utils.arrayMap(data.Games, function (steamGame) {
                    return new Game(steamGame);
                }));
            })
            .fail(function (error) {
                console.error(error);
                self.error("Verify your Steam64Id and try again");
            })
            .always(function() {
                self.processing(false);
            });
    };

    self.updateHltb = function(game) {
        $.get("api/games/update/" + game.steamAppId + "?hltb=" + game.hltbInfo.id);
        alert(game.steamAppId + "-" + game.hltbInfo.id);
    };

    self.formatDuration = function(minutes) {
        minutes = Math.max(minutes, 0);
        var hours = Math.floor(minutes / 60);
        var mins = minutes % 60;
        return hours + "h " + mins + "m";
    };
}

$(document).ready(function () {

    //fix console for old browsers
    if (!window.console) { 
        var noOp = function () { };
        window.console = { log: noOp, warn: noOp, error: noOp };
    }

    ko.applyBindings(new AppViewModel());

    $('#remainingTableHeaderInfoSpan').tooltip();
    $('#steamIdText').focus();
});