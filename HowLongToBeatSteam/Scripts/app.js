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

Game.prototype.match = function(filter) {
    return this.steamName.toLowerCase().indexOf(filter.toLowerCase()) != -1;
}

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
        perPage: 50,
        unsortedClass: "glyphicon glyphicon-sort",
        ascSortClass: "glyphicon glyphicon-sort-by-attributes",
        descSortClass: "glyphicon glyphicon-sort-by-attributes-alt"
    };

    self.gameTable = new DataTable([], tableOptions);
    self.pageSizeOptions =  [25, 50, 100];

    self.partialCache = ko.observable(false);
    self.processing = ko.observable(false);
    self.error = ko.observable(null);
    self.modelText = ko.observable('Working...');

    self.missingIdsAlertHidden = ko.observable(false);
    self.partialCacheAlertHidden = ko.observable(false);
    self.errorAlertHidden = ko.observable(false);
    self.imputedAlertHidden = ko.observable(false);

    self.toggleAllChecked = ko.observable(true);
    self.toggleAllCore = function(include) {
        ko.utils.arrayForEach(self.gameTable.rows(), function(game) {
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
        if (self.gameTable.rows().length < 100) {
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
        var imputedCount = 0;
        var totalPlaytime = 0;
        var totalMain = 0;
        var totalExtras = 0;
        var totalCompletionist = 0;
        var mainRemaining = 0;
        var extrasRemaining = 0;
        var completionistRemaining = 0;
        var length = self.gameTable.filteredRows().length;
        var arr = self.gameTable.filteredRows();

        for (var i = 0; i < length; ++i) {
            var game = arr[i];
            missingIdsCount += !game.known;
            imputedCount += (game.hltbMainTtbImputed || game.hltbExtrasTtbImputed || game.hltbCompletionistTtbImputed) ? 1 : 0;

            if (!game.included()) {
                continue;
            }

            count++;
            totalPlaytime += game.steamPlaytime;
            totalMain += game.hltbMainTtb;
            totalExtras += game.hltbExtrasTtb;
            totalCompletionist += game.hltbCompletionistTtb;
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
            missingIds: missingIdsCount > 0,
            imputedCount: imputedCount,
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
        if (self.steamVanityUrlName().length === 0) {
            self.badSteamVanityUrlName(true);
            self.error("Please specify your Steam Profile ID");
            return;
        } else {
            self.error(null);
        }

        self.processing(true);
        self.partialCache(false);
        self.missingIdsAlertHidden(false);
        self.partialCacheAlertHidden(false);
        self.errorAlertHidden(false);
        self.imputedAlertHidden(false);

        var updateGames = function (games, total) {
            var toAdd = games.splice(0, 50);
            self.modelText(games.length === 0 ? "Generating game table..." : "Loading games " + (total - games.length) + " / " + total);

            if (toAdd.length === 0) {
                self.processing(false);
                $('#workingModal').modal("hide");
                return;
            }

            ko.utils.arrayPushAll(self.gameTable.rows, toAdd);
            setTimeout(function () {
                updateGames(games, total);
            }, 0);
        };
        
        self.modelText("Retrieving games...");
        $('#workingModal').modal();

        setTimeout(function() { //let modal kick in
            $.get("api/games/library/" + self.steamVanityUrlName())
                .done(function(data) {
                    self.partialCache(data.PartialCache);
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
                    self.error("Verify your Steam profile ID and make sure it is public in your Steam profile settings");
                    self.processing(false);
                    $('#workingModal').modal("hide");
                });

            self.gameTable.rows([]); //do this after AJAX call has been made
        }, 0);
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

    $('[data-toggle="tooltip"]').tooltip();
    $('#steamIdText').focus();

    var id = getParameterByName("id");
    ko.applyBindings(new AppViewModel(id === null ? "" : id));

    if (id !== null) {
        $('#submit').trigger('click');
    }
});