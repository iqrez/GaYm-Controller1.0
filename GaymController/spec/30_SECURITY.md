# Security
- Broker runs as LocalService, pipe ACL limited to Interactive + System.
- Strict message size checks; drop malformed frames.
- Drivers validate buffer sizes; pool-tag allocations; WDF verifier clean.
- Telemetry opt-in only; enumerate counters precisely.
