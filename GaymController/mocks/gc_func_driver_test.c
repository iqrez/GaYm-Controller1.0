#include "gc_func_driver.c"
#include <assert.h>
#include <stdio.h>
#include <string.h>

int main(void) {
    GC_REPORT r = {1,2,3,4,5,6,7};
    int ret = GcFuncDeviceControl(IOCTL_GC_SET_REPORT, &r, sizeof(r), NULL, 0);
    assert(ret == 0);
    const GC_REPORT *last = GcFuncLastReport();
    assert(memcmp(last, &r, sizeof(r)) == 0);
    ret = GcFuncDeviceControl(IOCTL_GC_SUBSCRIBE_RUMBLE, NULL, 0, NULL, 0);
    assert(ret == 0);
    ret = GcFuncDeviceControl(0xDEADBEEF, NULL, 0, NULL, 0);
    assert(ret == -1);
    printf("ok\n");
    return 0;
}
