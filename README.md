# Hoop-Strike

A 2D basketball game built in Unity 6 (URP) with fully custom kinematic ball physics — no Rigidbody involved.

## Features
- Closed-form kinematic math drives ball flight (not PhysX) — precise, predictable trajectories
- Substepped collision detection (multiple substeps per FixedUpdate) to prevent tunneling at high velocity
- Full tournament bracket system and progression UI
- DOTween-driven UI/animation
- Pooled VFX and sound systems

## Why this approach
Built without Rigidbody, physics joints, or built-in trajectory helpers — every arc is computed analytically to demonstrate engine-level ownership of the simulation rather than relying on engine abstractions.

## Stack
Unity 6, URP, C#, DOTween, New Input System
