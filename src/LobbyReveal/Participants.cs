using System.Collections.Generic;

namespace LobbyReveal
{
    public class Participant
    {
        public object activePlatform { get; set; }
        public string cid { get; set; }
        public string game_name { get; set; }
        public string game_tag { get; set; }
        public bool muted { get; set; }
        public string name { get; set; }
        public string pid { get; set; }
        public string puuid { get; set; }
        public string region { get; set; }
    }

    public class Participants
    {
        public List<Participant>? participants { get; set; }
    }

    public class Userinfo
    {
        public string userinfo { get; set; }
    }
    public class Lol
    {
        public string cpid { get; set; }
    }

    public class UserinfoActual
    {
        public Lol lol { get; set; }
        
    }
}