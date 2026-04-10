using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

public class Program
{
    public static void Main()
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = 50000000;
        
        var fnLines = File.ReadAllLines("factions.csv");
        var factionsList = new List<object>();
        foreach(var line in fnLines.Skip(1)) {
            var p = line.Split('|');
            if (p.Length >= 3) {
                factionsList.Add(new { id = p[0], name = p[1] });
            }
        }
        string facJson = serializer.Serialize(factionsList);
        
        var stLines = File.ReadAllLines("stratagems.csv");
        var stratList = new List<object>();
        foreach(var line in stLines.Skip(1)) {
            var p = line.Split('|');
            if (p.Length >= 11) {
                string desc = p[10];
                List<string> kws = new List<string>();
                MatchCollection mc = Regex.Matches(desc, @"<span class=""kwb"">(.*?)</span>", RegexOptions.IgnoreCase);
                foreach(Match m in mc) { kws.Add(m.Groups[1].Value.ToUpper().Trim()); }
                
                var targetMatch = Regex.Match(desc, @"<b>TARGET:</b>(.*?)<br>", RegexOptions.IgnoreCase);
                if (targetMatch.Success) {
                    string txt = targetMatch.Groups[1].Value.ToUpper();
                    string[] comm = {"INFANTRY", "VEHICLE", "MONSTER", "CHARACTER", "PSYKER", "MOUNTED", "SWARM", "BEAST", "WALKER", "TITANIC", "AIRCRAFT", "DEDICATED TRANSPORT", "TERMINATOR", "BATTLELINE", "GRENADES", "SMOKE", "JUMP PACK"};
                    foreach(var k in comm) { if (txt.Contains(k) && !kws.Contains(k)) kws.Add(k); }
                    kws.Add("RAW_TARGET_TEXT:" + txt);
                }
                
                stratList.Add(new {
                    factionId = p[0], name = p[1], type = p[3], cpCost = p[4], legend = p[5], turn = p[6], phase = p[7], detachment = p[8], description = desc, targetKeywords = kws
                });
            }
        }
        string stratJson = serializer.Serialize(stratList);
        
        File.WriteAllText("data.js", "const FactionsData = " + facJson + ";\nconst StratagemsData = " + stratJson + ";");
    }
}
