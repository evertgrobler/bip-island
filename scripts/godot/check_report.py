#!/usr/bin/env python3
"""Checks a self-test report written by the exported game (--bip-report).

Usage: check_report.py REPORT [--version V] [--expect-save]
Fails (exit 1) unless the game found its fonts, content and voice clips; with --version, unless the
installed version is V (the update arrived); with --expect-save, unless the saved file survived.
"""
import argparse
import json
import sys

parser = argparse.ArgumentParser()
parser.add_argument("report")
parser.add_argument("--version")
parser.add_argument("--expect-save", action="store_true")
args = parser.parse_args()

with open(args.report, encoding="utf-8-sig") as f:
    report = json.load(f)
print(json.dumps(report, indent=2))

problems = []
if not report.get("ok"):
    problems.append(f"the game reported a problem: {report.get('error') or 'fonts, content or clips missing'}")
if report.get("clipCount", 0) < 700:
    problems.append(f"only {report.get('clipCount')} voice clips in the build")
if args.version and report.get("installedVersion") != args.version:
    problems.append(f"installed version is {report.get('installedVersion')}, expected {args.version}")
if args.expect_save and not report.get("saveMarker"):
    problems.append("the file saved before the update is gone")

for p in problems:
    print(f"::error::{p}")
sys.exit(1 if problems else 0)
