# Stage READY sequence

The runtime is independent of the animation gallery and does not move or spawn a player. It raises `Completed` once after the overlay has cleared so the stage can perform the character's entrance next.

## Integration

Before saving the scene, call `StageReadySequenceBuilder.AttachTo(stageGameObject)`. This assigns the existing first/cyan sprite in `Assets/Recursos/UI/Animated/StageIntro/Generated/ReadyPulse.png` and sets `AutoAdvance=false`, `DrawAutomatically=false` for stage-owned orchestration.

At runtime:

```csharp
ready.Completed += BeginPlayableEntrance;
ready.SetPlayback(paused, 1f);
ready.Play();
// Once per stage update. delta is already multiplied by the stage playback speed.
ready.Advance(scaledDelta);
// Last in the stage OnGUI, using its existing logical GUI.matrix.
ready.Draw(new Rect(0, 0, 1600, 900));
```

`SetCanvasRect(Rect)` sets the automatic overlay's viewport. `Draw(Rect)` uses the supplied viewport in the caller's current GUI coordinates. `SetLogicalCanvas(Vector2)` controls the standalone automatic letterbox mapping.

For an independently updating prefab, use `Build()` and leave `AutoAdvance=true`, `DrawAutomatically=true`. Automatic `Update` calls `Tick(Time.deltaTime)`, which applies the sequence playback-speed multiplier. External `Advance(delta)` does not multiply playback speed again. Both respect the pause flag. Do not call `Tick` and `Advance` for the same frame.

## Behavior

Default total duration: 1.80 seconds. `ready_sequence_reference.json` provides phase boundaries and suggested capture times. The order is horizontal blue/cyan sweep, expansion/white-cyan flash over the background, reveal of the original aqua READY, hold, and lateral separation of two complete word instances. Both initially coincide, both sample the full selected cyan sprite, and both carry a soft dark-cyan shadow. They move in opposite directions and fade near the end. A viewport GUI group clips the outgoing words; individual letters are not split.

The orange/gold palette cells in ReadyPulse are deliberately unused. No original PNG or sprite metadata is rewritten by this implementation. The optional material argument is accepted for stage setup compatibility; IMGUI draws the original RGBA without a SpriteRenderer material.

`Play`/`Replay` restart playback; `Reset` clears it. `Seek(seconds)` supports deterministic inspection and never fires `Completed`. `IsPlaying`, `CurrentPhase`, `PhaseProgress`, `Elapsed`, `Duration`, `IsPaused` and `PlaybackSpeed` are public. The stage owns character visibility, entry animation, gameplay unlocking and any input changes.

## Independent preview

Use `Mega Man X9/Animations/Preview Stage READY Sequence` for an editor-only preview with phase readout, scrubber, pause, replay and speed control. Its object lives in a disposable preview scene. No open stage scene is replaced.

## Reference and verification

The provided local X4 behavior references were inspected for the leading left-to-right sweep, timed word hold and lateral departure of whole-word/shadow instances. The original UV-half interpretation was replaced after that evidence review. The Unity implementation is independently authored; no decompiled C or GameMaker source was copied into Assets.

Both new C# files compiled successfully together with Roslyn against the existing Unity project references. Only existing reference-policy warnings were emitted. Unity was not launched by this agent; live visual playback, screenshots and final integration remain for the stage owner's verification.
