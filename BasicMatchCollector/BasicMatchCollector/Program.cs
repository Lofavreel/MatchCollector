using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BasicMatchCollector
{
    class Program
    {
        static string API_KEY = "api_key=RGAPI-c9cdafd4-cff6-4b1a-a204-b6a25d783181";
        static void Main(string[] args)
        {
            Console.WriteLine("Started.");
            DateTime timestamp = DateTime.Now;
            bool error = false;
            string jsonString = "";

            using (WebClient wc = new WebClient())
            {
                checkTimer(ref timestamp);

                try
                {
                    Console.WriteLine("Fetching challengerList");
                    jsonString = wc.DownloadString("https://euw.api.riotgames.com/api/lol/EUW/v2.5/league/challenger?type=RANKED_SOLO_5x5&" + API_KEY);
                    error = false;
                }
                catch
                {
                    Console.WriteLine("Problem fetching challengerList");
                    error = true;
                }
                if (!error)
                {
                    dynamic challengerList = JsonConvert.DeserializeObject(jsonString);

                    foreach (var entry in challengerList.entries)
                    {
                        checkTimer(ref timestamp);

                        try
                        {
                            Console.WriteLine("Fetching matchList");
                            jsonString = wc.DownloadString("https://euw.api.riotgames.com/api/lol/EUW/v2.2/matchlist/by-summoner/" + entry.playerOrTeamId + "?rankedQueues=TEAM_BUILDER_DRAFT_RANKED_5x5&seasons=SEASON2016&" + API_KEY);
                            error = false;
                        }
                        catch
                        {
                            Console.WriteLine("Problem fetching matchList");
                            error = true;
                        }
                        if (!error)
                        {
                            dynamic matchList = JsonConvert.DeserializeObject(jsonString);

                            foreach (var match in matchList.matches)
                            {
                                long matchId = match.matchId;

                                using (MatchesDbContext db = new MatchesDbContext())
                                {
                                    if (!(db.Matches.Any(m => m.MatchId == matchId)))
                                    {
                                        checkTimer(ref timestamp);
                                        try
                                        {
                                            Console.WriteLine("Fetching matchDetail");
                                            jsonString = wc.DownloadString("https://euw.api.riotgames.com/api/lol/EUW/v2.2/match/" + matchId + "?" + API_KEY);
                                            error = false;
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Problem fetching matchDetail");
                                            error = true;
                                        }
                                        if (!error)
                                        {
                                            Console.WriteLine("Converting and inserting new BasicMatchDetails for match " + matchId);
                                            dynamic matchDetail = JsonConvert.DeserializeObject(jsonString);

                                            BasicMatchDetails newMatch = new BasicMatchDetails()
                                            {
                                                MatchId = matchDetail.matchId,
                                                MatchVersion = matchDetail.matchVersion,
                                                Winner = matchDetail.teams[0].winner,
                                            };
                                            if (addChampIds(ref newMatch, matchDetail))
                                            {
                                                db.Matches.Add(newMatch);
                                                db.SaveChanges();
                                                Console.WriteLine(matchId + " added");
                                            }
                                            else { Console.WriteLine("Problem assigning champIds"); }
                                        }
                                    }
                                    else { Console.WriteLine(matchId + " already in db"); }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Finished.");
            Console.ReadLine();
        }

        public static void checkTimer(ref DateTime _timestamp)
        {
            TimeSpan timeDifference = DateTime.Now - _timestamp;
            if (timeDifference.TotalSeconds < 1.2)
                System.Threading.Thread.Sleep(1200 - (Int32)timeDifference.TotalMilliseconds);
            _timestamp = DateTime.Now;
        }

        public static bool addChampIds(ref BasicMatchDetails _newMatch, dynamic _matchDetail)
        {
            foreach (var participant in _matchDetail.participants)
            {
                string lane = participant.timeline.lane;
                switch (lane)
                {
                    case "TOP":
                        if (participant.teamId == 100)
                            _newMatch.BlueTopChampId = participant.championId;
                        else
                            _newMatch.PurpleTopChampId = participant.championId;
                        break;
                    case "JUNGLE":
                        if (participant.teamId == 100)
                            _newMatch.BlueJungleChampId = participant.championId;
                        else
                            _newMatch.PurpleJungleChampId = participant.championId;
                        break;
                    case "MIDDLE":
                        if (participant.teamId == 100)
                            _newMatch.BlueMiddleChampId = participant.championId;
                        else
                            _newMatch.PurpleMiddleChampId = participant.championId;
                        break;
                    case "BOTTOM":
                        if (participant.timeline.role == "DUO_CARRY")
                        {
                            if (participant.teamId == 100)
                                _newMatch.BlueCarryChampId = participant.championId;
                            else
                                _newMatch.PurpleCarryChampId = participant.championId;
                        }
                        else if(participant.timeline.role == "DUO_SUPPORT")
                        {
                            if (participant.teamId == 100)
                                _newMatch.BlueSupportChampId = participant.championId;
                            else
                                _newMatch.PurpleSupportChampId = participant.championId;
                        }
                        else
                            return false;
                        break;
                }
            }
            return true;
        }
    }
}
