#include "VPadFunc.h"

// Forward declarations for callbacks
NTSTATUS VPadFuncEvtDeviceAdd(WDFDRIVER Driver, PWDFDEVICE_INIT DeviceInit);
VOID VPadFuncEvtIoDeviceControl(WDFQUEUE Queue, WDFREQUEST Request,
                                size_t OutputBufferLength, size_t InputBufferLength,
                                ULONG IoControlCode);

static VOID VPadOnVhfReadyForWrite(PVOID Context);
static VOID VPadOnVhfProcessOutput(PVOID Context, PHID_XFER_PACKET OutputPacket);
static VOID VPadSendInputReport(PFUNC_CONTEXT ctx, PVPAD_STATE state);
static VOID ClampShort(SHORT* v);

static VOID MapRumbleToLeds(PFUNC_CONTEXT ctx, UCHAR left, UCHAR right)
{
    double nl = left / 255.0, nr = right / 255.0;
    nl = nl * nl; nr = nr * nr;
    UCHAR R = (UCHAR)(nr * 255.0 + 0.5);
    UCHAR B = (UCHAR)(nl * 255.0 + 0.5);
    UCHAR G = (UCHAR)(min(left, right));
    ctx->LedR = R; ctx->LedG = G; ctx->LedB = B;
}

NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING RegistryPath)
{
    WDF_DRIVER_CONFIG config;
    WDF_DRIVER_CONFIG_INIT(&config, VPadFuncEvtDeviceAdd);
    return WdfDriverCreate(DriverObject, RegistryPath, WDF_NO_OBJECT_ATTRIBUTES, &config, WDF_NO_HANDLE);
}

NTSTATUS VPadFuncEvtDeviceAdd(WDFDRIVER Driver, PWDFDEVICE_INIT DeviceInit)
{
    UNREFERENCED_PARAMETER(Driver);
    NTSTATUS status;

    DECLARE_CONST_UNICODE_STRING(sddl, L"D:P(A;;GA;;;SY)");
    WdfDeviceInitAssignSDDLString(DeviceInit, &sddl);

    WdfDeviceInitSetDeviceType(DeviceInit, FILE_DEVICE_UNKNOWN);

    WDF_OBJECT_ATTRIBUTES attrs;
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attrs, FUNC_CONTEXT);

    WDFDEVICE device;
    status = WdfDeviceCreate(&DeviceInit, &attrs, &device);
    if (!NT_SUCCESS(status)) return status;

    PFUNC_CONTEXT ctx = VPadFuncGetContext(device);
    RtlZeroMemory(ctx, sizeof(*ctx));

    status = WdfDeviceCreateDeviceInterface(device, &GUID_DEVINTERFACE_VPADPAD, NULL);
    if (!NT_SUCCESS(status)) return status;

    WDF_IO_QUEUE_CONFIG qcfg;
    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&qcfg, WdfIoQueueDispatchParallel);
    qcfg.EvtIoDeviceControl = VPadFuncEvtIoDeviceControl;
    status = WdfIoQueueCreate(device, &qcfg, WDF_NO_OBJECT_ATTRIBUTES, &ctx->IoctlQueue);
    if (!NT_SUCCESS(status)) return status;

    VHF_CONFIG cfg;
    VHF_CONFIG_INIT(&cfg, WdfDeviceWdmGetDeviceObject(device), g_VPadReportDescriptor, sizeof(g_VPadReportDescriptor));
    cfg.EvtVhfReadyForWrite = VPadOnVhfReadyForWrite;
    cfg.EvtVhfProcessOutputReport = VPadOnVhfProcessOutput;
    cfg.VhfClientContext = ctx;

    status = VhfCreate(&cfg, &ctx->VhfHandle);
    if (!NT_SUCCESS(status)) return status;

    VhfStart(ctx->VhfHandle);
    ctx->Started = TRUE;
    ctx->RumbleSeq = 0; ctx->RumbleLeft = 0; ctx->RumbleRight = 0;
    ctx->LedR = 0; ctx->LedG = 0; ctx->LedB = 0;

    VPAD_STATE zero = {0};
    VPadSendInputReport(ctx, &zero);
    return STATUS_SUCCESS;
}

static VOID VPadOnVhfReadyForWrite(PVOID Context)
{
    UNREFERENCED_PARAMETER(Context);
}

static VOID VPadOnVhfProcessOutput(PVOID Context, PHID_XFER_PACKET OutputPacket)
{
    PFUNC_CONTEXT ctx = (PFUNC_CONTEXT)Context;
    if (!ctx) return;

    if (OutputPacket->reportId == 1 && OutputPacket->reportBufferLen >= 2)
    {
        UCHAR left = ((UCHAR*)OutputPacket->reportBuffer)[0];
        UCHAR right = ((UCHAR*)OutputPacket->reportBuffer)[1];
        ctx->RumbleLeft = left;
        ctx->RumbleRight = right;
        InterlockedIncrement((volatile LONG*)&ctx->RumbleSeq);
        MapRumbleToLeds(ctx, left, right);
    }
    else if (OutputPacket->reportId == 2 && OutputPacket->reportBufferLen >= 3)
    {
        ctx->LedR = ((UCHAR*)OutputPacket->reportBuffer)[0];
        ctx->LedG = ((UCHAR*)OutputPacket->reportBuffer)[1];
        ctx->LedB = ((UCHAR*)OutputPacket->reportBuffer)[2];
    }
}

