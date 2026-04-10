using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class ArmyUnit 
{
    public string Name { get; set; }
    public List<string> Keywords { get; set; } = new List<string>();
}

class Program {
    static void Main(string[] args) {
        string html = File.ReadAllText(@"C:\Users\pat\Downloads\New Warp Ghosts (1).html");
        
        var cards = html.Split(new string[] { "type=\"card\"" }, StringSplitOptions.RemoveEmptyEntries);
        List<ArmyUnit> importedUnits = new List<ArmyUnit>();
        string detectedFactionName = "";

        foreach(var card in cards) {
            if (!card.Contains("catalogue=")) continue;
            
            string name = "Unknown";
            // Unit Name often inside a ConduitITCStd span
            var nameMatch = Regex.Match(card, @"font-family:\s*ConduitITCStd[^>]*>[\s\S]*?<span>(.*?)</span>", RegexOptions.IgnoreCase);
            if (nameMatch.Success) {
                name = nameMatch.Groups[1].Value.Trim();
            } else {
				// Alternative fallback for name if span is slightly different, actually let's try broader
				nameMatch = Regex.Match(card, @"font-family:\s*ConduitITCStd[^>]*>[\s\S]*?(?:<span>)?([^<]+)(?:</span>)?", RegexOptions.IgnoreCase);
                if (nameMatch.Success) name = nameMatch.Groups[1].Value.Trim();
			}

            List<string> kw = new List<string>();

            // Faction Keyword / Catalogue
            var catalogueMatch = Regex.Match(card, @"catalogue=""(.*?)""", RegexOptions.IgnoreCase);
            if (catalogueMatch.Success) {
                string cat = catalogueMatch.Groups[1].Value.Replace("Chaos - ", "").Replace("Imperium - ", "").Replace("Xenos - ", "").Trim();
                if (string.IsNullOrWhiteSpace(detectedFactionName)) detectedFactionName = cat;
            }

            // Faction Keywords block
            var factKWMatch = Regex.Match(card, @"FACTION KEYWORDS:([\s\S]*?)</div>", RegexOptions.IgnoreCase);
            if (factKWMatch.Success) {
                var kws = Regex.Matches(factKWMatch.Groups[1].Value, @"<span class=""split keyword""[^>]*>(.*?)</span>", RegexOptions.IgnoreCase);
                foreach(Match m in kws) {
                    string k = m.Groups[1].Value.Trim().ToUpper();
                    kw.Add(k);
                    if (string.IsNullOrWhiteSpace(detectedFactionName)) detectedFactionName = m.Groups[1].Value.Trim();
                }
            }

            // Keywords block
            var kwBlockMatch = Regex.Match(card, @"KEYWORDS:([\s\S]*?)</div>", RegexOptions.IgnoreCase);
            if (kwBlockMatch.Success) {
                var kws = Regex.Matches(kwBlockMatch.Groups[1].Value, @"<span class=""split rule""[^>]*>(.*?)</span>", RegexOptions.IgnoreCase);
                foreach(Match m in kws) {
                    kw.Add(m.Groups[1].Value.Trim().ToUpper());
                }
            }

            if (!importedUnits.Any(u => u.Name == name)) {
                importedUnits.Add(new ArmyUnit { Name = name, Keywords = kw });
            }
        }
        
        Console.WriteLine($"Detected Faction: {detectedFactionName}");
        foreach(var u in importedUnits) {
            Console.WriteLine($"{u.Name}: {string.Join(", ", u.Keywords)}");
        }
    }
}
