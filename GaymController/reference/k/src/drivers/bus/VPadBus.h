#pragma once

#ifndef _NTDDK_
// Fallbacks for non-WDK environments
#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <stdio.h>
#include <string.h>

typedef unsigned long ULONG;
typedef unsigned long* PULONG;
typedef long LONG;
typedef void VOID;
typedef int NTSTATUS;
typedef unsigned short USHORT;
typedef unsigned char UCHAR;
typedef wchar_t WCHAR; 

typedef void* WDFDEVICE;
typedef void* WDFQUEUE;
typedef void* WDFDRIVER;
typedef void* PWDFDEVICE_INIT;
typedef void* WDFREQUEST;
typedef void* WDFKEY;
typedef void* PDRIVER_OBJECT;
typedef void* PVOID;

typedef struct _UNICODE_STRING {
    USHORT Length;
    USHORT MaximumLength;
    WCHAR* Buffer;
} UNICODE_STRING, *PUNICODE_STRING;

#ifndef TRUE
#define TRUE 1
#endif
#ifndef FALSE
#define FALSE 0
#endif
#ifndef STATUS_SUCCESS
#define STATUS_SUCCESS 0
#endif
#ifndef STATUS_INVALID_DEVICE_REQUEST
#define STATUS_INVALID_DEVICE_REQUEST -1
#endif
#ifndef NT_SUCCESS
#define NT_SUCCESS(Status) ((Status) >= 0)
#endif

// Context accessor macro
#ifndef WDF_DECLARE_CONTEXT_TYPE_WITH_NAME
#define WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(type, name) \
    static inline type* name(void* ctx) { return (type*)(ctx); }
#endif

// UNICODE helpers
#define DECLARE_CONST_UNICODE_STRING(n, v) UNICODE_STRING n; RtlInitUnicodeString(&(n), (v))
#define DECLARE_UNICODE_STRING_SIZE(n, s) UNICODE_STRING n
#define RtlInitUnicodeString(p, w) do { \
    (p)->Buffer = (WCHAR*)(w); \
    (p)->Length = (USHORT)(wcslen((const WCHAR*)(w)) * sizeof(WCHAR)); \
    (p)->MaximumLength = (p)->Length; \
} while(0)
static inline void RtlAppendUnicodeToString(PUNICODE_STRING dest, const WCHAR* src) {
    size_t srcLen = wcslen(src);
    size_t dstChars = dest->MaximumLength / sizeof(WCHAR);
    size_t usedChars = dest->Length / sizeof(WCHAR);
    if (usedChars >= dstChars) return;
    size_t avail = dstChars - usedChars;
    size_t toCopy = srcLen < avail ? srcLen : avail;
    if (toCopy > 0) {
        wcsncpy_s(dest->Buffer + usedChars, avail, src, toCopy);
        dest->Length = (USHORT)((usedChars + toCopy) * sizeof(WCHAR));
    }
}
#define RtlStringCchPrintfW swprintf_s

// IOCTL macros and flags if not present
#ifndef FILE_DEVICE_UNKNOWN
#define FILE_DEVICE_UNKNOWN 0x00000022
#endif
#ifndef FILE_DEVICE_BUS_EXTENDER
#define FILE_DEVICE_BUS_EXTENDER 0x0000002d
#endif
#ifndef METHOD_BUFFERED
#define METHOD_BUFFERED 0
#endif
#ifndef FILE_READ_DATA
#define FILE_READ_DATA 0x0001
#endif
#ifndef FILE_WRITE_DATA
#define FILE_WRITE_DATA 0x0002
#endif
#ifndef CTL_CODE
#define CTL_CODE(DeviceType, Function, Method, Access) \
    (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method))
#endif

// Registry access flags
#ifndef PLUGPLAY_REGKEY_DEVICE
#define PLUGPLAY_REGKEY_DEVICE 0
#endif
#ifndef KEY_READ
#define KEY_READ 0x20019
#endif
#ifndef KEY_WRITE
#define KEY_WRITE 0x20006
#endif

// Object attributes stub
typedef struct _WDF_OBJECT_ATTRIBUTES {
    size_t Size;
    size_t ContextSize;
} WDF_OBJECT_ATTRIBUTES, *PWDF_OBJECT_ATTRIBUTES;

#define WDF_NO_OBJECT_ATTRIBUTES 0
#define WDF_NO_HANDLE 0
#define WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(a, t) do { (void)(a); } while(0)

