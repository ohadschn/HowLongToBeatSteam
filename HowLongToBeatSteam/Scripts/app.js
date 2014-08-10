function AppViewModel() {
    var self = this;

    self.steamId = ko.observable();

    self.games = ko.observableArray();

    this.howlongClicked = function () {

        $.get("api/games/" + self.steamId())
            .done(function(data) {
                self.games(data);
            })
            .fail(function(error) {
                self.games({ SteamAppId: error, SteamName: error, Playtime: error });
        });
    };
}

$(document).ready(function () {
    ko.applyBindings(new AppViewModel());
});