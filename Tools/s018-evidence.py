#!/usr/bin/env python3
"""Offline S018 record processing. No playtest, human judgement or G5 approval.

The form command writes only an unperformed session template. A RECORDED form
means the observer finished documenting it, not that the player completed it.
Evidence hashes establish file identity; they cannot establish truth or consent.
Nothing is uploaded, executed, inferred as participant feedback, or overwritten.
"""
import argparse
from collections import Counter, defaultdict
from datetime import datetime, date
import hashlib
import json
from pathlib import Path
import re
import sys

SCHEMA = 1
MILESTONES = (
    "water_acquired inventory_open inventory_close inventory_dwell inventory_confusion "
    "objective_understood threat_detected first_damage first_death return_decision "
    "relay_progress shelter_upgrade equip consume treatment save quit process_restart load "
    "continued_progress"
).split()
QUESTIONS = (
    "memorable_moment weakest_moment confusion intended_objective death_cause "
    "continue_reason leave_reason unmet_expectation voluntary_replay"
).split()
OUTCOMES = {"UNAIDED", "ASSISTED", "INCOMPLETE", "NOT_OBSERVED", "BLOCKED"}
COMPLETION_EVENTS = {
    "onboarding": "water_acquired inventory_open".split(),
    "expedition": ["relay_progress"],
    "canonical_loop": ("water_acquired inventory_open relay_progress shelter_upgrade equip consume treatment "
                       "save quit process_restart load continued_progress").split(),
}
ENVIRONMENT = (
    "os_version cpu gpu ram input_type controller_model controller_connection "
    "resolution quality fov ui_scale sensitivity audio_output"
).split()


def form(candidate, digest, tester):
    observation = dict(status="UNRECORDED", elapsed_seconds=None,
                       observed_until_seconds=None, value=None, behavior=None,
                       help_before_event=None, evidence_refs=[], missing_reason=None)
    return {
        "schema_version": SCHEMA, "record_state": "NOT_RUN", "session_id": None,
        "tester_id": tester, "participant_role": "EXTERNAL_PLAYER",
        "candidate": {"id": candidate, "artifact_sha256": digest, "source_digest": None},
        "phase": None, "platform": None, "segment": None, "exposure": None,
        "scope": None, "declared_scope_blockers": [],
        "profile": {"survival_experience": None, "fps_experience": None,
                    "similar_games": None, "project_familiarity": None, "segment_rationale": None},
        "environment": dict.fromkeys(ENVIRONMENT),
        "initial_state": {"scene": None, "seed": None, "save_fixture_sha256": None,
                          "tutorial_state": None, "reset_evidence_refs": []},
        "observation_mode": None,
        "consent": {"state": "UNKNOWN", "allowed_media": [], "access": "owner",
                    "delete_on": None, "withdrawal_instructions_provided": None},
        "provenance": {"human_recorded": False, "observer_code": None, "recorded_at": None,
                       "evidence": []},
        "timing": {"started_at": None, "ended_at": None, "active_seconds": None, "pauses": []},
        "observations": {key: {**observation, "evidence_refs": []} for key in MILESTONES},
        "intervention_review_complete": False, "interventions": [],
        "outcomes": {key: "NOT_OBSERVED" for key in ("onboarding", "expedition", "canonical_loop")},
        "completion_elapsed_seconds": dict.fromkeys(("onboarding", "expedition", "canonical_loop")),
        "end_state": None, "issue_review_complete": False, "issues": [],
        "comprehension": {key: {"value": "UNKNOWN", "statement": None, "statement_kind": None,
                               "evidence_refs": [], "missing_reason": None}
                          for key in ("death_cause", "next_action")},
        "followup": {"complete": False, "missing_reason": None,
                     "answers": {key: {"status": "UNRECORDED", "answer": None, "statement_kind": None,
                                       "evidence_refs": [], "missing_reason": None} for key in QUESTIONS}},
        "setting_changes": [], "confounders": [], "observer_interpretations": [],
        "missing_data": {},
    }


def filled(value):
    return isinstance(value, str) and bool(value.strip())


def sha(value):
    return isinstance(value, str) and re.fullmatch(r"[0-9a-f]{64}", value) is not None


