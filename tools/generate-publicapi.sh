#!/usr/bin/env bash
#
# generate-publicapi.sh — (re)generate PublicAPI baseline files for every shipped
# Akavache library, across each target framework that builds on this machine.
#
# PublicApiSharp.Analyzers (PAS0001-PAS0005) tracks one baseline per target framework:
#
#     <Project>/PublicAPI/<tfm>/PublicAPI.txt
#
# The file is nested C# describing the assembly's current surface — there is no
# shipped/unshipped split and nothing to promote. This script seeds an empty baseline so
# the analyzer reports the whole surface as PAS0001, then lets `dotnet format analyzers`
# apply the baseline fix, which writes the file.
#
# Both sides of the lean/.Reactive seam are tracked, so the same source change shows up
# as a diff against Akavache.X and Akavache.X.Reactive alike.
#
# Only projects with MSBuild property TrackPublicApi=true are processed; tests,
# benchmarks, samples and compat opt out centrally in src/Directory.Build.props.
#
# Each (project, TFM) pair is independent — `dotnet format` builds an in-memory
# MSBuildWorkspace and only writes its own PublicAPI/<tfm>/PublicAPI.txt — so the pairs
# run in parallel through a bounded pool (override the width with JOBS=<n>).
#
# Usage:
#   tools/generate-publicapi.sh [project-name-filter]
#
# Examples:
#   tools/generate-publicapi.sh                 # all tracked libraries, all buildable TFMs
#   tools/generate-publicapi.sh Sqlite3         # only projects whose path contains 'Sqlite3'
#   JOBS=4 tools/generate-publicapi.sh          # cap parallelism at 4
#
# Notes:
#   * Run on the OS that can build the target frameworks you need. Apple TFMs
#     (net*-ios / -macos / -maccatalyst) build only on macOS or Windows; Windows-desktop
#     TFMs build cross-platform here via EnableWindowsTargeting. Use the PowerShell
#     sibling (generate-publicapi.ps1) on Windows.
#   * A TFM whose workload/SDK is missing is reported as failed (its previous baseline is
#     restored) rather than aborting the whole run; the exit code is non-zero so CI can
#     detect an incomplete run.
#
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_DIR="$(cd "$SCRIPT_DIR/../src" && pwd)"
cd "$SRC_DIR"

# MSBuild properties that `dotnet format` cannot accept via -p:; pass through the env.
export EnableWindowsTargeting=true
export CheckEolTargetFramework=false
export MinVerVersionOverride="${MinVerVersionOverride:-255.255.255-dev}"

FILTER="${1:-}"
# PAS0001 (surface missing from the baseline) and PAS0003 (surface differs) are the two
# fixable rules; both resolve to the same "write the baseline" fix.
export DIAGS="PAS0001 PAS0003"
JOBS="${JOBS:-$(nproc 2>/dev/null || echo 4)}"
[ "$JOBS" -gt 8 ] && JOBS=8

echo "PublicAPI baseline generation"
echo "  src        : $SRC_DIR"
echo "  filter     : ${FILTER:-<none>}"
echo "  diagnostics: $DIAGS"
echo "  MinVer     : $MinVerVersionOverride"
echo "  jobs       : $JOBS"
echo

projects=()
while IFS= read -r p; do projects+=("$p"); done < <(
  find . -name '*.csproj' \
    -not -path '*/tests/*' -not -path '*/benchmarks/*' \
    -not -path '*/samples/*' -not -path '*/compat/*' \
    -not -name '*_wpftmp.csproj' \
    | sort
)

