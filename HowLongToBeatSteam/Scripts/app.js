/*global ko*/

function Game(steamId, steamName, steamPlaytime, hltbId) {
    var self = this;

    self.known = function() {
        return self.hltbId() !== -1;
    };

    self.steamAppId = steamId;
    self.steamName = steamName;
    self.steamPlaytime = steamPlaytime;
    self.hltbId = ko.observable(hltbId);
    var pending = "Pending...";
    var unknown = "Unknown";
    self.timeToBeat = {
        main: ko.observable(self.known() ? pending : unknown),
        extras: ko.observable(self.known() ? pending : unknown),
        completionist: ko.observable(self.known() ? pending : unknown),
        combined: ko.observable(self.known() ? pending : unknown),
    };
    self.hltbUrl = "http://www.howlongtobeat.com/search.php?t=games&s=" + steamName;
    self.visible = ko.computed(function () {
        return !self.known();
    });
}

function AppViewModel() {
    var self = this;

    self.steamId64 = ko.observable("76561198079151088"); //TODO remove ID
    self.games = ko.observableArray();
    self.processing = ko.observable(false);

    self.howlongClicked = function() {
        self.processing(true);
        self.games([]);
        $.get("api/games/library/" + self.steamId64())
            .done(function(data) {
                self.games(ko.utils.arrayMap(data, function(steamGame) {
                    return new Game(steamGame.SteamAppId, steamGame.SteamName, steamGame.Playtime, steamGame.HltbId);
                }));
            })
            .fail(function(error) {
                console.error(error);
                var msg = "Error - verify your Steam64Id and try again";
                self.games({ SteamAppId: msg, SteamName: msg, Playtime: msg });
            })
            .always(function() {
                self.processing(false);
            });
    };

    self.getHowLongToBeat = function (index) {
        var currentGame = self.games()[index];

        $.get("api/games/howlong/" + currentGame.hltbId)
            .done(function (data) {
                currentGame.timeToBeat.main(data.Main);
                currentGame.timeToBeat.extras(data.Extras);
                currentGame.timeToBeat.completionist(data.Completionist);
                currentGame.timeToBeat.combined(data.Combined);
            })
            .fail(function() {
                currentGame.timeToBeat.main("Error - verify HLTB ID and retry");
                currentGame.timeToBeat.extras("Error - verify HLTB ID and retry");
                currentGame.timeToBeat.completionist("Error - verify HLTB ID and retry");
                currentGame.timeToBeat.combined("Error - verify HLTB ID and retry");
            });
    };
}

$(document).ready(function () {
    ko.applyBindings(new AppViewModel());
});