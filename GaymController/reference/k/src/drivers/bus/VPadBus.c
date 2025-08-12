#include "VPadBus.h"

// Forward declarations for WDF callbacks
NTSTATUS VPadBusEvtDeviceAdd(WDFDRIVER Driver, PWDFDEVICE_INIT DeviceInit);
NTSTATUS VPadBusEvtChildCreate(WDFCHILDLIST ChildList, PWDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER IdentificationDescription, PWDFDEVICE_INIT ChildInit);
VOID VPadBusEvtIoctl(WDFQUEUE Queue, WDFREQUEST Request, size_t OutLen, size_t InLen, ULONG Ioctl);

#define CHILD_HARDWARE_ID L"INHOUSE_VPADPAD\0\0"
#define DEFAULT_PAD_COUNT 4

static VOID RescanChildren(WDFDEVICE Device, ULONG NewCount)
{
    PBUS_CONTEXT ctx = VPadBusGetContext(Device);
    WDFCHILDLIST list = WdfFdoGetDefaultChildList(Device);
    WdfChildListBeginScan(list);
    for (ULONG i = 0; i < NewCount; ++i)
    {
        WDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER desc;
        WDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER_INIT(&desc, sizeof(desc));
        WdfChildListAddOrUpdateChildDescriptionAsPresent(list, &desc, NULL);
    }
    WdfChildListEndScan(list);
    ctx->PadCount = NewCount;
}

static ULONG ReadPadCountFromRegistry(WDFDEVICE Device)
{
    ULONG count = DEFAULT_PAD_COUNT;
    WDFKEY hKey;
    NTSTATUS status = WdfDeviceOpenRegistryKey(Device, PLUGPLAY_REGKEY_DEVICE, KEY_READ, WDF_NO_OBJECT_ATTRIBUTES, &hKey);
    if (NT_SUCCESS(status))
    {
        ULONG value = 0;
        UNICODE_STRING name; RtlInitUnicodeString(&name, L"PadCount");
        status = WdfRegistryQueryULong(hKey, &name, &value);
        if (NT_SUCCESS(status) && value >= 1 && value <= 16) count = value;
        WdfRegistryClose(hKey);
    }
    return count;
}

static VOID WritePadCountToRegistry(WDFDEVICE Device, ULONG Count)
{
    WDFKEY hKey;
    NTSTATUS status = WdfDeviceOpenRegistryKey(Device, PLUGPLAY_REGKEY_DEVICE, KEY_WRITE, WDF_NO_OBJECT_ATTRIBUTES, &hKey);
    if (NT_SUCCESS(status))
    {
        UNICODE_STRING name; RtlInitUnicodeString(&name, L"PadCount");
        WdfRegistryAssignULong(hKey, &name, Count);
        WdfRegistryClose(hKey);
    }
}

NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING RegistryPath)
{
    WDF_DRIVER_CONFIG config;
    WDF_DRIVER_CONFIG_INIT(&config, VPadBusEvtDeviceAdd);
    return WdfDriverCreate(DriverObject, RegistryPath, WDF_NO_OBJECT_ATTRIBUTES, &config, WDF_NO_HANDLE);
}

NTSTATUS VPadBusEvtDeviceAdd(WDFDRIVER Driver, PWDFDEVICE_INIT DeviceInit)
{
    UNREFERENCED_PARAMETER(Driver);

    DECLARE_CONST_UNICODE_STRING(sddl, L"D:P(A;;GA;;;SY)");
    WdfDeviceInitAssignSDDLString(DeviceInit, &sddl);

    NTSTATUS status;
    WdfDeviceInitSetDeviceType(DeviceInit, FILE_DEVICE_BUS_EXTENDER);
    WdfDeviceInitSetExclusive(DeviceInit, FALSE);

    WDF_CHILD_LIST_CONFIG clcfg;
    WDF_CHILD_LIST_CONFIG_INIT(&clcfg, sizeof(WDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER), VPadBusEvtChildCreate);
    WdfFdoInitSetDefaultChildListConfig(DeviceInit, &clcfg, WDF_NO_OBJECT_ATTRIBUTES);

    WDF_OBJECT_ATTRIBUTES attrs;
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attrs, BUS_CONTEXT);

    WDFDEVICE device;
    status = WdfDeviceCreate(&DeviceInit, &attrs, &device);
    if (!NT_SUCCESS(status)) return status;

    PBUS_CONTEXT ctx = VPadBusGetContext(device);
    ctx->Fdo = device;
    ctx->PadCount = ReadPadCountFromRegistry(device);

    WDF_IO_QUEUE_CONFIG qcfg;
    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&qcfg, WdfIoQueueDispatchParallel);
    qcfg.EvtIoDeviceControl = VPadBusEvtIoctl;
    WDFQUEUE q;
    status = WdfIoQueueCreate(device, &qcfg, WDF_NO_OBJECT_ATTRIBUTES, &q);
    if (!NT_SUCCESS(status)) return status;

    status = WdfDeviceCreateDeviceInterface(device, &GUID_DEVINTERFACE_VPADBUS, NULL);
    if (!NT_SUCCESS(status)) return status;

    RescanChildren(device, ctx->PadCount);
    return STATUS_SUCCESS;
}

