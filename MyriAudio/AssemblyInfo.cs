using System.Runtime.CompilerServices;

// Project-side extension: the Facility's custom Audio ECS runner (FacilityRunner) needs to call
// Myri's internal codec dispatch to decode AudioClipBlob samples on the main thread for per-source
// LP filtering. See SOUNDS.md "Phase 2" for the architectural context. Added per project decision
// 2026-05-29 — this is a metadata-only change, no behavior modification to Myri itself.
[assembly: InternalsVisibleTo("Assembly-CSharp")]
