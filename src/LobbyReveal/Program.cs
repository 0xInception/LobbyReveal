using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ekko;
using Newtonsoft.Json;

namespace LobbyReveal
{
    internal class Program
    {
        private static List<LobbyHandler> _handlers = new List<LobbyHandler>();
        private static bool _update = true;

        public async static Task Main(string[] args)
        {
            var watcher = new LeagueClientWatcher();
            watcher.OnLeagueClient += (clientWatcher, client) =>
            {
                Console.WriteLine(client.Pid);
                var handler = new LobbyHandler(new LeagueApi(client.ClientAuthInfo.RiotClientAuthToken,
                    client.ClientAuthInfo.RiotClientPort));
                _handlers.Add(handler);
                handler.OnUpdate += (lobbyHandler, names) => { _update = true; };
                handler.Start();
                _update = true;
            };
            new Thread(async () => { await watcher.Observe(); })
            {
                IsBackground = true
            }.Start();

            new Thread(() => { Refresh(); })
            {
                IsBackground = true
            }.Start();


            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input, out var i) || i > _handlers.Count || i < 1)
                {
                    Console.WriteLine("Invalid input.");
                    continue;
                }

                var region = _handlers[i - 1].GetRegion();

                Process.Start(
                    $"https://www.op.gg/multisearch/{region ?? Region.EUW}?summoners={string.Join(",", _handlers[i - 1].GetSummoners())}");

                _update = true;
            }

        }

        private static void Refresh()
        {
            while (true)
            {
                if (_update)
                {
                    Console.Clear();
                    for (int i = 0; i < _handlers.Count; i++)
                    {
                        Console.WriteLine("-------------------------------");
                        Console.WriteLine($"Client {i+1} (press {i+1} to open opgg)");
                        Console.WriteLine($"Current summoners: {string.Join(",", _handlers[i].GetSummoners())}");
                        Console.WriteLine($"Current summoners encoded: {HttpUtility.UrlEncode(string.Join(",", _handlers[i].GetSummoners()))}");
                        Console.WriteLine();
                    }
                    _update = false;
                }
                Thread.Sleep(2000);
            }
        }
    }
}