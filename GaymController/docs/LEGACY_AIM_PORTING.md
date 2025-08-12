# Legacy Aim Porting
Implement ILegacyMouseAim in a plugin DLL and verify parity against golden traces.
1) Capture traces from original (reference/traces)
2) Implement translator math in .NET 8 class library
3) Compare outputs (≤0.5% error) and integrate via mapping node
