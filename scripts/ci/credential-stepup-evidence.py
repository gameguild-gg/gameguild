#!/usr/bin/env python3
"""Verify exact-head credential evidence; the Bash companion owns native processes."""

import argparse
from collections import Counter
import hashlib
import json
import os
from pathlib import Path
import sys
from datetime import datetime, timezone
from defusedxml import ElementTree as ET


PG_CLASSES = (
    "TrustedDeviceStepUpPostgreSqlHttpTests",
    "StepUpVerificationBudgetPostgreSqlTests",
    "StepUpBudgetMigrationPostgreSqlTests",
    "StepUpVerificationTransactionPostgreSqlTests",
    "StepUpWebAuthnBindingPostgreSqlTests",
    "StepUpWebAuthnMigrationPostgreSqlTests",
    "SmsSessionEvidenceMigrationPostgreSqlTests",
    "MfaSmsSignInPostgreSqlTests",
    "MfaSmsEnrollmentReverificationPostgreSqlTests",
    "MfaRecoveryLifecyclePostgreSqlTests",
    "MfaSettingsEnrollmentPostgreSqlTests",
    "MfaRecoveryRateLimitPostgreSqlTests",
    "OrchestratedAuthenticationPostgreSqlHttpTests",
    "ApiKeyCreationStepUpPostgreSqlHttpTests",
    "ServiceAccountSecretStepUpPostgreSqlHttpTests",
)
API_FILTER = "|".join("FullyQualifiedName~" + name for name in (
    "Architecture", "Security", "OpenApi", "JwtBearerValidationHttpTests", "RateLimit"
))
SUITES = (
    ("GameGuild.Identity.Authentication.UnitTests", ""),
    ("GameGuild.Identity.Authorization.UnitTests", ""),
    ("GameGuild.API.UnitTests", API_FILTER),
    ("GameGuild.API.IntegrationTests", "|".join(
        "FullyQualifiedName~" + name for name in PG_CLASSES)),
)


def utc_now():
    return datetime.now(timezone.utc).isoformat()


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_trx(content):
    root = ET.fromstring(content, forbid_dtd=True)
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    rows = root.findall("t:Results/t:UnitTestResult", ns)
    definitions = root.findall("t:TestDefinitions/t:UnitTest", ns)
    entries = root.findall("t:TestEntries/t:TestEntry", ns)
    pairs = Counter((row.get("testId"), row.get("executionId")) for row in rows)
    definition_pairs = Counter((item.get("id"), item.find("t:Execution", ns).get("id"))
                               for item in definitions)
    entry_pairs = Counter((item.get("testId"), item.get("executionId")) for item in entries)
    if not rows or pairs != definition_pairs or pairs != entry_pairs:
        raise ValueError("TRX result/definition/entry identities differ or are empty")
    if any(not all(pair) or count != 1 for pair, count in pairs.items()):
        raise ValueError("TRX contains missing or repeated execution identities")
    counts = Counter(row.get("outcome") for row in rows)
    counters = root.find("t:ResultSummary/t:Counters", ns)
    summary = root.find("t:ResultSummary", ns)
    if counters is None or summary.get("outcome") not in ("Completed", "Passed"):
        raise ValueError("TRX summary is missing or unsuccessful")
    expected = {"total": len(rows), "executed": len(rows), "passed": len(rows),
                "failed": 0, "notExecuted": 0}
    if any(int(counters.get(key, "-1")) != value for key, value in expected.items()):
        raise ValueError("TRX counters disagree with complete passing rows")
    if any(int(value) != 0 for key, value in counters.attrib.items() if key not in expected):
        raise ValueError("TRX contains additional nonzero outcome counters")
    if counts != {"Passed": len(rows)}:
        raise ValueError("TRX contains failed, skipped, or nonterminal rows")
    by_id = {item.get("id"): item for item in definitions}
    cases = []
    for row in rows:
        method = by_id[row.get("testId")].find("t:TestMethod", ns)
        cases.append({"testId": row.get("testId"), "executionId": row.get("executionId"),
                      "name": row.get("testName"), "class": method.get("className"),
                      "method": method.get("name"), "outcome": row.get("outcome")})
    return {"counts": dict(counts), "cases": cases}


def runtime_manifest(directory):
    root = Path(directory)
    return {str(path.relative_to(root)).replace(os.sep, "/"): sha256(path)
            for path in sorted(root.rglob("*")) if path.is_file() and (
                path.suffix == ".dll" or path.name.endswith((".deps.json", ".runtimeconfig.json")))}



def read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def write_json(path, value):
    Path(path).write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def source_manifest(output):
    paths = (output / "tracked-files.z").read_bytes().decode("utf-8").split("\0")
    result = {}
    root = Path.cwd().resolve()
    for name in paths:
        if not name or not (name.startswith(("apps/api/", "packages/infrastructure/client/", "docs/auth/",
                                             "scripts/ci/", ".github/"))
                            or name in ("global.json", ".editorconfig", "Directory.Build.props")):
            continue
        path = Path(name)
        if path.is_absolute() or ".." in path.parts or not path.resolve().is_relative_to(root):
            raise ValueError("Source path escapes the checkout")
        result[name] = sha256(path)
    if not result:
        raise ValueError("Tracked source inventory is empty")
    return result


def runtime_snapshot():
    result = {}
    for name, _ in SUITES:
        directory = Path("apps/api/tests") / name / "bin/Release/net10.0"
        if not (directory / (name + ".dll")).is_file():
            raise ValueError("Missing built test assembly: " + name)
        result[name] = {"path": str(directory), "before": runtime_manifest(directory)}
    return result