def seconds(value):
    return type(value) in (int, float) and 0 <= value < float("inf")


def timestamp(value):
    parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    if parsed.utcoffset() is None:
        raise ValueError("timestamp must include UTC offset")
    return parsed


def file_sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def validate(record, source):
    """Validate a finished human record, including explicit missing observations."""
    errors = []

    def check(condition, message):
        if not condition:
            errors.append(message)

    def required(value, name):
        check(filled(value), name + " is required")

    def missing(value, field):
        if value is None or value == "":
            required(record["missing_data"].get(field), "missing_data." + field)

    try:
        baseline = form("candidate", "0" * 64, "T01")
        def shape(actual, expected, field="record"):
            if isinstance(expected, dict):
                if not isinstance(actual, dict):
                    raise ValueError(field + " must be an object")
                for key, value in expected.items():
                    if key not in actual:
                        raise ValueError(field + "." + key + " is missing")
                    shape(actual[key], value, field + "." + key)
            elif isinstance(expected, list) and not isinstance(actual, list):
                raise ValueError(field + " must be an array")
        shape(record, baseline)
        check(type(record["schema_version"]) is int and record["schema_version"] == SCHEMA, "unsupported schema_version")
        check(record["record_state"] == "RECORDED", "record is not RECORDED; templates are not evidence")
        check(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,95}", record["session_id"] or "") is not None,
              "session_id must be a stable anonymous token")
        check(record["tester_id"] in {"T%02d" % n for n in range(1, 9)}, "tester_id must be T01–T08")
        check(record["participant_role"] == "EXTERNAL_PLAYER", "owner QA is not external-player evidence")
        required(record["candidate"]["id"], "candidate.id")
        for field in ("artifact_sha256", "source_digest"):
            check(sha(record["candidate"][field]), "candidate." + field + " must be lowercase SHA-256")
        for field in ("platform", "segment"):
            required(record[field], field)
        for field, choices in {"phase": {"PT1", "PT2"}, "exposure": {"FRESH", "REPEAT"},
                               "scope": {"CANONICAL", "NARROW", "DEFECT_INVESTIGATION"},
                               "observation_mode": {"NATURAL", "THINK_ALOUD"}}.items():
            check(record[field] in choices, field + " must be one of " + ", ".join(sorted(choices)))
        for section in ("profile", "environment", "initial_state"):
            for key, value in record[section].items():
                if key != "reset_evidence_refs":
                    missing(value, section + "." + key)
        fixture = record["initial_state"]["save_fixture_sha256"]
        check(fixture is None or sha(fixture), "initial_state.save_fixture_sha256 must be SHA-256 or null")
        consent = record["consent"]
        check(consent["state"] == "GRANTED", "only consent GRANTED records may be processed")
        check("notes" in consent["allowed_media"], "permission for observation notes is required")
        required(consent["access"], "consent.access")
        date.fromisoformat(consent["delete_on"])
        check(consent["withdrawal_instructions_provided"] is True, "withdrawal instructions must be provided")
        provenance = record["provenance"]
        check(provenance["human_recorded"] is True, "human_recorded must explicitly be true")
        required(provenance["observer_code"], "provenance.observer_code (anonymous)")
        recorded = timestamp(provenance["recorded_at"])
        start, end = (timestamp(record["timing"][key]) for key in ("started_at", "ended_at"))
        duration = (end - start).total_seconds()
        check(duration > 0 and recorded >= end, "session times must be ordered and recorded_at >= ended_at")
        active = record["timing"]["active_seconds"]
        check(seconds(active) and active <= duration, "active_seconds must be within session duration")
        previous_end, pause_total = 0, 0
        for pause in record["timing"]["pauses"]:
            begin, length = pause["start_seconds"], pause["duration_seconds"]
            if not seconds(begin) or not seconds(length):
                raise ValueError("pause times must be nonnegative numbers, not booleans")
            check(begin >= previous_end and length > 0 and begin + length <= duration, "pauses overlap or exceed session")
            previous_end, pause_total = begin + length, pause_total + length
            required(pause["reason"], "pause.reason")
        check(seconds(active) and abs(active + pause_total - duration) <= 1, "active time plus pauses must match wall time")
        ids = set()
        check(bool(provenance["evidence"]), "at least one human observation evidence file is required")
        for item in provenance["evidence"]:
            required(item["id"], "evidence.id")
            check(item["id"] not in ids, "duplicate evidence ID: " + str(item["id"]))
            ids.add(item["id"])
            required(item["description"], "evidence.description")
            check(item["media"] in consent["allowed_media"], "evidence media exceeds consent")
            check(sha(item["sha256"]), "evidence.sha256 must be lowercase SHA-256")
            path = (source.parent / item["path"]).resolve()
            check(path != source.resolve(), "session JSON cannot be its own source evidence")
            check(path.is_file(), "missing local evidence: " + str(path))
            if path.is_file():
                check(file_sha(path) == item["sha256"], "evidence hash mismatch: " + str(path))

        def refs(items, field, required_refs=False):
            check(isinstance(items, list) and all(isinstance(i, str) and i in ids for i in items),
                  field + " must reference declared evidence IDs")
            if required_refs:
                check(bool(items), field + " needs source evidence")

        refs(record["initial_state"]["reset_evidence_refs"], "initial_state.reset_evidence_refs")
        check(record["intervention_review_complete"] is True, "intervention review must be explicitly complete")
        intervention_times = []
        for item in record["interventions"]:
            elapsed = item["elapsed_seconds"]
            check(seconds(elapsed) and elapsed <= duration, "intervention elapsed_seconds outside session")
            intervention_times.append(elapsed)
            for key in ("reason", "exact_help"):
                required(item[key], "intervention." + key)
            check(item["kind"] in {"GAMEPLAY", "TECHNICAL"}, "invalid intervention kind")
            check(type(item["resumed"]) is bool, "intervention.resumed must be boolean")
            refs(item["evidence_refs"], "intervention.evidence_refs", True)
        for name, item in record["observations"].items():
            state = item["status"]
            check(state in {"OBSERVED", "NOT_REACHED", "NOT_OBSERVED", "NOT_APPLICABLE"}, name + " is unrecorded/invalid")
            refs(item["evidence_refs"], name + ".evidence_refs", state in {"OBSERVED", "NOT_REACHED"})
            if state == "OBSERVED":
                elapsed = item["elapsed_seconds"]
                check(seconds(elapsed) and elapsed <= duration, name + " elapsed_seconds outside session")
                required(item["behavior"], name + ".behavior")
                check(type(item["help_before_event"]) is bool, name + ".help_before_event must be boolean")
                if seconds(elapsed) and all(seconds(t) for t in intervention_times):
                    check(item["help_before_event"] == any(t <= elapsed for t in intervention_times),
                          name + ".help_before_event contradicts intervention timeline")
            else:
                check(item["elapsed_seconds"] is None, name + " did not occur; elapsed_seconds must be null")
                required(item["missing_reason"], name + ".missing_reason")
                if state in {"NOT_REACHED", "NOT_OBSERVED"}:
                    limit = item["observed_until_seconds"]
                    check(seconds(limit) and limit <= duration, name + " requires observed_until_seconds")
        for name, outcome in record["outcomes"].items():
            check(outcome in OUTCOMES, "invalid outcome: " + name)
            completed = record["completion_elapsed_seconds"][name]
            if outcome in {"UNAIDED", "ASSISTED"}:
                check(seconds(completed) and completed <= duration, name + " needs completion elapsed time")
                for milestone in COMPLETION_EVENTS[name]:
                    event = record["observations"][milestone]
                    check(event["status"] == "OBSERVED", name + " completion requires " + milestone)
                    if seconds(event["elapsed_seconds"]) and seconds(completed):
                        check(event["elapsed_seconds"] <= completed, name + " completion precedes " + milestone)
                if seconds(completed) and all(seconds(t) for t in intervention_times):
                    helped = any(t <= completed for t in intervention_times)
                    check((outcome == "ASSISTED") == helped, name + " outcome contradicts intervention timeline")
            else:
                check(completed is None, name + " did not complete; completion time must be null")
        canonical = record["outcomes"]["canonical_loop"]
        if canonical in {"UNAIDED", "ASSISTED"}:
            check(record["scope"] == "CANONICAL" and not record["declared_scope_blockers"],
                  "canonical completion contradicts research scope/blockers")
            completed = record["completion_elapsed_seconds"]["canonical_loop"]
            sequence = [record["observations"][key]["elapsed_seconds"]
                        for key in "save quit process_restart load continued_progress".split()]
            if all(seconds(t) for t in sequence) and seconds(completed):
                check(sequence == sorted(sequence) and sequence[-1] <= completed,
                      "canonical save/restart/load timeline contradicts completion")
        check(record["end_state"] in {"COMPLETED", "DEATH", "VOLUNTARY_STOP", "TIME_CAP", "BLOCKED", "CRASH", "OTHER"},
              "invalid end_state")
        check(record["issue_review_complete"] is True, "issue review must explicitly be complete")
        for issue in record["issues"]:
            for key in ("id", "description"):
                required(issue[key], "issue." + key)
            check(issue["severity"] in {"S0", "S1", "S2", "S3", "S4"}, "invalid issue severity")
            for key in ("event_count", "exposed_opportunities"):
                check(type(issue[key]) is int and issue[key] > 0, "issue." + key + " must be positive")
            refs(issue["evidence_refs"], "issue.evidence_refs", True)
        for name, item in record["comprehension"].items():
            check(item["value"] in {"YES", "PARTIAL", "NO", "NOT_APPLICABLE", "UNKNOWN"}, "invalid comprehension." + name)
            if item["value"] in {"YES", "PARTIAL", "NO"}:
                required(item["statement"], "comprehension." + name + ".statement")
            if name == "death_cause" and item["value"] == "NOT_APPLICABLE":
                check(record["observations"]["first_death"]["status"] != "OBSERVED", "death cause cannot be N/A after death")
            statement(item, "statement", "comprehension." + name, refs, required, check)
        followup = record["followup"]
        check(type(followup["complete"]) is bool, "followup.complete must be boolean")
        if not followup["complete"]:
            required(followup["missing_reason"], "followup.missing_reason")
        for name, item in followup["answers"].items():
            check(item["status"] in {"ANSWERED", "DECLINED", "NOT_APPLICABLE", "NOT_COLLECTED"}, "followup question unrecorded: " + name)
            check(not followup["complete"] or item["status"] != "NOT_COLLECTED", "followup.complete contradicts missing interview")
            if item["status"] == "ANSWERED":
                required(item["answer"], "followup." + name + ".answer")
            else:
                check(item["answer"] is None, "unanswered followup must have null answer: " + name)
            statement(item, "answer", "followup." + name, refs, required, check)
        for change in record["setting_changes"]:
            check(seconds(change["elapsed_seconds"]) and change["elapsed_seconds"] <= duration, "setting change needs time")
            for key in ("setting", "before", "after", "reason"):
                required(change[key], "setting_change." + key)
            refs(change["evidence_refs"], "setting_change.evidence_refs", True)
    except (KeyError, TypeError, ValueError, AttributeError, OSError) as exc:
        errors.append("Malformed or unreadable record: " + str(exc))
    return errors


