#!/usr/bin/env bash
set -euo pipefail

# Launch one test host and multiple automated test clients for CorruptPolice builds.
#
# Usage:
#   ./launch_test_clients.sh /path/to/CorruptPolice.exe 3
#
# Arguments:
#   $1 - Path to the built game executable
#   $2 - Number of test clients to launch (default: 2)

BUILD_PATH="${1:-}"
CLIENT_COUNT="${2:-2}"
ADDRESS="${ADDRESS:-127.0.0.1}"
DELAY="${DELAY:-0.75}"

if [[ -z "${BUILD_PATH}" ]]; then
  echo "Usage: $0 /path/to/CorruptPolice.exe [client_count]"
  exit 1
fi

if [[ ! -x "${BUILD_PATH}" ]]; then
  echo "Executable not found or not executable: ${BUILD_PATH}"
  exit 1
fi

echo "Starting test host..."
"${BUILD_PATH}" -testhost -name TestHost -autostart true -minplayers "${CLIENT_COUNT}" -delay "${DELAY}" &
HOST_PID=$!

sleep 2

for ((i=1; i<=CLIENT_COUNT; i++)); do
  echo "Starting test client ${i}..."
  "${BUILD_PATH}" -testclient -address "${ADDRESS}" -name "TestClient_${i}" -autoready true -autoplay true -delay "${DELAY}" &
  sleep 1
done

echo "Host PID: ${HOST_PID}"
echo "Launched ${CLIENT_COUNT} test client(s). Press Ctrl+C to stop."

wait "${HOST_PID}"
