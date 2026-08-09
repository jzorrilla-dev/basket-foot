# AGENTS.md

Godot game "basket-foot" (C#/.NET edition). Hybrid football/basketball: ball is
played with feet/head (no hands except throw-ins/corner kicks), scored in a big
basket. Scoring follows FIBA rules (2 pts inside the three-point line, 3 pts
beyond it, 1 pt free throws). See `README.md` for the full rules.

## Structure

- `scenes/main.tscn` — main scene (set as `run/main_scene`): court + basket
  (post/backboard/rim), camera, light, `Ball` (RigidBody3D), `Player`
  (CharacterBody3D). A large `Ground` plane (60×60, top at Y=0) is the only
  walkable surface; the brown court is decorative (embedded, no own collision,
  top at +0.005) so player/ball move freely between court and ground with no
  lip.
- `scripts/Player.cs` — WASD + Space movement, pushes the ball and records the
  last contact position on it. Grabs the ball automatically when close (1 m,
  below 1.2 m height); while carrying it keeps updating the contact position.
  Press `kick` (K) to shoot: the velocity is a ballistic arc (55°, speed solved
  for the distance to `HoopPosition`, clamped to [5, 18] m/s) fired along the
  ball's carry direction. A short grab cooldown prevents re-grabbing right after
  a kick. Respawns at `RespawnPosition` if it falls below `RespawnBelow`
  (safety net at the edge of the `Ground`).
- `scripts/Ball.cs` — light, high-bounce ball (bounce 0.85, mass 0.4, low
  friction); resets to spawn if it falls out of bounds. When carried it is
  frozen (`Freeze = true`) with collision disabled and follows the player at
  the feet; `Release()` is the counterpart for a future kick.
- `scripts/ScoreZone.cs` — Area3D inside the rim; scores 2 or 3 points based on
  the last player-contact position vs the three-point radius (6.75 m).
- `basket-foot.csproj`/`.sln` — hand-created, mirroring what Godot .NET would
  generate (`Godot.NET.Sdk/4.7.1`, `net8.0`). Keep them in sync if Godot
  regenerates them.

## Stack (from `project.godot`)

- Godot 4.7 .NET edition — **scripting language is C#**, not GDScript.
- Renderer: Forward Plus. Physics: Jolt Physics. Windows render driver: D3D12.
- `window/stretch/mode="canvas_items"` + `aspect="expand"` — UI is resolution-scaled; use anchors.
- Not a git repo yet (no `.git`), though `.gitignore`/`.gitattributes` exist.

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
- Input actions (`move_left/right/forward/back`, `jump`) are defined in
  `project.godot` `[input]`; WASD + arrows + Space.
- Before adding any file, confirm whether it belongs in `.godot/` (no) vs the
  project root (yes).
