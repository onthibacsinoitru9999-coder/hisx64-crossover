$codes = @(
    "0004062838",
    "0001530113",
    "0004079114",
    "0004050211",
    "0004089090",
    "0002840646",
    "0004081634",
    "0003837595",
    "0004079203",
    "0004067971",
    "0003863470",
    "0002740977"
)

foreach ($c in $codes) {
    Write-Host "======================== PATIENT $c ========================"
    & ".\.agents\skills\his-clinical-operations\scripts\HisClinicalCli.exe" lookup $c
}
