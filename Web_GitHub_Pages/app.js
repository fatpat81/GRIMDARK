// Data Preparation
const stratagems = StratagemsData.filter(s => s.cpCost.trim() !== "0" && s.cpCost.trim().toUpperCase() !== "0CP" && !s.type.toUpperCase().includes("BOARDING"));
const factions = [{id: "Core", name: "Core Rules Only"}].concat(
    FactionsData.sort((a,b) => a.name.localeCompare(b.name))
);

let importedUnits = [];
let selectedFactionId = "Core";
let selectedDetachmentName = "";

// Storage Manager
const StorageManager = {
    saveProfile: function(username) { localStorage.setItem('strat_profile', username); },
    loadProfile: function() { return localStorage.getItem('strat_profile') || ""; },
    toggleFav: function(username, stratId) {
        if (!username) return;
        let favs = this.getFavs(username);
        if (favs.includes(stratId)) favs = favs.filter(id => id !== stratId);
        else favs.push(stratId);
        localStorage.setItem('strat_favs_' + username, JSON.stringify(favs));
    },
    getFavs: function(username) {
        if (!username) return [];
        let data = localStorage.getItem('strat_favs_' + username);
        return data ? JSON.parse(data) : [];
    },
    saveListHistory: function(listName, dataObj) {
        let history = this.getHistory();
        history = history.filter(h => h.name !== listName);
        history.unshift({ name: listName, data: dataObj, time: new Date().getTime() });
        if (history.length > 5) history = history.slice(0, 5);
        try { localStorage.setItem('strat_history', JSON.stringify(history)); } catch(e) { }
    },
    getHistory: function() {
        let data = localStorage.getItem('strat_history');
        return data ? JSON.parse(data) : [];
    }
};

window.toggleFavStrat = function(stratName, factionId, elem) {
    let profileInput = document.getElementById('profileInput');
    let username = profileInput ? profileInput.value.trim() : "";
    if (!username) { alert("Please enter a Profile Username in the top right to save favorites!"); return; }
    
    let key = stratName + "|" + factionId;
    StorageManager.toggleFav(username, key);
    let favs = StorageManager.getFavs(username);
    
    if (favs.includes(key)) {
        elem.innerHTML = "⭐";
        elem.classList.add("favorited");
        elem.style.color = "gold";
        elem.style.textShadow = "0 0 5px rgba(255, 215, 0, 0.5)";
    } else {
        elem.innerHTML = "☆";
        elem.classList.remove("favorited");
        elem.style.color = "var(--text-color)";
        elem.style.textShadow = "none";
    }
    
    let displaySelect = document.getElementById('displaySelect');
    if (displaySelect && displaySelect.value === 'favorites') renderView();
};

const facSelect = document.getElementById('factionSelect');
const detSelect = document.getElementById('detachmentSelect');
const mainContent = document.getElementById('mainContent');
const fileInput = document.getElementById('fileImport');
const profileInput = document.getElementById('profileInput');
const historySelect = document.getElementById('historySelect');
const displaySelect = document.getElementById('displaySelect');

if(profileInput) {
    profileInput.value = StorageManager.loadProfile();
    profileInput.addEventListener('change', (e) => {
        StorageManager.saveProfile(e.target.value.trim());
        renderView();
    });
}
if(displaySelect) {
    displaySelect.addEventListener('change', () => { renderView(); });
}

window.updateHistoryDropdown = function() {
    if(!historySelect) return;
    let h = StorageManager.getHistory();
    historySelect.innerHTML = '<option value="">No Saved Lists</option>';
    h.forEach((item, index) => {
        let opt = document.createElement('option');
        opt.value = index;
        let d = new Date(item.time);
        opt.textContent = `[${d.getMonth()+1}/${d.getDate()}] ${item.name.substring(0, 20)}`;
        historySelect.appendChild(opt);
    });
};
updateHistoryDropdown();

