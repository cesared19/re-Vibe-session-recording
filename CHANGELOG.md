# Changelog

## [Unreleased]

### Added

- Works with **Wave XR** (HTC's Wave SDK / Wave Essence) as well as OpenXR. The overlay shows which plug-in is running (`· OpenXR` or `· Wave`, at the end of its last line), each capture records it (`xrBackend`, `captureSource` in its info) and writes `xr-info.txt` with the XR subsystems and the devices seen by the Input System and by `UnityEngine.XR.InputDevices`; the Editor window shows it too.
- *Capture Source* on the Session Capture Manager: **Auto** reads the Input System and falls back to Unity's XR input subsystem (`InputDevices`, fed by every XR plug-in) when the Input System doesn't have the headset or a controller; **Input System** and **XR Subsystem** force one of them. That path has no aim pose, so the controller pose stands in for it.
- `ReplayPoseTargets` component (XR Session Capture → Replay Pose Targets): places the camera and the controllers at the recorded poses during a replay, whatever the XR plug-in, for projects whose tracking doesn't follow the virtual devices. `ReplayInput.ReadHead()` / `ReadController(left)` return the replayed input while replaying and the real one otherwise, for code that reads the devices itself.
- *Replay Options → Virtual Devices* (on by default) turns the virtual `XRHMD` / `XRController` off. At the start of a replay, re-Play warns in the console about input actions bound to a plug-in's own layout (such as `<ViveWaveController>`), which the generic virtual devices don't feed.
- Guide step 6.2 bis and README section *Wave XR y OpenXR*, with the two new troubleshooting rows.

### Changed

- The overlay illustrations show the plug-in label.

### Not tested

- Nothing in this update was run on a Focus 3 with Wave XR: it compiles against Unity 2020.3 and 2021.3 and Input System 1.4.3 and 1.7, and the plug-in classifier has unit tests, but the capture fallback and `ReplayPoseTargets` still need a real headset. `xr-info.txt` is there to find out what a Wave project exposes.

### Fixed

- Compile error in Unity projects that use the .NET Standard 2.0 API level (the default in Unity 2020.3): `error CS1069: The type name 'ZipArchive' could not be found in the namespace 'System.IO.Compression'`, in `SparkyTCPublisher.cs`. The .zip that goes with each published replay is now written by the package's own code (`SimpleZip`), stored without compression since its files are mostly PNG images, so it doesn't depend on that API level. The .zip is valid for Python's `zipfile`, `unzip` and .NET's reader, and the same replay always gives the same bytes.

## [0.3.0] - 2026-11-12

### Added

- Setup assistant for Windows and macOS (`Tools~/Setup`): a window that checks what the PC needs (Git, Unity, Unity's Android module, adb, the headset), adds re-Play to a Unity project at a chosen version, installs an `.apk` on the headset, opens the app and pulls its captures. It uses only what the system ships with (Windows PowerShell; bash, perl and osascript on macOS).
- Illustrations of the in-headset overlay while recording, replaying and idle, in the README and the guide. They are drawn from the overlay's own rules, not captured on a headset.

### Changed

- The install URLs use the repository's current name, `re-Vibe-session-recording`.
- Works with **Unity 2020.3 LTS** (the package asked for 2021.3 before). The Editor window no longer uses `ToHashSet`, which Unity 2020.3's .NET Standard 2.0 doesn't have. The code compiles against Unity 2020.3 and 2021.3, and against Input System 1.4.3 and 1.7. The README and the guide say that on 2020.3 the VIVE OpenXR plugin must be 2.4.2 or earlier, since 2.5.0 requires Unity 2021.3, and give its URL.

## [0.2.0] - 2026-11-11

### Added

- Link captures to SparkyTC test cases: in Window > XR Session Capture, set the SparkyTC URL and a project API token (kept in EditorPrefs on this PC, never in a build), then pick a test case for each capture. The link is saved as `sparkytc.json` in the capture's folder and travels with it. The window also lists captures in folders that group them, such as `SessionCaptures/regression`.
- Publish replays to SparkyTC: **Publish replays…** in Window > XR Session Capture sends the chosen replays (made in the Editor, the test runner or on the headset and pulled) as one automated test run for a build registered in SparkyTC, with a result per linked test case (Pass or Fail, findings, duration and the report) and, as evidence, every screenshot of the recording, the replay and its diff, plus the replay as a .zip. Publishing the same replays again creates nothing new; unlinked captures are listed and skipped. The regression test sample publishes at the end when `SPARKYTC_URL`, `SPARKYTC_TOKEN` and `SPARKYTC_BUILD` are set.
- The step-by-step guide covers the whole SparkyTC connection: what an admin sets up in SparkyTC (an API token and the build), connecting Unity, linking captures, publishing from the window and from the test runner (macOS/Linux and Windows), and troubleshooting.

### Requirements

- Linking and publishing need SparkyTC 2.5.0 or later, which answers the integration API on the app's address. With older versions, use an SSH tunnel and the API URL field (see the guide).

## [0.1.0] - 2026-11-10

### Added

- Record XR sessions on the headset: head and controller poses, buttons, analog inputs, tracking state, screenshot checkpoints and notes (`.xrsc` format).
- Replay through virtual Input System devices (`ReplayHMD`, `ReplayController`) in the Editor or on the headset, frame-locked (deterministic game time) or real time.
- Screenshot comparison with diff images, and an HTML/JSON report with recording vs replay performance.
- Per-frame metrics (frame, CPU, GPU and XR runtime times, dropped frames, memory, GC, battery, temperature, thermal status, controller battery) in CSV, with a summary.
- In-headset metrics overlay with a frame-time graph, on the left wrist or following the head.
- Window > XR Session Capture: browse captures, pull them from the headset with adb, replay in the Editor or on the headset, open reports.
- Sample PlayMode regression test.
- Step-by-step guide in Spanish (`Documentation~/guia-paso-a-paso.md`): PC and headset setup, installation, recording, pulling and replaying a session.

[Unreleased]: https://github.com/cesared19/re-Vibe-session-recording/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/cesared19/re-Vibe-session-recording/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/cesared19/re-Vibe-session-recording/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/cesared19/re-Vibe-session-recording/releases/tag/v0.1.0
