#!/usr/bin/env bash
# Generate code coverage report for KanbanApi
set -e

export DOTNET_ROOT=${DOTNET_ROOT:-$HOME/.dotnet}
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"

RESULTS_DIR="TestResults"
REPORT_DIR="$RESULTS_DIR/coverage-report"

echo "Running tests with coverage..."
dotnet test KanbanApi.Tests/KanbanApi.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory "$RESULTS_DIR"

COVERAGE_XML=$(find "$RESULTS_DIR" -name "coverage.cobertura.xml" | sort | tail -1)
echo "Coverage data: $COVERAGE_XML"

echo "Generating report..."
DOTNET_ROOT=$DOTNET_ROOT reportgenerator \
  -reports:"$COVERAGE_XML" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:"Html;lcov;TextSummary"

echo ""
cat "$REPORT_DIR/Summary.txt"
echo ""
echo "HTML report: $REPORT_DIR/index.html"
echo "lcov report: $REPORT_DIR/lcov.info"
