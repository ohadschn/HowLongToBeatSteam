/*global ko*/

function Game(steamGame) {
    var self = this;
    
    if (steamGame.HltbInfo === null) {
        self.inCache = false;
        self.known = false;
    } else {
        self.inCache = true;
        self.known = steamGame.HltbInfo.Id !== -1;
    }

    self.steamAppId = steamGame.SteamAppId;
    self.steamName = steamGame.SteamName;
    self.steamPlaytime = steamGame.Playtime;

    self.hltbInfo = {
        id: self.inCache ? steamGame.HltbInfo.Id : -1,
        name: self.inCache ? steamGame.HltbInfo.Name : "Not in cache, try again later",
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

    self.games = ko.observableArray();
    self.processing = ko.observable(false);
    self.error = ko.observable(null);

    self.missingIds = ko.computed(function() {
        return ko.utils.arrayFirst(self.games(), function(game) {
            return !game.known;
        }) !== null;
    });

    self.notInCache = ko.computed(function() {
        return ko.utils.arrayFirst(self.games(), function(game) {
            return !game.inCache;
        }) !== null;
    });

    self.missingIdsAlertHidden = ko.observable(false);
    self.hideMissingIdsAlert = function() { self.missingIdsAlertHidden(true); };

    self.notInCacheAlertHidden = ko.observable(false);
    self.hideNotInCacheAlert = function() { self.notInCacheAlertHidden(true); };

    self.errorAlertHidden = ko.observable(false);
    self.hideErrorAlert = function() { self.errorAlertHidden(true); };

    self.total = ko.computed(function () {

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

        return {
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
        self.missingIdsAlertHidden(false);
        self.notInCacheAlertHidden(false);
        self.errorAlertHidden(false);

        $.get("api/games/library/" + self.steamId64())
            .done(function(data) {
                self.games(ko.utils.arrayMap(data, function(steamGame) {
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
    $('#steamIdText').focus();
});