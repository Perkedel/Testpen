# Sbox UI Designer — Implementation Status

Snapshot of where the codebase stands.

Updated: 2026-05-10 (V1.0 finalization autonomous run — Test in Play + Alignment + User SCSS sidecar)

---

## V1.0 — feature complete (2026-05-10)

Implemented per `docs/prd/16_v1_finalization.md`:

### Phase 1 — Test in Play (real Play-mode preview)
- `Code/Runtime/SuiPreviewState.cs` — process-local handoff (PendingTypeFullName)
- `Code/Runtime/SuiPreviewMount.cs` — Component that mounts the user's UI on a child ScreenPanel via `TypeLibrary.GetType` + `Components.Create`
- `Editor/SuiPreviewLauncher.cs` — compile → poll TypeLibrary → set state → `AssetSystem.FindByPath(...).OpenInEditor()` → `EditorScene.Play(session)`
- `Assets/sui_preview/preview_stage.scene` — bundled stage (TPS Player Controller + Citizen body + ground cube + Sun + Ambient + Skybox + SUI Preview Mount GO). Strips dev-only `SuiSelfTestRunner` / `SuiTestPanel` test fixtures.
- Top toolbar: "Preview" button renamed → "Test in Play".
- `Editor/Widgets/SuiCenterTabsWidget.cs`: Preview tab dropped (real Play replaces it).
- Deleted: `Editor/SuiPreviewWindow.cs`, `Editor/Widgets/SuiPreviewTabWidget.cs`.

### Phase 2 — Alignment Tools
- `Editor/Commands/SuiAlignElementsCommand.cs` — 6 modes (Left, HCenter, Right, Top, VCenter, Bottom).
- `Editor/Commands/SuiDistributeElementsCommand.cs` — 2 axes (Horizontal, Vertical).
- `Editor/SuiDesignerController.cs` — `AlignSelection`, `DistributeSelection`, `CollectAlignableSelection` (filters: same parent, Absolute mode, not locked).
- Edit menu → Align submenu (8 entries).

### Phase 3 — User SCSS Sidecar
- `Code/Generation/SuiScssGenerator.cs` — emits `@import "<className>.User.scss"` AFTER the generated block (user rules win cascade).
- `Editor/SuiCompileWriter.cs` — `EmitUserScssSidecars` writes `<className>.User.scss` once per generated SCSS, leaves existing files untouched.
- `Editor/SuiCompileResult.cs` — new `UserOwned` category.
- `Editor/Widgets/SuiCompileResultsWidget.cs` — reports User-Owned section.

### Phase 4 — Cleanup
- Removed dead `_hover` fields from `SuiToggleField`, `SuiDropdownField`, `SuiAnchorPickerButton`, `SuiPaletteCategoryHeader`.
- Fixed XML doc warnings in `SuiCanvasViewport.cs`, `SuiDesignerWindow.cs`, `SuiPreviewLauncher.cs`.
- **Build state: 0 errors, 0 warnings.** First time since project start.

---

## Pending V1.0 user-side validation (smoke test tomorrow)

1. Open a `.sui` document.
2. Click "Test in Play" — confirm preview stage scene loads, Play starts, UI mounts on a ScreenPanel.
3. Stop play — verify return to preview stage scene (user re-opens their original scene manually if needed).
4. Multi-select 2+ elements → Edit → Align → confirm each of 8 ops + verify Ctrl+Z restores.
5. Compile a fresh `.sui` → confirm `<name>.User.scss` is created next to `<name>.razor.scss`.
6. Edit User.scss with a custom rule → recompile → confirm User.scss not overwritten + the rule applies in Test in Play.
7. Confirm Compile Results widget shows "User-Owned" section.

---

## Milestone progress