if(historySelect) {
    historySelect.addEventListener('change', (e) => {
        let idx = e.target.value;
        if (idx !== "") {
            let h = StorageManager.getHistory();
            let loaded = h[idx].data;
            importedUnits = loaded.units;
            selectedFactionId = loaded.faction;
            facSelect.value = selectedFactionId;
            updateDetachments();
            selectedDetachmentName = loaded.detachment;
            detSelect.value = selectedDetachmentName;
            renderView();
            
            let t = document.getElementById('toast');
            t.textContent = "List Loaded from History!";
            t.classList.add("show");
            setTimeout(() => t.classList.remove("show"), 3000);
        }
    });
}

// Initialize
factions.forEach(f => {
    let opt = document.createElement('option');
    opt.value = f.id;
    opt.textContent = f.name;
    facSelect.appendChild(opt);
});

facSelect.addEventListener('change', (e) => {
    selectedFactionId = e.target.value;
    updateDetachments();
});

detSelect.addEventListener('change', (e) => {
    selectedDetachmentName = e.target.value;
    renderView();
});

function updateDetachments() {
    detSelect.innerHTML = '<option value="">None (Faction Specific)</option>';
    if (selectedFactionId !== "Core") {
        let dets = [...new Set(stratagems.filter(s => s.factionId === selectedFactionId && s.detachment.trim() !== "").map(s => s.detachment))].sort();
        dets.forEach(d => {
            let opt = document.createElement('option');
            opt.value = d;
            opt.textContent = d;
            detSelect.appendChild(opt);
        });
    }
    selectedDetachmentName = "";
    renderView();
}

// Subset mapping
const subsetFactions = {
    "SPACE WOLVES": "SPACE MARINES",
    "BLOOD ANGELS": "SPACE MARINES",
    "DARK ANGELS": "SPACE MARINES",
    "DEATHWATCH": "SPACE MARINES",
    "BLACK TEMPLARS": "SPACE MARINES",
    "CRIMSON FISTS": "SPACE MARINES",
    "IMPERIAL FISTS": "SPACE MARINES",
    "RAVEN GUARD": "SPACE MARINES",
    "SALAMANDERS": "SPACE MARINES",
    "IRON HANDS": "SPACE MARINES",
    "WHITE SCARS": "SPACE MARINES",
    "ULTRAMARINES": "SPACE MARINES"
};

function getParentFaction(facName) {
    let fn = facName.toUpperCase().trim();
    return subsetFactions[fn] ? subsetFactions[fn] : fn;
}

function renderStratagem(s) {
    let leg = s.legend ? `<div class="legend">"${s.legend}"</div>` : '';
    let detDisplay = (s.detachment && s.detachment.trim() !== '') ? s.detachment : 'Core';
    let blockClass = detDisplay !== 'Core' ? 'stratagem faction-detachment' : 'stratagem';

    let username = profileInput ? profileInput.value.trim() : "";
    let key = s.name + "|" + s.factionId;
    let isFav = username && StorageManager.getFavs(username).includes(key);
    let star = isFav ? "⭐" : "☆";
    let favClass = isFav ? "favorited" : "";
    let starStyle = isFav ? 'color: gold; text-shadow: 0 0 5px rgba(255, 215, 0, 0.5);' : 'color: var(--text-color);';

    return `
        <div class="${blockClass}">
            <div style="float: right; font-size: 24px; cursor: pointer; ${starStyle}" class="fav-star ${favClass}" onclick="toggleFavStrat('${s.name.replace(/'/g,"\\'")}', '${s.factionId}', this)">${star}</div>
            <span class="cp">${s.cpCost} CP</span>
            <h2>${s.name}</h2>
            <div class="meta">
                <span class="meta-detachment">Detachment: ${detDisplay}</span>
                <span><b>Type:</b> ${s.type}</span>
                <span><b>Turn:</b> ${s.turn} | <b>Phase:</b> ${s.phase}</span>
            </div>
            ${leg}
            <div class="desc">${s.description}</div>
        </div>
    `;
}