def statement(item, key, field, refs, required, check):
    if filled(item[key]):
        check(item["statement_kind"] in {"VERBATIM", "PARAPHRASE"}, field + " needs statement_kind")
        refs(item["evidence_refs"], field + ".evidence_refs", True)
    else:
        required(item["missing_reason"], field + ".missing_reason")


def summarize(records):
    groups = defaultdict(list)
    for path, record in records:
        candidate = record["candidate"]
        key = tuple(candidate[k] for k in ("id", "artifact_sha256", "source_digest"))
        key += tuple(record[k] for k in ("phase", "platform", "segment", "exposure", "scope"))
        groups[key].append((path, record))
    output = []
    names = "candidate_id artifact_sha256 source_digest phase platform segment exposure scope".split()
    for key, entries in sorted(groups.items()):
        values = [record for _, record in entries]
        output.append({**dict(zip(names, key)), "session_count": len(values),
                       "unique_testers": sorted({r["tester_id"] for r in values}),
                       "outcomes": {name: dict(Counter(r["outcomes"][name] for r in values))
                                    for name in ("onboarding", "expedition", "canonical_loop")},
                       "followup_completed": sum(r["followup"]["complete"] for r in values),
                       "sessions": [{"file": str(path), **{k: r[k] for k in (
                           "session_id", "tester_id", "completion_elapsed_seconds", "observations", "interventions", "issues", "comprehension",
                           "end_state", "followup", "missing_data", "confounders", "setting_changes")}}
                                    for path, r in entries]})
    return {"schema_version": SCHEMA, "status": "RECORDED_EVIDENCE_SUMMARY", "g5": "NOT_EVALUATED",
            "limitations": "Human-entered records; hashes do not verify claims. No inferred fun, prevalence, improvement or gate approval.",
            "groups": output}