| MS | Title | Status | Notes |
|----|-------|--------|-------|
| M0 | Repo setup | ✅ Done | |
| M1 | Project inspection | ✅ Done | See `docs/M1_PROJECT_INSPECTION.md` |
| M2 | `.sui` asset model | ✅ Done | |
| M3 | Asset creation/opening | ✅ Done | |
| M4 | Editor shell | ✅ Done | |
| M5 | Document controller + selection | ✅ Done | |
| M6 | Palette + add element | ✅ Done | |
| M7 | Hierarchy | ✅ Done | |
| M8 | Details panel | ✅ Done | |
| M9 | Generator MVP | ✅ Done | |
| M10 | Runtime preview | ✅ Done — **demoted to opt-in modal** | See "Canvas redesign" below. |
| **M11 (canvas redesign)** | **Editor-native paint canvas** | **✅ Phases 0-3 done overnight** | Phases 4-5 deferred (toolbar inline, alignment guides, rulers/grid) |
| **M12** | **Compile system (final write to user folder)** | **✅ Done (Batch 1)** | `SuiCompileWriter` + manifest + safe-overwrite + backup folder + folder picker |
| **M13** | **Polish** | **✅ Done (Batch 2)** | README + 4 samples via Tools menu + canvas toolbar + rulers + grid |
| **M14** | **UI/UX redesign** | **✅ Phases A-J done** | Paired rows + Anchor 3×3 picker + Lock-aware hit-test + Search bars + Center tabs (Designer/Preview/Code) + Bottom multi-tab (Animations/Bindings/Compile/Logs) + lighter section headers. **V2 deferred:** Style/Class system, Inventory wizard, Responsive Debug mode, Bindings table real, Code editable. |

---

## M14 — UI/UX redesign autonomous run (2026-05-09)

Plan em [`MILESTONE_M14_REDESIGN.md`](MILESTONE_M14_REDESIGN.md) + 5 imagens-mockup do user. Phases A-J implementadas:

### Phase A-D — Paired rows + Anchor picker (Details panel)

- [`SuiDetailsWidget`](Editor/Widgets/SuiDetailsWidget.cs): helpers novos `AddFloatPairRow`, `AddFloatQuadRow`, `AddMiniFloatField`, `FormatFloat`. Position X/Y, Size W/H, Pivot X/Y agora em linha única paired. Margin/Padding LTRB em 1 linha cada (era 4).
- [`SuiAnchorPicker`](Editor/Widgets/SuiAnchorPicker.cs) (novo) — grid 3×3 visual clicável + hover state. Substitui dropdown anchor. Stretch variants (Stretch/StretchH/StretchV) ficam num botão "Stretch..." à direita.
- Section headers: chevron + título + linha sutil, sem fundo (era box pesada).
- MakeRow + AddRowLabel: spacing/labels reduzidos (96px label, 4px spacing).

### Phase E-F — Lock-aware hit-test + Hierarchy lock/visibility

- [`SuiCanvasWidget.HitTestWalk`](Editor/Widgets/SuiCanvasWidget.cs): elementos com `Flags.Locked = true` são ignorados (e seu subtree também). Click passa direto pra elemento atrás. Marquee respeita lock também.
- [`SuiHierarchyWidget`](Editor/Widgets/SuiHierarchyWidget.cs) `SuiElementTreeNode.OnPaint`: pinta ícones lock + visibility na direita de cada row como **status indicators** (TreeNode não expõe OnMousePress). Toggle via right-click context menu (`Lock/Unlock`, `Hide in designer/Show in designer`) OU via boolean rows do Details > Designer.
- `FlagsChanged` event propaga pro Window que invalida o canvas.

### Phase G — Search bars

- [`SuiPaletteWidget`](Editor/Widgets/SuiPaletteWidget.cs): search input no topo, filtra elementos por nome substring. Categorias somem quando 0 itens visíveis.
- [`SuiHierarchyWidget`](Editor/Widgets/SuiHierarchyWidget.cs): search filtra nodes por nome — matching nodes + ancestors visíveis.
- [`SuiDetailsWidget`](Editor/Widgets/SuiDetailsWidget.cs): search filtra rows por label substring. `_searchableRows` registra rows + labels durante build.

### Phase H — Section headers leves

- [`SuiCollapsibleSection`](Editor/Widgets/SuiCollapsibleSection.cs): chevron + label + linha sutil de underline; sem fundo, padding reduzido.

### Phase I — Tabs Designer/Preview/Code no centro

