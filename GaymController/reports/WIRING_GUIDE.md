# WIRING GUIDE

## GC-PAR-006 — Func Driver IOCTL Stub

**Component:** parallel  
**Reference consulted:** True  
**Reference files:** reference/k/include/VPadShared.h, reference/k/src/drivers/func/VPadFunc.c  
### Wiring Instructions

Include gc_func_ioctl.h and link gc_func_driver.c in user-mode components to simulate driver IOCTL calls.

### Files

- `interfaces/gc_func_ioctl.h`  `2c576ee03286c7fe3960fe41ba37fa35876a0273f4af30d5194965fbe40cd321`
- `mocks/gc_func_driver.c`  `25958f021e0e0795a5ad6aff346fed3af5d47ed901629aeae85c4d97da160692`
- `mocks/gc_func_driver_test.c`  `fc6cc5a40bd99f60b6fca19ceaa82089bf245c7dac18bdf26497f52112009e54`
