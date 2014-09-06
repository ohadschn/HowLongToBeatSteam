/*global ko*/

function Game(steamGame) {
    var self = this;

    self.known = steamGame.HltbInfo.Id !== -1;

    self.steamAppId = steamGame.SteamAppId;
    self.steamName = steamGame.SteamName;
    self.steamPlaytime = steamGame.Playtime;

    var unknown = "Unknown";
    self.hltbInfo = {
        id: steamGame.HltbInfo.Id,
        name: steamGame.HltbInfo.Name,
        mainTtb: self.known ? steamGame.HltbInfo.MainTtb : unknown,
        extrasTtb: self.known ? steamGame.HltbInfo.ExtrasTtb : unknown,
        completionistTtb: self.known ? steamGame.HltbInfo.CompletionistTtb : unknown,
        combinedTtb: self.known ? steamGame.HltbInfo.CombinedTtb : unknown,
        url: self.known
        ? "http://www.howlongtobeat.com/game.php?id=" + steamGame.HltbInfo.Id
        : "http://www.howlongtobeat.com/search.php?t=games&s=" + self.steamName,
        linkText: self.known ? "Browse on HLTB" : "Find on HLTB",
    };
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
                    return new Game(steamGame);
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

    self.updateHltb = function (game) {
        $.get("api/games/update/" + game.steamAppId + "?hltb=" + game.hltbInfo.id);
        alert(game.steamAppId + "-" + game.hltbInfo.id);
    }
}

$(document).ready(function () {
    ko.applyBindings(new AppViewModel());
});