function renderView() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
    let fac = factions.find(f => f.id === selectedFactionId);
    let title = `${fac.name} ${selectedDetachmentName ? '- ' + selectedDetachmentName : ''} Stratagems`;
    
    let html = `<h1 class="section-header">${title}</h1>`;

    let coreStrats = stratagems.filter(s => s.factionId === "" || s.factionId === "Core" || (s.type && s.type.includes("Core")));
    let facStrats = [];
    if (selectedFactionId !== "Core") {
        facStrats = stratagems.filter(s => s.factionId === selectedFactionId && (selectedDetachmentName === "" || s.detachment === selectedDetachmentName || s.detachment === ""));
    }

    let displaySelect = document.getElementById('displaySelect');
    let filterVal = displaySelect ? displaySelect.value : "both";
    
    let baseStrats = coreStrats.concat(facStrats);
    if (filterVal === "core") baseStrats = coreStrats;
    if (filterVal === "faction") baseStrats = facStrats;
    
    let username = profileInput ? profileInput.value.trim() : "";
    let favs = username ? StorageManager.getFavs(username) : [];
    
    let allStrats = baseStrats;
    if (filterVal === "favorites") {
        allStrats = baseStrats.filter(s => favs.includes(s.name + "|" + s.factionId));
    }
    
    allStrats = allStrats.sort((a,b) => (a.factionId==="" ? 0 : 1) - (b.factionId==="" ? 0 : 1) || a.type.localeCompare(b.type) || a.name.localeCompare(b.name));

    // Deduplicate by Name (Removes duplicate New Orders)
    allStrats = [...new Map(allStrats.map(item => [item.name, item])).values()];

    if (allStrats.length === 0) {
        html += "<p>No stratagems found.</p>";
        mainContent.innerHTML = html;
        return;
    }

    if (importedUnits.length === 0) {
        html += `<div class="stratagems-grid">`;
        allStrats.forEach(s => html += renderStratagem(s));
        html += `</div>`;
    } else {
        let unitStrats = new Map();
        importedUnits.forEach(u => unitStrats.set(u, []));

        allStrats.forEach(strat => {
            let filteredTargets = strat.targetKeywords.filter(k => !k.startsWith("RAW_TARGET_TEXT:"));
            let rawTarget = strat.targetKeywords.find(k => k.startsWith("RAW_TARGET_TEXT:")) || "";
            
            let isCore = (strat.factionId === "" || strat.factionId === "Core" || (strat.type && strat.type.includes("Core")));
            let needsNoKeyword = (filteredTargets.length === 0 && (rawTarget.trim() === "" || isCore));

            for (let [unit, stratList] of unitStrats.entries()) {
                let matches = false;
                
                if (needsNoKeyword) {
                    matches = true;
                } else {
                    let allKw = " " + unit.keywords.join(" ") + " ";
                    if (filteredTargets.some(kw => allKw.includes(" " + kw + " "))) {
                        matches = true;
                    } else if (rawTarget !== "") {
                        if (rawTarget.toUpperCase().includes(unit.name.toUpperCase())) {
                            matches = true;
                        }
                    }
                }

                if (matches) {
                    stratList.push(strat);
                }
            }
        });

        for (let [unit, stratList] of unitStrats.entries()) {
            if (stratList.length > 0) {
                // Remove duplicates if any
                let unique = [...new Map(stratList.map(item => [item.name, item])).values()];
                html += `<div class="unit-block"><div class="unit-header">Unit: ${unit.name}</div><div class="stratagems-grid">`;
                unique.forEach(s => html += renderStratagem(s));
                html += `</div></div>`;
            }
        }
    }
    
    mainContent.innerHTML = html;
}

// File importing logic
fileInput.addEventListener('change', (e) => {
    let file = e.target.files[0];
    if (!file) return;

    let txt = file.name.toLowerCase();
    
    // Clear list
    importedUnits = [];
    
    if (txt.endsWith('.json')) {
        let reader = new FileReader();
        reader.onload = (re) => {
            let data = JSON.parse(re.target.result);
            extractJsonUnits(data);
            postImport(file.name);
        };
        reader.readAsText(file);
    } else if (txt.endsWith('.ros')) {
        let reader = new FileReader();
        reader.onload = (re) => {
            parseRosXml(re.target.result);
            postImport(file.name);
        };
        reader.readAsText(file);
    } else if (txt.endsWith('.rosz')) {
        let reader = new FileReader();
        reader.onload = (re) => {
            JSZip.loadAsync(re.target.result).then(zip => {
                let xmlFile = Object.values(zip.files).find(f => f.name.endsWith('.ros'));
                if (xmlFile) {
                    xmlFile.async("string").then(content => {
                        parseRosXml(content);
                        postImport(file.name);
                    });
                }
            });
        };
        reader.readAsArrayBuffer(file);
    } else if (txt.endsWith('.html') || txt.endsWith('.htm')) {
        let reader = new FileReader();
        reader.onload = (re) => {
            parseHtml(re.target.result);
            postImport(file.name);
        };
        reader.readAsText(file);
    }
});

