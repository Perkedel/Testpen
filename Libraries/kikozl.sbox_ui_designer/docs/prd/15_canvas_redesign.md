# 15 — Canvas Redesign: Editor-Native Render + On-Demand Runtime Preview

**Status:** Proposed (pending approval to begin implementation)
**Author:** Sui Designer team
**Date:** 2026-05-08
**Supersedes (partially):** doc 09 (runtime preview host) — see § Migration Plan
**Related:** docs 04, 06, 08, 10, 11, 13

---

## 1. Vision

The current Canvas continuously regenerates Razor + SCSS into a hot-loadable preview cache so the user sees a real runtime render of their `.sui`. Doc 09 specified this. Six weeks of implementation proved that — under the s&box editor — this is **the wrong default**:

- Each edit triggers a full assembly hotload of `local.estudonovo`. End-to-end latency from "edit a property" to "see the result" is 1–3 seconds. The window's docks and toolbar rebuild on every hotload.
- `Editor.SceneRenderingWidget` does not dispatch component lifecycle for editor-owned scenes. We work around this with reflection on `WorldPanel.OnEnabled`, `WorldPanel.OnPreRender`, `PanelComponent.OnEnabledInternal`, and the `Sandbox.UI.WorldPanel.Scale` private property.
- `Paint`-based selection chrome is overwritten by the SceneRenderingWidget's native render path — handles and borders never appear.
- Camera math has to compensate for `CameraComponent.FieldOfView` being horizontal by default (verified in Facepunch source); the moment `FovAxis` is set to `Vertical`, rendering breaks entirely.

The replacement: **render the document directly in the editor** using the `Editor.Widget` Paint API (the same primitive that powers every other dock in s&box). The runtime preview becomes an **opt-in modal** invoked from a toolbar button — exactly the model UMG and Unity UI Builder use.

**Rule:** the Canvas is no longer a thin wrapper around a runtime panel. It is the **single source of truth for what the user sees while editing**, with output verified by an explicit Preview action.

---

## 2. What every comparable editor actually does

To avoid hallucinating, we read the design tools shipping in production engines. Citations are linked; they're load-bearing for the decisions in §4.

### 2.1 Unreal UMG — `SDesignerView` ([Epic API ref][1], [source path][2])

- `SDesignerView` extends `SDesignSurface` + implements `IUMGDesigner`. It is a Slate widget — Slate is Unreal's editor UI framework, equivalent to `Editor.Widget` in s&box.
- Children are real `UWidget`s. The designer **calls each widget's preview render** through Slate's normal layout/paint flow — there is no separate "approximation" code path. Same primitives the runtime uses, hosted inside the editor with overlay widgets for selection.
- Selection chrome, anchor handles, ruler, and snap are implemented as separate `SDesignerSurfaceExtension` widgets layered on top of the design surface — not painted into the same context as the runtime widgets. This is the architectural detail that matters most for us.
- Pan = right-click + drag. Zoom = scroll wheel. State is persisted on the asset.
- Anchor manipulation is an interaction-only feature on the designer surface; it edits the underlying widget's anchor data.

### 2.2 Unity UI Builder — `BuilderViewport` + `BuilderCanvas` ([source][3])

Read `Editor/Builder/Viewport/BuilderViewport.cs` and `BuilderCanvas.cs` end-to-end. The architecture is small and clean and we should mirror it:

```
BuilderViewport (the dock content)
├── toolbar              — zoom dropdown, resolution picker, preview button
├── viewport-wrapper     — clipping container; pan happens here
│   ├── viewport         — same as wrapper, holds surface
│   │   └── viewport-surface  — translatable container holding everything below
│   │       ├── canvas (BuilderCanvas)   — the resizable visible document area
│   │       │   ├── default background   — checkerboard / solid / image
│   │       │   ├── document             — the live UI tree under design
│   │       │   ├── editor-layer         — inline text edit overlay
│   │       │   └── canvas resize handles
│   │       ├── pickOverlay              — invisible, captures mouse for hit-test
│   │       ├── highlightOverlay         — paints hover outline + multi-match highlights
│   │       ├── BuilderSelectionIndicator — the selection chrome
│   │       ├── BuilderPlacementIndicator — the drop preview line
│   │       ├── BuilderResizer            — 8-handle resize manipulator
│   │       ├── BuilderMover              — drag-to-move manipulator
│   │       ├── BuilderAnchorer           — anchor manipulator
│   │       └── BuilderParentTracker      — outline of the selected element's parent chain
│   └── BuilderNotifications              — toasts for compile errors / info
└── BuilderZoomer + BuilderPanner         — input handlers, mutate viewport zoom/pan
```

