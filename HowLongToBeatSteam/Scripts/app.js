/*global ko*/

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

    self.known = steamGame.HltbInfo !== null;
    self.included = ko.observable(true);
    self.updatePhase = ko.observable(GameUpdatePhase.None);

    self.steamAppId = steamGame.SteamAppId;
    self.steamName = steamGame.SteamName;
    self.steamPlaytime = steamGame.Playtime;

    self.hltbInfo = {
        id: self.known ? steamGame.HltbInfo.Id : "",
        name: ko.observable(self.known ? steamGame.HltbInfo.Name : "Unknown, please update"), //we'll abuse this for update status
        mainTtb: self.known ? steamGame.HltbInfo.MainTtb : 0,
        extrasTtb: self.known ? steamGame.HltbInfo.ExtrasTtb : 0,
        completionistTtb: self.known ? steamGame.HltbInfo.CompletionistTtb : 0,
        steamUrl: "http://store.steampowered.com/app/" + steamGame.SteamAppId,
        hltbUrl: self.known 
                ? "http://www.howlongtobeat.com/game.php?id=" + steamGame.HltbInfo.Id
                : "http://www.howlongtobeat.com",
    };
}

function AppViewModel(id) {
    var self = this;

    self.steamId64 = ko.observable(id);
    self.badSteamId64 = ko.observable(false);

    $("#steamIdText").keydown(function() {
        self.badSteamId64(false);
    });

    self.games = ko.observableArray();

    self.personaName = ko.observable(null);
    self.partialCache = ko.observable(false);
    self.processing = ko.observable(false);
    self.error = ko.observable(null);
    self.modelText = ko.observable('Working...');

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
        }, 50);
    };

    self.toggleAll = function () {
        var include = !self.toggleAllChecked(); //binding is one way (workaround KO issue) so toggleAllChecked still has its old value
        if (self.games().length < 100) {
            self.toggleAllCore(include);
            return true;
        }

        self.modelText("Toggling...");
        doModalWork(function () {
            self.toggleAllCore(include);
        });

        return false;
    };

    self.total = ko.pureComputed(function () {

        var count = 0;
        var missingIdsCount = 0;
        var totalPlaytime = 0;
        var totalMain = 0;
        var totalExtras = 0;
        var totalCompletionist = 0;
        var mainRemaining = 0;
        var extrasRemaining = 0;
        var completionistRemaining = 0;
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
            mainRemaining += Math.max(0, game.hltbInfo.mainTtb - game.steamPlaytime);
            extrasRemaining += Math.max(0, game.hltbInfo.extrasTtb - game.steamPlaytime);
            completionistRemaining += Math.max(0, game.hltbInfo.completionistTtb - game.steamPlaytime);
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
            mainRemaining: mainRemaining,
            extrasRemaining: extrasRemaining,
            completionistRemaining: completionistRemaining,
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

        var updateGames = function (games, total) {
            var toAdd = games.splice(0, 50);
            self.modelText(games.length === 0 ? "Generating game table..." : "Loading games " + (total - games.length) + " / " + total);

            if (toAdd.length === 0) {
                self.processing(false);
                $('#workingModal').modal("hide");
                return;
            }

            ko.utils.arrayPushAll(self.games, toAdd);
            setTimeout(function () {
                updateGames(games, total);
            }, 0);
        };
        
        self.modelText("Retrieving games...");
        $('#workingModal').modal();

        setTimeout(function() { //let modal kick in
            $.get("api/games/library/" + self.steamId64())
                .done(function(data) {
                    self.partialCache(data.PartialCache);
                    self.personaName(data.PersonaName);
                    data.Games.sort(function(a, b) {
                        var aName = a.SteamName.toLowerCase();
                        var bName = b.SteamName.toLowerCase();
                        return ((aName < bName) ? -1 : ((aName > bName) ? 1 : 0));
                    });
                    var games = ko.utils.arrayMap(data.Games, function(steamGame) { return new Game(steamGame); });
                    updateGames(games, games.length);
                })
                .fail(function(error) {
                    console.error(error);
                    self.error("Verify your Steam64Id and try again");
                    self.processing(false);
                });

            self.games([]); //do this after AJAX call has been made
        }, 0);
    };

    self.updateHltb = function (game) {
        game.hltbInfo.name("Updating...");
        game.updatePhase(GameUpdatePhase.InProgress);
        $.post("api/games/update/" + game.steamAppId + "/" + game.hltbInfo.id)
            .done(function() {
                game.hltbInfo.name("Update submitted for approval, please check back later");
                game.updatePhase(GameUpdatePhase.Success);
            })
            .fail(function(error) {
                console.error(error);
                game.hltbInfo.name("Update failed, please verify the HLTB ID");
                game.updatePhase(GameUpdatePhase.Failure);
            });
    };

    self.allowUpdate = function(game) { //defined on view model to avoid multiple definitions (one for each game in array)
        return (game.updatePhase() === GameUpdatePhase.None) || (game.updatePhase() === GameUpdatePhase.Failure);
    };
}

function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)");
    var results = regex.exec(location.search);
    return results === null ? null : decodeURIComponent(results[1].replace(/\+/g, " "));
}

$(document).ready(function () {

    if (!window.console) { //fix console for old browsers
        var noOp = function () { };
        window.console = { log: noOp, warn: noOp, error: noOp };
    }

    $('#remainingTableHeaderInfoSpan').tooltip();
    $('#steamIdText').focus();

    var id = getParameterByName("id");
    ko.applyBindings(new AppViewModel(id === null ? "" : id));

    if (id !== null) {
        $('#submit').trigger('click');
    }
});