# AGENTS.md

Godot game "basket-foot" (C#/.NET edition). Hybrid football/basketball: ball is
played with feet/head (no hands except throw-ins/corner kicks), scored in a big
basket. Scoring follows FIBA rules (2 pts inside the three-point line, 3 pts
beyond it, 1 pt free throws). See `README.md` for the full rules.

## Structure

- `scenes/main.tscn` — main scene (set as `run/main_scene`): futsal-sized court
  (40×20, wood floor) with **two baskets** (`Basket` at z=-13.1, `Basket2` at
  z=+13.1, each post/backboard/rim), basketball markings (boundary, half-court
  line, center circle, three-point rings, red small areas), light, a chase
  `Camera3D` (`PlayerCamera.cs`: sits `DistanceBehind` the player opposite to
  his `FacingDirection`, looking `LookAhead` toward the hoop he targets),
  `Ball` (RigidBody3D), `Player` (CharacterBody3D). A large `Ground` plane
  (60×60, top at Y=0) is the only walkable surface; the court is decorative
  (embedded, no own collision, top at +0.005) so player/ball move freely
  between court and ground with no lip. The player always attacks the **nearest**
  basket (`TargetHoop` picks between `HoopPosition`/`SecondHoopPosition`), and
  each basket's `ScoreZone` scores independently.
- `scripts/Player.cs` — WASD + Space movement, pushes the ball and records the
  last contact position on it. Grabs the ball automatically when close (1 m,
  below 1.2 m height); while carrying it keeps updating the contact position.
  With `IsAI = true` (used by the scene's `Player2`, a red-torso mannequin) the
  same script is driven by `ComputeAIInput`: it chases the ball, stops to grab,
  carries it toward the nearest hoop, backs off if under the rim, and shoots
  after `AICarryTimeBeforeShot` within `AIShootDistance`; it ignores shared
  human inputs (jump, Q/E turn) so only Player 1 reacts to the keyboard.
  The visual child (`Player/Visual`, a mannequin of primitives — head, torso,
  shorts, arms, legs, and boots pointing forward, i.e. toward -Z at facing
  angle 0). There is **no automatic aiming**: while moving the player faces the
  movement direction (A/D strafe, S backpedal, `MoveTurnSpeedDeg` 1440°/s);
  when idle the last facing holds. Q/E (`turn_left`/`turn_right`) is the aim
  control and always wins: it spins the facing manually at the slower
  `MaxTurnSpeedDeg` (270°/s) for full 360° fine aim, idle, moving or carrying
  (positive angle = clockwise seen from above). The exposed `FacingDirection`
  drives where the carried ball sits and where kicks go. The mannequin is
  rotated with `Rotation.y = -_facingAngle` (see the angle-sense gotcha below).
  Shooting is manual too: hold `kick` (K) while carrying to **charge** power
  (`ChargeTime` to go from `MinKickSpeed` 5 to `MaxKickSpeed` 15.5 m/s) and
  release to fire at 65° along `FacingDirection`; the carried ball visibly
  rises/advances with charge (`KickCharge`, consumed by `Ball.FollowCarrier`).
  A tap with less than `MinChargeToShoot` cancels. The AI (`IsAI = true`)
  charges the exact power needed (`RequiredCharge`, the inverse ballistic
  solve) once in range after `AICarryTimeBeforeShot`, so it aims at the hoop
  itself. Shot accuracy is `ApplyShotSpread`: standing still is precise, but
  shooting while running adds a random horizontal error (up to `ShotSpreadDeg`,
  eased to zero close to the hoop over `SpreadDistance`) so stopping to shoot
  pays off. Press `bounce` (J) while carrying to pop the ball up
  just in front of you (it rebounds in front); during a short window K volleys
  that loose ball (within `VolleyRadius`, below `VolleyMaxHeight`) at the fixed
  `VolleyKickSpeed`. A short grab cooldown prevents re-grabbing right after a
  kick or bounce.
  While moving, the legs swing (`UpdateLegs`): each leg is a `Node3D` pivot
  (`Player/Visual/LegPivotLeft/Right`, at hip height ~Y=-0.05) carrying the
  thigh/shin/boot meshes; the pivots pitch around their local X with opposite
  phases (amplitude `MaxLegSwingDeg` ∝ speed, frequency `StrideFrequency`,
  smoothed by `LegSwingResponse`) and yaw to align the swing plane with the
  actual movement direction (strafe/backpedal included), resetting to identity
  when idle. Verified numerically, not visually.
  Respawns at `RespawnPosition` if it falls below `RespawnBelow` (safety net at
  the edge of the `Ground`).
- `scripts/Ball.cs` — light, high-bounce ball (bounce 0.85, mass 0.4, low
  friction); resets to spawn if it falls out of bounds. The scene sets
  `linear_damp/angular_damp = 0` with `*_damp_mode = 1` (REPLACE) so free flight
  is truly ballistic — otherwise the project default damp (~0.1) makes every
  kick fall short of the hoop. When carried it is
  frozen (`Freeze = true`) with collision disabled and follows the player at
  the feet; `Release()` unfreezes it. Carry direction comes from the carrier's
  `FacingDirection` when it's a `Player` (smooth), else from its velocity.
  `ScoredFlag` prevents scoring the same possession twice; `RecordContact`
  or the out-of-bounds reset clears it.
- `scripts/ScoreZone.cs` — Area3D used as the hoop marker; every physics frame
  it scores only when the ball's center crosses the hoop plane (Y=3) going
  downward inside `EntryRadius` (0.15 m) of the rim axis, awarding 2 or 3
  points from the last player-contact position vs the three-point radius
  (6.75 m). Rim touches/deflections do NOT score.
- `basket-foot.csproj`/`.sln` — hand-created, mirroring what Godot .NET would
  generate (`Godot.NET.Sdk/4.7.1`, `net8.0`). Keep them in sync if Godot
  regenerates them.

## Stack (from `project.godot`)

- Godot 4.7 .NET edition — **scripting language is C#**, not GDScript.
- Renderer: Forward Plus. Physics: Jolt Physics. Windows render driver: D3D12.
- `window/stretch/mode="canvas_items"` + `aspect="expand"` — UI is resolution-scaled; use anchors.
- Git repo; the generated `/extension_api.json` dump is gitignored.

## Gotchas an agent would miss

- `godot` is on PATH via a symlink at
  `%LOCALAPPDATA%\Microsoft\WindowsApps\godot.exe` → `%LOCALAPPDATA%\Programs\Godot\Godot_v4.7.1-stable_mono_win64.exe`.
  Because Godot resolves its own path through the symlink, it looks for
  `GodotSharp/` in `WindowsApps` — that is provided by a **directory junction**
  at `%LOCALAPPDATA%\Microsoft\WindowsApps\GodotSharp` → `...\Programs\Godot\GodotSharp`.
  If .NET fails with "Assemblies not found", check both the symlink and the junction.
- The standard Godot editor (non-.NET) must not be used; it strips the `[dotnet]`
  section. Use the "Godot 4.7 .NET" editor for this project.
- **Jolt Physics has no `TorusShape3D`** (it does not exist in 4.7). The rim
  collision is 8 small `SphereShape3D`s arranged in a ring — keep that pattern.
- **`Transform3D(...)` in `.tscn` text stores the basis ROW-MAJOR** (matrix rows,
  not axis vectors): the 9 values are `(X.x, Y.x, Z.x, X.y, Y.y, Z.y, X.z, Y.z, Z.z)`.
  Writing axis vectors in order silently transposes the rotation (a camera ended
  up looking at the sky because of this). Prefer setting transforms in the editor;
  if hand-writing, verify with a runtime check of `global_transform.basis`.
- **`Player._facingAngle` and Godot `Rotation.y` have opposite senses**:
  `_facingAngle` positive = clockwise seen from above (turn right), but a
  positive `Rotation.y` is counter-clockwise. The visual must be rotated with
  `Rotation.y = -_facingAngle`; without the minus the mannequin mirrors the
  facing (boots and the carried ball orbit in opposite directions — a bug
  that was invisible with the old small cone marker).
- `dotnet build` outputs to `.godot/mono/temp/bin/Debug` (Godot loads from there);
  `bin/` and `obj/` are gitignored.
- A second Godot instance (e.g. headless CLI) **hangs while the editor is open**
  on this machine. Close the editor before headless runs.
- `.godot/` (editor cache, imports, shader caches) is gitignored — never edit or
  commit it; it regenerates on editor open.
- `.gitattributes` forces LF line endings and `.editorconfig` forces UTF-8 — keep
  new files consistent with both.
- No test framework or CI exists; verification is running the game in the editor.

## Workflow

- Open/edit scenes and scripts in the Godot .NET editor, not via CLI.
- Verify from the shell: `dotnet build` then `godot --headless --path . --quit-after 5`
  (main scene loads; expect no errors). `--import` also works but only when the
  editor is closed.
- Input actions in `project.godot` `[input]`: movement (WASD + arrows),
  `jump` (Space), `kick` (K), `bounce` (J), `turn_left`/`turn_right` (Q/E).
- Before adding any file, confirm whether it belongs in `.godot/` (no) vs the
  project root (yes).
