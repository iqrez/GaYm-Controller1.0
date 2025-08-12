#pragma once

#ifndef _NTDDK_
#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <stdio.h>
#include <string.h>

typedef unsigned long ULONG;
typedef unsigned char UCHAR;
typedef long NTSTATUS;
typedef long LONG;
typedef void VOID;
typedef unsigned short USHORT;
typedef wchar_t WCHAR;
typedef const WCHAR* PCWSTR;
typedef WCHAR* PWSTR;
typedef void* WDFDEVICE;
typedef void* WDFQUEUE;
typedef void* WDFDRIVER;
typedef void* PWDFDEVICE_INIT;
typedef void* PVOID;
typedef short SHORT;
typedef int BOOLEAN;
typedef void* WDFKEY;
typedef void* PDRIVER_OBJECT;

typedef struct _UNICODE_STRING {
    USHORT Length;
    USHORT MaximumLength;
    PWSTR  Buffer;
} UNICODE_STRING, *PUNICODE_STRING;

#ifndef WDF_DECLARE_CONTEXT_TYPE_WITH_NAME
#define WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(type, name) \
    static inline type* name(void* ctx) { return (type*)(ctx); }
#endif

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
#ifndef UNREFERENCED_PARAMETER
#define UNREFERENCED_PARAMETER(P) (void)(P)
#endif

typedef struct _HID_XFER_PACKET {
    PVOID reportBuffer;
    ULONG reportBufferLen;
    UCHAR reportId;
} HID_XFER_PACKET, *PHID_XFER_PACKET;

typedef void* WDFREQUEST;

typedef struct _WDF_OBJECT_ATTRIBUTES {
    size_t Size;
    size_t ContextSize;
} WDF_OBJECT_ATTRIBUTES, *PWDF_OBJECT_ATTRIBUTES;

#define WDF_NO_OBJECT_ATTRIBUTES 0
#define WDF_NO_HANDLE 0

#define WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(a, t) do { (void)(a); } while(0)

typedef struct _WDF_DRIVER_CONFIG {
    NTSTATUS (*EvtDriverDeviceAdd)(WDFDRIVER, PWDFDEVICE_INIT);
} WDF_DRIVER_CONFIG, *PWDF_DRIVER_CONFIG;

#define WDF_DRIVER_CONFIG_INIT(c, f) do { (c)->EvtDriverDeviceAdd = (f); } while(0)
#define WdfDriverCreate(DriverObject, RegistryPath, Attributes, Config, HandleOut) STATUS_SUCCESS

typedef enum _WDF_IO_QUEUE_DISPATCH {
    WdfIoQueueDispatchParallel = 0
} WDF_IO_QUEUE_DISPATCH;

typedef struct _WDF_IO_QUEUE_CONFIG {
    WDF_IO_QUEUE_DISPATCH DispatchType;
    VOID (*EvtIoDeviceControl)(WDFQUEUE, WDFREQUEST, size_t, size_t, ULONG);
} WDF_IO_QUEUE_CONFIG, *PWDF_IO_QUEUE_CONFIG;

#define WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(q, d) do { (q)->DispatchType=(d); (q)->EvtIoDeviceControl=NULL; } while(0)

typedef struct _VHF_CONFIG {
    VOID (*EvtVhfReadyForWrite)(PVOID);
    VOID (*EvtVhfProcessOutputReport)(PVOID, PHID_XFER_PACKET);
    PVOID VhfClientContext;
    PVOID WdmDeviceObject;
    const UCHAR* ReportDescriptor;
    ULONG ReportDescriptorLen;
} VHF_CONFIG, *PVHF_CONFIG;

#define VHF_CONFIG_INIT(c, d, r, s) do { \
    (c)->WdmDeviceObject = (d); \
    (c)->ReportDescriptor = (const UCHAR*)(r); \
    (c)->ReportDescriptorLen = (ULONG)(s); \
    (c)->EvtVhfReadyForWrite = NULL; \
    (c)->EvtVhfProcessOutputReport = NULL; \
    (c)->VhfClientContext = NULL; \
} while(0)

typedef void* WDFCHILDLIST;

typedef struct _WDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER {
    ULONG Size;
} WDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER, *PWDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER;

#define WDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER_INIT(p, s) do { (p)->Size=(ULONG)(s); } while(0)

#define InterlockedIncrement(p) (++(*p))

#define DECLARE_CONST_UNICODE_STRING(n, v) UNICODE_STRING n; RtlInitUnicodeString(&(n), (v))
#define DECLARE_UNICODE_STRING_SIZE(n, s) UNICODE_STRING n
#define RtlInitUnicodeString(p, w) do { \
    (p)->Buffer = (PWSTR)(w); \
    (p)->Length = (USHORT)(wcslen((PCWSTR)(w)) * sizeof(WCHAR)); \
    (p)->MaximumLength = (p)->Length; \
} while(0)
static inline void RtlAppendUnicodeToString(PUNICODE_STRING dest, PCWSTR src) {
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

#ifndef FILE_DEVICE_UNKNOWN
#define FILE_DEVICE_UNKNOWN 0x00000022
#endif
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
#define WdfDeviceInitAssignSDDLString(d, s) (void)0
#define WdfDeviceInitSetDeviceType(d, t) (void)0
#define WdfDeviceInitSetExclusive(d, b) (void)0

#define WdfDeviceCreate(i, a, d) ((*(d) = (WDFDEVICE)0x1), STATUS_SUCCESS)
#define WdfDeviceCreateDeviceInterface(d, g, n) STATUS_SUCCESS
#define WdfIoQueueCreate(d, c, a, q) ((*(q) = (WDFQUEUE)0x1), STATUS_SUCCESS)
#define WdfDeviceWdmGetDeviceObject(d) (PVOID)0x1
#define VhfCreate(c, h) ((*(h) = (PVOID)0x1), STATUS_SUCCESS)
#define VhfStart(h) (void)0
#define VhfReadReportSubmit(h, p) (void)0
#define WdfRequestRetrieveOutputBuffer(r, s, p, l) STATUS_INVALID_DEVICE_REQUEST
#define WdfRequestSetInformation(r, i) (void)0
#define WdfRequestRetrieveInputBuffer(r, s, p, l) STATUS_INVALID_DEVICE_REQUEST
#define WdfIoQueueGetDevice(q) (WDFDEVICE)0
#define WdfRequestComplete(r, s) (void)0

#ifndef min
#define min(a, b) ((a) < (b) ? (a) : (b))
#endif

#ifndef RtlZeroMemory
#define RtlZeroMemory(Destination,Length) memset((Destination), 0, (Length))
#endif

#else
#include <ntddk.h>
#include <wdf.h>
#include <ntstrsafe.h>
#endif
#include "VPadShared.h"
#include "HidDescriptor.h"

typedef struct _FUNC_CONTEXT
{
    void*  VhfHandle;
    void*   IoctlQueue;
    VPAD_STATE LastState;
    int    Started;
    ULONG      RumbleSeq;
    UCHAR      RumbleLeft;
    UCHAR      RumbleRight;
    UCHAR      LedR, LedG, LedB;
} FUNC_CONTEXT, *PFUNC_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(FUNC_CONTEXT, GcFuncGetContext);

