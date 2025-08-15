#ifndef HID_CLOAKING_FILTER_H
#define HID_CLOAKING_FILTER_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// Simplified status codes for tests
#define HID_STATUS_SUCCESS 0
#define HID_STATUS_ACCESS_DENIED 1
#define HID_STATUS_INVALID_PARAMETER 2
#define HID_STATUS_INSUFFICIENT_RESOURCES 3

// IOCTL code for allow-list management (mocked)
#define IOCTL_HID_CLOAK_ALLOW 0x800

// Initialize internal allow-list state. Must be called before use.
void HidCloak_Init(void);

// Handle IOCTL from broker to add a process ID to allow-list.
int HidCloak_IoControl(uint32_t code, uint32_t pid);

// Handle an open request from the given process ID. Returns HID_STATUS_*.
int HidCloak_DispatchCreate(uint32_t pid);

// For tests: invalidate internal state to simulate lookup failure.
void HidCloak_Invalidate(void);

#ifdef __cplusplus
}
#endif

#endif // HID_CLOAKING_FILTER_H
