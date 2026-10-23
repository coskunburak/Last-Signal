# Explicit owner invocation only. Forward arguments without constructing shell command strings.
$ErrorActionPreference = 'Stop'
$runner = Join-Path $PSScriptRoot 'manual_checks.py'
if (Get-Command py -ErrorAction SilentlyContinue) {
    & py -3 $runner @args
} elseif (Get-Command python -ErrorAction SilentlyContinue) {
    & python $runner @args
} else {
    throw 'Install Python 3, then rerun this command.'
}
exit $LASTEXITCODE
