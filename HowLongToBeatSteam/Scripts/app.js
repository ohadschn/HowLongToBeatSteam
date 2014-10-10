/*global ko*/

var trimNumber = function (number, digits) {
    if (digits === undefined) {
        digits = 2;
    }
    return +number.toFixed(digits);
};

var getHours = function (minutes, digits) { // jshint ignore:line
    var hours = Math.max(minutes, 0) / 60;
    return trimNumber(hours, digits);
};
var numberWithCommas = function (x) { // jshint ignore:line
    return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
};

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
    };
}

function AppViewModel(id) {
    var self = this;

    self.steamId64 = ko.observable(id);
    self.badSteamId64 = ko.observable(false);

    $("#steamIdText").keydown(function() {
        self.badSteamId64(false);
    });

    self.personaName = ko.observable(null);
    self.games = ko.observableArray();
    self.partialCache = ko.observable(false);
    self.processing = ko.observable(false);
    self.error = ko.observable(null);

    self.missingIdsAlertHidden = ko.observable(false);
    self.partialCacheAlertHidden = ko.observable(false);
    self.errorAlertHidden = ko.observable(false);

    self.toggleAllChecked = ko.observable(true);
    self.toggleAllCore = function(include) {
        ko.utils.arrayForEach(self.games(), function(game) {
            game.included(include);
        });
    };

    var doModalWork = function(func) {
        $('#workingModal').modal();
        setTimeout(function() {
            func();
            $('#workingModal').modal("hide");
        }, 0);
    };

    self.toggleAll = function () {
        var include = !self.toggleAllChecked(); //binding is one way (workaround KO issue) so toggleAllChecked still has its old value
        if (self.games().length < 100) {
            self.toggleAllCore(include);
            return true;
        }

        doModalWork(function() {
            self.toggleAllCore(include);
        });

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
            totalPlaytime: trimNumber(totalPlaytime),
            totalMain: trimNumber(totalMain),
            totalExtras: trimNumber(totalExtras),
            totalCompletionist: trimNumber(totalCompletionist),
            totalCombined: trimNumber(totalCombined),
            mainRemaining: trimNumber(mainRemaining),
            extrasRemaining: trimNumber(extrasRemaining),
            completionistRemaining: trimNumber(completionistRemaining),
            combinedRemaining: trimNumber(combinedRemaining)
        };
    });

    self.howlongClicked = function () {
        if (self.steamId64().length === 0 || !(/^\s*-?\d+\s*$/.test(self.steamId64()))) {
            self.badSteamId64(true);
            self.error("Your Steam64ID must be a 64-bit integer");
            return;
        } else {
            self.error(null);
        }

        self.processing(true);
        self.partialCache(false);
        self.missingIdsAlertHidden(false);
        self.partialCacheAlertHidden(false);
        self.errorAlertHidden(false);

        $.get("api/games/library/" + self.steamId64())
            .done(function (data) {
                self.partialCache(data.PartialCache);
                self.personaName(data.PersonaName);
                data.Games.sort(function(a, b) {
                    var aName = a.SteamName.toLowerCase();
                    var bName = b.SteamName.toLowerCase();
                    return ((aName < bName) ? -1 : ((aName > bName) ? 1 : 0));
                });
                var games = ko.utils.arrayMap(data.Games, function (steamGame) { return new Game(steamGame); });
                if (games.length < 100) {
                    self.games(games);
                } else {
                    doModalWork(function() {
                        self.games(games);
                    });
                }
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
}

function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)");
    var results = regex.exec(location.search);
    return results === null ? null : decodeURIComponent(results[1].replace(/\+/g, " "));
}

$(document).ready(function () {

    //fix console for old browsers
    if (!window.console) { 
        var noOp = function () { };
        window.console = { log: noOp, warn: noOp, error: noOp };
    }

    $('#remainingTableHeaderInfoSpan').tooltip();
    $('#steamIdText').focus();

    var id = getParameterByName("id");
    ko.applyBindings(new AppViewModel(id));

    if (id !== null) {
        $('#submit').trigger('click');
    }
});