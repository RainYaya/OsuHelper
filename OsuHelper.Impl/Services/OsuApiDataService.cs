﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OsuHelper.Models;
using Tyrrrz.Extensions;

namespace OsuHelper.Services
{
    public class OsuApiDataService : IDataService
    {
        private readonly ISettingsService _settingsService;
        private readonly IHttpService _httpService;

        public OsuApiDataService(ISettingsService settingsService, IHttpService httpService)
        {
            if (settingsService == null)
                throw new ArgumentNullException(nameof(settingsService));
            if (httpService == null)
                throw new ArgumentNullException(nameof(httpService));

            _settingsService = settingsService;
            _httpService = httpService;
        }

        public async Task<Beatmap> GetBeatmapAsync(GameMode gameMode, string beatmapId)
        {
            if (beatmapId == null)
                throw new ArgumentNullException(nameof(beatmapId));

            string apiRoot = _settingsService.OsuApiRoot.EnsureEndsWith("/");
            string apiKey = _settingsService.OsuApiKey;

            // Get
            string url = apiRoot + $"get_beatmaps?k={apiKey}&m={(int) gameMode}&b={beatmapId}&limit=1&a=1";
            string response = await _httpService.GetStringAsync(url);

            // Parse
            var parsed = JToken.Parse(response).First;

            // Extract data
            var result = new Beatmap();
            result.Id = parsed["beatmap_id"].Value<string>();
            result.MapSetId = parsed["beatmapset_id"].Value<string>();
            result.GameMode = gameMode;
            result.RankingStatus = (BeatmapRankingStatus) parsed["approved"].Value<int>();
            result.Creator = parsed["creator"].Value<string>();
            result.LastUpdate = parsed["last_update"].Value<DateTime>();
            result.Artist = parsed["artist"].Value<string>();
            result.Title = parsed["title"].Value<string>();
            result.DifficultyName = parsed["version"].Value<string>();
            result.Duration = TimeSpan.FromSeconds(parsed["hit_length"].Value<double>());
            result.MaxCombo = parsed["max_combo"].Value<int?>().GetValueOrDefault(); // can be null sometimes
            result.BeatsPerMinute = parsed["bpm"].Value<double>();
            result.Stars = parsed["difficultyrating"].Value<double>();
            result.ApproachRate = parsed["diff_approach"].Value<double>();
            result.OverallDifficulty = parsed["diff_overall"].Value<double>();
            result.CircleSize = parsed["diff_size"].Value<double>();
            result.Drain = parsed["diff_drain"].Value<double>();

            return result;
        }

        public async Task<IEnumerable<Play>> GetUserTopPlaysAsync(GameMode gameMode, string userId)
        {
            if (userId == null)
                throw new ArgumentNullException(nameof(userId));

            string apiRoot = _settingsService.OsuApiRoot.EnsureEndsWith("/");
            string apiKey = _settingsService.OsuApiKey;

            // Get
            string url = apiRoot + $"get_user_best?k={apiKey}&m={(int) gameMode}&u={userId.UrlEncode()}&limit=100";
            string response = await _httpService.GetStringAsync(url);

            // Parse
            var parsed = JToken.Parse(response);

            // Extract data
            var result = new List<Play>();
            foreach (var jPlay in parsed)
            {
                var play = new Play();
                play.PlayerId = jPlay["user_id"].Value<string>();
                play.BeatmapId = jPlay["beatmap_id"].Value<string>();
                play.Mods = (EnabledMods) jPlay["enabled_mods"].Value<int>();
                play.Rank = jPlay["rank"].Value<string>().ParseEnum<PlayRank>();
                play.MaxCombo = jPlay["maxcombo"].Value<int>();
                play.Count300 = jPlay["count300"].Value<int>();
                play.Count100 = jPlay["count100"].Value<int>();
                play.Count50 = jPlay["count50"].Value<int>();
                play.CountMiss = jPlay["countmiss"].Value<int>();
                play.PerformancePoints = jPlay["pp"].Value<double>();

                result.Add(play);
            }

            return result;
        }

        public async Task<IEnumerable<Play>> GetBeatmapTopPlaysAsync(GameMode gameMode, string beatmapId,
            EnabledMods enabledMods)
        {
            if (beatmapId == null)
                throw new ArgumentNullException(nameof(beatmapId));

            string apiRoot = _settingsService.OsuApiRoot.EnsureEndsWith("/");
            string apiKey = _settingsService.OsuApiKey;

            // Get
            string url = apiRoot + $"get_scores?k={apiKey}&m={(int) gameMode}&b={beatmapId}&limit=100";
            if (enabledMods != EnabledMods.Any) url += $"&mods={(int) enabledMods}";
            string response = await _httpService.GetStringAsync(url);

            // Parse
            var parsed = JToken.Parse(response);

            // Extract data
            var result = new List<Play>();
            foreach (var jPlay in parsed)
            {
                var play = new Play();
                play.PlayerId = jPlay["user_id"].Value<string>();
                play.BeatmapId = beatmapId;
                play.Mods = (EnabledMods) jPlay["enabled_mods"].Value<int>();
                play.Rank = jPlay["rank"].Value<string>().ParseEnum<PlayRank>();
                play.MaxCombo = jPlay["maxcombo"].Value<int>();
                play.Count300 = jPlay["count300"].Value<int>();
                play.Count100 = jPlay["count100"].Value<int>();
                play.Count50 = jPlay["count50"].Value<int>();
                play.CountMiss = jPlay["countmiss"].Value<int>();
                play.PerformancePoints = jPlay["pp"].Value<double>();

                result.Add(play);
            }

            return result;
        }
    }
}