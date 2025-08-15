# GC-PAR-016 — HID Cloaking Filter (kernel lower-filter)

## Context
Work from **interfaces/** and **mocks/** only. Keep hot paths allocation-free. **Consult `reference/` first** to understand legacy behavior/feel.

## Paths to touch
- drivers/gc_filter/hid_cloaking_filter.c
- drivers/gc_filter/hid_cloaking_filter.h
- drivers/gc_filter/hid_cloaking_filter_tests.c
- reports/GC-PAR-016.md
- reports/GC-PAR-016.json

## Reference guidelines
- Look for any related files in `reference/originals/*`, `reference/aim/*`, or `reference/traces/*`.
- If behavior is replicated, list the files in your report.

## Steps
1) Implement per the spec. 
2) Add unit/integration tests or a harness snippet.
3) Document wiring steps in your report.

## Deliverables
- Code, tests, and `reports/GC-PAR-016.json` + `.md`

## Acceptance tests
- As specified in spec and your brief; include perf targets where relevant.
