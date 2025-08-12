#include <stdio.h>
#include "../drivers/gc_func/HidDescriptor.h"

int main(void) {
    printf("descriptor bytes:%zu\n", sizeof(g_GcReportDescriptor));
    return 0;
}
