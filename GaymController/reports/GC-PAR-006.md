# GC-PAR-006 — Func Driver IOCTL Stub

## Summary
Implemented interface definitions for GameCube functional driver IOCTLs and a mock driver stub with basic handlers for `IOCTL_GC_SET_REPORT` and `IOCTL_GC_SUBSCRIBE_RUMBLE`. Added a small harness test verifying request routing and state storage.

## Interfaces/Contracts
- `GC_REPORT` structure for controller state
- `IOCTL_GC_SET_REPORT` / `IOCTL_GC_SUBSCRIBE_RUMBLE` codes

## Files Changed
- `interfaces/gc_func_ioctl.h`: defined IOCTL constants and `GC_REPORT`
- `mocks/gc_func_driver.c`: stubbed `GcFuncDeviceControl` handler
- `mocks/gc_func_driver_test.c`: minimal harness to exercise the stub

## Wiring Instructions
Include `gc_func_ioctl.h` and link `gc_func_driver.c` into user-mode components needing to simulate driver IOCTL calls.

## Tests & Results
- Compiled and ran `mocks/gc_func_driver_test.c` — outputs `ok`

## Reference Used
- `reference/k/include/VPadShared.h`
- `reference/k/src/drivers/func/VPadFunc.c`
