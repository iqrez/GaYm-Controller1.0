#define INITGUID

#ifndef EXTERN_C
#  ifdef __cplusplus
#    define EXTERN_C extern "C"
#  else
#    define EXTERN_C extern
#  endif
#endif

#ifdef _NTDDK_
#include <wdm.h>
#else
#include <windows.h>
#endif
#include <guiddef.h>
#include "VPadShared.h"

/* Define the two device interface GUIDs exactly once */
DEFINE_GUID(GUID_DEVINTERFACE_VPADPAD,
0xe2a2d4a8, 0x8bb3, 0x41d8, 0xbf, 0xc7, 0x43, 0xb0, 0xb7, 0xd2, 0x3b, 0x19);

DEFINE_GUID(GUID_DEVINTERFACE_VPADBUS,
0x9289f3a7, 0x6e3a, 0x4a3b, 0x91, 0x7a, 0x3f, 0x66, 0x2c, 0x52, 0x7c, 0x5d);
