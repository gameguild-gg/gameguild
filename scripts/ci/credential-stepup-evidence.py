#!/usr/bin/env python3
"""Run credential regressions on the exact PR head, retaining failure evidence."""

import argparse
from collections import Counter
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
from datetime import datetime, timezone
import xml.etree.ElementTree as ET


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
    root = ET.fromstring(content)
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


def git(*args):
    return subprocess.check_output(["git", *args], text=True).strip()


def source_manifest():
    # Enumerate tracked files only: never traverse dependencies or build output.
    paths = subprocess.check_output(["git", "ls-files", "-z"], text=True).split("\0")
    return {path: sha256(path) for path in paths if path and (
        path.startswith(("apps/api/", "packages/infrastructure/client/", "docs/auth/"))
        or path in ("global.json", ".editorconfig", "Directory.Build.props")
        or path.startswith(("scripts/ci/", ".github/")))}


def write_json(path, value):
    Path(path).write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def run_phase(output, name, command, environment):
    receipt = {"startedUtc": utc_now(), "command": command}
    with (output / (name + ".log")).open("w", encoding="utf-8") as log:
        process = subprocess.Popen(command, stdout=log, stderr=subprocess.STDOUT, env=environment)
        receipt["pid"] = process.pid
        write_json(output / (name + "-process.json"), receipt)
        receipt["exitCode"] = process.wait()
    receipt["completedUtc"] = utc_now()
    write_json(output / (name + "-exit.json"), receipt)
    print(f"{name}: exit {receipt['exitCode']}", flush=True)
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--head", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=False)
    report = {"startedUtc": utc_now(), "expectedHead": args.head, "head": git("rev-parse", "HEAD"),
              "tree": git("rev-parse", "HEAD^{tree}"), "event": os.getenv("GITHUB_EVENT_NAME"),
              "runId": os.getenv("GITHUB_RUN_ID"), "runAttempt": os.getenv("GITHUB_RUN_ATTEMPT"),
              "wholeExit": 1, "suites": {}, "scope": "Native credential regression evidence; SDK and merge gates are separate"}
    before = None
    runtimes = {}
    environment = dict(os.environ)
    environment.update({"DOTNET_GCConserveMemory": "9", "DOTNET_gcServer": "0"})
    for key, filename in (("GAMEGUILD_CREDENTIAL_STEPUP_OPENAPI_CAPTURE", "credential-openapi.json"),
                          ("GAMEGUILD_STEPUP_OPENAPI_CAPTURE", "trusted-openapi.json"),
                          ("GAMEGUILD_ORCHESTRATED_OPENAPI_CAPTURE", "orchestrated-openapi.json")):
        environment[key] = str(output / filename)
    try:
        if report["head"] != args.head or git("status", "--porcelain", "--untracked-files=no"):
            raise ValueError("Expected an unchanged checkout of the exact PR head")
        # This lane creates disposable local Testcontainers databases; external DBs are not allowed.
        if any(environment.get(key) for key in ("ECONOMY_POSTGRES_CONNECTION", "ECONOMY_POSTGRES_TEMPLATE_DATABASE")):
            raise ValueError("External PostgreSQL configuration is not allowed in this lane")
        before = source_manifest()
        write_json(output / "source-before.json", before)
        # Retain text diagnostics, not an MSBuild binary log containing environment values.
        report["build"] = run_phase(output, "solution-build", ["dotnet", "build", "apps/api/GameGuild.sln",
            "-c", "Release", "--nologo", "-p:TreatWarningsAsErrors=true", "-m:2"], environment)
        if report["build"]["exitCode"]:
            raise ValueError("Full solution build failed; original log and exit retained")
        for name, _ in SUITES:
            runtime = Path("apps/api/tests") / name / "bin/Release/net10.0"
            if not (runtime / (name + ".dll")).is_file():
                raise ValueError("Missing built test assembly: " + name)
            runtimes[name] = {"path": str(runtime), "before": runtime_manifest(runtime)}
        write_json(output / "runtime-before.json", runtimes)
        failures = []
        for name, test_filter in SUITES:
            results = output / name
            results.mkdir()
            dll = str(Path(runtimes[name]["path"]) / (name + ".dll"))
            command = ["dotnet", "test", dll, "--nologo", "--blame-hang-timeout", "5m",
                       "--logger", "trx;LogFileName=tests.trx", "--results-directory", str(results)]
            if test_filter:
                command.extend(["--filter", test_filter])
            suite = {"filter": test_filter, "phase": run_phase(output, name, command, environment)}
            report["suites"][name] = suite
            try:
                trx_path = results / "tests.trx"
                suite["trxSha256"] = sha256(trx_path)
                suite.update(read_trx(trx_path.read_text(encoding="utf-8-sig")))
                if suite["phase"]["exitCode"]:
                    raise ValueError("Actual test process exited nonzero")
                if name == "GameGuild.API.IntegrationTests":
                    observed = {case["class"].rsplit(".", 1)[-1] for case in suite["cases"]}
                    if observed != set(PG_CLASSES):
                        raise ValueError("PostgreSQL regression classes differ from the declared scope")
            except (ValueError, OSError, ET.ParseError) as error:
                suite["error"] = str(error)
                failures.append(name)
        schema_paths = [output / name for name in (
            "credential-openapi.json", "trusted-openapi.json", "orchestrated-openapi.json")]
        report["schemas"] = {path.name: sha256(path) for path in schema_paths if path.is_file()}
        if len(report["schemas"]) != 3 or len(set(report["schemas"].values())) != 1:
            failures.append("Three actual full-host Swagger captures must exist and match")
        if failures:
            raise ValueError("Failed validation: " + ", ".join(failures))
        report["wholeExit"] = 0
    except Exception as error:
        report["error"] = str(error)
        print(str(error), file=sys.stderr, flush=True)
    finally:
        try:
            after = source_manifest()
            write_json(output / "source-after.json", after)
            runtime_after = {name: runtime_manifest(item["path"]) for name, item in runtimes.items()}
            write_json(output / "runtime-after.json", runtime_after)
            if before != after or any(item["before"] != runtime_after[name] for name, item in runtimes.items()):
                raise ValueError("Source or executed DLL/loader files changed during native validation")
            if git("rev-parse", "HEAD") != args.head or git("status", "--porcelain", "--untracked-files=no"):
                raise ValueError("Checkout identity changed during native validation")
        except Exception as error:
            report["wholeExit"] = 1
            report["integrityError"] = str(error)
        report["completedUtc"] = utc_now()
        write_json(output / "whole-producer.json", report)
    return report["wholeExit"]


if __name__ == "__main__":
    sys.exit(main())
