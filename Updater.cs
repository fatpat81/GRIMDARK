using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

public class Updater
{
    public static void Main()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  Wahapedia Live Updater Initiated");
        Console.WriteLine("========================================");
        
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                
                Console.WriteLine("[ ] Fetching Factions.csv from Wahapedia...");
                string factionsCsv = client.DownloadString("https://wahapedia.ru/wh40k10ed/Factions.csv");
                File.WriteAllText("factions.csv", factionsCsv);
                Console.WriteLine("[X] Factions downloaded securely.");
                
                Console.WriteLine("[ ] Fetching Stratagems.csv from Wahapedia...");
                string stratagemsCsv = client.DownloadString("https://wahapedia.ru/wh40k10ed/Stratagems.csv");
                File.WriteAllText("stratagems.csv", stratagemsCsv);
                Console.WriteLine("[X] Stratagems downloaded securely.");
            }
            
            Console.WriteLine("\n[ ] Compiling definitions into Web App data.js...");
            BuildDataJs();
            Console.WriteLine("[X] Compilation Successful!");
            
            Console.WriteLine("\n[SUCCESS] Update Routine Complete! You can now launch index.html.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("\n[ERROR] The update encountered an issue: " + ex.Message);
            Console.WriteLine("If Wahapedia servers are overloaded, use the manual download link instead.");
        }
        
        System.Threading.Thread.Sleep(3000);
    }
    
    private static void BuildDataJs()
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