def read_json(path):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("duplicate JSON key: " + key)
            result[key] = value
        return result
    with path.open(encoding="utf-8") as stream:
        return json.load(stream, object_pairs_hook=unique,
                         parse_constant=lambda value: (_ for _ in ()).throw(ValueError("invalid JSON number: " + value)))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    template = commands.add_parser("form", help="create an empty NOT_RUN form; no participation is asserted")
    template.add_argument("--candidate-id", required=True)
    template.add_argument("--artifact-sha256", required=True)
    template.add_argument("--tester", choices=["T%02d" % n for n in range(1, 9)], required=True)
    template.add_argument("--output", type=Path, required=True)
    for name in ("validate", "summarize"):
        command = commands.add_parser(name, help="process finished human records; never evaluates G5")
        command.add_argument("sessions", type=Path, nargs="+")
        command.add_argument("--output", type=Path, required=name == "summarize")
    args = parser.parse_args()
    try:
        if args.command == "form":
            if not filled(args.candidate_id) or not sha(args.artifact_sha256):
                raise ValueError("candidate ID and lowercase 64-character artifact SHA-256 are required")
            result, code = form(args.candidate_id, args.artifact_sha256, args.tester), 0
        else:
            records, reports, seen, fresh, candidates = [], [], set(), set(), {}
            for path in args.sessions:
                record = read_json(path)
                errors = validate(record, path)
                session = record.get("session_id") if isinstance(record, dict) else None
                if isinstance(session, str):
                    if session in seen:
                        errors.append("duplicate session_id; aggregation refused: " + session)
                    seen.add(session)
                if not errors and record["exposure"] == "FRESH":
                    if record["tester_id"] in fresh:
                        errors.append("same tester has multiple FRESH sessions; classify repeats explicitly")
                    fresh.add(record["tester_id"])
                if not errors:
                    candidate = record["candidate"]
                    identity = (candidate["artifact_sha256"], candidate["source_digest"])
                    # A named artifact is immutable; platforms use distinct candidate IDs.
                    if candidate["id"] in candidates and candidates[candidate["id"]] != identity:
                        errors.append("candidate ID maps to conflicting artifact/source hashes")
                    candidates[candidate["id"]] = identity
                reports.append({"file": str(path), "errors": errors})
                records.append((path, record))
            if not any(report["errors"] for report in reports):
                earliest = {}
                for _, record in sorted(records, key=lambda pair: timestamp(pair[1]["timing"]["started_at"])):
                    tester = record["tester_id"]
                    if tester in earliest:
                        if record["exposure"] == "FRESH":
                            reports.append({"file": "batch", "errors": [tester + " FRESH session follows an earlier session"]})
                        if timestamp(record["timing"]["started_at"]) < earliest[tester]:
                            reports.append({"file": "batch", "errors": [tester + " sessions overlap"]})
                    earliest[tester] = timestamp(record["timing"]["ended_at"])
            code = 1 if any(report["errors"] for report in reports) else 0
            result = {"status": "INVALID" if code else "VALID_RECORDS", "g5": "NOT_EVALUATED", "records": reports}
            if args.command == "summarize" and not code:
                result = summarize(records)
        content = json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False) + "\n"
        if args.output is not None and (args.command != "summarize" or code == 0):
            with args.output.open("x", encoding="utf-8") as stream:
                stream.write(content)
            print(str(args.output))
        else:
            print(content, end="")
        return code
    except (OSError, ValueError, TypeError) as exc:
        print("ERROR: " + str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