NTSTATUS VPadBusEvtChildCreate(WDFCHILDLIST ChildList, PWDF_CHILD_IDENTIFICATION_DESCRIPTION_HEADER IdentificationDescription, PWDFDEVICE_INIT ChildInit)
{
    UNREFERENCED_PARAMETER(ChildList);
    UNREFERENCED_PARAMETER(IdentificationDescription);

    NTSTATUS status;
    DECLARE_UNICODE_STRING_SIZE(hwid, 64);
    RtlInitUnicodeString(&hwid, CHILD_HARDWARE_ID);
    status = WdfPdoInitAssignHardwareIDs(ChildInit, &hwid, NULL);
    if (!NT_SUCCESS(status)) return status;

    static LONG s_index = 0;
    LONG index = InterlockedIncrement(&s_index) - 1;
    WCHAR buf[16] = {0};
    UNICODE_STRING iid;
    iid.Buffer = buf; iid.MaximumLength = sizeof(buf); iid.Length = 0;
    WCHAR tmp[16]; RtlStringCchPrintfW(tmp, 16, L"%d", (int)index);
    RtlAppendUnicodeToString(&iid, tmp);
    status = WdfPdoInitAssignInstanceID(ChildInit, &iid);
    if (!NT_SUCCESS(status)) return status;

    DECLARE_UNICODE_STRING_SIZE(desc, 64);
    RtlInitUnicodeString(&desc, L"InHouse Virtual Pad (Child)");
    status = WdfPdoInitAddDeviceText(ChildInit, &desc, &desc, 0x409);
    if (!NT_SUCCESS(status)) return status;
    WdfPdoInitSetDefaultLocale(ChildInit, 0x409);

    WDFDEVICE pdo;
    status = WdfDeviceCreate(&ChildInit, WDF_NO_OBJECT_ATTRIBUTES, &pdo);
    return status;
}

VOID VPadBusEvtIoctl(WDFQUEUE Queue, WDFREQUEST Request, size_t OutLen, size_t InLen, ULONG Ioctl)
{
    UNREFERENCED_PARAMETER(OutLen);
    UNREFERENCED_PARAMETER(InLen);
    NTSTATUS status = STATUS_SUCCESS;
    WDFDEVICE device = WdfIoQueueGetDevice(Queue);
    PBUS_CONTEXT ctx = VPadBusGetContext(device);

    switch (Ioctl)
    {
    case IOCTL_VPADBUS_GET_PADCOUNT:
    {
        PULONG pOut = NULL; size_t len=0;
        status = WdfRequestRetrieveOutputBuffer(Request, sizeof(ULONG), (PVOID*)&pOut, &len);
        if (NT_SUCCESS(status)) { *pOut = ctx->PadCount; WdfRequestSetInformation(Request, sizeof(ULONG)); }
        break;
    }
    case IOCTL_VPADBUS_SET_PADCOUNT:
    {
        PULONG pIn = NULL; size_t len=0;
        status = WdfRequestRetrieveInputBuffer(Request, sizeof(ULONG), (PVOID*)&pIn, &len);
        if (NT_SUCCESS(status))
        {
            ULONG n = *pIn; if (n < 1) n = 1; if (n > 16) n = 16;
            WritePadCountToRegistry(device, n);
            RescanChildren(device, n);
            WdfRequestSetInformation(Request, 0);
        }
        break;
    }
    case IOCTL_VPADBUS_RESCAN:
    {
        RescanChildren(device, ctx->PadCount);
        WdfRequestSetInformation(Request, 0);
        break;
    }
    default:
        status = STATUS_INVALID_DEVICE_REQUEST;
        break;
    }
    WdfRequestComplete(Request, status);
}
