using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

public class RDR2Location
{

    private static ActivitySource ACTIVITIES = new ActivitySource(Observability.ACTIVITY_SOURCE_NAME);

    private Dictionary<string, double>  names = new Dictionary<string, double>();

    public RDR2Location()
    {
    }

    public string? Get()
    {
        string? result = null;
        foreach (string name in names.Keys)
        {
            if (result == null || names[name] > names[result]) result = name;
        }
        return result;
    }

    public void Reset()
    {
        names.Clear();
    }

    public double Parse(string? input, Type? hint, string? state = null, string? area = null, string? location = null)
    {
        using var s = ACTIVITIES.StartActivity("discord.rich_presence.rdr2.parse_location");
        s?.AddTag("rdr2.location.input", input);
        if (input == null) { return double.MinValue; }
        // if (input.ToLower().StartsWith("the")) input = input.Substring(3);
        while (0 < input.Length && !char.IsLetter(input[0])) input = input.Substring(1);
        while (0 < input.Length && !char.IsLetter(input[input.Length - 1])) input = input.Substring(0, input.Length - 1);
        (string name, double score) result = Lookup(input, hint, state, area, location);
        s?.AddTag("rdr2.location.output", result.name);
        s?.AddTag("rdr2.location.score", result.score);
        double score = 0;
        names.TryGetValue(result.name, out score);
        names[result.name] = score + result.score;
        return names[result.name];
    }

    internal static bool IsValid(string? state, string? area, string? location)
    {
        int si = state != null ? FindIndex(DICTIONARY_STATES, state) : -1;
        int ai = area != null ? FindIndex(DICTIONARY_AREAS, area) : -1;
        int li = location != null ? FindIndex(DICTIONARY_LOCATIONS, location) : -1;
        return (si < 0 || ai < 0 || si == ai)
            && (si < 0 || li < 0 || si == li)
            && (ai < 0 || li < 0 || ai == li);
    }

    public enum Type
    {
        LOCATION, AREA, STATE
    }

    private static string[][] DICTIONARY_STATES = new string[][]
    {
        new [] { "Lemoyne", "LE" },
        new [] { "New Hanover", "NH" } ,
        new [] { "Ambarino", "AM" },
        new [] { "West Elizabeth", "WE" },
        new [] { "New Austin", "NA" },
        // new [] { "Guarma", "GU" }
    };

    private static string[][] DICTIONARY_AREAS = new string[][]
    {
        new [] { "Bayou Nwa", "Bluewater Marsh",  "Scarlet Meadows" },
        new [] { "Heartlands", "Cumberland Forest", "Roanoke Ridge" },
        new [] { "Grizzlies", "Grizzlies East", "Grizzlies West" },
        new [] { "Big Valley", "Great Plains", "Tall Trees" },
        new [] { "Hannigan's Stead", "Cholla Springs", "Rio Bravo", "Gaptooth Ridge" },
        // new string[] {}
    };

