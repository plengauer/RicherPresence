using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AOE2DERicherPresenceManager : RicherPresenceManager
{

    private enum State
    {
        MAIN_MENU,
        CREATING_GAME,
        PLAYING,
    }

    private string? name;

    private State? state;

    private bool ranked;
    private readonly FuzzyEnum victory;
    private readonly FuzzyEnum map;
    private readonly FuzzyEnum players;
    private readonly FuzzyEnum humans;
    private readonly FuzzyEnum difficulty;

    private readonly FuzzyEnum civilization;

    private long? time;

    public AOE2DERicherPresenceManager(Screen screen, OCR ocr, int sleepTime, bool limitQueues = true, bool deleteCaptures = true) : base("Age of Empires II: Definitive Edition", "AoE2DE_s", screen, ocr, sleepTime, limitQueues, deleteCaptures)
    {
        name = null;
        state = null;
        ranked = false;
        victory = new FuzzyEnum("Standard", "Conquest", "Score", "Time Limit", "Last Man Standing"); 
        map = new FuzzyEnum(
            "Random", "Custom Map Pool",
            // random maps
            "Acclivity", "Acropolis", "African Clearing", "Aftermath", "Alpine Lakes", "Amazon Tunnel",
            "Arabia", "Archipelago", "Arena", "Atacama", "Baltic", "Black Forest",
            "Bog Island", "Bogland", "Budapest", "Cenotes", "City of Lakes", "Coastal",
            "Coastal Forest", "Continental", "Crater", "Crater Lake", "Crossroads", "Enclosed",
            "Eruption", "Fortress", "Four Lakes", "Frigid Lake", "Ghost Lake", "Gold Rush",
            "Golden Pit", "Golden Swamp", "Greenland", "Haboob", "Hamburger", "Hideout",
            "Highland", "Hill Fort", "Islands", "Kawasan", "Kilimanjaro", "Land Madness",
            "Land Nomad", "Lombardia", "Lowland", "Mangrove Jungle", "Marketplace", "Meadow",
            "Mediterranean", "MegaRandom", "Michi", "Migration", "Mongolia", "Mountain Pass",
            "Mountain Range", "Mountain Ridge", "Nile Delta", "Nomad", "Northern Isles", "Oasis",
            "Pacific Islands", "Ravines", "Ring Fortress", "Rivers", "Runestones", "Sacred Springs",
            "Salt Marsh", "Sandbank", "Scandinavia", "Seize The Mountain", "Serengeti", "Socotra",
            "Steppe", "Team Islands", "Team Moats", "Valley", "Volcanic Island", "Wade",
            "Water Nomad", "Wolf Hill", "Yucatan",
            // real world
            "Amazon", "Antartica", "Aral Sea", "Australia", "Black Sea", "Bohemia",
            "Britain", "Byzantium", "Caucasus", "Central America", "China", "Earth",
            "France", "Horn of Africa", "Iberia", "India", "Indochina", "Indonesia",
            "Italy", "Madagascar", "Mideast", "Norse Lands", "Philippines", "Sea of Japan (East Sea)",
            "Siberia", "Strait for Malecca", "Texas", "West Africa",
            // special maps
            "Border Stones", "Canyons", "Enemy Archipelago", "Enemy Islands", "Far Out", "Front Line",
            "Holy Line", "Inner Circle", "Journey South", "Jungle Islands", "Jungle Lanes", "Motherland",
            "Open Plains", "Ring of Water", "Snake Forest", "Snakepit", "Sprawling Streams", "Swirling River",
            "The Eye", "Twin Forests", "Yin Yang"
        );
        players = new FuzzyEnum("1", "2", "3", "4", "5", "6", "7", "8");
        humans = new FuzzyEnum("1", "2", "3", "4", "5", "6", "7", "8");
        difficulty = new FuzzyEnum("Easiest", "Standard", "Moderate", "Hard", "Hardest", "Extreme");
        civilization = new FuzzyEnum(
            "Random", "Full Random", "Mirror",
            "Britons", "Byzantines", "Celts", "Chinese", "Franks", "Goths", "Japanese", "Mongols", "Persians", "Saracens", "Teutons", "Turks", "Vikings",
            "Aztecs", "Huns", "Koreans", "Mayans", "Spanish",
            "Hindustians", "Incas", "Italians", "Magyars", "Slavs",
            "Berbers", "Ethiopians", "Malians", "Portuguese",
            "Burmese", "Khmer", "Malay", "Vietnamese",
            "Bulgarians", "Cumans", "Lithuanians", "Tatas",
            "Burgundians", "Sicilians",
            "Bohemians", "Poles",
            "Bengalis", "Dravidians", "Gurjaras"
        );
        time = null;
    }

    protected override Activity CreateInitialActivity()
    {
        return CreateActivity();
    }

    protected override IRichPresence CreateRichPresence()
    {
        return new RichPresence(980440522193776690, 0);
    }

    protected override Activity? ParseActivity(string text)
    {
        if (text.FuzzyContains("SINGLE PLAYER", 0.9) && text.FuzzyContains("MULTIPLAYER", 0.9) && text.FuzzyContains("EXIT", 0.9))
        {
            // main menu
            state = State.MAIN_MENU;
            name = text.Split('\n')[0];
            ranked = false;
            victory.Reset();
            map.Reset();
            players.Reset();
            humans.Reset();
            difficulty.Reset();
            civilization.Reset();
            time = null;
        }
        else if (text.FuzzyContains("Standard Game", 0.9) && text.FuzzyContains("Player", 0.9) && text.FuzzyContains("Civilization", 0.9) && text.FuzzyContains("Team", 0.9) && text.FuzzyContains("Game Settings", 0.9) && text.FuzzyContains("Start Game", 0.9))
        {
            // single player create game screen
            state = State.CREATING_GAME;
            ranked = false;
            victory.Parse(SearchAndParse(text, "Victory", ":"));
            map.Parse(SearchAndParse(text, "Location", ":"));
            players.Parse(SearchAndParse(text, "Players", " "));
            // humans.Parse("1"); // TODO
            difficulty.Parse(SearchAndParse(text, "AI Difficulty", ":"));
            if (name != null) {
                string[] lines = text.Split('\n');
                int index = 0;
                while (index < lines.Length && !lines[index].FuzzyContains(name, 0.9)) index++;
                if (index < lines.Length)
                {
                    (int i, int l) = lines[index].FuzzyIndexOf(name, 0.9);
                    civilization.Parse(lines[index].Substring(i + l + 1).Trim()); // we could tokenize here on first word after the name, but do we need to? we just need "best" match?
                }
            }
        }
        else if (text.FuzzyContains("Lobby", 0.9) && text.FuzzyContains("Player", 0.9) && text.FuzzyContains("Civilization", 0.9) && text.FuzzyContains("Team", 0.9) && text.FuzzyContains("Game Settings", 0.9) && text.FuzzyContains("Start Game", 0.9))
        {
            // multiplayer lobby screen
            state = State.CREATING_GAME;
            ranked = text.FuzzyContains("Ranked", 0.9) && !text.FuzzyContains("Unranked", 0.9); map.Parse(SearchAndParse(text, "Location", ":"));
            players.Parse(SearchAndParse(text, "Players", " "));
            // humans.Parse("1"); // TODO
            difficulty.Parse(SearchAndParse(text, "AI Difficulty", ":"));
            if (name != null)
            {
                string[] lines = text.Split('\n');
                int index = 0;
                while (index < lines.Length && !lines[index].FuzzyContains(name, 0.9)) index++;
                if (index < lines.Length)
                {
                    (int i, int l) = lines[index].FuzzyIndexOf(name, 0.9);
                    civilization.Parse(lines[index].Substring(i + l + 1).Trim()); // we could tokenize here on first word after the name, but do we need to? we just need "best" match?
                }
            }
        }
        else if (text.FuzzyContains("GAME MODE: ", 0.9) && text.FuzzyContains("ADVANCED SETTINGS", 0.9) && text.FuzzyContains("Tip", 0.9))
        {
            if (time == null) civilization.Reset();
            if (time == null) map.Reset();
            // loading screen
            {
                string[] lines = text.Split('\n');
                int index = 0;
                while (index < lines.Length && !lines[index].FuzzyContains("GAME MODE: ", 0.9)) index++;
                while (index < lines.Length && lines[index].Trim().Length == 0) index++;
                if (index < lines.Length) map.Parse(lines[index].Trim());
            }
            if (name != null)
            {
                string[] lines = text.Split('\n');
                int index = 0;
                while (index < lines.Length && !lines[index].FuzzyContains(name, 0.9)) index++;
                while (index < lines.Length && lines[index].Trim().Length == 0) index++;
                if (index < lines.Length) civilization.Parse(lines[index].Trim()); // we could tokenize here on first word, but do we need to? we just need "best" match?
            }
            if (time == null) time = Environment.TickCount64;
        }
        else if (text.FuzzyContains("LEAVE MAP", 0.9) && text.FuzzyContains("RETURN TO MAP", 0.9))
        {
            // post game screen
            state = State.MAIN_MENU;
            ranked = false;
            map.Reset();
            players.Reset();
            humans.Reset();
            difficulty.Reset();
            civilization.Reset();
            time = null;
        }
        return CreateActivity();
    }

    private string? SearchAndParse(string haystack, string key, string separator)
    {
        foreach (string line in haystack.Split('\n'))
        {
            if (!line.FuzzyContains(key, 0.9)) continue;
            (int index, int length) = line.FuzzyIndexOf(key, 0.9);
            while (index < line.Length && !(char.IsLetterOrDigit(line[index]) || char.IsWhiteSpace(line[index]))) index++;
            if (index < 0) return null;
            return line.Substring(index + length).Trim();

        }
        return null;
    }

    private Activity CreateActivity()
    {
        Activity activity = new Activity();
        activity.Name = "Age of Empires II: Definitive Edition";
        switch (state)
        {
            case State.MAIN_MENU:
                break;
            case State.CREATING_GAME:
            case State.PLAYING:
                activity.Details = ""
                    + (victory.IsValid() ? victory + (ranked ? " (ranked)" : "") : "")
                    + (civilization.IsValid() ? (victory.IsValid() ? " with " : "") + civilization : "")
                    + (map.IsValid() ? (civilization.IsValid() ? " on " : "") + map : "")
                    + (
                        (victory.IsValid() || civilization.IsValid() || map.IsValid()) && players.IsValid()
                            ? 
                                " (" + players + " Players"
                                    + (humans.IsValid() && int.Parse(humans.ToString()) < int.Parse(players.ToString())
                                        ? " and " + (int.Parse(players.ToString()) - int.Parse(humans.ToString())) + (difficulty.IsValid() ? " " + difficulty : "") + " AIs"
                                        : "")
                                + ")"
                            : ""
                    );
                if (state == State.CREATING_GAME) activity.State = "In Lobby";
                else activity.State = "In Game";
                if (time != null) activity.Timestamps.Start = time.Value;
                break;
            case null:
                break;
        }
        return activity;
    }
}
