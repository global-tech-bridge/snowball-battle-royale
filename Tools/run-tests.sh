#!/usr/bin/env bash
# Runs the Unity test suite from the command line (CI-friendly).
#
#   ./Tools/run-tests.sh                 # EditMode (default)
#   ./Tools/run-tests.sh PlayMode        # PlayMode
#   UNITY_PATH=/path/to/Unity ./Tools/run-tests.sh
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PLATFORM="${1:-EditMode}"

UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' "${PROJECT_ROOT}/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
DEFAULT_UNITY="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
UNITY_PATH="${UNITY_PATH:-$DEFAULT_UNITY}"

if [[ ! -x "${UNITY_PATH}" ]]; then
  echo "Unity ${UNITY_VERSION} not found at ${UNITY_PATH}." >&2
  echo "Install it via Unity Hub, or set UNITY_PATH to the editor binary." >&2
  exit 1
fi

RESULTS_DIR="${PROJECT_ROOT}/Logs"
mkdir -p "${RESULTS_DIR}"
RESULTS_FILE="${RESULTS_DIR}/test-results-${PLATFORM}.xml"

# Remove the previous run's results first. If Unity cannot start (e.g. the project is already open
# in the Editor) it writes nothing, and a stale file would otherwise be reported as this run.
rm -f "${RESULTS_FILE}"

echo "Running ${PLATFORM} tests with Unity ${UNITY_VERSION}..."

# -runTests must not be combined with -quit; Unity exits on its own.
set +e
"${UNITY_PATH}" \
  -runTests \
  -batchmode \
  -nographics \
  -projectPath "${PROJECT_ROOT}" \
  -testPlatform "${PLATFORM}" \
  -testResults "${RESULTS_FILE}" \
  -logFile "${RESULTS_DIR}/test-run-${PLATFORM}.log"
EXIT_CODE=$?
set -e

if [[ -f "${RESULTS_FILE}" ]]; then
  python3 - "${RESULTS_FILE}" <<'PY'
import sys, xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
print("total={} passed={} failed={} skipped={} inconclusive={}".format(
    root.get("total"), root.get("passed"), root.get("failed"),
    root.get("skipped"), root.get("inconclusive")))
for case in root.iter("test-case"):
    if case.get("result") not in ("Passed", "Skipped"):
        print("FAILED: {}".format(case.get("fullname")))
        msg = case.find(".//message")
        if msg is not None and msg.text:
            print("  " + msg.text.strip().splitlines()[0])
PY
  echo "Results: ${RESULTS_FILE}"
else
  echo "No results file produced. See ${RESULTS_DIR}/test-run-${PLATFORM}.log" >&2
  if [[ -f "${PROJECT_ROOT}/Temp/UnityLockfile" ]]; then
    echo "The project appears to be open in the Unity Editor. Close it, or run the tests from the Editor's Test Runner." >&2
  fi
  [[ "${EXIT_CODE}" -eq 0 ]] && EXIT_CODE=1
fi

exit "${EXIT_CODE}"
