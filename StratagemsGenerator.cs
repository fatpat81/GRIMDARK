using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Xml.Linq;

public class Stratagem
{
    public string FactionId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string CpCost { get; set; }
    public string Legend { get; set; }
    public string Turn { get; set; }
    public string Phase { get; set; }
    public string Detachment { get; set; }
    public string Description { get; set; }
    public List<string> TargetKeywords { get; set; }
    
    public Stratagem() { TargetKeywords = new List<string>(); }
}

public class Faction
{
    public string Id { get; set; }
    public string Name { get; set; }
    public override string ToString() { return Name; }
}

public class DetachmentOption
{
    public string Name { get; set; }
    public override string ToString() { return string.IsNullOrWhiteSpace(Name) ? "None (Faction Specific)" : Name; }
}

public class ArmyUnit 
{
    public string Name { get; set; }
    public List<string> Keywords { get; set; }
    public ArmyUnit() { Keywords = new List<string>(); }
}

public class Program : Form
{
    private ComboBox factionComboBox;
    private ComboBox detachmentComboBox;
    private Button importListBtn;
    private WebBrowser webBrowser;
    
    private List<Stratagem> stratagems = new List<Stratagem>();
    private List<Faction> factions = new List<Faction>();
    
    private List<ArmyUnit> importedUnits = new List<ArmyUnit>();
    private string logoUri = "";

    public Program()
    {
        this.Text = "GRIMDARK Stratagem Generator";
        this.Size = new Size(1000, 750);
        this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);

