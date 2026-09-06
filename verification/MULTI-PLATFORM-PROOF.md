# Multi-platform foundation proof — 2026-09-06

Implemented the first platform-neutral workflow boundary. `UPA.Workflows` owns `IPlatformAdapter`, `WorkflowPlan`, `PreparedWorkflow`, `IVerifiedTransaction`, `OutputVerifierRegistry`, durable run journal and shared `WorkflowRunner`. Unity-specific orchestration is supplied through its adapter; a test-only text adapter supplies a different platform.

The text adapter test creates `artifact.txt` containing `hello`, presents the common approval hash, writes the artifact, and lets the common verifier read it back. It does not import Unity assemblies, Unity project metadata or Unity scene parsers. Result: **1/1 pass** in `verification/multiplatform-evidence/after3`.

The full pipeline test project after the new foundation: **36/36 passed, 0 failed, 0 skipped** in `verification/multiplatform-evidence/full2`. The solution build passed with zero errors; NuGet reported one vulnerability-metadata warning (`NU1900`) because the network index was unavailable.

This proves shared lifecycle reuse, not broad platform support. There is still no production web, Python, Unreal, Godot or desktop adapter. A platform is not supported until it supplies a real adapter, an independent output verifier and its own acceptance evidence.
