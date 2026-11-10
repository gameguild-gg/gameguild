"""Failure controls for credential CI evidence; these are not API test results."""

import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from defusedxml.common import DefusedXmlException


SPEC = importlib.util.spec_from_file_location(
    "credential_evidence", Path(__file__).parents[1] / "credential-stepup-evidence.py"
)
evidence = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(evidence)


def trx(outcome="Passed", entry="execution", passed="1", total="1"):
    return f'''<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
      <Results><UnitTestResult testId="case" executionId="execution"
          testName="guard" outcome="{outcome}" /></Results>
      <TestDefinitions><UnitTest id="case" name="guard">
        <Execution id="execution"/><TestMethod className="Security.Guards" name="guard"/>
      </UnitTest></TestDefinitions>
      <TestEntries><TestEntry testId="case" executionId="{entry}"/></TestEntries>
      <ResultSummary outcome="Completed"><Counters total="{total}" executed="{total}"
        passed="{passed}" failed="0" notExecuted="0" error="0" timeout="0"
        aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0"
        disconnected="0" warning="0" completed="0" inProgress="0" pending="0"/>
      </ResultSummary></TestRun>'''


class EvidenceTests(unittest.TestCase):
    def test_complete_passing_run_retains_case_identity(self):
        result = evidence.read_trx(trx())
        self.assertEqual(result["counts"], {"Passed": 1})
        self.assertEqual(result["cases"][0]["testId"], "case")
        self.assertEqual(result["cases"][0]["class"], "Security.Guards")

    def test_fail_skip_and_nonterminal_rows_are_rejected(self):
        for outcome in ("Failed", "NotExecuted", "Inconclusive", "Timeout"):
            with self.subTest(outcome=outcome), self.assertRaises(ValueError):
                evidence.read_trx(trx(outcome=outcome))

    def test_entry_from_another_execution_is_rejected(self):
        with self.assertRaises(ValueError):
            evidence.read_trx(trx(entry="another-execution"))

    def test_missing_or_inconsistent_counters_are_rejected(self):
        for content in (trx(passed="0"), trx(total="2"), trx().replace('passed="1"', "")):
            with self.subTest(content=content), self.assertRaises(ValueError):
                evidence.read_trx(content)

    def test_unpaired_duplicate_result_is_rejected(self):
        content = trx().replace("</Results>", '<UnitTestResult testId="case" '
                                'executionId="execution" testName="guard" '
                                'outcome="Passed"/></Results>')
        with self.assertRaises(ValueError):
            evidence.read_trx(content)

    def test_runtime_manifest_detects_changed_added_and_missing_files(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            dll = root / "guard.dll"
            loader = root / "guard.deps.json"
            dll.write_bytes(b"original")
            loader.write_text("{}", encoding="utf-8")
            original = evidence.runtime_manifest(root)
            self.assertEqual(set(original), {"guard.dll", "guard.deps.json"})
            dll.write_bytes(b"different")
            self.assertNotEqual(evidence.runtime_manifest(root), original)
            dll.write_bytes(b"original")
            loader.unlink()
            self.assertNotEqual(evidence.runtime_manifest(root), original)
            loader.write_text("{}", encoding="utf-8")
            (root / "extra.dll").write_bytes(b"new")
            self.assertNotEqual(evidence.runtime_manifest(root), original)


    def test_dtd_and_internal_entities_are_rejected(self):
        content = '<!DOCTYPE TestRun [<!ENTITY evidence "guard">]>' + trx().replace(
            'testName="guard"', 'testName="&evidence;"')
        with self.assertRaises(DefusedXmlException):
            evidence.read_trx(content)

    def test_external_references_are_rejected_before_file_access(self):
        content = '<!DOCTYPE TestRun [<!ENTITY evidence SYSTEM "file:///nonexistent-evidence-control">]>' + trx().replace(
            'testName="guard"', 'testName="&evidence;"')
        with self.assertRaises(DefusedXmlException):
            evidence.read_trx(content)

    def test_cli_preserves_and_rejects_nonzero_process_exit(self):
        # Metadata-only control; this does not launch or approve a native process.
        with tempfile.TemporaryDirectory() as directory:
            start = ["evidence", "phase-start", "--output", directory, "--name", "solution-build",
                     "--pid", "1", "--started", "control-only", "--command", "dotnet", "build"]
            with patch("sys.argv", start):
                self.assertEqual(evidence.main(), 0)
            end = ["evidence", "phase-end", "--output", directory, "--name", "solution-build", "--exit-code", "7"]
            with patch("sys.argv", end):
                self.assertEqual(evidence.main(), 0)
            receipt = json.loads((Path(directory) / "solution-build-exit.json").read_text(encoding="utf-8"))
            self.assertEqual(receipt["exitCode"], 7)
            self.assertEqual(receipt["command"], ["dotnet", "build"])
            with self.assertRaises(ValueError):
                evidence.check_phase(Path(directory), "solution-build", ["dotnet", "build"])


if __name__ == "__main__":
    unittest.main()