    private static string[][] DICTIONARY_LOCATIONS = new string[][]
    {
        new [] { "Aberdeen Pig Farm", "Argil Rise", "Bayall Edge", "Braithwaite Manor", "Bolger Glade", "Caliga Hall", "Canebreak Manor", "Catfish Jacksons", "Clemens Cove", "Clemens Point", "Compson's Stead", "Copperhead Landing", "Crawdad Willies", "Dewberry Creek", "Eris Field", "Face Rock", "Fishing Spot", "Hagen Orchards", "Hill Haven Ranch", "Houseboat", "Lagras", "Lakay", "Lonnie's Shack", "Macomb's End", "Mattock Pond", "Merkins Waller", "Old Greenbank Mill", "Old Harry Fen", "Old Trail Rise", "Pleasance", "Prinz & Co.", "Radley's House", "Radley's Pasture", "Rhodes", "Ringneck Creek", "Robard Farm", "Saint Denis", "Shady Belle", "Siltwater Strand", "Sisika Penitentiary", "Southfield Flats", "Théâtre Râleur", "Fontana Theatre", "The Grand Korrigan", "Trapper's Cabin" },
        new [] { "Abandoned Trading Post", "Annesburg", "Bacchus Station", "Beaver Hollow", "Black Balsam Rise", "Brandywine Drop", "Butcher Creek", "Caliban's Seat", "Carmody Dell", "Castor's Ridge", "Chadwick Farm", "Citadel Rock", "Cornwall Kerosene & Tar", "Cumberland Falls", "Cumberland Forest", "Deer Cottage", "Doverhill", "Downes Ranch", "Elysian Pool", "Emerald Ranch", "Emerald Station", "Fire Lookout Tower", "Firwood Rise", "Flatneck Station", "Fort Brennand", "Fort Wallace", "Gill Landing", "Granger's Hoggery", "Guthrie Farm", "Hani's Bethel", "Heartland Oil Fields", "Heartland Overflow", "Horseshoe Overlook", "Huron Glen", "Larned Sod", "Limpany", "Lucky's Cabin", "MacLean's House", "Manito Glade", "Meteor House", "Mossy Flats", "Oil Derrick", "Osman Grove", "Reed Cottage", "Ridge View", "Roanoke Valley", "Six Point Cabin", "Sawbone Clearing", "Trading Post", "Twin Stack Pass", "Valentine", "Van Horn Mansion", "Van Horn Trading Post", "Willard's Rest" },
        new [] { "Adler Ranch", "Barrow Lagoon", "Beartooth Beck", "Cairn Lake", "Cairn Lodge", "Calumet Ravine", "Cattail Pond", "Chez Porter", "Clawson's Rest", "Colter", "Cotorra Springs", "Deadboot Creek", "Dodd's Bluff", "Donner Falls", "Dormin Crest", "Ewing Basin", "Fairvale Shanty", "Flattened Cabin", "Glacier", "Granite Pass", "Lake Isabella", "Martha's Swain", "Micah's Hideout", "Millesani Claim", "Moonstone Pond", "Mount Hagen", "Mysterious Hill Home", "O'Creagh's Run", "Planters Baun", "Spider Gorge", "Tempest Rim", "The Loft", "Three Sisters", "Wapiti", "Wapiti Indian Reservation", "Whinyard Strait", "Window Rock", "Witches Cauldron", "Veteran's Homestead" },
        new [] { "Appleseed Timber Company", "Aurora Basin", "Bear Claw", "Beecher's Hope", "Beryl's Dream", "Black Bone Forest", "Blackwater", "Broken Tree", "Cochinay", "Diablo Ridge", "Evelyn Miller Camp", "Fort Riggs", "Hanging Dog Ranch", "Hawks Eye Creek", "Lenora View", "Little Creek River", "Lone Mule Stead", "Manzanita Post", "Montana Ford", "Monto's Rest", "Mount Shann", "Nekoti Rock", "Old Tom's Blind", "Owanjila", "Owanjila Dam", "Painted Sky", "Pronghorn Ranch", "Quaker's Cove", "Riggs Station", "Shepherds Rise", "Stilt Shack", "Strawberry", "Swadbass Point", "Tanner's Reach", "Taxidermist House", "Valley View", "Vetter's Echo", "Wallace Overlook", "Wallace Station", "Watson's Cabin" },
        new [] { "Armadillo", "Benedict Pass", "Benedict Point", "Brittlebrush Trawl", "Coot's Chapel", "Critchley's Ranch", "Cueva Seca", "Dixon Crossing", "Fort Mercer", "Gaptooth Breach", "Greenhollow", "Hamlin's Passing", "Hanging Rock", "Hennigan's Stead", "Jorge's Gap", "Lake Don Julio", "MacFarlane's Ranch", "Manteca Falls", "Mercer Station", "Mescalero", "Odd Fellow's Rest", "Old Bacchus Place", "Pike's Basin", "Plainview", "Pleasance House", "Rathskeller Fork", "Rattlesnake Hollow", "Repentance", "Ridgewood Farm", "Riley's Charge", "Río del Lobo", "Río del Lobo Rock", "Scratching Post", "Silent Stead", "Solomon's Folly", "Stillwater Creek", "Thieves' Landing", "Tumbleweed", "Twin Rocks", "Two Crows", "Venter's Place", "Warthington Ranch" },
        // new [] { "Aguasdulces", "Arroyo de la Vibora", "Bahía de la Paz", "Cinco Torres", "El Nido", "La Capilla", "Manicato" },
        new [] {
            "Bacchus Bridge", "Bard's Crossing", "Dakota River", "Dixon Crossing", "Flat Iron Lake", "Kamassa River", "Lannahechee River", "Lower Montana River", "Manteca Falls", "Montana Ford", "Redemption Mountains", "San Luis River", "Sea of Coronado", "Upper Montana River",
            "the shack", "the ranch", "the homestead"
        }
    };

