#include "hid_cloaking_filter.h"

#define HID_CLOAK_MAX_PIDS 64

static uint32_t g_allowlist[HID_CLOAK_MAX_PIDS];
static size_t g_allow_count;
static bool g_state_valid;

void HidCloak_Init(void) {
    g_allow_count = 0;
    g_state_valid = true;
}

int HidCloak_IoControl(uint32_t code, uint32_t pid) {
    if (code != IOCTL_HID_CLOAK_ALLOW) {
        return HID_STATUS_INVALID_PARAMETER;
    }

    if (g_allow_count >= HID_CLOAK_MAX_PIDS) {
        return HID_STATUS_INSUFFICIENT_RESOURCES;
    }

    g_allowlist[g_allow_count++] = pid;
    return HID_STATUS_SUCCESS;
}

static int HidCloak_CheckAllowed(uint32_t pid, bool *allowed) {
    if (!g_state_valid) {
        return -1;
    }

    *allowed = false;
    for (size_t i = 0; i < g_allow_count; ++i) {
        if (g_allowlist[i] == pid) {
            *allowed = true;
            break;
        }
    }
    return 0;
}

int HidCloak_DispatchCreate(uint32_t pid) {
    bool allowed;
    if (HidCloak_CheckAllowed(pid, &allowed) != 0) {
        // Fail-safe pass-through on error
        return HID_STATUS_SUCCESS;
    }
    return allowed ? HID_STATUS_SUCCESS : HID_STATUS_ACCESS_DENIED;
}

void HidCloak_Invalidate(void) {
    g_state_valid = false;
}