// Driver config
typedef struct _WDF_DRIVER_CONFIG {
    NTSTATUS (*EvtDriverDeviceAdd)(WDFDRIVER, PWDFDEVICE_INIT);
} WDF_DRIVER_CONFIG, *PWDF_DRIVER_CONFIG;
#define WDF_DRIVER_CONFIG_INIT(c, f) do { (c)->EvtDriverDeviceAdd = (f); } while(0)
#define WdfDriverCreate(DriverObject, RegistryPath, Attributes, Config, HandleOut) STATUS_SUCCESS

// IO Queue config
typedef enum _WDF_IO_QUEUE_DISPATCH {
    WdfIoQueueDispatchParallel = 0
} WDF_IO_QUEUE_DISPATCH;

typedef struct _WDF_IO_QUEUE_CONFIG {
    WDF_IO_QUEUE_DISPATCH DispatchType;
    VOID (*EvtIoDeviceControl)(WDFQUEUE, WDFREQUEST, size_t, size_t, ULONG);
} WDF_IO_QUEUE_CONFIG, *PWDF_IO_QUEUE_CONFIG;
#define WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(q, d) do { (q)->DispatchType=(d); (q)->EvtIoDeviceControl=NULL; } while(0)

// Child list
typedef void* WDFCHILDLIST;

typedef struct _WDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER {
    ULONG Size;
} WDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER, *PWDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER;
#define WDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER_INIT(p, s) do { (p)->Size=(ULONG)(s); } while(0)

typedef struct _WDF_CHILD_LIST_CONFIG {
    ULONG IdentificationDescriptionSize;
    NTSTATUS (*EvtChildListCreateDevice)(WDFCHILDLIST, PWDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER, PWDFDEVICE_INIT);
} WDF_CHILD_LIST_CONFIG, *PWDF_CHILD_LIST_CONFIG;
#define WDF_CHILD_LIST_CONFIG_INIT(cfg, idSize, cb) do { \
    (cfg)->IdentificationDescriptionSize = (idSize); \
    (cfg)->EvtChildListCreateDevice = (cb); \
} while(0)

// Device init and child list APIs stubs
#define WdfDeviceInitAssignSDDLString(d, s) (void)0
#define WdfDeviceInitSetDeviceType(d, t) (void)0
#define WdfDeviceInitSetExclusive(d, b) (void)0
#define WdfFdoInitSetDefaultChildListConfig(d, c, a) (void)0
#define WdfPdoInitAssignHardwareIDs(i, s, x) STATUS_SUCCESS
#define WdfPdoInitAssignInstanceID(i, s) STATUS_SUCCESS
#define WdfPdoInitAddDeviceText(i, d, r, l) STATUS_SUCCESS
#define WdfPdoInitSetDefaultLocale(i, l) (void)0

// Device and queue creation stubs
#undef WdfDeviceCreate
#define WdfDeviceCreate(i, a, d) ((*(d) = (WDFDEVICE)0x1), STATUS_SUCCESS)
#undef WdfIoQueueCreate
#define WdfIoQueueCreate(d, c, a, q) ((*(q) = (WDFQUEUE)0x1), STATUS_SUCCESS)
#define WdfDeviceCreateDeviceInterface(d, g, n) STATUS_SUCCESS
#define WdfIoQueueGetDevice(q) (WDFDEVICE)0

// Child list enumeration stubs
#define WdfFdoGetDefaultChildList(d) (WDFCHILDLIST)0
#define WdfChildListBeginScan(l) (void)0
#define WdfChildListAddOrUpdateChildDescriptionAsPresent(l, d, x) (void)0
#define WdfChildListEndScan(l) (void)0

// Registry stubs
#define WdfDeviceOpenRegistryKey(d, k, a, o, h) STATUS_INVALID_DEVICE_REQUEST
#define WdfRegistryQueryULong(h, n, v) STATUS_INVALID_DEVICE_REQUEST
#define WdfRegistryAssignULong(h, n, v) (void)0
#define WdfRegistryClose(h) (void)0

// Request helpers
#define WdfRequestRetrieveOutputBuffer(r, s, p, l) STATUS_INVALID_DEVICE_REQUEST
#define WdfRequestRetrieveInputBuffer(r, s, p, l) STATUS_INVALID_DEVICE_REQUEST
#define WdfRequestSetInformation(r, i) (void)0
#define WdfRequestComplete(r, s) (void)0

// Interlocked stub
#define InterlockedIncrement(p) (++(*p))

#ifndef RtlZeroMemory
#define RtlZeroMemory(Destination,Length) memset((Destination), 0, (Length))
#endif

#else
#include <ntddk.h>
#include <wdf.h>
#include <ntstrsafe.h>
#endif
#include "VPadShared.h"

typedef struct _BUS_CONTEXT
{
    WDFDEVICE       Fdo;
    ULONG           PadCount;
} BUS_CONTEXT, *PBUS_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(BUS_CONTEXT, VPadBusGetContext);