- [`SuiCenterTabsWidget`](Editor/Widgets/SuiCenterTabsWidget.cs) (novo) — `TabWidget` com 3 pages. Substitui `SuiCanvasWidget` direto no dock central.
- [`SuiPreviewTabWidget`](Editor/Widgets/SuiPreviewTabWidget.cs) (novo) — extrai a lógica do `SuiPreviewWindow` modal pra widget reutilizável. Refresh button + status label. Recompila + remonta type quando tab é ativada ou Refresh clicado. **Não atualiza live** (per spec).
- [`SuiCodeTabWidget`](Editor/Widgets/SuiCodeTabWidget.cs) (novo) — `TabWidget` interno com .razor + .razor.scss read-only via `TextEdit`. Refresh re-roda generator e popula.

### Phase J — Bottom multi-tab panel

- [`SuiBottomTabsWidget`](Editor/Widgets/SuiBottomTabsWidget.cs) (novo) — single dock com 4 tabs:
  - **Animations** (placeholder V2 — schema reservado)
  - **Bindings** ([`SuiBindingsWidget`](Editor/Widgets/SuiBindingsWidget.cs) — placeholder V2)
  - **Compile Results** (existing widget movido pra dentro)
  - **Logs** ([`SuiLogsWidget`](Editor/Widgets/SuiLogsWidget.cs) — placeholder V2; aponta pra console)
- Substitui os 2 docks separados (CompileResults + Animations) que ocupavam 2 slots no BottomOuter.

### Window changes

- [`SuiDesignerWindow`](Editor/SuiDesignerWindow.cs): `_canvas` agora é property que retorna `_centerTabs.Canvas` (todo código antigo continua funcionando). `_compileResults` getter retorna `_bottomTabs.CompileResults`. Cookie bumpado pra `SuiDesigner.v3` pra forçar dock state fresh.

### V2 deferreds explícitos do M14

- Style/Class dropdown system (Image 4 do mockup) — schema novo necessário
- Inventory Grid creation wizard (Image 5) — modal complexo
- Responsive Debug mode + Issues panel (Image 2) — sistema de validação live
- Bindings table real com Add/Edit/Mode columns
- Code tab editável (.sui é source of truth, edits no .razor seriam perdidos)
- Compact toolbar superior unificada (visual polish global)

### Compile state at end of run

Source ↔ runtime mirror sync clean (zero diff fora de obj/Properties/csproj).

---

## Batch 2 — autonomous run (2026-05-08) ✅ validated end-to-end

**Validation passou em todos os cenários:**
- Toolbar inline: Screen size + Zoom + Snap + Grid + Rulers + Fit
- Rulers adaptativos em qualquer zoom + pan sync
- Grid overlay com proteção de densidade
- Status bar (Nothing selected / single / multi)
- 4 samples instaláveis via Tools → todos renderizam corretamente
- ZIndex com stacking context per-parent (Image z=999 dentro Panel A z=0 NÃO sobrepõe Panel B z=5 — comportamento CSS standard)
- InventorySlot preview icon + count
- InventoryGrid/Grid auto-tile (flex+wrap default)
- Hotbar fila horizontal (regression fix do SolveGrid)
- Multi-select group drag preserva offset
- Anchor change recalcula X/Y/W/H sem saltar
- Rename auto-sync ClassName quando não customizado
- Duplicate suffix _2/_3 com strip do _N anterior

**Bugs fixed durante validação:**
- InventorySlot props (PreviewIcon/PreviewCount) não renderizavam → renderer agora chama PaintItemIcon
- InventoryGrid empilhava children no (0,0) → ApplyTypeDefaults usa Flex+Wrap pra Grid/InventoryGrid; solver tem SolveGrid dedicado pra Columns×Rows
- Hotbar virou vertical depois do SolveGrid → removido de SolveGrid, usa SuiFlexLayout.Solve (flex row)
- ZIndex ignorado no canvas → renderer + hit-test + marquee usam GetRenderOrderedChildren



### Phase 4 — Toolbar inline do canvas