Key patterns we adopt verbatim:

1. **Each interaction is its own widget.** Resizer, mover, anchorer, panner, zoomer are sibling overlays — not methods on the canvas. Adding a new interaction adds a new widget; nothing else changes.
2. **Two-overlay separation.** `pickOverlay` captures mouse silently; `highlightOverlay` paints hover. Hit-test and feedback are decoupled, so you can disable picking (Preview Mode) without losing chrome rendering.
3. **Pan via translating the surface.** The viewport-surface element is repositioned with `style.left/top`. No coordinate transforms in render code — the renderer always draws at logical 0,0 and the surface's transform handles pan + zoom. Critical: this means our Paint API renders use a single `PushScale + Translate` and everything inside is automatic.
4. **Zoom + pan persist per document.** Saved on the document settings, restored on reopen. Two floats and one Vector2.
5. **Match Game View** (mirrors a runtime resolution, optionally polled every N ms). For us this is "Match s&box Game window".
6. **Checkerboard background** behind the canvas — communicates "this area is your panel; outside is editor space".

### 2.3 Yoga (the flex layout engine used by React Native, Litho, Boden) ([algorithm summary][4])

A flexbox layout pass is **three tree traversals**:

1. **Top-down level-order** — collect children into per-level queues.
2. **Bottom-up** — resolve auto sizes (children that wrap their content compute their preferred size first, propagate up).
3. **Top-down** — apply justify-content along main axis, align-items along cross axis, distribute remaining space via `flex-grow`/`flex-shrink`, position children at their final coordinates.

A working subset of flexbox (no `flex-grow`/`shrink`, just `flex-start` / `center` / `flex-end` / `space-between` / `stretch`) is **~600 lines of code**. We can ship that for HorizontalBox / VerticalBox / Grid in the MVP.

### 2.4 What every editor handles that we missed in the first pass

- **Selection chrome layered separately from content render.** UMG, UI Builder, Figma, Sketch all do this. Our first pass painted chrome in the same `OnPaint` as the SceneRenderingWidget's native render — the engine's render swap discarded our draws. Conclusion: chrome must live on a sibling widget that paints **after** the content widget in z-order.
- **Single coordinate transform applied to all rendering.** No per-element math. Children render at their own `Layout.X`/`Y` — the transform stack handles pan/zoom/parent-offset.
- **Document-relative pan/zoom persistence.** Without this, every reopen resets the view.
- **Hover indicator independent of selection.** Two channels: "what would I select if I clicked now" (hover) and "what is selected" (selection). Critical for productive editing.
- **Multi-resolution with locked aspect.** Designers test 1080p, 1440p, 4K, ultrawide, mobile portrait. Click a preset, the canvas resizes; nothing else changes.

[1]: https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Editor/UMGEditor/Designer/SDesignerView
[2]: https://github.com/EpicGames/UnrealEngine (path: `Engine/Source/Editor/UMGEditor/Private/Designer/SDesignerView.cpp` — auth-walled)
[3]: https://github.com/needle-mirror/com.unity.ui.builder/blob/master/Editor/Builder/Viewport/BuilderViewport.cs
[4]: https://tchayen.com/how-to-write-a-flexbox-layout-engine

---

## 3. What s&box's editor primitives actually let us do

Verified against the Skill (`~/.claude/skills/sbox-pro/references/`) + the working code in `local.sboxpro` + the existing `Sui*` widgets.

### 3.1 `Editor.Widget` + Paint API — confirmed available

The Paint API is a stateful immediate-mode 2D drawing surface scoped to a `Widget.OnPaint`. Methods we already use successfully in `SuiHierarchyWidget` and the current `SuiCanvasWidget.PaintOverlay`:

- `Paint.SetPen( Color, float width )`
- `Paint.SetBrush( Color )` / `Paint.ClearBrush()` / `Paint.ClearPen()`
- `Paint.DrawRect( Rect )` — filled if brush is set, outlined if pen is set, both if both.
- `Paint.DrawText( Rect, string, TextFlag )` — flags handle alignment.
- `Paint.DrawIcon( Rect, string materialIcon, float size, TextFlag )`
- `Paint.SetDefaultFont( int size )` — picks the editor font at the requested px size.

