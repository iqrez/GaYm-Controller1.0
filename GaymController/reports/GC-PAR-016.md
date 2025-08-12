# GC-PAR-016 — HID Cloaking Filter (kernel lower-filter)

## Summary
Implemented a HIDClass lower-filter simulation that denies IRP_MJ_CREATE for processes not in an allow-listed set managed via IOCTL. Hot path uses a static array to avoid allocations and fails open on lookup errors.

## Interfaces/Contracts
- `IOCTL_HID_CLOAK_ALLOW` (0x800) — add process ID to allow-list.
- `HidCloak_DispatchCreate(uint32_t pid)` — returns `HID_STATUS_*` codes on open attempts.

## Files Changed
- `drivers/gc_filter/hid_cloaking_filter.h`: Declares API, status codes, and IOCTL.
- `drivers/gc_filter/hid_cloaking_filter.c`: Implements allow-list and create dispatch.
- `drivers/gc_filter/hid_cloaking_filter_tests.c`: Exercises allow/deny and fail-safe behavior.

## Wiring Instructions
Compile `hid_cloaking_filter.c` into the HIDClass lower-filter and expose `HidCloak_IoControl` to the broker. The broker must issue `IOCTL_HID_CLOAK_ALLOW` for each permitted process before device open.

## Tests & Results
- `gcc drivers/gc_filter/hid_cloaking_filter.c drivers/gc_filter/hid_cloaking_filter_tests.c -o drivers/gc_filter/hid_cloaking_filter_tests && drivers/gc_filter/hid_cloaking_filter_tests` — passed.

## Reference Used
- `spec/61_HID_CLOAKING.md`
