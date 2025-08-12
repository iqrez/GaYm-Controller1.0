#pragma once
#include <stdint.h>

#ifndef METHOD_BUFFERED
#define METHOD_BUFFERED 0
#endif
#ifndef FILE_ANY_ACCESS
#define FILE_ANY_ACCESS 0
#endif
#ifndef CTL_CODE
#define CTL_CODE(DeviceType, Function, Method, Access) \
    (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method))
#endif

#define FILE_DEVICE_GC 0x8009
#define IOCTL_GC_SET_REPORT       CTL_CODE(FILE_DEVICE_GC, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)
#define IOCTL_GC_SUBSCRIBE_RUMBLE CTL_CODE(FILE_DEVICE_GC, 0x802, METHOD_BUFFERED, FILE_ANY_ACCESS)

#pragma pack(push,1)
typedef struct _GC_REPORT {
    uint16_t LX;
    uint16_t LY;
    uint16_t RX;
    uint16_t RY;
    uint16_t LT;
    uint16_t RT;
    uint32_t Buttons;
} GC_REPORT;
#pragma pack(pop)