Confirmed-by-use additional methods we need (cross-checked against sbox-public Editor.Widget patterns):

- `Paint.DrawLine( Vector2 a, Vector2 b )`
- `Paint.PushScale( float )` / `Paint.PopScale()` (or equivalent transform stack — name to verify when implementing)
- `Paint.Translate( Vector2 )` for the pan offset
- `Paint.MeasureText( string, TextFlag )` for ellipsis / wrap math
- `Paint.DrawTexture( Rect, Texture )` for Image elements (cross-reference: editor uses this for asset thumbnails)

If `PushScale`/`Translate` are not exposed, we fall back to **per-element math** that bakes pan/zoom into each rect — measurably slower and uglier but functionally equivalent.

### 3.2 What s&box does NOT give us, and how we work around

| Missing | Workaround |
|---|---|
| No equivalent to Unity's "render real UI elements in editor mode". `Sandbox.UI.Panel` is runtime-only. | We render via Paint API (approximation). Match the SCSS/CSS subset rules exactly. Validate with the on-demand Preview button. |
| Editor scenes don't dispatch component lifecycle. | Already known — solved with reflection in the existing `SuiPreviewHost`, which becomes the Preview button's backing scene. |
| `Editor.AssetPicker` returns `Asset.Path` with lossy casing/extension. | Already fixed in M8 polish: derive from `Asset.AbsolutePath` + walk-up to project's `Assets/` folder. |
| Editor `Paint` doesn't have CSS-grade text shaping (kerning, ligatures, complex script). | Acceptable: most game UIs use simple latin/CJK fonts. Document the limitation. Preview button shows the real result. |

---

## 4. System architecture

Mirror Unity UI Builder's structure with s&box-native widgets. New types live under `Editor/Canvas/`.

### 4.1 Widget tree

```
SuiCanvasWidget : Widget                    (the dock — replaces today's class)
├── SuiCanvasToolbar                        (top: zoom %, resolution, preview, snap toggle)
├── SuiCanvasViewport : Widget              (clipping container; this widget OWNS pan + zoom)
│   ├── (paint pass) checkerboard background
│   ├── (paint pass) document tree          (renders SuiElement tree → rectangles/text/images)
│   ├── (paint pass) hover outline          (single 1px rect of hovered element)
│   ├── (paint pass) selection chrome       (border + 8 handles + size label)
│   ├── (paint pass) marquee rect           (drawn while drag-selecting empty area)
│   ├── (paint pass) alignment guides       (V1 — siblings/parent edges)
│   ├── (paint pass) drag preview ghost     (V1 — placement indicator while dropping from palette)
│   └── (paint pass) ruler ticks + grid     (V1)
└── SuiCanvasNotifier                       (overlay toast for compile errors)
```

**Why one widget instead of many overlays?** s&box's `Editor.Widget` doesn't have a transparent overlay pattern as cheap as Unity's `VisualElement` tree — every widget is a real Qt widget under the hood. We render all paint passes inside `SuiCanvasViewport.OnPaint` in a fixed order. Mouse events go to the same widget. The "passes" are method calls, not separate widgets.

### 4.2 Data flow

```
SuiDocument (source of truth)
  │
  ├── reads ──► SuiCanvasRenderer.Paint( SuiCanvasViewport, SuiDocument )
  │                                       │
  │                                       └── recurses tree, draws each element
  │
  ├── reads ──► SuiHitTester.Pick( logicalPos ) → SuiElement?
  │                                       │
  │                                       └── reverse iteration, top-most match wins
  │
  └── mutated by ──► SuiDesignerController.Execute(ISuiCommand)
                                          │
                                          └── triggers DocumentChanged → repaint
```

Renderer and HitTester share a single `SuiLayoutSolver` that, given the document, produces a flat `Dictionary<elementId, Rect>` of computed bounds in **logical pixels** (1920×1080 space). The solver is the only place flex/anchor/pivot math lives. Renderer reads the dict to draw. HitTester reads it to pick. Painted chrome reads it to position itself. **One math, three consumers.** This is what § 14 of the user's brief calls "matemática 1:1".

### 4.3 Coordinate systems

Three spaces, one pair of conversion functions.