function parseHtml(htmlString) {
    let parser = new DOMParser();
    let doc = parser.parseFromString(htmlString, "text/html");
    
    let cards = doc.querySelectorAll("[type='card']");
    for (let i=0; i<cards.length; i++) {
        let card = cards[i];
        let name = "Unknown";
        
        let nameSpan = card.querySelector("div[style*='font-family: ConduitITCStd'] span");
        if (nameSpan && nameSpan.textContent.trim() !== "") {
            name = nameSpan.textContent.trim();
        } else {
            let flexDivs = card.querySelectorAll("div[style*='font-family: ConduitITCStd']");
            if (flexDivs.length > 0) {
                name = flexDivs[0].textContent.trim().split('\n')[0].trim();
            }
        }
        
        let kw = [];
        
        let cat = card.getAttribute("catalogue");
        if (cat) {
            let facDetect = cat.replace("Chaos - ", "").replace("Imperium - ", "").replace("Xenos - ", "").trim();
            let parentFac = getParentFaction(facDetect);
            let matched = factions.find(f => f.name.toUpperCase() === parentFac);
            if (matched) { facSelect.value = matched.id; selectedFactionId = matched.id; updateDetachments(); }
        }
        
        let kwContainer = card.querySelectorAll(".keywords");
        for (let j=0; j<kwContainer.length; j++) {
            let container = kwContainer[j];
            let title = container.textContent.toUpperCase();
            if (title.includes("FACTION KEYWORDS:")) {
                let spans = container.querySelectorAll(".split.keyword");
                spans.forEach(s => {
                    let k = s.textContent.trim().toUpperCase();
                    kw.push(k);
                    let parentFac = getParentFaction(s.textContent.trim());
                    let matched = factions.find(f => f.name.toUpperCase() === parentFac);
                    if (matched) { facSelect.value = matched.id; selectedFactionId = matched.id; updateDetachments(); }
                });
            }
        }

        let ruleSpans = card.querySelectorAll(".split.rule");
        ruleSpans.forEach(s => {
            kw.push(s.textContent.trim().toUpperCase());
        });

        if (!importedUnits.some(u => u.name === name)) {
            importedUnits.push({name: name, keywords: kw});
        }
    }
}

function postImport(fileName) {
    renderView();
    let toast = document.getElementById("toast");
    if(toast) {
        toast.textContent = "Army list successfully loaded!";
        toast.className = "show";
        setTimeout(function(){ toast.className = toast.className.replace("show", ""); }, 3000);
    }
    
    if(fileName) {
        let dataObj = {
            units: importedUnits,
            faction: selectedFactionId,
            detachment: selectedDetachmentName
        };
        StorageManager.saveListHistory(fileName, dataObj);
        if(window.updateHistoryDropdown) window.updateHistoryDropdown();
        let historySelect = document.getElementById('historySelect');
        if(historySelect) historySelect.value = "0";
    }
}