# Collect (project|tfm) work items; the worker seeds and generates each one.
items=()
restore_set=()
skipped=0
for proj in "${projects[@]}"; do
  if [ -n "$FILTER" ] && [[ "$proj" != *"$FILTER"* ]]; then continue; fi

  track="$(dotnet msbuild "$proj" -getProperty:TrackPublicApi -nologo 2>/dev/null | tr -d '[:space:]')"
  if [ "$track" != "true" ]; then
    echo "skip  (TrackPublicApi != true): $proj"
    skipped=$((skipped + 1))
    continue
  fi

  tfms="$(dotnet msbuild "$proj" -getProperty:TargetFrameworks -nologo 2>/dev/null | tr -d '[:space:]')"
  if [ -z "$tfms" ]; then
    tfms="$(dotnet msbuild "$proj" -getProperty:TargetFramework -nologo 2>/dev/null | tr -d '[:space:]')"
  fi
  if [ -z "$tfms" ]; then
    echo "skip  (no TargetFramework(s)): $proj"
    skipped=$((skipped + 1))
    continue
  fi

  projdir="$(dirname "$proj")"
  echo "queue $proj"
  echo "    TFMs: $tfms"
  restore_set+=("$proj")
  IFS=';' read -ra tfm_arr <<<"$tfms"
  for tfm in "${tfm_arr[@]}"; do
    [ -z "$tfm" ] && continue
    mkdir -p "$projdir/PublicAPI/$tfm"
    items+=("$proj|$tfm")
  done
done
echo

if [ "${#items[@]}" -eq 0 ]; then
  echo "Nothing to generate. projects skipped: $skipped"
  exit 0
fi

# Restore once per project so the parallel `dotnet format` workers never race on restore
# (they each load a read-only workspace afterwards).
echo "Restoring ${#restore_set[@]} project(s)..."
for proj in "${restore_set[@]}"; do
  dotnet restore "$proj" -v quiet || echo "    WARN: restore reported issues for $proj"
done
echo

# Worker: regenerate one (project, TFM) baseline.
generate_one() {
  local item="$1"
  local proj="${item%%|*}"
  local tfm="${item##*|}"
  local projdir apidir baseline tag
  projdir="$(dirname "$proj")"
  apidir="$projdir/PublicAPI/$tfm"
  baseline="$apidir/PublicAPI.txt"
  tag="$(printf '%s' "$item" | tr '/|.' '___')"
  local backup="$RESULTS_DIR/$tag.bak"
  # Back up any existing baseline so a build failure (e.g. a TFM that needs a workload
  # this platform lacks) restores it instead of wiping it.
  [ -f "$baseline" ] && cp "$baseline" "$backup"
  # An empty baseline makes the analyzer report the entire current surface as PAS0001,
  # which the fix then writes back in full. The file has to exist: with no baseline at
  # all the analyzer reports PAS0004, which has no fix.
  : >"$baseline"
  if dotnet format analyzers "$proj" -f "$tfm" --diagnostics $DIAGS --severity info -v quiet; then
    printf 'OK   [%s] %s\n' "$tfm" "$proj"
    : >"$RESULTS_DIR/$tag.ok"
  else
    # Restore the prior baseline (if any) so nothing is wiped for a TFM we can't build here.
    if [ -f "$backup" ]; then
      cp "$backup" "$baseline"
    else
      rm -f "$baseline"
    fi
    printf 'FAIL [%s] %s (missing workload/SDK for this platform?)\n' "$tfm" "$proj"
    : >"$RESULTS_DIR/$tag.fail"
  fi
}
export -f generate_one

RESULTS_DIR="$(mktemp -d)"
export RESULTS_DIR
trap 'rm -rf "$RESULTS_DIR"' EXIT

echo "Generating ${#items[@]} (project, TFM) baseline(s) across $JOBS job(s)..."
printf '%s\n' "${items[@]}" | xargs -P "$JOBS" -I{} bash -c 'generate_one "$1"' _ {}
echo

generated="$(find "$RESULTS_DIR" -name '*.ok' | wc -l | tr -d '[:space:]')"
failed="$(find "$RESULTS_DIR" -name '*.fail' | wc -l | tr -d '[:space:]')"

echo "Done. generated: $generated TFM baseline(s), failed: $failed, projects skipped: $skipped"
[ "$failed" -eq 0 ]
