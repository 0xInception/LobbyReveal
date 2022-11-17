using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ekko;
using Newtonsoft.Json;

namespace LobbyReveal
{
    internal class Program
    {
        public static Dictionary<int, string[]> Participants { get; set; } = new Dictionary<int, string[]>();
        public static Dictionary<int, string[]> Cache { get; set; } = new Dictionary<int, string[]>();
        public static Dictionary<int, bool> Running { get; set; } = new Dictionary<int, bool>();
        public static bool LoopRunning { get; set; } = false;

        public async static Task Main(string[] args)
        {
            
            Console.Title = "Lobby Reveal - https://github.com/0xInception/LobbyReveal";
            var watcher = new LeagueClientWatcher();
            Console.ForegroundColor = ConsoleColor.White;
            watcher.OnLeagueClient += async (x, d) =>
            {
                Console.WriteLine($"[+] Client with id {d.Pid}");
                if (!LoopRunning)
                {
                    LoopRunning = true;
                    new Thread(new ThreadStart(() =>
                    {
                        while (true)
                        {
                            bool needRefresh = false;
                            foreach (var key in Participants.Keys.ToArray())
                            {
                                if (Participants.TryGetValue(key, out var v))
                                {
                                    if (Cache.TryGetValue(key, out var va))
                                    {
                                        if (!v.SequenceEqual(va))
                                        {
                                            Cache[key] = v.ToArray();
                                            needRefresh = true;
                                        }
                                    }
                                    else
                                    {
                                        Cache.Add(key,v);
                                        needRefresh = true;
                                    }
                                }
                            }
                           
                       

                            if (needRefresh)
                            {
                                needRefresh = false;
                                Console.Clear();
                                int client = 1;
                                foreach (var p in Cache)
                                {
                                    Console.WriteLine("-----------------------------------------");
                                    Console.WriteLine($"Client {client++}");
                                    Console.WriteLine($"Current summoners: {string.Join(",", p.Value)}");
                                    Console.WriteLine("");
                                }
                            }
                            Thread.Sleep(2000);
                        }

                    })).Start();
                }
                var api = new LeagueApi(d.ClientAuthInfo.RiotClientAuthToken, d.ClientAuthInfo.RiotClientPort);

                Running.Add(d.Pid,true);
                Participants.Add(d.Pid, Array.Empty<string>());
                while (Running[d.Pid])
                {
                    try
                    {
                        var result = await api.SendAsync(HttpMethod.Get, "/chat/v5/participants/champ-select");
                        if (string.IsNullOrWhiteSpace(result))
                            break;
                        var deserialized = JsonConvert.DeserializeObject<Participants>(result);
                        if (deserialized is null)
                            break;
                        Participants[d.Pid] =  deserialized.participants.Select(f => f.name).ToArray();

                        Thread.Sleep(2000);
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText("log.txt",ex+Environment.NewLine);
                    }
                }
            };
            watcher.OnLeagueClientExit += (clientWatcher, client) =>
            {
                Running[client.Pid] = false;
                Console.WriteLine($"Client {client} has exited...");
            }; 
            Console.WriteLine("Waiting for client...");
            await watcher.Observe();
        }
    }
}