- `Editor/Canvas/SuiCanvasToolbar.cs` — widget novo, 32px de altura, dentro do `SuiCanvasWidget` acima do viewport.
- Dropdowns: **Screen size** (1080p/720p/QHD/4K/16:10/Ultrawide/Reset), **Zoom** (25–400% + Fit), **Snap** (off/4/8/16/32/64px). Todos persistem em `SuiDocumentSettings` ou `SuiCanvasSettings`.
- Toggles: **Grid** (dot overlay), **Rulers**. Estado visual via SetStyles bg-tint quando ativo.
- **Fit** button à direita.
- Schema: `SuiCanvasSettings.PreviewWidth/PreviewHeight` (override BaseW/H sem mexer no design); `SuiDocumentSettings.ShowGrid/ShowAlignmentGuides`.
- Solver `PanelSize` agora dinâmico (set no `SetDocument` + reaplicado a cada paint).

### Phase 5 — Design aids

- **Rulers** (`PaintRulers`): faixa 18px top + left, major ticks a cada 100px lógico (com label), minor a cada 50px. Tick density adapta ao zoom (multiplica por 2× quando major < 50 widget pixels).
- **Grid overlay** (`PaintGridOverlay`): dots brancos 8% alpha em intervalos de `Settings.GridSize`. Skip render quando dots ficariam < 4 widget pixels (zoom muito baixo).
- **Status bar** no canvas widget: linha 22px no rodapé, mostra `Name · Type · WxH @ X,Y · Anchor:X` ou "Nothing selected" / "N elements selected".
- **Alignment guides**: deferred pra M14 (snap-to-sibling-edges é trabalho não-trivial e baixo ROI vs README + samples).

### M13 polish

- README reescrito com Quick start, Concepts, Sample documents section, Editor walkthrough, Keyboard shortcuts, Troubleshooting (CS0111, conflicts, blank preview).
- `Editor/SuiSampleGenerator.cs` (novo) — 4 samples canônicos via API: simple_panel, inventory_basic, hotbar_basic, hud_survival.
- `Tools > Install Sample Documents` cria `Assets/SuiSamples/` e popula via `AssetSystem.CreateResource`. Skip se já existir.
- Ruler/Grid toggles cabeados em Tools menu também (via canvas toolbar buttons).

### Files added / modified neste run

```
Added:
  Editor/Canvas/SuiCanvasToolbar.cs
  Editor/SuiSampleGenerator.cs
  samples/ui/simple_panel.sui   (referência hand-rolled)

Modified:
  Code/Runtime/SuiCanvasSettings.cs       — PreviewWidth/PreviewHeight
  Code/Runtime/SuiDocumentSettings.cs     — ShowGrid, ShowAlignmentGuides
  Editor/Canvas/SuiCanvasViewport.cs      — PaintRulers, PaintGridOverlay, ApplyPanelSizeFromDocument
  Editor/Canvas/SuiLayoutSolver.cs        — PanelSize settable
  Editor/Widgets/SuiCanvasWidget.cs       — toolbar + status bar wiring
  Editor/SuiDesignerWindow.cs             — InstallSamples menu action
  README.md                               — full rewrite
```

---

## Batch 1 — autonomous run (2026-05-08) ✅ validated end-to-end

**Validation passou em todos os cenários (M12 + menus + drag-drop + banner):**
- Recompile sem mudança → Skipped
- Recompile com mudança → Preserved + backup em `.sui-backups/` (fora de Code/)
- Conflict detection → arquivo sem header gera Conflict + banner vermelho no canvas, arquivo não sobrescrito
- Open Generated Folder → Explorer abre no path correto
- Change Output Folder → folder picker reabre, próximo compile vai pro destino novo
- Concurrent guard → segundo click rápido em Compile é ignorado
- Edit Cut/Copy/Paste/Rename via menu → todos funcionais
- View Zoom In/Out + Fit to Screen → zoom altera + persiste
- Tools Regenerate Preview + Clean Preview Cache → arquivos atualizados/limpos
- Atalho Ctrl+B compila
- Drag Panel pra área vazia → child do Root
- Drag Image em cima de Panel → child do Panel (deepest wins)
- Drop point vira X/Y do elemento (não centro)
- Click-to-add antigo continua funcionando

