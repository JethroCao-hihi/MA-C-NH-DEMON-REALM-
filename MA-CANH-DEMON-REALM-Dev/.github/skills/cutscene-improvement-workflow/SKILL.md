---
name: cutscene-improvement-workflow
description: 'Stability-first, full-workflow playbook for Unity cutscene upgrades. Use for evidence-first discovery, phased delivery (P0-P3), dependency-aware sequencing, and regression checks before expanding features.'
argument-hint: 'Which phase (P0-P3), objective, and blocker should this pass focus on?'
user-invocable: true
---

# Cutscene Improvement Workflow

## Outcome
Deliver stable, testable cutscene improvements in Unity without regressions to gameplay handoff, with baseline reliability completed before feature expansion.

## When To Use
- You are extending or stabilizing cutscene runtime behavior.
- You need to add Timeline/Cinemachine integration safely.
- You are introducing branching, localization bridge, or watched-state persistence.
- You need a repeatable verification matrix before merge.

## Inputs
- Target objective for this pass (for example: "integrate PlayableDirector for intro").
- Current cutscene scripts and related systems.
- Unity packages available in `Packages/manifest.json`.

## Procedure
1. Run evidence-first discovery.
2. Build a gap list and prioritize by dependency.
3. Execute phased implementation (P0 -> P3).
4. Verify each phase with explicit checks.
5. Record risks, rollback, and release readiness.

This skill defaults to full workflow mode, but allows scoped phase-only execution when explicitly requested.

## Step 1: Evidence-First Discovery
- Inventory existing cutscene runtime and tooling.
- Confirm related systems that share lifecycle boundaries:
  - Scene transition
  - Music
  - Player control/respawn
- Confirm package availability for Timeline, Cinemachine, Input System, and Test Framework.
- Produce two lists:
  - Present capabilities
  - Missing capabilities (gaps)

## Step 2: Build Dependency-Aware Plan
Use this phase model:
- P0: Runtime baseline stability
- P1: Camera and input standardization
- P2: Narrative flow features
- P3: Hardening and observability

Apply these dependency rules:
- P0 must complete first.
- P1 and P2 can branch after P0.
- P3 starts after P1 and P2 have stable outputs.

## Step 3: Implement By Phase
### P0: Runtime Baseline
- Integrate PlayableDirector into cutscene orchestration.
- Normalize enter/exit handoff with scene, music, and player systems.
- Add fail-safe error handler fallback to return control to gameplay.
- Add baseline EditMode and PlayMode tests.

### P1: Camera and Input
- Integrate Cinemachine shot/blend flow.
- Standardize skip behavior through Input System for keyboard/gamepad parity.
- Update editor prefab creation flow to auto-wire required components.

### P2: Narrative Features
- Add branching model for cutscene flow.
- Add localization bridge for subtitle/text resolution.
- Add persistence for watched/skip/resume state.

### P3: Hardening
- Add profiler markers for start, subtitle update, skip, transition, and completion.
- Expand fallback coverage for missing references, timeline errors, and mid-skip edge cases.
- Run full regression matrix.

## Decision Points
- If PlayableDirector integration is unstable, stop at P0 and fix baseline before P1/P2.
- If input parity fails between keyboard and gamepad, block P1 completion.
- If branching changes state semantics, gate release until persistence tests pass.
- If profiler markers show frame spikes, defer feature expansion and optimize first.

## Completion Criteria
- P0 checks pass:
  - Cutscene start/end lifecycle stable.
  - Control returns to gameplay in all tested failure paths.
  - Baseline tests pass.
- P1 checks pass:
  - Camera blends restore correctly.
  - Skip behavior consistent across input devices.
- P2 checks pass:
  - Branching routes correctly.
  - Locale switch updates subtitles/text.
  - Watched-state and resume persistence survive reload.
- P3 checks pass:
  - Required profiler markers visible.
  - No hard lock under injected failure scenarios.
  - Regression gate met for release:
    - 100% pass on all P0 blocker scenarios.
    - 95% or higher pass on non-blocking regression scenarios.

## Verification Matrix Template
For each phase item, define:
- Verification type: EditMode, PlayMode, Manual, Profiler.
- Exact scenario.
- Pass/fail signal.
- Blocking severity if failed.

## Risk and Rollback
- Use per-phase flags when possible (for example: PlayableDirector/Cinemachine toggles).
- Tag baseline before each major phase.
- If critical regression appears, disable the latest phase and revert to last stable tag.

## Output Format
Produce:
1. Current phase status.
2. Completed checks and evidence.
3. Open risks and blockers.
4. Next smallest safe increment.
