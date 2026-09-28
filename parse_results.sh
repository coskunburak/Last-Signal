#!/bin/bash
function count_passed() {
    file=$1
    if [ -f "$file" ]; then
        grep "test-run" "$file" | sed -n 's/.*passed="\([0-9]*\)".*/\1/p'
    else
        echo "0"
    fi
}
function count_failed() {
    file=$1
    if [ -f "$file" ]; then
        grep "test-run" "$file" | sed -n 's/.*failed="\([0-9]*\)".*/\1/p'
    else
        echo "0"
    fi
}

echo "R05 EditMode:" $(count_passed "r05-editmode.xml") "PASS," $(count_failed "r05-editmode.xml") "FAIL"
echo "R05 PlayMode:" $(count_passed "r05-playmode.xml") "PASS," $(count_failed "r05-playmode.xml") "FAIL"
echo "Full EditMode:" $(count_passed "full-editmode-final.xml") "PASS," $(count_failed "full-editmode-final.xml") "FAIL"
echo "Full PlayMode:" $(count_passed "full-playmode-final.xml") "PASS," $(count_failed "full-playmode-final.xml") "FAIL"