| Name | Origin | Unit | Lives where |
|---|---|---|---|
| **Logical** | top-left of the document's drawable area | pixels (0..PanelSize) | `SuiElement.Layout.X/Y/W/H`, generated SCSS, hit-test rects |
| **Canvas** | top-left of the visible canvas area (after pan + zoom) | pixels | what gets passed to `Paint.DrawRect` |
| **Widget** | top-left of `SuiCanvasViewport` widget (Qt frame) | pixels | what comes out of `MouseEvent.LocalPosition` |

```csharp
Vector2 LogicalToWidget( Vector2 logical )
    => (logical * Zoom) + PanOffset + CanvasOriginInWidget;

Vector2 WidgetToLogical( Vector2 widget )
    => (widget - CanvasOriginInWidget - PanOffset) / Zoom;
```

`CanvasOriginInWidget` is the position where the document's (0,0) lands when the canvas is centered with no pan. Computed once per layout pass.

Critically: **all rendering happens in widget-pixel space**. We don't "render in logical space and let a transform scale it" because s&box's Paint API may not expose a robust transform stack. So every `DrawRect` gets logical-to-widget'd inline; one helper, used everywhere.

### 4.4 Layout solver

Pass 1 — top-down — builds a parent→children index (already in document).

Pass 2 — bottom-up — for each Flex container, sum children's intrinsic sizes along main axis, compute container's resolved size if `auto`. Skip for absolute children (size is explicit in `Layout.W/H`).

Pass 3 — top-down — for each element, compute its rect in **logical-pixels of its panel's drawable area** (always 1920×1080 when at the document root):

```
rect = ResolveAbsoluteRect( element, parentRect ) when parent is not flex
     | ResolveFlexChildRect( element, parentRect, justify, align, gap ) when parent is flex
```

Anchor + Pivot apply at this step. The math we already have in `ComputeElementLogicalRect` (the version from M11 v3 with the `signX/signY` and implicit-pivot-per-anchor table) is correct for absolute children — we keep it.

For flex, implementation follows the three-pass Yoga subset described in § 2.3. Initial milestone supports:

- `flex-direction: row | column`
- `justify-content: flex-start | center | flex-end | space-between | space-around`
- `align-items: flex-start | center | flex-end | stretch`
- `gap`
- Margin / padding

Skipped for V1 (acceptable subset documented in the SCSS generator already): `flex-grow`, `flex-shrink`, `flex-wrap`, `align-self`. These are V2.

### 4.5 Renderer per element type

Element type → paint primitive(s). All sizes already in widget-pixels via the converter.

| Type | Paint operations |
|---|---|
| Canvas (root) | Outline 1px (Theme.WidgetBackground.Lighten()), no fill. Acts as the document boundary. |
| Panel | Background-color filled rect → border-rect at border-width → border-radius approximated by drawing 4 corner arcs (V2: real rounded rects if Paint exposes them, else live with sharp corners) |
| Text | `Paint.SetDefaultFont(size); Paint.SetPen(color); Paint.DrawText(rect, content, alignFlag)` |
| Image | `Paint.DrawTexture(rect, Texture.Load(path))` with FitMode → either fit, fill, or stretch — math computed before draw call |
| Button | Panel (bg + border) + Text (label, centered) — composed |
| ProgressBar | Outer Panel + inner filled Panel sized by (Value / Max) |
| HorizontalBox / VerticalBox | No own visual. Layout solver positions children. |
| Grid | No own visual. Layout solver positions children with row/column rules. |
| Overlay | No own visual. Children stack at z-order. |
| ScrollPanel | Panel + clipping flag (V2: actual scroll math) |
| InventoryGrid / InventorySlot / ItemIcon | Panel + Image + Text composites — explicit because they're `Game UI` v1 elements |
| Tooltip / Hotbar | Hidden in design canvas (these are runtime-only behaviors); shown only as palette entries |

**Background image**: same `Paint.DrawTexture` plus tint via the brush color.

**Visibility / opacity**: `Hidden` → skip paint entirely. `Collapsed` → skip paint AND skip from layout (treat as not present). `Opacity < 1` → multiply all colors by opacity at render time (no separate alpha channel pass needed since brush colors are already RGBA).

### 4.6 Selection chrome — the spec

For the selected element, after content render:

