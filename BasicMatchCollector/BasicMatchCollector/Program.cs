using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;

namespace BasicMatchCollector
{
    class Program
    {
        static string API_KEY = "api_key=RGAPI-c9cdafd4-cff6-4b1a-a204-b6a25d783181";

        static void Main(string[] args)
        {
            StartTheThread("BR");
            StartTheThread("EUNE");
            StartTheThread("EUW");
            StartTheThread("JP");
            StartTheThread("KR");
            StartTheThread("LAN");
            StartTheThread("LAS");
            StartTheThread("NA");
            StartTheThread("OCE");
            StartTheThread("TR");
            StartTheThread("RU");
        }

        public static Thread StartTheThread(string _region)
        {
            var t = new Thread(() => FetchDataByRegion(_region));
            t.Start();
            return t;
        }

        public static void FetchDataByRegion(string _region)
        {
            Console.WriteLine(_region + ": " + "Started.");
            DateTime timestamp = DateTime.Now;
            bool error = false;
            string jsonString = "";

            while (true)
            { 
                using (WebClient wc = new WebClient())
            {
                checkTimer(ref timestamp);

                try
                {
                    Console.WriteLine(_region + ": " + "Fetching challengerList");
                    jsonString = wc.DownloadString("https://" + _region + ".api.riotgames.com/api/lol/" + _region + "/v2.5/league/challenger?type=RANKED_SOLO_5x5&" + API_KEY);
                    error = false;
                }
                catch
                {
                    Console.WriteLine(_region + ": " + "Problem fetching challengerList");
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
                                Console.WriteLine(_region + ": " + "Fetching matchList");
                                jsonString = wc.DownloadString("https://" + _region + ".api.riotgames.com/api/lol/" + _region + "/v2.2/matchlist/by-summoner/" + entry.playerOrTeamId + "?rankedQueues=TEAM_BUILDER_RANKED_SOLO&beginTime=1480000000000&" + API_KEY);
                                error = false;
                            }
                            catch
                            {
                                Console.WriteLine(_region + ": " + "Problem fetching matchList");
                                error = true;
                            }
                            if (!error && (jsonString != "{\"startIndex\":0,\"endIndex\":0,\"totalGames\":0}"))
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
                                                Console.WriteLine(_region + ": " + "Fetching matchDetail");
                                                jsonString = wc.DownloadString("https://" + _region + ".api.riotgames.com/api/lol/" + _region + "/v2.2/match/" + matchId + "?" + API_KEY);
                                                error = false;
                                            }
                                            catch
                                            {
                                                Console.WriteLine(_region + ": " + "Problem fetching matchDetail");
                                                error = true;
                                            }
                                            if (!error)
                                            {
                                                Console.WriteLine(_region + ": " + "Converting and inserting new BasicMatchDetails for match " + matchId);
                                                dynamic matchDetail = JsonConvert.DeserializeObject(jsonString);
                                                long matchVersion = Convert.ToInt64(matchDetail.matchVersion.ToString().Replace(".", ""));

                                                BasicMatchDetails newMatch = new BasicMatchDetails()
                                                {
                                                    MatchId = matchDetail.matchId,
                                                    MatchVersion = matchVersion,
                                                    Region = matchDetail.region,
                                                    Winner = matchDetail.teams[0].winner,
                                                };
                                                if (!addChampIds(ref newMatch, matchDetail))
                                                    newMatch.MatchVersion = null;
                                                Console.WriteLine(_region + ": " + matchId + " added");
                                                db.Matches.Add(newMatch);
                                                db.SaveChanges();
                                            }
                                        }
                                        else { Console.WriteLine(_region + ": " + matchId + " already in db"); }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Console.WriteLine("Finished.");
            //Console.ReadLine();
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
                        {
                            if (_newMatch.BlueTopChampId == 0)
                                _newMatch.BlueTopChampId = participant.championId;
                            else
                                return false;
                        }
                        else
                        {
                            if (_newMatch.PurpleTopChampId == 0)
                                _newMatch.PurpleTopChampId = participant.championId;
                            else
                                return false;
                        }
                        break;
                    case "JUNGLE":
                        if (participant.teamId == 100)
                        {
                            if (_newMatch.BlueJungleChampId == 0)
                                _newMatch.BlueJungleChampId = participant.championId;
                            else
                                return false;
                        }
                        else
                        {
                            if (_newMatch.PurpleJungleChampId == 0)
                                _newMatch.PurpleJungleChampId = participant.championId;
                            else
                                return false;
                        }
                        break;
                    case "MIDDLE":
                        if (participant.teamId == 100)
                        {
                            if (_newMatch.BlueMiddleChampId == 0)
                                _newMatch.BlueMiddleChampId = participant.championId;
                            else
                                return false;
                        }
                        else
                        {
                            if (_newMatch.PurpleMiddleChampId == 0)
                                _newMatch.PurpleMiddleChampId = participant.championId;
                            else
                                return false;
                        }
                        break;
                    case "BOTTOM":
                        if (participant.timeline.role == "DUO_CARRY")
                        {
                            if (participant.teamId == 100)
                            {
                                if (_newMatch.BlueCarryChampId == 0)
                                    _newMatch.BlueCarryChampId = participant.championId;
                                else
                                    return false;
                            }
                            else
                            {
                                if (_newMatch.PurpleCarryChampId == 0)
                                    _newMatch.PurpleCarryChampId = participant.championId;
                                else
                                    return false;
                            }
                        }
                        else if(participant.timeline.role == "DUO_SUPPORT")
                        {
                            if (participant.teamId == 100)
                            {
                                if (_newMatch.BlueSupportChampId == 0)
                                    _newMatch.BlueSupportChampId = participant.championId;
                                else
                                    return false;
                            }
                            else
                            {
                                if (_newMatch.PurpleSupportChampId == 0)
                                    _newMatch.PurpleSupportChampId = participant.championId;
                                else
                                    return false;
                            }
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
