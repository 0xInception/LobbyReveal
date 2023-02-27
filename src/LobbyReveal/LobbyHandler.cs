using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ekko;
using Newtonsoft.Json;

namespace LobbyReveal;

public enum Region
{
    UNKNOWN,
    EUW,
    EUNE,
    NA,
    TR,
    OCE,
    LAN,
    LAS,
    RU,
    BR,
    JP,
    SG,
    TW,
    TH,
    VN,
    PH
}

public enum Platform
{
    UNKNOWN,
    EUW1,
    EUN1,
    NA1,
    TR1,
    OC1,
    LA1,
    LA2,
    RU,
    BR1,
    JP1,
    SG2,
    TW2,
    TH2,
    VN2,
    PH2
}
public delegate void OnUpdate(LobbyHandler handler, string[] names);

public class LobbyHandler
{
    private readonly LeagueApi _api;
    private string[] _cache;
    private Region? _region;

    public LobbyHandler(LeagueApi api)
    {
        _api = api;
        _cache = Array.Empty<string>();
    }

    public OnUpdate OnUpdate;
    public EventHandler OnError;


    public void Start()
    {
        new Thread(async () => { await Loop(); })
        {
            IsBackground = true
        }.Start();
    }

    public string[] GetSummoners()
    {
        return _cache;
    }

    public Region? GetRegion()
    {
        return _region;
    }

    private async Task Loop()
    {
        while (true)
        {
            Thread.Sleep(2000);

            try
            {
                if (_region is null)
                {
                    var z = await _api.SendAsync(HttpMethod.Get, "/rso-auth/v1/authorization/userinfo");
                    if (string.IsNullOrWhiteSpace(z))
                        continue;

                    var resp1 = JsonConvert.DeserializeObject<Userinfo>(z);
                    if (resp1 is null)
                    {
                        continue;
                    }

                    var resp2 = JsonConvert.DeserializeObject<UserinfoActual>(resp1.userinfo);
                    if (resp2 is null)
                    {
                        continue;
                    }

                    var reg = Enum.TryParse(resp2.lol.cpid, out Platform region);
                    if (!reg)
                    {
                        Console.WriteLine("Could not figure out region. Setting EUW");
                        _region = Region.EUW;
                    }
                    else
                    {
                        _region = (Region)region;
                    }
                }

                var participants = await _api.SendAsync(HttpMethod.Get, "/chat/v5/participants/champ-select");
                if (string.IsNullOrWhiteSpace(participants))
                    continue;

                var participantsJson = JsonConvert.DeserializeObject<Participants>(participants);
                if (participantsJson?.participants is null)
                    continue;

                var names = participantsJson.participants.Select(x => x.name).ToArray();

                if (!_cache.SequenceEqual(names))
                {
                    _cache = names;
                    OnUpdate?.Invoke(this, names);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                OnError?.Invoke(this, EventArgs.Empty);
                break;
            }


        }

    }

}
