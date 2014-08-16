function Game(steamId, steamName, steamPlaytime, hltbId) {
    var self = this;

    self.steamId = steamId;
    self.steamName = steamName;
    self.steamPlaytime = steamPlaytime;
    self.hltbId = hltbId;
    self.timeToBeat = {
        main: ko.observable(0),
        extras: ko.observable(0),
        completionist: ko.observable(0),
        combined: ko.observable(0),
    };
    self.hltbUrl = "http://www.howlongtobeat.com/search.php?t=games&s=" + steamName;
    self.visible = ko.computed(function () {
        return self.hltbId === -1;
    });
}

function AppViewModel() {
    var self = this;

    self.steamId = ko.observable("76561198079151088"); //TODO remove ID
    self.games = ko.observableArray();

    this.howlongClicked = function() {

        $.get("api/games/library/" + self.steamId())
            .done(function(data) {
                self.games(ko.utils.arrayMap(data, function (steamGame) {
                    return new Game(steamGame.SteamAppId, steamGame.SteamName, steamGame.Playtime, steamGame.HltbId);
                }));
                self.getHowLongToBeat(0); //TODO remove
            })
            .fail(function(error) {
                console.error(error);
                var msg = "Error - verify your Steam64Id and try again";
                self.games({ SteamAppId: msg, SteamName: msg, Playtime: msg });
            });
    };

    this.getHowLongToBeat = function (index) {
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