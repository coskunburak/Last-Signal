"""Synthetic unit fixtures only. These are never S018 human/player evidence.

Prepared for owner execution after implementation; no Unity process is started.
"""
import importlib.util
import contextlib
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location("s018_evidence", Path(__file__).parents[1] / "s018-evidence.py")
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class EvidenceContractTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name) / "synthetic.json"
        notes = self.path.parent / "synthetic-notes.txt"
        notes.write_text("SYNTHETIC UNIT FIXTURE; NOT HUMAN EVIDENCE", encoding="utf-8")
        r = MODULE.form("SYNTHETIC", "0" * 64, "T01")
        r.update(record_state="RECORDED", session_id="synthetic-01", phase="PT1",
                 platform="SYNTHETIC", segment="SYNTHETIC", exposure="FRESH",
                 scope="NARROW", observation_mode="NATURAL", end_state="TIME_CAP",
                 intervention_review_complete=True, issue_review_complete=True)
        r["candidate"]["source_digest"] = "1" * 64
        for section in ("profile", "environment", "initial_state"):
            for key, value in r[section].items():
                if value is None:
                    r["missing_data"][section + "." + key] = "Synthetic missing field"
        r["consent"].update(state="GRANTED", allowed_media=["notes"], delete_on="2099-01-01",
                            withdrawal_instructions_provided=True)
        r["provenance"].update(human_recorded=True, observer_code="SYNTHETIC",
                               recorded_at="2026-10-08T12:02:00Z", evidence=[{
                                   "id": "fixture", "description": "Synthetic notes", "media": "notes",
                                   "path": notes.name, "sha256": MODULE.file_sha(notes)}])
        r["timing"].update(started_at="2026-10-08T12:00:00Z", ended_at="2026-10-08T12:01:00Z",
                           active_seconds=60)
        for item in r["observations"].values():
            item.update(status="NOT_OBSERVED", observed_until_seconds=60, missing_reason="Synthetic missing event")
        for item in r["comprehension"].values():
            item["missing_reason"] = "Synthetic missing interview"
        r["followup"]["missing_reason"] = "Synthetic missing interview"
        for item in r["followup"]["answers"].values():
            item.update(status="NOT_COLLECTED", missing_reason="Synthetic missing interview")
        self.record = r

    def errors(self):
        return MODULE.validate(self.record, self.path)

    def test_documented_missing_data_is_not_success(self):
        self.assertEqual([], self.errors())
        summary = MODULE.summarize([(self.path, self.record)])
        self.assertEqual("NOT_EVALUATED", summary["g5"])
        self.assertEqual({"NOT_OBSERVED": 1}, summary["groups"][0]["outcomes"]["canonical_loop"])

    def test_template_is_not_evidence(self):
        self.assertTrue(MODULE.validate(MODULE.form("SYNTHETIC", "0" * 64, "T01"), self.path))

    def test_withdrawn_consent_and_owner_qa_are_rejected(self):
        for key, value in (("participant_role", "OWNER_QA"), ("record_state", "NOT_RUN")):
            with self.subTest(key=key):
                previous = self.record[key]
                self.record[key] = value
                self.assertTrue(self.errors())
                self.record[key] = previous
        self.record["consent"]["state"] = "WITHDRAWN"
        self.assertTrue(self.errors())

    def test_modified_evidence_is_rejected(self):
        (self.path.parent / "synthetic-notes.txt").write_text("modified", encoding="utf-8")
        self.assertTrue(any("hash mismatch" in e for e in self.errors()))

    def test_incomplete_canonical_route_is_rejected(self):
        self.record["scope"] = "CANONICAL"
        self.record["outcomes"]["canonical_loop"] = "UNAIDED"
        self.record["completion_elapsed_seconds"]["canonical_loop"] = 50
        errors = self.errors()
        for milestone in ("consume", "treatment", "process_restart", "continued_progress"):
            self.assertTrue(any("requires " + milestone in e for e in errors))

    def test_assistance_cannot_be_hidden(self):
        self.record["interventions"] = [{"elapsed_seconds": 5, "kind": "GAMEPLAY", "reason": "Synthetic",
                                         "exact_help": "Synthetic", "resumed": True, "evidence_refs": ["fixture"]}]
        self.record["outcomes"]["onboarding"] = "UNAIDED"
        self.record["completion_elapsed_seconds"]["onboarding"] = 10
        self.assertTrue(any("outcome contradicts" in e for e in self.errors()))

    def test_candidate_hashes_remain_separate(self):
        other = json.loads(json.dumps(self.record))
        other["candidate"]["artifact_sha256"] = "2" * 64
        self.assertEqual(2, len(MODULE.summarize([(self.path, self.record), (self.path, other)])["groups"]))

    def test_duplicate_keys_and_nonfinite_json_are_rejected(self):
        for data in ('{"id":1,"id":2}', '{"seconds":NaN}'):
            self.path.write_text(data, encoding="utf-8")
            with self.assertRaises(ValueError):
                MODULE.read_json(self.path)

    def test_duplicate_sessions_refuse_summary_output(self):
        self.path.write_text(json.dumps(self.record), encoding="utf-8")
        output = self.path.parent / "summary.json"
        args = ["s018-evidence.py", "summarize", str(self.path), str(self.path), "--output", str(output)]
        with patch("sys.argv", args), contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(1, MODULE.main())
        self.assertFalse(output.exists())

    def test_form_never_overwrites_an_existing_file(self):
        self.path.write_text("KEEP", encoding="utf-8")
        args = ["s018-evidence.py", "form", "--candidate-id", "SYNTHETIC",
                "--artifact-sha256", "0" * 64, "--tester", "T01", "--output", str(self.path)]
        with patch("sys.argv", args), contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(2, MODULE.main())
        self.assertEqual("KEEP", self.path.read_text(encoding="utf-8"))

    def test_success_requires_its_observed_events(self):
        for name in ("onboarding", "expedition"):
            self.record["outcomes"][name] = "UNAIDED"
            self.record["completion_elapsed_seconds"][name] = 50
        errors = self.errors()
        self.assertTrue(any("onboarding completion requires water_acquired" in e for e in errors))
        self.assertTrue(any("expedition completion requires relay_progress" in e for e in errors))

    def test_complete_canonical_timeline_can_validate(self):
        self.record["scope"] = "CANONICAL"
        self.record["outcomes"]["canonical_loop"] = "UNAIDED"
        self.record["completion_elapsed_seconds"]["canonical_loop"] = 55
        for index, name in enumerate(MODULE.COMPLETION_EVENTS["canonical_loop"]):
            self.record["observations"][name].update(status="OBSERVED", elapsed_seconds=index + 1,
                behavior="Synthetic event", help_before_event=False, evidence_refs=["fixture"])
        self.assertEqual([], self.errors())
        self.record["observations"]["load"]["elapsed_seconds"] = 1
        self.assertTrue(any("timeline contradicts" in e for e in self.errors()))

    def test_same_candidate_cannot_change_hash_in_batch(self):
        other = json.loads(json.dumps(self.record))
        other["session_id"] = "synthetic-02"
        other["tester_id"] = "T02"
        other["candidate"]["artifact_sha256"] = "2" * 64
        second = self.path.parent / "second.json"
        self.path.write_text(json.dumps(self.record), encoding="utf-8")
        second.write_text(json.dumps(other), encoding="utf-8")
        args = ["s018-evidence.py", "validate", str(self.path), str(second)]
        with patch("sys.argv", args), contextlib.redirect_stdout(io.StringIO()) as output:
            self.assertEqual(1, MODULE.main())
        self.assertIn("conflicting artifact/source hashes", output.getvalue())


if __name__ == "__main__":
    unittest.main()
