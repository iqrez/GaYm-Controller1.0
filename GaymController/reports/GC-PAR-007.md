# GC-PAR-007 — HID Read Path + Descriptor

## Summary
Implemented a virtual HID function driver exposing a fixed report descriptor and allocation-free input report path using the Virtual HID Framework.

## Interfaces/Contracts
- `VPAD_STATE`, `VPAD_RUMBLE`, `VPAD_LEDS` structures for driver I/O.

## Files Changed
- `drivers/gc_func/HidDescriptor.h`: defines HID report descriptor bytes.
- `drivers/gc_func/GcFunc.h`: driver context and WDF/VHF stubs.
- `drivers/gc_func/GcFunc.c`: VHF setup, IOCTL handling and read path.
- `drivers/gc_func/VPadShared.h`: shared IOCTL and struct definitions.
- `tests/gc_func_descriptor_test.c`: descriptor length harness.

## Wiring Instructions
Bind `gc_func` as the function driver on the virtual bus. The descriptor in `HidDescriptor.h` is supplied to VHF during `GcFuncEvtDeviceAdd`.

## Tests & Results
- `gcc -c drivers/gc_func/GcFunc.c`
- `gcc tests/gc_func_descriptor_test.c -o descriptor_test && ./descriptor_test`

## Reference Used
- `reference/k/src/drivers/func/HidDescriptor.h`
- `reference/k/src/drivers/func/VPadFunc.c`
- `reference/k/include/VPadShared.h`
