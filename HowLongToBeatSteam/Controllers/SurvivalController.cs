﻿using System;
using System.Threading.Tasks;
using System.Web.Http;
using Common.Util;
using HowLongToBeatSteam.Controllers.Responses;
using HowLongToBeatSteam.Models;
using static System.FormattableString;

namespace HowLongToBeatSteam.Controllers
{
    public enum Gender
    {
        Male,
        Female
    }

    [RoutePrefix("api/survival")]
    public class SurvivalController : ApiController
    {
        private static readonly HttpRetryClient Client = new HttpRetryClient(0);

        [Route("life-expectancy/remaining/{country}/{gender}/{age:range(0,200)}")]
        public async Task<LifeExpectancy> GetRemainingLifeExpectancy(string country, Gender gender, int age)
        {
            var now = DateTime.UtcNow;
            var iso8601Date = now.ToString("yyyy-MM-dd");
            var sex = gender.ToString().ToLowerInvariant();
            using (var respone = await Client.GetAsync<RemainingLifeExpectancyResponse>(
                Invariant($"http://api.population.io:80/1.0/life-expectancy/remaining/{sex}/{country}/{iso8601Date}/{age}y")))
            {
                var remainingHours = now.AddYears(respone.Content.remaining_life_expectancy) - now;
                return new LifeExpectancy(remainingHours.TotalHours);
            }
        }
    }
}