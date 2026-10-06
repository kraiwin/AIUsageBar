#!/usr/bin/env bash
# Synthetic candidate only. Never installed in any Claude settings.
set +e
set +o pipefail
mode=$1
original=$2
sink=$3
scratch=$4
fault=$5
capture() {
    case "$fault" in
        slow) /usr/bin/sleep 3 ;;
        hung) /usr/bin/sleep 30 ;;
    esac
    "$sink" --capture-test --scratch "$scratch"
}
if [[ "$mode" == pipeline ]]; then
    /usr/bin/tee --output-error=warn-nopipe >(capture >/dev/null 2>&1) 2>/dev/null | "$BASH" --noprofile --norc -c "$original"
    original_status=${PIPESTATUS[1]}
else
    "$BASH" --noprofile --norc -c "$original" < <(/usr/bin/tee --output-error=warn-nopipe >(capture >/dev/null 2>&1) 2>/dev/null)
    original_status=$?
fi
exit "$original_status"
