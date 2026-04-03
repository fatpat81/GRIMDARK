// Data Preparation
const stratagems = StratagemsData.filter(s => s.cpCost.trim() !== "0" && s.cpCost.trim().toUpperCase() !== "0CP" && !s.type.toUpperCase().includes("BOARDING ACTIONS"));
const factions = [{id: "Core", name: "Core Rules Only"}].concat(
    FactionsData.sort((a,b) => a.name.localeCompare(b.name))
);

let importedUnits = [];
let selectedFactionId = "Core";
let selectedDetachmentName = "";

// DOM Elements
const facSelect = document.getElementById('factionSelect');
const detSelect = document.getElementById('detachmentSelect');
const mainContent = document.getElementById('mainContent');
const fileInput = document.getElementById('fileImport');

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

    return `
        <div class="${blockClass}">
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
        facStrats = stratagems.filter(s => s.factionId === selectedFactionId && (s.detachment.trim() === "" || s.detachment === selectedDetachmentName));
    }
    
    let allStrats = coreStrats.concat(facStrats)
        .sort((a,b) => (a.factionId==="" ? 0 : 1) - (b.factionId==="" ? 0 : 1) || a.type.localeCompare(b.type) || a.name.localeCompare(b.name));

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
        let generalStrats = [];
        let pureCoreStrats = [];
        let unitStrats = new Map();
        importedUnits.forEach(u => unitStrats.set(u, []));

        allStrats.forEach(strat => {
            let filteredTargets = strat.targetKeywords.filter(k => !k.startsWith("RAW_TARGET_TEXT:") && k !== fac.name.toUpperCase());
            let rawTarget = strat.targetKeywords.find(k => k.startsWith("RAW_TARGET_TEXT:")) || "";
            
            if (filteredTargets.length === 0 && rawTarget.trim() === "" && strat.detachment.trim() === "") {
                if (strat.factionId === "" || strat.factionId === "Core") {
                    pureCoreStrats.push(strat);
                } else {
                    generalStrats.push(strat);
                }
            } else {
                let appliedToAny = false;
                for (let [unit, stratList] of unitStrats.entries()) {
                    let matches = false;
                    if (filteredTargets.some(kw => unit.keywords.includes(kw))) {
                        matches = true;
                    } else if (rawTarget !== "" && filteredTargets.length === 0) {
                        if (rawTarget.includes(unit.name.toUpperCase())) {
                            matches = true;
                        }
                    }
                    
                    if (filteredTargets.length === 0 && strat.factionId !== "" && strat.factionId !== "Core") {
                        matches = true;
                    }

                    if (matches) {
                        stratList.push(strat);
                        appliedToAny = true;
                    }
                }
                if (!appliedToAny) {
                    if (strat.factionId === "" || strat.factionId === "Core") pureCoreStrats.push(strat);
                    else generalStrats.push(strat);
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
        
        if (generalStrats.length > 0) {
            html += `<div class="unit-block"><div class="unit-header">Army-Wide & General Stratagems</div><div class="stratagems-grid">`;
            generalStrats.forEach(s => html += renderStratagem(s));
            html += `</div></div>`;
        }

        if (pureCoreStrats.length > 0) {
            html += `<div class="unit-block"><div class="unit-header" style="background: linear-gradient(90deg, #444 0%, transparent 100%);">Core Stratagems</div><div class="stratagems-grid">`;
            pureCoreStrats.forEach(s => html += renderStratagem(s));
            html += `</div></div>`;
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
            postImport();
        };
        reader.readAsText(file);
    } else if (txt.endsWith('.ros')) {
        let reader = new FileReader();
        reader.onload = (re) => {
            parseRosXml(re.target.result);
            postImport();
        };
        reader.readAsText(file);
    } else if (txt.endsWith('.rosz')) {
        let reader = new FileReader();
        reader.onload = (re) => {
            // Need jszip mapping. We included jszip in HTML
            JSZip.loadAsync(re.target.result).then(zip => {
                let xmlFile = Object.values(zip.files).find(f => f.name.endsWith('.ros'));
                if (xmlFile) {
                    xmlFile.async("string").then(content => {
                        parseRosXml(content);
                        postImport();
                    });
                }
            });
        };
        reader.readAsArrayBuffer(file);
    }
});

function postImport() {
    renderView();
    let toast = document.getElementById("toast");
    toast.className = "show";
    setTimeout(function(){ toast.className = toast.className.replace("show", ""); }, 3000);
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