def check_phase(output, name, command):
    process = read_json(output / (name + "-process.json"))
    phase = read_json(output / (name + "-exit.json"))
    if (phase["exitCode"] != 0 or phase["pid"] != process["pid"]
            or phase["command"] != process["command"] or phase["command"] != command):
        raise ValueError("Actual phase failed or differs from the declared command: " + name)
    return phase


def check_suite(output, name, test_filter, suite):
    runtime = Path("apps/api/tests") / name / "bin/Release/net10.0"
    command = ["dotnet", "test", str(runtime / (name + ".dll")), "--nologo",
               "--blame-hang-timeout", "5m", "--logger", "trx;LogFileName=tests.trx",
               "--results-directory", str(output / name)]
    if test_filter:
        command.extend(["--filter", test_filter])
    suite["phase"] = check_phase(output, name, command)
    trx = output / name / "tests.trx"
    suite["trxSha256"] = sha256(trx)
    suite.update(read_trx(trx.read_text(encoding="utf-8-sig")))
    if name == "GameGuild.API.IntegrationTests":
        observed = {case["class"].rsplit(".", 1)[-1] for case in suite["cases"]}
        if observed != set(PG_CLASSES):
            raise ValueError("PostgreSQL regression classes differ from the declared scope")


def check_inputs(output, head, report):
    before = (output / "checkout-before.txt").read_text(encoding="utf-8").splitlines()
    after = (output / "checkout-after.txt").read_text(encoding="utf-8").splitlines()
    if len(before) != 2 or before != after or before[0] != head:
        raise ValueError("Exact checkout identity changed")
    report.update(head=before[0], tree=before[1])
    for label in ("before", "after"):
        if (output / ("status-" + label + ".txt")).read_text(encoding="utf-8").strip():
            raise ValueError("Tracked checkout has uncommitted changes")
    if read_json(output / "source-before.json") != read_json(output / "source-after.json"):
        raise ValueError("Source changed during native validation")
    runtime_before = read_json(output / "runtime-before.json")
    runtime_after = read_json(output / "runtime-after.json")
    if set(runtime_before) != set(runtime_after) or set(runtime_before) != {name for name, _ in SUITES}:
        raise ValueError("Incomplete executed runtime inventory")
    for name, files in runtime_after.items():
        if not files or files != runtime_before[name]["before"]:
            raise ValueError("Executed DLL/loader files changed: " + name)
        if not {name + suffix for suffix in (".dll", ".deps.json", ".runtimeconfig.json")} <= set(files):
            raise ValueError("Missing test assembly or loader: " + name)


def finalize(output, head, wrapper_exit):
    report = {"startedUtc": (output / "started-utc.txt").read_text(encoding="utf-8").strip(),
              "expectedHead": head, "event": os.getenv("GITHUB_EVENT_NAME"),
              "runId": os.getenv("GITHUB_RUN_ID"), "runAttempt": os.getenv("GITHUB_RUN_ATTEMPT"),
              "wrapperExit": wrapper_exit, "wholeExit": 1, "suites": {},
              "scope": "Native credential regression evidence; SDK and merge gates are separate"}
    try:
        report["build"] = check_phase(output, "solution-build", ["dotnet", "build", "apps/api/GameGuild.sln",
            "-c", "Release", "--nologo", "-p:TreatWarningsAsErrors=true", "-m:2"])
        check_inputs(output, head, report)
        failures = []
        for name, test_filter in SUITES:
            suite = {"filter": test_filter}
            report["suites"][name] = suite
            try:
                check_suite(output, name, test_filter, suite)
            except Exception as error:
                suite["error"] = str(error)
                failures.append(name)
        schema_paths = [output / name for name in (
            "credential-openapi.json", "trusted-openapi.json", "orchestrated-openapi.json")]
        report["schemas"] = {path.name: sha256(path) for path in schema_paths if path.is_file()}
        if len(report["schemas"]) != 3 or len(set(report["schemas"].values())) != 1:
            failures.append("Three actual full-host Swagger captures must exist and match")
        if failures or wrapper_exit:
            raise ValueError("Failed native execution: " + ", ".join(failures))
        report["wholeExit"] = 0
    except Exception as error:
        report["error"] = str(error)
        print(str(error), file=sys.stderr, flush=True)
    report["completedUtc"] = utc_now()
    write_json(output / "whole-producer.json", report)
    return report["wholeExit"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("source", "runtime", "phase-start", "phase-end", "finalize"))
    parser.add_argument("--output", required=True)
    parser.add_argument("--head")
    parser.add_argument("--label", choices=("before", "after"))
    parser.add_argument("--name", choices=("solution-build",) + tuple(name for name, _ in SUITES))
    parser.add_argument("--pid", type=int)
    parser.add_argument("--started")
    parser.add_argument("--exit-code", type=int)
    parser.add_argument("--command", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    output = Path(args.output).resolve(strict=True)
    if args.mode == "source":
        write_json(output / ("source-" + args.label + ".json"), source_manifest(output))
    elif args.mode == "runtime":
        snapshot = runtime_snapshot()
        value = snapshot if args.label == "before" else {name: item["before"] for name, item in snapshot.items()}
        write_json(output / ("runtime-" + args.label + ".json"), value)
    elif args.mode == "phase-start":
        if not args.pid or not args.started or not args.command:
            raise ValueError("Incomplete actual process receipt")
        write_json(output / (args.name + "-process.json"),
                   {"pid": args.pid, "startedUtc": args.started, "command": args.command})
    elif args.mode == "phase-end":
        receipt = read_json(output / (args.name + "-process.json"))
        receipt.update(exitCode=args.exit_code, completedUtc=utc_now())
        write_json(output / (args.name + "-exit.json"), receipt)
    else:
        return finalize(output, args.head, args.exit_code)
    return 0


if __name__ == "__main__":
    sys.exit(main())
