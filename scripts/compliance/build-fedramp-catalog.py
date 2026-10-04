"""Generate pinned Rev5 provider evidence mappings and vendor official JSON schemas.

Run from the repository root with the two downloaded, pinned source files.
The generated catalog contains identifiers/metadata, not an inferred certification.
"""
import argparse
import hashlib
import json
import re
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path

RULES_COMMIT = "58487bda77d76d9ce334304ec2e779ece7cc7d54"
RULES_BLOB = "fa0925ec64f66b4f62bf24729da2ef1388562470"
SITE_COMMIT = "f3819f13210fe2a5ccb51bfb2df0833608b09079"
OUTPUT = Path("apps/api/Source/Modules/GameGuild.Compliance.Audit/Resources/FedRamp2026")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rules", type=Path)
    parser.add_argument("nist_catalog", type=Path)
    args = parser.parse_args()
    raw = args.rules.read_bytes()
    require(hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest() == RULES_BLOB,
            "Unexpected FedRAMP rules source fingerprint")
    data = json.loads(raw)
    require(data["info"]["version"] == "2026.09.13.02", "Unexpected rules source version")
    nist_raw = args.nist_catalog.read_bytes()
    require(hashlib.sha256(nist_raw).hexdigest() == "a9e23b09116d5e651461d61777c2e7dc1f3454ab3f9e1e8fdf8af01c37dc01be",
            "Unexpected pinned NIST catalog fingerprint")
    nist = ET.fromstring(nist_raw)
    ns = {"o": "http://csrc.nist.gov/ns/oscal/1.0"}
    controls = {item.attrib["id"]: item for item in nist.findall(".//o:control", ns)}
    baselines = {}
    for class_id, row in data["FRR"]["FRC"]["data"]["rev5"]["CSF"]["FRC-CSF-BSL"]["varies_by_class"].items():
        baseline = {}
        for family in row["rev5_controls_list"].values():
            for identifier in family:
                parts = [part for part in re.split(r"[^A-Za-z0-9]+", identifier) if part]
                nist_id = parts[0].lower() + "-" + str(int(parts[1]))
                if len(parts) == 3:
                    nist_id += "." + str(int(parts[2]))
                control = controls[nist_id]
                # prm_* values are OSCAL selections, not organisation-defined parameters.
                baseline[identifier] = [p.attrib["id"] for p in control.findall("o:param", ns) if "_odp" in p.attrib["id"]]
        baselines[class_id.upper()] = baseline
    require([len(baselines[k]) for k in "BCD"] == [155, 322, 409], "Unexpected tailored baseline counts")
    rules = []
    schema_uris = set()
    for group, section in data["FRR"].items():
        info = section["info"]
        subsets = dict(info.get("subsets", {}))
        subsets.update(info.get("rev5", {}).get("subsets", {}))
        effective = info.get("rev5", {}).get("effective", info.get("effective", {}))
        for flavor in ("all", "rev5"):
            for subset, members in section.get("data", {}).get(flavor, {}).items():
                applicability = subsets[subset]["applicability"]
                if "Rev5" not in applicability["types"]:
                    continue
                for identifier, rule in members.items():
                    if "Providers" not in rule.get("affects", applicability["affects"]):
                        continue
                    classes = [k for k in "BCD" if k in applicability["classes"]]
                    if not classes:
                        continue
                    schema = rule.get("schema", {}).get("url")
                    if schema:
                        schema_uris.add(schema)
                    rules.append({"id": identifier, "name": rule["name"], "classes": classes,
                                  "paths": applicability["paths"], "subset": subset,
                                  "force": {k: rule.get("varies_by_class", {}).get(k.lower(), {}).get("force", rule.get("force", "")) for k in classes},
                                  "sourceUri": "https://www.fedramp.gov/2026/providers/rev5/rules/" + info["web_name"] + "/",
                                  "effective": effective, "schemaUri": schema})
    require(len(rules) == len({row["id"] for row in rules}), "Duplicate provider rule identifier")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    schemas = []
    pending = list(sorted(schema_uris))
    seen = set()
    while pending:
        uri = pending.pop(0)
        if uri in seen:
            continue
        require(uri.startswith("https://fedramp.gov/schemas/") and uri.endswith(".json"), "Untrusted schema source URI")
        request = urllib.request.Request(uri.replace("https://fedramp.gov/", "https://www.fedramp.gov/"),
                                         headers={"User-Agent": "GameGuild-compliance-source-verification/1.0"})
        with urllib.request.urlopen(request, timeout=45) as response:
            require(response.url.startswith(("https://fedramp.gov/schemas/", "https://www.fedramp.gov/schemas/")),
                    "Untrusted schema redirect")
            content = response.read(1048577)
        require(len(content) <= 1048576, "Schema exceeds the source size limit")
        schema = json.loads(content)
        require(schema["$id"] == uri and schema["$schema"] == "https://json-schema.org/draft/2020-12/schema",
                "Unexpected government schema identifier or dialect")
        def refs(value):
            if isinstance(value, dict):
                for key, child in value.items():
                    if key == "$ref" and isinstance(child, str) and child.startswith("https://"):
                        pending.append(child.split("#")[0])
                    refs(child)
            elif isinstance(value, list):
                for child in value:
                    refs(child)
        refs(schema)
        filename = uri.rsplit("/", 1)[1]
        (OUTPUT / filename).write_bytes(content)
        schemas.append({"uri": uri, "filename": filename, "version": schema["$schemaVersion"],
                        "sha256": hashlib.sha256(content).hexdigest()})
        seen.add(uri)
    result = {"rulesVersion": data["info"]["version"], "rulesCommit": RULES_COMMIT, "rulesBlob": RULES_BLOB,
              "rulesSha256": hashlib.sha256(raw).hexdigest(), "nistSiteCommit": SITE_COMMIT,
              "nistCatalogSha256": hashlib.sha256(nist_raw).hexdigest(),
              "baselines": baselines, "rules": rules, "schemas": sorted(schemas, key=lambda row: row["uri"])}
    (OUTPUT / "catalog.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"baselineCounts": {k: len(v) for k, v in baselines.items()}, "providerRules": len(rules), "schemas": len(schemas)}))


if __name__ == "__main__":
    main()
