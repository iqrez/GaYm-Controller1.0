#pragma once

/* Single shared header for both drivers and user-mode. */

#ifndef _NTDDK_
#ifndef FILE_READ_DATA
#define FILE_READ_DATA 0x0001
#endif
#ifndef FILE_WRITE_DATA
#define FILE_WRITE_DATA 0x0002
#endif
#ifndef METHOD_BUFFERED
#define METHOD_BUFFERED 0
#endif
#ifndef CTL_CODE
#define CTL_CODE(DeviceType, Function, Method, Access) \
    (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method))
#endif
#endif

/* Portable static assert macro for C/C++ */
#ifndef VPAD_STATIC_ASSERT
#  ifdef __cplusplus
#    define VPAD_STATIC_ASSERT(cond, msg) static_assert(cond, msg)
#  elif defined(__STDC_VERSION__) && __STDC_VERSION__ >= 201112L
#    define VPAD_STATIC_ASSERT(cond, msg) _Static_assert(cond, msg)
#  else
#    define VPAD_CONCAT_(a,b) a##b
#    define VPAD_CONCAT(a,b) VPAD_CONCAT_(a,b)
#    define VPAD_STATIC_ASSERT(cond, msg) typedef char VPAD_CONCAT(static_assertion_, __LINE__)[(cond)?1:-1]
#  endif
#endif

/* Use fixed-width types for ABI clarity */
#include <stdint.h>

/* Version information */
#define VPAD_VERSION_MAJOR 1
#define VPAD_VERSION_MINOR 0
#define VPAD_VERSION_PATCH 3
#define VPAD_VERSION ((VPAD_VERSION_MAJOR << 16) | (VPAD_VERSION_MINOR << 8) | VPAD_VERSION_PATCH)

/* Device interface GUIDs
   Declared extern here; define exactly once in a C file with INITGUID before including this header. */
#ifndef INITGUID
#  define VPAD_EXTERN_GUID extern const GUID
#else
#  define VPAD_EXTERN_GUID EXTERN_C const GUID
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Forward decl for GUID type in non-WDK, user-mode builds */
#if !defined(GUID_DEFINED) && !defined(_GUID_DEFINED)
#define GUID_DEFINED
#define _GUID_DEFINED
typedef struct _GUID { uint32_t Data1; uint16_t Data2; uint16_t Data3; uint8_t Data4[8]; } GUID;
#endif

VPAD_EXTERN_GUID GUID_DEVINTERFACE_VPADPAD;
VPAD_EXTERN_GUID GUID_DEVINTERFACE_VPADBUS;

#define FILE_DEVICE_VPAD     0x9A00
#define FILE_DEVICE_VPADBUS  0x9A10

#define IOCTL_VPAD_GET_VERSION   CTL_CODE(FILE_DEVICE_VPAD,    0x901, METHOD_BUFFERED, FILE_READ_DATA)
#define IOCTL_VPAD_SET_STATE     CTL_CODE(FILE_DEVICE_VPAD,    0x902, METHOD_BUFFERED, FILE_WRITE_DATA)
#define IOCTL_VPAD_CREATE        CTL_CODE(FILE_DEVICE_VPAD,    0x903, METHOD_BUFFERED, FILE_WRITE_DATA)
#define IOCTL_VPAD_DESTROY       CTL_CODE(FILE_DEVICE_VPAD,    0x904, METHOD_BUFFERED, FILE_WRITE_DATA)
#define IOCTL_VPAD_GET_RUMBLE    CTL_CODE(FILE_DEVICE_VPAD,    0x905, METHOD_BUFFERED, FILE_READ_DATA)
#define IOCTL_VPAD_SET_LEDS      CTL_CODE(FILE_DEVICE_VPAD,    0x906, METHOD_BUFFERED, FILE_WRITE_DATA)
#define IOCTL_VPAD_GET_LEDS      CTL_CODE(FILE_DEVICE_VPAD,    0x907, METHOD_BUFFERED, FILE_READ_DATA)

#define IOCTL_VPADBUS_GET_PADCOUNT CTL_CODE(FILE_DEVICE_VPADBUS, 0xA01, METHOD_BUFFERED, FILE_READ_DATA)
#define IOCTL_VPADBUS_SET_PADCOUNT CTL_CODE(FILE_DEVICE_VPADBUS, 0xA02, METHOD_BUFFERED, FILE_WRITE_DATA)
#define IOCTL_VPADBUS_RESCAN       CTL_CODE(FILE_DEVICE_VPADBUS, 0xA03, METHOD_BUFFERED, FILE_WRITE_DATA)

#pragma pack(push, 1)
typedef struct _VPAD_STATE
{
    uint16_t Buttons;
    uint8_t  LeftTrigger;
    uint8_t  RightTrigger;
    int16_t  LX;
    int16_t  LY;
    int16_t  RX;
    int16_t  RY;
} VPAD_STATE, *PVPAD_STATE;

/* Input: sequence from device, left/right amplitudes 0..255 */
typedef struct _VPAD_RUMBLE
{
    uint32_t Sequence;
    uint8_t  Left;
    uint8_t  Right;
} VPAD_RUMBLE, *PVPAD_RUMBLE;

typedef struct _VPAD_LEDS
{
    uint8_t R;
    uint8_t G;
    uint8_t B;
} VPAD_LEDS, *PVPAD_LEDS;
#pragma pack(pop)

/* ABI checks */
VPAD_STATIC_ASSERT(sizeof(VPAD_STATE)  == 12, "VPAD_STATE must be 12 bytes");
VPAD_STATIC_ASSERT(sizeof(VPAD_RUMBLE) == 6,  "VPAD_RUMBLE must be 6 bytes");
VPAD_STATIC_ASSERT(sizeof(VPAD_LEDS)   == 3,  "VPAD_LEDS must be 3 bytes");

#ifdef __cplusplus
}
#endif
