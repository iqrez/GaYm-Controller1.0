# WIRING GUIDE

## GC-PAR-007 — HID Read Path + Descriptor

**Component:** drivers/gc_func  
**Reference consulted:** True  
**Reference files:** reference/k/src/drivers/func/HidDescriptor.h, reference/k/src/drivers/func/VPadFunc.c, reference/k/include/VPadShared.h  
### Wiring Instructions

Install gc_func.sys on the virtual bus; VHF uses g_GcReportDescriptor during device add.

### Files

- `drivers/gc_func/HidDescriptor.h`  `6ab5c0685e8e644bbe38bd262699fafd1fca870f65ab751d508d7c5cc2f66355`
- `drivers/gc_func/GcFunc.h`  `f1435f149662c57d7cc2654182e12558566269b34b220041bd4af58e232ddb44`
- `drivers/gc_func/GcFunc.c`  `da8c4865f8cd458521aeed9ecda5aab6c7222e8f58e3c05a820e6a82e61264bd`
- `drivers/gc_func/VPadShared.h`  `f1888808ee9f322aeb780d56f542dc583495403b4174a056a0d9af521614460e`
- `tests/gc_func_descriptor_test.c`  `17e2c8785516d86bd5256c2322c09ee68db3cb16d68390ade4fc46841ce8547a`
