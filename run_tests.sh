#!/bin/bash
# CRAB Test Runner Script
# Runs the CRAB test suite

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Print colored message
print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Print header
print_header() {
    echo ""
    echo "=============================================="
    echo "  CRAB Compiler Test Suite"
    echo "=============================================="
    echo ""
}

# Test a single file
test_file() {
    local file=$1
    local category=$2
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    echo -n "Testing ${category}: $(basename $file)... "
    
    if dotnet run -- check "$file" > /dev/null 2>&1; then
        print_status "$GREEN" "✓ PASS"
        PASSED_TESTS=$((PASSED_TESTS + 1))
        return 0
    else
        print_status "$RED" "✗ FAIL"
        FAILED_TESTS=$((FAILED_TESTS + 1))
        return 1
    fi
}

# Test category
test_category() {
    local category=$1
    local path=$2
    
    print_status "$BLUE" "\n=== Testing ${category} ==="
    
    if [ -d "$path" ]; then
        for file in $(find "$path" -name "*.cs" -type f); do
            test_file "$file" "$category"
        done
    else
        print_status "$YELLOW" "Warning: Directory $path not found"
    fi
}

# Main test execution
print_header

# Parse arguments
if [ $# -eq 0 ]; then
    # Run all tests
    test_category "Automatic Memory" "Testing/Automatic"
    test_category "Manual Memory" "Testing/Manual"
    test_category "Language Features" "Testing/Language"
    test_category "Integration" "Testing/Integration"
    test_category "WASM Output" "Testing/WASM"
elif [ "$1" == "automatic" ]; then
    test_category "Automatic Memory" "Testing/Automatic"
elif [ "$1" == "manual" ]; then
    test_category "Manual Memory" "Testing/Manual"
elif [ "$1" == "language" ]; then
    test_category "Language Features" "Testing/Language"
elif [ "$1" == "integration" ]; then
    test_category "Integration" "Testing/Integration"
elif [ "$1" == "wasm" ]; then
    test_category "WASM Output" "Testing/WASM"
else
    print_status "$RED" "Unknown test category: $1"
    echo "Usage: $0 [automatic|manual|language|integration|wasm]"
    exit 1
fi

# Print summary
echo ""
echo "=============================================="
echo "  Test Summary"
echo "=============================================="
echo "Total Tests:  $TOTAL_TESTS"
print_status "$GREEN" "Passed:       $PASSED_TESTS"
if [ $FAILED_TESTS -gt 0 ]; then
    print_status "$RED" "Failed:       $FAILED_TESTS"
else
    echo "Failed:       $FAILED_TESTS"
fi
echo "=============================================="
echo ""

# Exit with appropriate code
if [ $FAILED_TESTS -eq 0 ]; then
    print_status "$GREEN" "✓ All tests passed!"
    exit 0
else
    print_status "$RED" "✗ Some tests failed"
    exit 1
fi