**Bugs caçados durante validação:**
- Path duplicado `Code/TestUI/Code/TestUI/...` — pipeline prefixava folder + writer juntava de novo. Fix: passar `OutputFolder=""` pro pipeline em final mode, writer faz o join único
- Preview cache colidindo com final output (mesmo namespace) — fix: preview emite sub-namespace `.SuiPreview`
- Backups dentro de Code/ duplicando partial class — fix: backups vão pra `<projectRoot>/.sui-backups/` fora de Code/
- Auto-clean em document load + compile pre-flight remove legacy backups



Plano em `AUTONOMOUS_PLAN_2026-05-08.md` (root). Tasks 1-4 implementadas:

### T1 — M12 Compile-to-disk + manifest + safe-overwrite + backup folder

- `Editor/SuiCompileResult.cs` — classification (Generated/Skipped/Preserved/Conflicts/Obsolete)
- `Editor/SuiCompileWriter.cs` — orquestra write+backup+manifest+conflict logic
  - Manifest persistido em `<output>/.sui-manifest/<DocumentId>.json`
  - Backup pré-overwrite em `<output>/sui-generated-backups/<DocName>/<UTC-timestamp>/`
  - Header parse via existing `SuiHeaderEmitter` decide ownership
- `SuiDesignerWindow.Compile()` chama `SuiCompileWriter.Run`. Concurrent guard via `_compileRunning`.
- `SuiDesignerWindow.PromptOutputFolder()` usa `Editor.FileDialog` com `SetFindDirectory()`. Stored project-relative quando dentro do project root.
- `OpenGeneratedFolder()` abre Explorer no folder de output via `Process.Start` + `UseShellExecute=true`.
- `SuiCompileResultsWidget.DisplayCompileResult` renderiza 5 seções classificadas.

### T2 — Wire dos no-ops dos menus

- Edit menu: Cut/Copy/Paste/Duplicate/Delete/Rename agora funcionais
- View menu: Zoom In/Out/Fit to Screen via `_canvas.GetViewport()`
- Tools menu: Regenerate Preview (limpa + reescreve cache), Clean Preview Cache (deleta inteiro)
- Atalhos novos: Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+B (compile), Ctrl++/-/0 (zoom)
- Clipboard process-local: `SuiClipboard` static + `SuiPasteElementCommand`
- Controller: `CopyElement`, `CutElement`, `PasteElement`, `CanPaste`

### T3 — Banner de erro de compile sobre canvas

- `Editor/Canvas/SuiCanvasErrorBanner.cs` — Title + Detail + OnClick + OnDismiss
- `SuiCanvasViewport.ErrorBanner` property + paint pass + click handler (X dismisses)
- `SuiDesignerWindow.SetCompileBanner` propaga erros do compile pro banner

### T4 — Drag-drop Palette → canvas

- `SuiPaletteButton` subclass de Button com `IsDraggable=true` + `OnDragStart` payload `SuiElementType`
- Canvas viewport: `AcceptDrops=true` + `OnDragHover/OnDragDrop/OnDragLeave`
- Visual feedback: container highlightado em verde durante hover
- Drop point convertido pra logical coords + posição relativa ao parent
- Click-to-add antigo continua funcionando

### Compile state at end of batch 1

`get_compile_errors` reporta zero diagnostics. Source ↔ runtime mirror sync clean (zero diff fora de obj/Properties/csproj).

---

## Canvas redesign — overnight run (2026-05-08)

Per `docs/prd/15_canvas_redesign.md`, the runtime-preview canvas was replaced
by an **editor-native paint canvas** (Unity UI Builder pattern). The old
runtime preview is preserved as an **on-demand modal** invoked via the toolbar
"Preview" button. Edits during design no longer trigger hotload churn — paint
redraws are sub-millisecond.

### What was built

**Phase 0 — Preview-as-button**
- `Editor/SuiPreviewWindow.cs` — modal window hosting the existing
  `SuiPreviewHost` (with all the OnEnabled/OnPreRender reflection workarounds
  preserved). Compiles + writes preview cache + mounts the generated type.
- `SuiDesignerWindow` toolbar gained a "Preview" button (`play_circle`) that
  opens the modal. The "Refresh Preview" placeholder was removed.

