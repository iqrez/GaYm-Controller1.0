# HID Cloaking (Per-Process)
Lower-filter on HIDClass; deny IRP_MJ_CREATE for non-allowlisted processes.
Allow-list managed by broker via secure IOCTL. Fail-safe pass-through on error.