        Label lblFaction = new Label() { Text = "Select Faction:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        factionComboBox = new ComboBox() { Location = new Point(140, 18), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
        factionComboBox.SelectedIndexChanged += FactionComboBox_SelectedIndexChanged;

        Label lblDetachment = new Label() { Text = "Select Detachment:", Location = new Point(410, 20), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        detachmentComboBox = new ComboBox() { Location = new Point(560, 18), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
        detachmentComboBox.SelectedIndexChanged += DetachmentComboBox_SelectedIndexChanged;

        importListBtn = new Button() { Text = "40K Templates", Location = new Point(780, 17), Width = 110, BackColor = Color.LightSteelBlue, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        importListBtn.Click += ImportListBtn_Click;

        Button printBtn = new Button() { Text = "Print List", Location = new Point(895, 17), Width = 80, BackColor = Color.Gainsboro, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        printBtn.Click += (s, e) => { webBrowser.ShowPrintDialog(); };

        webBrowser = new WebBrowser() { Location = new Point(20, 60), Size = new Size(940, 630), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        
        Label versionLabel = new Label() { Text = "v1.2", AutoSize = true, Location = new Point(20, this.ClientSize.Height - 30), Anchor = AnchorStyles.Bottom | AnchorStyles.Left, ForeColor = Color.Gray };
        
        Controls.Add(lblFaction);
        Controls.Add(factionComboBox);
        Controls.Add(lblDetachment);
        Controls.Add(detachmentComboBox);
        Controls.Add(importListBtn);
        Controls.Add(printBtn);
        Controls.Add(webBrowser);
        Controls.Add(versionLabel);

        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            try {
                using (Stream stream = assembly.GetManifestResourceStream("wh40k_logo.png") ?? assembly.GetManifestResourceStream("StratagemsGenerator.wh40k_logo.png") ?? assembly.GetManifestResourceStream("Program.wh40k_logo.png")) {
                    if (stream != null) {
                        string tempPath = Path.Combine(Path.GetTempPath(), "wh40k_logo.png");
                        using (FileStream fs = new FileStream(tempPath, FileMode.Create)) {
                            stream.CopyTo(fs);
                        }
                        logoUri = "wh40k_logo.png";
                    }
                }
            } catch {}

            // Load Factions
            using (Stream stream = assembly.GetManifestResourceStream("factions.csv"))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        bool isHeader = true;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (isHeader) { isHeader = false; continue; }
                            var parts = line.Split('|');
                            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]))
                            {
                                factions.Add(new Faction { Id = parts[0], Name = parts[1] });
                            }
                        }
                    }
                }
            }

            // Load Stratagems
            using (Stream stream = assembly.GetManifestResourceStream("stratagems.csv"))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        bool isHeader = true;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (isHeader) { isHeader = false; continue; }
                            var parts = line.Split('|');
                            if (parts.Length >= 11)
                            {
                                var strat = new Stratagem {
                                    FactionId = parts[0],
                                    Name = parts[1],
                                    Type = parts[3],
                                    CpCost = parts[4],
                                    Legend = parts[5],
                                    Turn = parts[6],
                                    Phase = parts[7],
                                    Detachment = parts[8],
                                    Description = parts[10]
                                };
                                
                                MatchCollection mc = Regex.Matches(strat.Description, @"<span class=""kwb"">(.*?)</span>", RegexOptions.IgnoreCase);
                                foreach(Match m in mc) {
                                    strat.TargetKeywords.Add(m.Groups[1].Value.ToUpper().Trim());
                                }
                                
                                // Fallback for standard keywords if kwb spans are missing
                                var targetMatch = Regex.Match(strat.Description, @"<b>TARGET:</b>(.*?)<br>", RegexOptions.IgnoreCase);
                                if (targetMatch.Success) {
                                    string targetText = targetMatch.Groups[1].Value.ToUpper();
                                    string[] commonKws = {"INFANTRY", "VEHICLE", "MONSTER", "CHARACTER", "PSYKER", "MOUNTED", "SWARM", "BEAST", "WALKER", "TITANIC", "AIRCRAFT", "DEDICATED TRANSPORT"};
                                    foreach(var kw in commonKws) {
                                        if (targetText.Contains(kw) && !strat.TargetKeywords.Contains(kw)) {
                                            strat.TargetKeywords.Add(kw);
                                        }
                                    }
                                    strat.TargetKeywords.Add("RAW_TARGET_TEXT:" + targetText);
                                }
                                
                                if (strat.CpCost != null && strat.CpCost.Trim().ToUpper() != "0" && strat.CpCost.Trim().ToUpper() != "0CP") {
                                    stratagems.Add(strat);
                                }
                            }
                        }
                    }
                }
            }

            factions = factions.OrderBy(f => f.Name).ToList();
            factions.Insert(0, new Faction { Id = "Core", Name = "Core Rules Only" });

            factionComboBox.Items.Clear();
            foreach(var f in factions) factionComboBox.Items.Add(f);

            if (factionComboBox.Items.Count > 0)
                factionComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading data: " + ex.Message);
        }
    }
    
    string detectedFactionName = "";
    string detectedDetachmentName = "";

    private void ExtractUnits(object node, List<ArmyUnit> units) {
        var dict = node as Dictionary<string, object>;
        if (dict != null) {
            if (dict.ContainsKey("type")) {
                string type = dict["type"].ToString().ToLower();
                if (type == "unit" || type == "model") {
                    string name = dict.ContainsKey("name") ? dict["name"].ToString() : "Unknown";
                    string customName = dict.ContainsKey("customName") ? dict["customName"].ToString() : "";
                    
                    string dispName = string.IsNullOrWhiteSpace(customName) ? name : string.Format("{0} ({1})", customName, name);
                    
                    List<string> kw = new List<string>();
                    var cats = dict.ContainsKey("categories") ? dict["categories"] as System.Collections.ArrayList : null;
                    if (cats != null) {
                        foreach(var catNode in cats) {
                            var catDict = catNode as Dictionary<string, object>;
                            if (catDict != null && catDict.ContainsKey("name")) {
                                string catName = catDict["name"].ToString().ToUpper().Trim();
                                if (catName.StartsWith("FACTION: ")) {
                                    detectedFactionName = catDict["name"].ToString().Substring(9).Trim();
                                    catName = detectedFactionName.ToUpper();
                                }
                                kw.Add(catName);
                            }
                        }
                    }
                    
                    // Prevent duplicates (e.g. Abaddon type: model inside an entry vs root)
                    if (!units.Any(u => u.Name == dispName)) {
                        units.Add(new ArmyUnit { Name = dispName, Keywords = kw });
                    }
                }
                
                if (dict.ContainsKey("group") && dict["group"].ToString() == "Detachment") {
                    if (dict.ContainsKey("name")) detectedDetachmentName = dict["name"].ToString();
                }
            }
            
            foreach(var key in dict.Keys) {
                ExtractUnits(dict[key], units);
            }
        } else {
            var list = node as System.Collections.ArrayList;
            if (list != null) {
                foreach(var item in list) {
                    ExtractUnits(item, units);
                }
            }
        }
    }

    private void ParseRosXml(Stream stream)
    {
        XDocument doc = XDocument.Load(stream);
        if (doc.Root == null) return;
        
        XNamespace ns = doc.Root.GetDefaultNamespace();
        
        // Find force to get faction if possible
        var forceElement = doc.Descendants(ns + "force").FirstOrDefault();
        if (forceElement != null && forceElement.Attribute("catalogueName") != null) {
            detectedFactionName = forceElement.Attribute("catalogueName").Value.Trim();
        }

        // Iterate selections
        var selections = doc.Descendants(ns + "selection").ToList();
        foreach (var sel in selections) {
            string type = sel.Attribute("type") != null ? sel.Attribute("type").Value.ToLower() : null;
            string name = "Unknown";
            if (sel.Attribute("name") != null) name = sel.Attribute("name").Value;
            else if (sel.Attribute("customName") != null) name = sel.Attribute("customName").Value;

            if (type == "model" || type == "unit") {
                List<string> kw = new List<string>();
                var cats = sel.Descendants(ns + "category").ToList();
                foreach(var cat in cats) {
                    string catName = cat.Attribute("name") != null ? cat.Attribute("name").Value.ToUpper().Trim() : "";
                    if (catName.StartsWith("FACTION: ")) {
                        detectedFactionName = cat.Attribute("name").Value.Substring(9).Trim();
                        catName = detectedFactionName.ToUpper();
                    }
                    if (!string.IsNullOrWhiteSpace(catName)) kw.Add(catName);
                }

                if (!importedUnits.Any(u => u.Name == name)) {
                    importedUnits.Add(new ArmyUnit { Name = name, Keywords = kw });
                }
            }
            
            // Check for detachment
            if (type == "upgrade" || type == "unit" || type == "model") {
                var cats = sel.Descendants(ns + "category").ToList();
                bool isDetachmentChoice = cats.Any(c => c.Attribute("name") != null && c.Attribute("name").Value == "Detachment Choice");
                bool hasDetachmentInName = (sel.Attribute("name") != null && sel.Attribute("name").Value.Contains("Detachment"));
                
                if (isDetachmentChoice || hasDetachmentInName) {
                    string detName = sel.Attribute("name") != null ? sel.Attribute("name").Value : "";
                    if (!string.IsNullOrWhiteSpace(detName) && detName != "Detachment") {
                        detectedDetachmentName = detName.Replace(" Detachment", "").Trim();
                    }
                    if (sel.Attribute("name") != null && sel.Attribute("name").Value == "Detachment") {
                        var rules = sel.Descendants(ns + "rule").ToList();
                        if (rules.Count > 0 && rules[0].Attribute("name") != null) {
                            detectedDetachmentName = rules[0].Attribute("name").Value;
                        }
                    }
                }
            }
        }
    }

    private void ParseHtml(string html)
    {
        var cards = html.Split(new string[] { "type=\"card\"" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach(var card in cards) {
            if (!card.Contains("catalogue=")) continue;
            
            string name = "Unknown";
            // Unit Name
            var nameMatch = Regex.Match(card, @"font-family:\s*ConduitITCStd[^>]*>[\s\S]*?(?:<span>)?([^<]+)(?:</span>)?", RegexOptions.IgnoreCase);
            if (nameMatch.Success) {
                name = nameMatch.Groups[1].Value.Trim();
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
                ArmyUnit newUnit = new ArmyUnit();
                newUnit.Name = name;
                foreach(var k in kw) newUnit.Keywords.Add(k);
                importedUnits.Add(newUnit);
            }
        }
    }


    private void ImportListBtn_Click(object sender, EventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog();
        ofd.Filter = "40K Templates|*.html;*.htm|Army Lists|*.json;*.rosz;*.ros|JSON Files|*.json|ROSZ Files|*.rosz|ROS Files|*.ros|All Files|*.*";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try {
                importedUnits.Clear();
                detectedFactionName = "";
                detectedDetachmentName = "";

                string ext = Path.GetExtension(ofd.FileName).ToLower();
                if (ext == ".json") {
                    string json = File.ReadAllText(ofd.FileName);
                    var jss = new JavaScriptSerializer();
                    jss.MaxJsonLength = 2147483647;
                    var root = jss.Deserialize<object>(json);
                    ExtractUnits(root, importedUnits);
                } else if (ext == ".rosz") {
                    using (ZipArchive archive = ZipFile.OpenRead(ofd.FileName)) {
                        foreach (ZipArchiveEntry entry in archive.Entries) {
                            if (entry.FullName.EndsWith(".ros", StringComparison.OrdinalIgnoreCase)) {
                                using (Stream stream = entry.Open()) {
                                    ParseRosXml(stream);
                                }
                                break;
                            }
                        }
                    }
                } else if (ext == ".ros") {
                    using (Stream stream = File.OpenRead(ofd.FileName)) {
                        ParseRosXml(stream);
                    }
                } else if (ext == ".html" || ext == ".htm") {
                    string htmlText = File.ReadAllText(ofd.FileName);
                    ParseHtml(htmlText);
                }
                
                // Try to auto set combo box
                if (!string.IsNullOrWhiteSpace(detectedFactionName)) {
                    foreach(var item in factionComboBox.Items) {
                        if ((item as Faction).Name.Equals(detectedFactionName, StringComparison.OrdinalIgnoreCase)) {
                            factionComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(detectedDetachmentName)) {
                    foreach(var item in detachmentComboBox.Items) {
                        if ((item as DetachmentOption).Name.Equals(detectedDetachmentName, StringComparison.OrdinalIgnoreCase)) {
                            detachmentComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                
                MessageBox.Show(string.Format("Successfully imported {0} units.\nDetected Faction: {1}\nDetected Detachment: {2}", importedUnits.Count, detectedFactionName, detectedDetachmentName));
                UpdateView();
            }
            catch(Exception ex) {
                MessageBox.Show("Error parsing list: " + ex.Message);
            }
        }
    }

    private void FactionComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedFaction = factionComboBox.SelectedItem as Faction;
        if (selectedFaction == null) return;

        var detachments = stratagems
            .Where(s => s.FactionId == selectedFaction.Id && !string.IsNullOrWhiteSpace(s.Detachment))
            .Select(s => s.Detachment)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        detachmentComboBox.Items.Clear();
        detachmentComboBox.Items.Add(new DetachmentOption { Name = "" }); // Option for none/all
        foreach(var d in detachments)
        {
            detachmentComboBox.Items.Add(new DetachmentOption { Name = d });
        }
        
        if (detachmentComboBox.Items.Count > 0)
            detachmentComboBox.SelectedIndex = 0;
            
        UpdateView();
    }

    private void DetachmentComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateView();
    }
    
    private string RenderStratagem(Stratagem s) {
        string h = "<div class='stratagem'>";
        h += string.Format("<span class='cp'>{0} CP</span>", s.CpCost);
        h += string.Format("<h2>{0}</h2>", s.Name);
        h += string.Format("<div class='meta'><span><b>Type:</b> {0}</span>", s.Type);
        h += string.Format("<span><b>Turn:</b> {0} | <b>Phase:</b> {1}</span></div>", s.Turn, s.Phase);
        if (!string.IsNullOrWhiteSpace(s.Legend)) h += string.Format("<div class='legend'>\"{0}\"</div>", s.Legend);
        h += string.Format("<div class='desc'>{0}</div>", s.Description);
        h += "</div>";
        return h;
    }

    private void UpdateView()
    {
        try {
            var selectedFaction = factionComboBox.SelectedItem as Faction;
            var selectedDetachment = detachmentComboBox.SelectedItem as DetachmentOption;
            
            if (selectedFaction == null || selectedDetachment == null) return;

        var coreStrats = stratagems.Where(s => string.IsNullOrWhiteSpace(s.FactionId) || s.FactionId == "Core" || (string.IsNullOrWhiteSpace(s.Type) == false && s.Type.Contains("Core"))).ToList();
        
        coreStrats.RemoveAll(s => (s.Type != null && s.Type.IndexOf("Boarding", StringComparison.OrdinalIgnoreCase) >= 0) || 
                                  (s.Detachment != null && s.Detachment.IndexOf("Boarding", StringComparison.OrdinalIgnoreCase) >= 0) || 
                                  (s.Name != null && s.Name.IndexOf("Boarding", StringComparison.OrdinalIgnoreCase) >= 0));

        var factionStrats = new List<Stratagem>();
        if (selectedFaction.Id != "Core") {
            factionStrats = stratagems.Where(s => s.FactionId == selectedFaction.Id && 
                (string.IsNullOrWhiteSpace(s.Detachment) || s.Detachment == selectedDetachment.Name)).ToList();
            
            factionStrats.RemoveAll(s => (s.Type != null && s.Type.IndexOf("Boarding", StringComparison.OrdinalIgnoreCase) >= 0) || 
                                         (s.Detachment != null && s.Detachment.IndexOf("Boarding", StringComparison.OrdinalIgnoreCase) >= 0) || 
                                         (s.Name != null && s.Name.IndexOf("Boarding", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        var allStrats = coreStrats.Union(factionStrats)
                                   .OrderBy(s => string.IsNullOrWhiteSpace(s.FactionId) ? 0 : 1)
                                   .ThenBy(s => s.Type)
                                   .ThenBy(s => s.Name)
                                   .ToList();

        string html = "<html><head><style>" +
            "body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #1e1e1e; color: #f0f0f0; padding: 20px; line-height: 1.5; }" +
            ".stratagem { background-color: #2d2d30; border: 1px solid #3e3e42; border-left: 5px solid #007acc; margin-bottom: 15px; padding: 15px; border-radius: 4px; box-shadow: 2px 2px 5px rgba(0,0,0,0.3); }" +
            ".stratagem h2 { margin-top: 0; color: #4da6ff; font-size: 20px; border-bottom: 1px solid #3e3e42; padding-bottom: 5px; }" +
            ".meta { font-size: 13px; color: #b0b0b0; margin-bottom: 10px; display:flex; justify-content:space-between; }" +
            ".cp { background-color: #d9534f; color: white; padding: 3px 8px; border-radius: 12px; font-weight: bold; font-size: 14px; float:right; }" +
            ".desc { background-color: #252526; padding: 10px; border-radius: 4px; border: 1px solid #3e3e42; }" +
            "b { color: #dcdcaa; }" +
            ".legend { font-style: italic; color: #a9a9a9; margin-bottom: 10px; }" +
            ".unit-header { background-color: #007acc; color: white; padding: 10px; font-size: 22px; font-weight: bold; margin-top: 40px; border-radius: 4px; box-shadow: 2px 2px 5px rgba(0,0,0,0.5); }" +
            "</style></head><body>";
            
        if (!string.IsNullOrWhiteSpace(logoUri) && importedUnits.Count == 0) {
            html += string.Format("<div style='text-align: center; margin-bottom: 20px;'><img src='{0}' style='max-width: 400px; max-height: 200px;'/></div>", logoUri);
        }
            
        html += string.Format("<h1 style='color: white; border-bottom: 2px solid white; padding-bottom:10px;'>{0} {1} Stratagems</h1>", selectedFaction.Name, (string.IsNullOrWhiteSpace(selectedDetachment.Name) ? "" : " - " + selectedDetachment.Name));

        if(allStrats.Count == 0)
        {
             html += "<p>No stratagems found.</p>";
        }

        if (importedUnits.Count == 0) {
            // General display when no list is imported
            foreach(var s in allStrats) {
                html += RenderStratagem(s);
            }
        } else {
            // Group Stratagems by Unit matching logic
            Dictionary<ArmyUnit, List<Stratagem>> unitStratagems = new Dictionary<ArmyUnit, List<Stratagem>>();
            foreach(var u in importedUnits) unitStratagems[u] = new List<Stratagem>();
            
            foreach(var strat in allStrats) {
                var filteredTargets = strat.TargetKeywords.Where(k => !k.StartsWith("RAW_TARGET_TEXT:")).ToList();
                string rawTarget = strat.TargetKeywords.FirstOrDefault(k => k.StartsWith("RAW_TARGET_TEXT:")) ?? "";
                
                bool isCore = (strat.FactionId == "" || strat.FactionId == "Core" || strat.Type.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0);
                bool needsNoKeyword = (filteredTargets.Count == 0 && (string.IsNullOrWhiteSpace(rawTarget) || isCore));

                foreach(var unit in importedUnits) {
                    bool matches = false;
                    
                    if (needsNoKeyword) {
                        matches = true; // Assign Core stratagems to all units unless restricted
                    } else {
                        string allKw = " " + string.Join(" ", unit.Keywords) + " ";
                        if (filteredTargets.Any(kw => allKw.Contains(" " + kw + " "))) {
                            matches = true;
                        } else if (!string.IsNullOrWhiteSpace(rawTarget)) {
                            if (rawTarget.IndexOf(unit.Name, StringComparison.OrdinalIgnoreCase) >= 0) {
                                matches = true;
                            } else if (unit.Keywords.Any(kw => kw.Length > 3 && rawTarget.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)) {
                                matches = true;
                            }
                        }
                    }
                    
                    if (matches) {
                        unitStratagems[unit].Add(strat);
                    }
                }
            }
            
            // Output Units
            foreach(var kvp in unitStratagems) {
                if (kvp.Value.Count > 0) {
                    html += string.Format("<div class='unit-header'>Unit: {0}</div><br/>", kvp.Key.Name);
                    html += "<div style='margin-left: 20px; border-left: 3px solid #007acc; padding-left: 15px;'>";
                    foreach(var s in kvp.Value.Distinct().ToList()) {
                        html += RenderStratagem(s);
                    }
                    html += "</div>";
                }
            }
        }

        html += "</body></html>";
        
        string outPath = Path.Combine(Path.GetTempPath(), "stratagems_view.html");
        File.WriteAllText(outPath, html);
        webBrowser.Navigate("file:///" + outPath.Replace("\\", "/"));
        
        } catch (Exception ex) {
            MessageBox.Show("Display Error: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Program());
    }
}