**Phase 1 — Paint-based canvas**
- `Editor/Canvas/SuiLayoutSolver.cs` — given a `SuiDocument`, produces a flat
  `Dictionary<elementId, Rect>` in logical-pixel space. **Single source of truth**:
  renderer + hit-test + chrome all read from this dict, so visual ↔ hit-test
  drift is impossible. Includes anchor + pivot math (mirrors
  `SuiScssGenerator.EmitAnchorRules`) and a `RectToLayoutValues` reverse pass
  used by drag/resize commit.
- `Editor/Canvas/SuiFlexLayout.cs` — Yoga-subset flex pass. Supported:
  `flex-direction` (4 values), `justify-content` (6 values), `align-items` (4
  values), `gap`, `margin`, `padding`. Skipped (V2): `flex-grow/shrink`,
  `flex-wrap`, `align-self`, baseline.
- `Editor/Canvas/SuiCanvasRenderer.cs` — paints each `SuiElementType` via the
  Editor `Paint` API. Honors `BackgroundColor`, `BorderColor/Width/Radius`,
  `Opacity`, `Visibility`, `BackgroundImage` (FitMode + Position), `Tint`,
  Text font/size/weight/color/align/overflow, Button label, ProgressBar fill,
  ItemIcon stack count.
- `Editor/Canvas/SuiCanvasViewport.cs` — the actual paintable widget. Owns
  zoom + pan, applies the logical→widget transform via `Paint.Translate` +
  `Paint.Scale`, paints the document, then resets transform and paints chrome
  in widget pixels. Includes checkerboard background, hover outline,
  selection chrome (border + 8 handles + size label), marquee rect.
  Mouse events forwarded to parent canvas via delegate hooks.
- `Editor/Widgets/SuiCanvasWidget.cs` — completely rewritten. Now a thin
  shell over `SuiCanvasViewport`. Owns hit-test (using solver dict) +
  drag/resize logic with Shift (axis-lock), Ctrl (proportional resize), Alt
  (snap-off). Live drag mutates `Layout.X/Y/W/H` directly; commits a
  single `SuiMoveElementCommand` / `SuiResizeElementCommand` on release for
  clean undo.

**Phase 2 — Interactions**
Built into the canvas widget rewrite:
- Click-to-select (deepest element wins).
- Hover outline (1px blue).
- Selection chrome (border + 8 handles for absolute elements; "FLEX" label
  for flex-mode elements).
- Drag body to move (4px threshold to avoid accidental drag on click).
- Drag handles to resize (8 directions).
- Marquee select (visual rect; multi-element pick-up reserved for V2).

**Phase 3 — Flex layout**
- `SuiFlexLayout.Solve()` runs as part of the layout pass when a parent's
  `Layout.Mode == Flex`. Children get rects from the flex pass; nested
  containers recurse normally.
- Supports HorizontalBox / VerticalBox / Hotbar / any element switched to
  Flex mode in the schema.

### What was preserved

- All M0–M9 work (schema, generator, controller, palette, hierarchy, details).
- `SuiPreviewHost` + `SuiPreviewCacheWriter` + reflection workarounds — backing
  store for the Preview button.

### What was added to the schema

- `SuiDocumentSettings.CanvasZoom`, `CanvasPanX`, `CanvasPanY` — persist the
  design canvas view state across reopens.

### Known caveats and what to validate (tomorrow)

1. **Visual fidelity vs. SCSS render** — the canvas paint is an approximation.
   Side-by-side comparison with the Preview button is the validation step.
   Likely first divergences: text rendering (font hinting), border-radius
   anti-aliasing, image tint blending. Document these for V1 polish.

2. **Selection chrome over flex children** — they get the "FLEX" label
   instead of resize handles. By design (resize on flex children doesn't
   write back to a meaningful X/Y). Edit through Details.

3. **Reverse anchors during drag** — for TopRight / BottomLeft / BottomRight /
   etc., the X/Y are interpreted as inward offsets. Current drag math uses
   the same `signX/signY` convention as the SCSS, but visual feedback during
   drag may feel inverted on reverse anchors. Test before declaring it a bug.

4. **Multi-select** — marquee draws but only single-element pick-up wired.
   Shift+click and marquee-multi-pickup are V2 follow-ups.

5. **Toolbar: zoom dropdown / resolution preset / snap toggle** — not built.
   The viewport itself supports zoom (mouse wheel) and a `FitCanvas()` API,
   but the toolbar UI buttons for those are V1.