static VOID ClampShort(SHORT* v)
{
    if (*v < -32768) *v = -32768;
    if (*v >  32767) *v =  32767;
}

static VOID VPadSendInputReport(PFUNC_CONTEXT ctx, PVPAD_STATE state)
{
    UCHAR report[2 + 2 + 8] = {0};
    report[0] = (UCHAR)(state->Buttons & 0xFF);
    report[1] = (UCHAR)((state->Buttons >> 8) & 0xFF);
    report[2] = state->LeftTrigger;
    report[3] = state->RightTrigger;
    *(SHORT*)&report[4]  = state->LX;
    *(SHORT*)&report[6]  = state->LY;
    *(SHORT*)&report[8]  = state->RX;
    *(SHORT*)&report[10] = state->RY;

    HID_XFER_PACKET pkt; pkt.reportBuffer = report; pkt.reportBufferLen = (ULONG)sizeof(report); pkt.reportId = 0;

    VhfReadReportSubmit(ctx->VhfHandle, &pkt);
    ctx->LastState = *state;
}

VOID VPadFuncEvtIoDeviceControl(WDFQUEUE Queue, WDFREQUEST Request,
                                size_t OutputBufferLength, size_t InputBufferLength,
                                ULONG IoControlCode)
{
    UNREFERENCED_PARAMETER(OutputBufferLength);
    UNREFERENCED_PARAMETER(InputBufferLength);

    NTSTATUS status = STATUS_SUCCESS;
    WDFDEVICE device = WdfIoQueueGetDevice(Queue);
    PFUNC_CONTEXT ctx = VPadFuncGetContext(device);

    switch (IoControlCode)
    {
    case IOCTL_VPAD_GET_VERSION:
    {
        ULONG* pOut = NULL;
        size_t len = 0;
        status = WdfRequestRetrieveOutputBuffer(Request, sizeof(ULONG), (PVOID*)&pOut, &len);
        if (NT_SUCCESS(status))
        {
            *pOut = VPAD_VERSION;
            WdfRequestSetInformation(Request, sizeof(ULONG));
        }
        break;
    }
    case IOCTL_VPAD_CREATE:
        WdfRequestSetInformation(Request, 0); break;
    case IOCTL_VPAD_DESTROY:
    {
        VPAD_STATE zero = {0};
        VPadSendInputReport(ctx, &zero);
        WdfRequestSetInformation(Request, 0);
        break;
    }
    case IOCTL_VPAD_SET_STATE:
    {
        PVPAD_STATE st = NULL; size_t len = 0;
        status = WdfRequestRetrieveInputBuffer(Request, sizeof(VPAD_STATE), (PVOID*)&st, &len);
        if (NT_SUCCESS(status))
        {
            ClampShort(&st->LX); ClampShort(&st->LY); ClampShort(&st->RX); ClampShort(&st->RY);
            VPadSendInputReport(ctx, st);
            WdfRequestSetInformation(Request, 0);
        }
        break;
    }
    case IOCTL_VPAD_GET_RUMBLE:
    {
        PVPAD_RUMBLE out = NULL; size_t len = 0;
        status = WdfRequestRetrieveOutputBuffer(Request, sizeof(VPAD_RUMBLE), (PVOID*)&out, &len);
        if (NT_SUCCESS(status))
        {
            out->Sequence = ctx->RumbleSeq;
            out->Left = ctx->RumbleLeft;
            out->Right = ctx->RumbleRight;
            WdfRequestSetInformation(Request, sizeof(VPAD_RUMBLE));
        }
        break;
    }
    case IOCTL_VPAD_SET_LEDS:
    {
        PVPAD_LEDS in = NULL; size_t len = 0;
        status = WdfRequestRetrieveInputBuffer(Request, sizeof(VPAD_LEDS), (PVOID*)&in, &len);
        if (NT_SUCCESS(status))
        {
            ctx->LedR = in->R; ctx->LedG = in->G; ctx->LedB = in->B;
            WdfRequestSetInformation(Request, 0);
        }
        break;
    }
    case IOCTL_VPAD_GET_LEDS:
    {
        PVPAD_LEDS out = NULL; size_t len = 0;
        status = WdfRequestRetrieveOutputBuffer(Request, sizeof(VPAD_LEDS), (PVOID*)&out, &len);
        if (NT_SUCCESS(status))
        {
            out->R = ctx->LedR; out->G = ctx->LedG; out->B = ctx->LedB;
            WdfRequestSetInformation(Request, sizeof(VPAD_LEDS));
        }
        break;
    }
    default:
        status = STATUS_INVALID_DEVICE_REQUEST;
        break;
    }

    WdfRequestComplete(Request, status);
}
