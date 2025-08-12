#include "../interfaces/gc_func_ioctl.h"
#include <string.h>

static GC_REPORT g_last_report;

int GcFuncDeviceControl(uint32_t code, const void *inBuf, size_t inLen, void *outBuf, size_t outLen) {
    (void)outBuf; (void)outLen;
    switch(code) {
        case IOCTL_GC_SET_REPORT:
            if (inBuf && inLen >= sizeof(GC_REPORT)) {
                memcpy(&g_last_report, inBuf, sizeof(GC_REPORT));
                return 0;
            }
            return -1;
        case IOCTL_GC_SUBSCRIBE_RUMBLE:
            return 0; // no-op
        default:
            return -1;
    }
}

const GC_REPORT* GcFuncLastReport(void) {
    return &g_last_report;
}