6. **Image case/extension bug from M8** — `AssetPicker` was already fixed in
   the prior session to derive from `AbsolutePath`. New code uses
   `Project.Current.RootDirectory + Assets/<ImagePath>` to resolve the disk
   path for `Paint.LoadImage`. Should work; test with the
   `InventoryArmorIcon.png` case from yesterday.

7. **Hot reload state** — `_dragElement`, `_marqueeStartWidget` etc. live on
   the widget instance. Hotload replaces the widget (rebuilds via OnHotload).
   Mid-drag hotload would lose drag state — practically not an issue (hotload
   only fires on file save), but worth knowing.

8. **The original `SuiPreviewHost` reflection hacks remain** — used only by
   the Preview window now. If the engine ever surfaces editor-scene lifecycle
   dispatch properly, these can be removed.

### Files added / modified this run

```
Added:
  Editor/SuiPreviewWindow.cs
  Editor/Canvas/SuiLayoutSolver.cs
  Editor/Canvas/SuiCanvasRenderer.cs
  Editor/Canvas/SuiCanvasViewport.cs
  Editor/Canvas/SuiFlexLayout.cs

Modified:
  Editor/SuiDesignerWindow.cs           — toolbar adds "Preview" button
  Editor/Widgets/SuiCanvasWidget.cs     — full rewrite, no more SceneRenderingWidget
  Code/Runtime/SuiDocumentSettings.cs   — adds CanvasZoom + CanvasPanX/Y

Synced to runtime path:
  estudonovo/Libraries/kikozl.sbox_ui_designer/...
```

### Compile state at end of run

`kikozl.sbox_ui_designer.editor v0.0.146` fast-hotloaded successfully.
53 types registered. All builds green.

---

## What's deferred (V1 polish — Batch 2 onwards)

- **Toolbar widgets** (Phase 4 of canvas redesign): zoom dropdown, resolution preset, snap toggle, fit-canvas button.
- **Multi-resolution presets**: `SuiCanvasSettings.PreviewWidth/Height` exists, no UI to drive it.
- **Smart alignment guides** (Phase 5): snap to siblings/parent edges during drag.
- **Rulers + grid overlay** (Phase 5): hooks ready (`Settings.ShowRulers`, `Settings.GridSize`), no paint code yet.
- **Multi-edit no Details**: editar propriedade comum em N elementos selecionados.
- **Anchor preset picker** UI (visual em vez de dropdown enum).
- **M13 polish**: error message improvements + README + 4 sample `.sui` files.
- **ISSUEs 001/002/003**: ver `ISSUES.md`.

---

## Validation checklist for the morning

Open any `.sui`. The canvas should now show all elements rendered via Paint
API, no SceneRenderingWidget. Test in this order:

1. **Render**: do all elements render at the right positions / sizes /
   colors / fonts? (Compare to `Preview` button output.)
2. **Hover**: mouse over an element → blue outline appears around it.
3. **Click**: click an element → border + 8 handles appear; Hierarchy
   selects matching item.
4. **Click empty area**: chrome disappears, selection clears.
5. **Drag body**: element moves with cursor; X/Y in Details update on
   release. Undo (Ctrl+Z) restores pre-drag.
6. **Drag corner handle**: element resizes from that corner; W/H update.
7. **Drag edge handle**: element resizes one axis only.
8. **Shift+drag move**: locks to dominant axis.
9. **Ctrl+drag corner**: preserves aspect ratio.
10. **Mouse wheel**: zooms anchored at cursor; persists across reopen.
11. **Anchor variations**: try MiddleCenter, TopRight, BottomLeft elements;
    drag each; verify chrome and visual stay aligned.
12. **Flex containers**: add HorizontalBox + child Texts; verify they
    layout left-to-right with gap; switch to VerticalBox; verify column.
13. **Image element**: pick a png; verify it renders with correct fit.
14. **Preview button**: click toolbar Preview; modal opens; runtime UI
    appears; compare side-by-side to canvas. Note divergences.

If any of those fails, check the Console for `[Sui ...]` log lines — most of
the diagnostic logs from the previous session were retained.