    private static (string, double) Lookup(string name, Type? type = null, string? state = null, string? area = null, string? location = null)
    {
        (string name, double score) result = Lookup0(name, type, state, area, location);
        if (result.name.Length == 2 && result.name.ToUpper().Equals(result.name))
        {
            int index = FindIndex(DICTIONARY_STATES, result.name);
            if (index < 0) return result;
            result.name = DICTIONARY_STATES[index][0];
        }
        return result;
    }

    private static (string, double) Lookup0(string name, Type? type = null, string? state = null, string? area = null, string? location = null)
    {
        switch (type)
        {
            case Type.STATE: return Lookup(GetStatesDictionary(area, location), name);
            case Type.AREA: return Lookup(GetAreasDictionary(state, location), name);
            case Type.LOCATION: return Lookup(GetLocationsDictionary(state, area), name);
            case null: return Lookup(GetStatesDictionary().Concat(GetAreasDictionary()).Concat(GetLocationsDictionary()).ToArray(), name);
            default: throw new Exception();
        }
    }

    private static string[] GetStatesDictionary(string? area = null, string? location = null)
    {
        int index;
        if (area != null) index = FindIndex(DICTIONARY_AREAS, area);
        else if (location != null) index = FindIndex(DICTIONARY_LOCATIONS, location);
        else index = -1;
        return 0 <= index && index < DICTIONARY_STATES.Length ? DICTIONARY_STATES[index] : DICTIONARY_STATES.SelectMany(n => n).ToArray();
    }

    private static string[] GetAreasDictionary(string? state = null, string? location = null)
    {
        int index;
        if (state != null) index = FindIndex(DICTIONARY_STATES, state);
        else if (location != null) index = FindIndex(DICTIONARY_LOCATIONS, location);
        else index = -1;
        return 0 <= index && index < DICTIONARY_AREAS.Length ? DICTIONARY_AREAS[index] : DICTIONARY_AREAS.SelectMany(n => n).ToArray();
    }

    private static string[] GetLocationsDictionary(string? state = null, string? area = null)
    {
        int index;
        if (state != null) index = FindIndex(DICTIONARY_STATES, state);
        else if (area != null) index = FindIndex(DICTIONARY_AREAS, area);
        else index = -1;
        return 0 <= index && index < DICTIONARY_LOCATIONS.Length ? DICTIONARY_LOCATIONS[index] : DICTIONARY_LOCATIONS.SelectMany(n => n).ToArray();
    }

    private static int FindIndex(string[][] haystack, string needle)
    {
        for (int i = 0; i < haystack.Length; i++) if (haystack[i].Contains(needle)) return i;
        return -1;
    }

    private static (string, double) Lookup(string[] dictionary, string name)
    {
        int index = -1;
        double score = double.MinValue;
        for (int i = 0; i < dictionary.Length; i++)
        {
            double s = NeedlemanWunsch.ComputeScore(name, dictionary[i], false);
            s = Math.Max(s, NeedlemanWunsch.ComputeScore(name.ToLower(), dictionary[i].ToLower(), false));
            s = Math.Max(s, NeedlemanWunsch.ComputeScore(name.ToUpper(), dictionary[i].ToUpper(), false));
            if (s < score) continue;
            index = i;
            score = s;
        }
        return (dictionary[index], score);
    }

}