- **Border**: `Color(0.20, 0.55, 1.0, 1.0)`, width 1.5px, drawn just outside the element's rect (so it doesn't cover content).
- **8 resize handles**: 8×8px filled squares at corners + edge midpoints. Brush color = border color; pen white 1px. Skipped when element is in a Flex parent (resize doesn't apply to flex children).
- **Anchor visual**: small T-shaped marker at the element's anchor reference point in its parent (V2).
- **Size label**: 9px text below the rect: `"{W}×{H} @ {X},{Y}"`.
- **Distance-to-parent labels** (V1): tiny pixel counts on each side showing margin from element edge to parent edge.

For the hovered (not selected) element:

- **Border only**, color `Color(0.20, 0.55, 1.0, 0.6)`, width 1px. No handles. No labels.

### 4.7 Drag + resize semantics

Mouse states:

```
Idle
  ├── press on handle of selected → DragResize(direction)
  ├── press on body of hovered    → MaybeDragMove (committed once mouse moves > 4px)
  └── press on empty canvas       → DragMarquee
```

Live drag updates `Layout.X/Y/W/H` of the live element directly — **does not emit a command yet**. Document doesn't fire `DocumentChanged` during the drag. The chrome moves with the cursor because the renderer reads the current values each paint.

On release, compute the delta vs. drag-start, emit a single `SuiMoveElementCommand` or `SuiResizeElementCommand` with the final values. Undo restores pre-drag state in one step.

Modifiers:
- **Shift held**: lock to single axis (whichever the user moved more).
- **Ctrl held**: proportional resize for corner handles (preserve aspect ratio).
- **Alt held**: resize from center instead of opposite corner.

Snap-to-grid: applied to the final values before the command is emitted, not during live drag (else cursor and element drift apart).

Smart guides (V1): during drag, find sibling/parent edges within 6 logical-pixels of the moving element's edges. Draw red guide line + snap to that edge.

### 4.8 Multi-resolution + zoom + pan

**Resolutions** (toolbar dropdown):
- 1920 × 1080 (default, FHD landscape)
- 2560 × 1440 (QHD landscape)
- 3840 × 2160 (4K)
- 1280 × 720 (HD)
- 1080 × 1920 (FHD portrait)
- 720 × 1280 (HD portrait)
- 2560 × 1080 (ultrawide 21:9)
- 3440 × 1440 (ultrawide 21:9 QHD)
- Custom (W,H input)
- Match Game View (polls `Editor.Game.Window.Size` every 500ms — verify availability in Skill)

Switching resolution **resizes the document's panel area**, not the elements. Element sizes/positions in logical pixels stay the same; how much of the canvas they cover changes. This is exactly the testing flow designers want.

**Zoom**: discrete dropdown (25% / 50% / 75% / 100% / 125% / 150% / 200% / 300% / 500%) plus continuous via mouse wheel anchored at cursor. Stored on document settings.

**Pan**: middle-mouse drag (Unity convention) AND alt+left-drag (UMG convention). Stored on document settings. Reset to centered via toolbar "Fit Canvas" button.

### 4.9 Preview button — the runtime escape hatch

The current `SuiPreviewHost` (with all its lifecycle reflection) is **kept as-is** but disconnected from the Canvas auto-regen. Toolbar adds a "Preview" button that:

1. Compiles the document via the existing `SuiGenerationPipeline` if it's dirty.
2. Writes to the preview cache (existing `SuiPreviewCacheWriter`).
3. Opens a modal `Editor.Window` containing a `SceneRenderingWidget` bound to a fresh `SuiPreviewHost`.
4. Mounts the generated type (existing `TrySetPanelTypeByName`).
5. Disables all designer interaction (this window is read-only).
6. Closing the window destroys the scene.

This is essentially "M10 as a button". No code is thrown away. The auto-regen on every edit is what we kill — the rest is reused.

---

## 5. Migration plan

The new canvas does not replace M0–M9 (schema, generator, hierarchy, palette, details). Only the canvas widget changes.

### Phase 0 — preserve runtime preview as opt-in (½ day)

- Add `Tools → Preview` menu entry + toolbar button that opens the existing preview host in a modal. Disable auto-regen on Document changes.
- Verify the existing `SuiPreviewHost` works in modal context (it will — same scene, same widget, just lifetime-scoped to the modal).

### Phase 1 — new canvas: render-only (1–2 days)

- New `Editor/Canvas/SuiCanvasViewport.cs` — paint document tree, no interaction yet.
- New `Editor/Canvas/SuiLayoutSolver.cs` — absolute-only first pass.
- New `Editor/Canvas/SuiCanvasRenderer.cs` — element type → paint operations.
- Replace `SuiCanvasWidget._sceneWidget` with `SuiCanvasViewport`.
- Acceptance: opening any `.sui` shows the document elements painted in the canvas at the right positions. No interaction yet.

### Phase 2 — interaction (1–2 days)

- Hit-test using the layout solver dict.
- Click → select. Hover → outline.
- Drag body → move (live, command on release).
- Drag handle → resize 8 directions (live, command on release).
- Marquee select.
- Multi-select via Shift+click.

### Phase 3 — flex layout (1 day)

- Implement the three-pass Yoga subset in `SuiLayoutSolver`.
- Acceptance: HorizontalBox / VerticalBox with children renders with `justify-content` + `align-items` exactly as the SCSS would. Visual-vs-runtime diff zero.

### Phase 4 — viewport polish (1 day)

- Toolbar: zoom dropdown + slider, resolution dropdown, snap toggle, preview button, fit-canvas button.
- Pan + zoom with persistence.
- Multi-resolution presets.
- Checkerboard background outside the canvas area.

### Phase 5 — design aids (V1 — 1–2 days, may slip post-MVP)

- Smart alignment guides during drag.
- Rulers on top + left edges.
- Grid overlay toggle.
- Distance labels on selection.
- Anchor handle visualization.

### Phase 6 — Validation

- Compare canvas render side-by-side with Preview-button render. Bugs go on a "render fidelity" list and we iterate until match is ≥ 98% across all element types and all anchors. **No element type is "shipped" until its diff is acceptable.**

---

## 6. Risks + mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Paint API doesn't expose `PushScale` / `Translate` cleanly | Medium | Fall back to per-element math (already prototyped in current code). Slower but works. |
| Text rendering diverges from runtime CSS (font hinting, line-height) | High for V1 | Acceptable — Preview button validates. Document the diff. Match conservatively. |
| `Paint.DrawTexture` doesn't exist or has different signature | Medium | Verify via Skill + describe_type at start of Phase 1. If unavailable, render placeholder colored rect + filename label — explicit, not silent fail. |
| Flex implementation has bugs vs. real CSS flex | Medium | Yoga reference algorithm + side-by-side test cases (see "Validation" phase). |
| Multi-resolution preset list misses something | Low | User-extensible Custom option. |
| Migration breaks existing M0–M9 | Low | Schema and generator are independent. Only `SuiCanvasWidget` changes. M10 preview host is preserved as Phase 0 opt-in. |

---

## 7. Definition of Done

The canvas redesign is "done" when:

1. Opening any `.sui` paints all elements at correct positions, sizes, colors, fonts, images.
2. Click + hover + drag + resize + marquee work without lag (sub-frame response).
3. Pan + zoom + multi-resolution work and persist per document.
4. Side-by-side: design canvas vs. runtime Preview button shows ≥ 98% visual parity for every element type at every anchor across the resolutions in § 4.8.
5. Selection chrome is always visible (no paint conflict).
6. Zero reflection hacks against engine internals in the canvas code.
7. All existing M0–M9 features still work.
8. STATUS.md updated.

---

## 8. Out of scope (explicit)

To prevent scope creep:

- Animation playback / scrubbing in the canvas (V2).
- Custom CSS authored in the editor (the `Designer` section of element properties already covers this; canvas just reads `Style`).
- Inline text editing in the canvas (V2 — for now, edit `Props.Text` in Details).
- Drag-from-Hierarchy reparenting (M7 already does this in the tree itself).
- Theme switching in the canvas (V2).
- Undo of pan/zoom changes (these are view state, not document state).

---

## 9. Open questions to resolve before Phase 1

1. **Paint API transform stack** — is `Paint.PushScale`/`Translate` available, or do we per-element math? Verify in the Skill or by writing a single throwaway widget.
2. **Game window size API** — what's the call to read `Editor.Game.Window.Size` for "Match Game View"? Skill check.
3. **Texture loading from path in Editor context** — does `Texture.Load("ui/...")` work in editor code, or do we need `Asset.LoadAsync<Texture>`? The Image element rendering depends on this answer.
4. **Whether `BuildDocks` rebuild on hotload still resets canvas state** — if yes, persist canvas-internal state (selection, zoom, pan) on document, not on widget instance.

These are short investigations (≤30 min each). Resolve before writing Phase 1 code.