function extractJsonUnits(obj) {
    if (!obj || typeof obj !== 'object') return;

    if (obj.type === 'unit' || obj.type === 'model') {
        let name = obj.name || "Unknown";
        if (obj.customName) name = obj.customName;
        
        let kw = [];
        if (Array.isArray(obj.categories)) {
            obj.categories.forEach(c => {
                let cname = (c.name || "").toUpperCase().trim();
                if (cname.startsWith("FACTION: ")) {
                    let facDetect = cname.substring(9).trim();
                    let parentFac = getParentFaction(facDetect);
                    let matched = factions.find(f => f.name.toUpperCase() === parentFac);
                    if (matched) { facSelect.value = matched.id; selectedFactionId = matched.id; updateDetachments(); }
                    cname = facDetect;
                }
                if (cname !== "") kw.push(cname);
            });
        }
        
        if (!importedUnits.some(u => u.name === name)) {
            importedUnits.push({name: name, keywords: kw});
        }
    }
    
    // Detachment detect
    if ((obj.type === 'upgrade' || obj.type === 'unit' || obj.type === 'model') && Array.isArray(obj.categories)) {
        let isDet = obj.categories.some(c => c.name === "Detachment Choice");
        let hasDet = (obj.name && obj.name.includes("Detachment"));
        if (isDet || hasDet) {
            let detName = obj.name || "";
            if (detName && detName !== "Detachment") {
                let cleaned = detName.replace(" Detachment", "").trim();
                detSelect.value = cleaned;
                selectedDetachmentName = cleaned;
            }
            if (obj.name === "Detachment" && Array.isArray(obj.rules) && obj.rules.length > 0) {
                detSelect.value = obj.rules[0].name;
                selectedDetachmentName = obj.rules[0].name;
            }
        }
    }

    if (Array.isArray(obj.selections)) {
        obj.selections.forEach(s => extractJsonUnits(s));
    }
    if (obj.forces && Array.isArray(obj.forces)) {
        obj.forces.forEach(f => {
            if (f.catalogueName) {
                let facName = f.catalogueName.trim();
                let matched = factions.find(fac => fac.name.toUpperCase() === facName.toUpperCase());
                if (matched) { facSelect.value = matched.id; selectedFactionId = matched.id; updateDetachments(); }
            }
            if (Array.isArray(f.selections)) f.selections.forEach(s => extractJsonUnits(s));
        });
    }
}

function parseRosXml(xmlString) {
    let parser = new DOMParser();
    let doc = parser.parseFromString(xmlString, "application/xml");
    
    // Attempt Faction
    let forces = doc.getElementsByTagName("force");
    if (forces.length > 0) {
        let cat = forces[0].getAttribute("catalogueName");
        if (cat) {
            let matched = factions.find(fac => fac.name.toUpperCase() === cat.toUpperCase());
            if (matched) { facSelect.value = matched.id; selectedFactionId = matched.id; updateDetachments(); }
        }
    }

    let selections = doc.getElementsByTagName("selection");
    for (let i=0; i<selections.length; i++) {
        let sel = selections[i];
        let type = (sel.getAttribute("type") || "").toLowerCase();
        let name = sel.getAttribute("name") || sel.getAttribute("customName") || "Unknown";

        if (type === "model" || type === "unit") {
            let kw = [];
            let cats = sel.getElementsByTagName("category");
            for (let j=0; j<cats.length; j++) {
                let catName = (cats[j].getAttribute("name") || "").toUpperCase().trim();
                if (catName.startsWith("FACTION: ")) {
                    let facDetect = catName.substring(9).trim();
                    let parentFac = getParentFaction(facDetect);
                    let matched = factions.find(f => f.name.toUpperCase() === parentFac);
                    if (matched) { facSelect.value = matched.id; selectedFactionId = matched.id; updateDetachments(); }
                    catName = facDetect;
                }
                if (catName !== "") kw.push(catName);
            }
            
            if (!importedUnits.some(u => u.name === name)) {
                importedUnits.push({name: name, keywords: kw});
            }
        }

        if (type === "upgrade" || type === "unit" || type === "model") {
            let cats = Array.from(sel.getElementsByTagName("category"));
            let isDet = cats.some(c => c.getAttribute("name") === "Detachment Choice");
            let hasDet = (name.includes("Detachment"));
            
            if (isDet || hasDet) {
                let detName = name;
                if (detName && detName !== "Detachment") {
                    let cleaned = detName.replace(" Detachment", "").trim();
                    detSelect.value = cleaned;
                    selectedDetachmentName = cleaned;
                }
                if (name === "Detachment") {
                    let rules = sel.getElementsByTagName("rule");
                    if (rules.length > 0) {
                        detSelect.value = rules[0].getAttribute("name");
                        selectedDetachmentName = rules[0].getAttribute("name");
                    }
                }
            }
        }
    }
}

// Initial render
window.onload = function() {
    window.scrollTo(0, 0);
};
updateDetachments();
