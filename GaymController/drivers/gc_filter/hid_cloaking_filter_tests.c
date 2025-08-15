#include <assert.h>
#include <stdio.h>
#include "hid_cloaking_filter.h"

int main(void) {
    uint32_t test_pid = 1234;

    HidCloak_Init();

    // Not allow-listed yet -> deny
    int st = HidCloak_DispatchCreate(test_pid);
    if (st != HID_STATUS_ACCESS_DENIED) {
        printf("expected deny for unknown pid\n");
        return 1;
    }

    // Add via IOCTL and ensure allowed
    st = HidCloak_IoControl(IOCTL_HID_CLOAK_ALLOW, test_pid);
    assert(st == HID_STATUS_SUCCESS);
    st = HidCloak_DispatchCreate(test_pid);
    if (st != HID_STATUS_SUCCESS) {
        printf("expected allow after ioctl\n");
        return 1;
    }

    // Simulate internal error -> fail-safe pass-through
    HidCloak_Invalidate();
    st = HidCloak_DispatchCreate(9999);
    if (st != HID_STATUS_SUCCESS) {
        printf("expected pass-through on error\n");
        return 1;
    }

    printf("all tests passed\n");
    return 0;
}
