# Sbox UI Designer — Save Point (2026-05-09)

Snapshot completo do projeto antes de começar o M14 (rebrand visual). Lê esse doc em qualquer sessão futura pra reconstruir contexto sem precisar varrer o codebase.

---

## TL;DR

- **MVP funcional: 100%.** Designer completo: criar `.sui` → editar visual → compilar → arquivos `.razor`/`.razor.scss` no projeto-alvo + manifest + safe-overwrite.
- **PRD literal: ~97%.** 3 itens cosméticos pendentes (Show Anchors overlay, Show Safe Area overlay, state indicators ricos) — todos absorvidos pelo M14.
- **3 ISSUEs resolvidos** (color picker custom, text auto-size, color picker SV stale).
- **Zero compile errors.** Source ↔ runtime mirror sync clean (zero diff fora de artefatos).
- **Próxima frente:** M14 — rebrand visual UMG-like (visual mockup definido em conversa, doc detalhado em `MILESTONE_M14_REDESIGN.md`).

---

## 1. Estado de milestones

| MS | Title | Status | Deliverable |
|----|-------|--------|-------------|
| M0 | Repo setup | ✅ | `Code/`, `Editor/`, `samples/`, `docs/prd/`, `.sbproj` |
| M1 | Project inspection | ✅ | `docs/M1_PROJECT_INSPECTION.md` |
| M2 | `.sui` schema | ✅ | 15 POCO files in `Code/Runtime/`, validator, 17 self-tests |
| M3 | Asset + IAssetEditor | ✅ | `SuiAsset : GameResource`, double-click opens designer |
| M4 | Editor shell | ✅ | DockWindow + menu/toolbar/5-region docks |
| M5 | Controller + commands | ✅ | `SuiDesignerController` + 11 commands, undo/redo |
| M6 | Palette + add | ✅ | Palette widget + click-to-add + drag-drop (Batch 1) |
| M7 | Hierarchy | ✅ | TreeView + context menu + drag-reorder/reparent |
| M8 | Details panel | ✅ | Sectioned property editor + custom color picker (Batch 3) |
| M9 | Generator | ✅ | Razor + SCSS pipeline + header parser + sha256 |
| M10 | Runtime preview | ✅ | Modal opt-in via `SuiPreviewWindow` |
| M11 | Canvas redesign | ✅ Phases 0-3 done | Paint-based 2D + zoom/pan + drag/resize/marquee + flex layout. Phases 4-5 partial; alignment guides deferred to M14 |
| M12 | Compile system | ✅ | `SuiCompileWriter` + manifest + backup + folder picker |
| M13 | Polish | ✅ | README + 4 samples (Tools menu) + canvas toolbar + rulers + grid |
| M14 | UI/UX redesign | 📝 planejado | Doc em `MILESTONE_M14_REDESIGN.md` — não iniciado |

---

## 2. Arquitetura — visão de 30 segundos

**Source of truth:** o arquivo `.sui` (JSON via `GameResource`).

**Pipeline de compile:**
```
.sui document
  ↓ (validator)
  ↓ (Razor generator)        ← Code/Generation/SuiRazorGenerator.cs
  ↓ (SCSS generator)         ← Code/Generation/SuiScssGenerator.cs
  ↓ (header emitter)         ← SUI:GENERATED:BEGIN/END markers
SuiGenerationResult (in-memory)
  ↓ (SuiCompileWriter)       ← Editor/SuiCompileWriter.cs
disk: <output>/UiTest2.razor + UiTest2.razor.scss
       <output>/.sui-manifest/<DocumentId>.json
       <projectRoot>/.sui-backups/<doc>/<UTC-ts>/* (when overwriting)
```

**Editor stack:**
```
SuiDesignerWindow (DockWindow, IAssetEditor)
├── BuildMenuBar / BuildToolBar
├── DockManager.AddDock (5 docks)
│   ├── SuiPaletteWidget       (left top)
│   ├── SuiHierarchyWidget     (left bottom, TreeView)
│   ├── SuiCanvasWidget        (center)
│   │   ├── SuiCanvasToolbar   (inline top: Screen/Zoom/Snap/Grid/Rulers/Fit)
│   │   ├── SuiCanvasViewport  (paint-based 2D)
│   │   │   ├── SuiLayoutSolver
│   │   │   ├── SuiCanvasRenderer
│   │   │   └── SuiFlexLayout (+ SolveGrid for InventoryGrid/Grid)
│   │   └── status bar (selection size + position)
│   ├── SuiDetailsWidget       (right, sectioned + collapsible)
│   ├── SuiCompileResultsWidget (bottom outer)
│   └── SuiAnimationsWidget    (bottom outer, placeholder)
├── SuiDesignerController (selection + commands + dirty)
└── EditorEvent.Hotload → rebuild docks
```

**Canvas pipeline (per paint):**
```
OnPaint
  → Solver.Solve(doc)
      → MeasureAutoTexts (Editor.Paint.MeasureText for Auto Text)
      → SolveChildren recursive
          → SolveGrid for InventoryGrid/Grid
          → SuiFlexLayout.Solve for flex containers
          → ResolveAbsoluteRectWithOverride for absolute (uses measured text size)
  → Renderer.Paint(doc)
      → PaintElement recursive
          → PaintChildren in render order (sorted by ZIndex via GetRenderOrderedChildren)
          → Per-type render (PaintText, PaintImage with Paint.Draw GPU, PaintItemIcon, etc)
  → Chrome (selection, hover, drop indicator, marquee, rulers, grid, banner)
```

---

## 3. Decisões arquiteturais notáveis

### 3.1 Canvas paint-based (M11 redesign)

**Antes:** canvas usava `SceneRenderingWidget` + runtime engine → live preview que causava hotload churn em cada edit.

**Agora:** canvas é puramente `Editor.Paint` API. Renderiza o documento desenhando rectangles, text, images via Pixmap. Sem hotload, sub-millisecond redraw. Preview real virou modal opt-in.

**Trade-off:** canvas paint é aproximação do runtime. Pequenas divergências em font hinting, border-radius AA. ISSUE-002 (text auto-size) resolveu o caso mais grave (vertical alignment).

### 3.2 Generator paths

- **Preview mode** (`SuiPreviewCacheWriter`) → escreve pra `<projectRoot>/Code/_sui_preview/<ClassName>/` com namespace `<x>.SuiPreview` (sentinel pra evitar colidir com final output)
- **Final mode** (`SuiCompileWriter`) → escreve pra folder do user com namespace `<x>` original

Os 2 podem coexistir em runtime sem CS0111.

### 3.3 Backups fora de Code/

Backups antigos colidiam (`.razor` em `Code/sui-generated-backups/` virava `partial class` duplicado). Solução: backups novos em `<projectRoot>/.sui-backups/...` (fora de Code/). Auto-clean roda em document load + compile pre-flight pra remover legacy.

### 3.4 Event split (anti-cascade)

`DocumentChanged` no controller fira em qualquer property edit. Window NÃO cascadeia em `SelectionChanged` (esse era o bug do "Details fica preto durante color drag"). Eventos hoje:

- **DocumentChanged** → Canvas/Hierarchy/Details.SetDocument (idempotent quando instância igual)
- **SelectionChanged** → SetSelected/SetSelectedSet (REbuild Details rows). Fira só em mudança real de seleção OU quando data do selecionado muda externamente (canvas drag, anchor change → `NotifySelectionDataMaybeChanged`)

### 3.5 Stacking context per-parent

`SuiLayoutSolver.GetRenderOrderedChildren(parent)` ordena filhos por `Layout.ZIndex` ascending (stable). Renderer + hit-test + marquee usam mesma ordem. Comportamento idêntico ao CSS standard: ZIndex é local ao parent, não global.

### 3.6 ISSUE-002 Text auto-size

`SuiTextSizeMode { Auto, Fixed, AutoHeightWrap }`:
- **Auto** (default novo): solver mede texto via `Paint.MeasureText`, override W/H. Sem wrap. Rect == texto.
- **Fixed**: user define W/H. Suporta `VerticalAlign` via flex (`display:flex; flex-direction:column; justify-content:<map>`).
- **AutoHeightWrap**: W fixo, H grow. `white-space: normal`.

Migration on load: Text legacy com W/H>0 vira Fixed automaticamente.

### 3.7 ISSUE-003 Color picker custom

Substitui `Editor.ColorPicker.OpenColorPopup` (que tinha 5 bugs) por implementação própria:
- `SuiColorPickerPopup` com SV square (Pixmap cacheado por hue), Hue slider (rainbow 0-360 graus), Alpha slider, Old/New comparison swatches, Hex/RGB inputs
- `SuiColorSwatchField` substitui o LineEdit no Details: mostra a cor full-width com hex overlay em cor de contraste por luminância. × button pra clear, right-click menu (Copy/Paste/Clear)
- Estado interno em `Sandbox.ColorHsv` (Hue 0-360 graus) — sem round-trips lossy via hex/RGB

---

## 4. Bugs hit + soluções (memória de combate)

| Bug | Causa raiz | Fix |
|---|---|---|
| Hierarchy não destacava ao clicar canvas | `OnPaint` chamava `PaintSelection(item)` do base que olhava tree internal state, não nosso callback | Override pra desenhar via `_isSelectedFn` callback + `_tree.Update()` em `SetSelected/SetSelectedSet` |
| "Filhos saem do lugar dentro do pai" ao arrastar | Hit-test deepest-wins pegava filho quando user clicava em painel coberto pela imagem | Click-through guard: se selecionado é ancestral do hit, manter ancestral |
| Imagem repetindo no preview com Contain | SCSS faltava `background-repeat: no-repeat` (default CSS é `repeat`) | Emit no SCSS generator |
| Imagem qualidade ruim no canvas | `SetBrush(pixmap) + DrawRect` tilea, e CPU pre-resize ruim | Trocou pra `Paint.Draw(rect, pixmap, alpha)` (Qt drawPixmap GPU) + oversample guard pra heavy downsample |
| Scroll do Details ia pro bottom em cada edit | DocumentChanged → SetDocument com mesma instância → Refresh disparado spuriously | Gate Refresh por `ReferenceEquals` da instância em `SetDocument` |
| Anchor change fazia elemento saltar | `Layout.Anchor =` direto, sem recompute X/Y | `SuiSetAnchorCommand` snapshot rect → set anchor → `RectToLayoutValues` recalc |
| Rename não atualizava ClassName | `SuiRenameElementCommand` só setava Name | Auto-sync ClassName quando match `Sanitize(oldName)` (= não foi customizado) |
| Duplicate vinha com nome igual | `Clone()` preservava Name | `SuggestUniqueDuplicateName` strip `_N` sufixo + tentativa `_2/_3/...` |
| `Method not found: SuiDocumentSettings.get_CanvasZoom` | Editor assembly hotloaded antes do runtime rebuildar | `touch` no .cs runtime força rebuild |
| `string.AsSpan` não compila | Sandbox bloqueia | Usar `string.Substring` |
| `_action` é internal | `DragEvent._action` privado, mas `DragEvent.Action` é property pública | Usar `ev.Action = DropAction.Copy` |
| `Sandbox.UI.Layout` é namespace | Type real é `Editor.Layout` | Trocar param type |
| Color picker só mostrava paleta vermelha | `ColorHsv.Hue` é 0-360 graus, eu passava 0-1 | Multiply by 360 ao construir, divide ao ler |
| Details bugava durante color drag | DocumentChanged cascadeava em SelectionChanged → Refresh em loop | Removido cascade no Window |
| CS0111 partial class UiTest2 duplicate | Preview cache + final output mesma namespace | Preview emite namespace `.SuiPreview` |
| CS0111 backups dentro de Code/ | `sui-generated-backups/.../UiTest2.razor` virava partial class | Backups vão pra `<root>/.sui-backups/` (fora de Code/) + auto-clean |
| InventorySlot props ignorados | Renderer só chamava PaintPanelLike, não PaintItemIcon | Adicionado PaintItemIcon no case |
| InventoryGrid mostrava 1 slot | ApplyTypeDefaults não setava Flex+Wrap | Adicionou no defaults + solver SolveGrid pra grid types |
| Hotbar virou vertical | SolveGrid usa `Props.Columns=1` default → 1-coluna | Hotbar sai do SolveGrid path, vai pro flex normal |
| ZIndex ignorado no canvas | Renderer iterava em ordem da hierarquia, sem sort | `GetRenderOrderedChildren` helper compartilhado |
| Path duplicado `Code/TestUI/Code/TestUI/` | Pipeline prefixava folder + writer juntava de novo | Pipeline com `OutputFolder=""` em final mode |
| "Unable to find matching substitution for static method" log spam | Static field `_cache` Pixmap atravessava hotload com ponteiro Qt zombie | Trocou pra instance field |

---

## 5. File map (canon)

### `Code/Runtime/` (POCO + validator + migration)
- `SuiDocument.cs` — root + factories + lookup
- `SuiElement.cs` — node + ApplyTypeDefaults (incluindo defaults de Text/Button content)
- `SuiLayoutData.cs`, `SuiStyleData.cs`, `SuiElementProps.cs` — bags de propriedades
- `SuiCanvasSettings.cs` — BaseW/H + ScaleMode + SafeArea + BgPreview + PreviewW/H
- `SuiDocumentSettings.cs` — AutoPreview + Snap + Grid + Rulers + Anchors + SafeArea + ShowGrid + ShowAlignmentGuides + Canvas zoom/pan
- `SuiOutputSettings.cs` — output folder + namespace + class name + flags
- `SuiGeneratedFileManifest.cs` — manifest entries
- `SuiEnums.cs` — todos os enums (incluindo `SuiTextSizeMode`, `SuiVerticalAlign`)
- `SuiSchema.cs` — schema constants
- `SuiSafeArea.cs`, `SuiBackgroundPreview.cs` — sub-data
- `SuiEventBinding.cs`, `SuiAnimationData.cs` — V1.5/V2 placeholders
- `SuiDocumentValidator.cs` — invariantes + sanitizers
- `SuiDocumentMigration.cs` — migration on load (Text auto/fixed)
- `SuiSelfTest.cs`, `SuiSelfTestRunner.cs` — 17 testes runtime

### `Code/Generation/` (pure logic, no I/O)
- `SuiGenerationContext.cs` — input bag (Mode, OutputFolder, ClassName, Namespace)
- `SuiGenerationPipeline.cs` — validator → razor → scss → result. Preview mode emite `.SuiPreview` namespace
- `SuiGenerationResult.cs` — output (files, errors, warnings)
- `SuiRazorGenerator.cs`, `SuiScssGenerator.cs` — emitters (SCSS tem TextSizeMode-specific rules)
- `SuiHeaderEmitter.cs` — SUI:GENERATED:BEGIN/END
- `SuiAllowedPropertyList.cs` — CSS allowlist
- `SuiHashUtility.cs` — sha256
- `SuiNameSanitizer.cs` — class name sanitization

### `Editor/` (editor-side)
- `SuiDesignerWindow.cs` — DockWindow + IAssetEditor + menu/toolbar
- `SuiDesignerController.cs` — selection (multi-select) + commands + dirty
- `SuiPreviewHost.cs`, `SuiPreviewCacheWriter.cs`, `SuiPreviewWindow.cs` — runtime preview modal
- `SuiCompileWriter.cs`, `SuiCompileResult.cs` — disk write + manifest + backup
- `SuiClipboard.cs`, `SuiSampleGenerator.cs` — utilities

### `Editor/Canvas/`
- `SuiLayoutSolver.cs` — solve + Auto-text measure + grid solve + render order
- `SuiCanvasRenderer.cs` — per-element paint (Paint.Draw for images, ResolveTextFlag for text)
- `SuiCanvasViewport.cs` — paint widget + zoom/pan + drag/drop accept + rulers + grid + banner
- `SuiCanvasToolbar.cs` — inline toolbar (Screen/Zoom/Snap/Grid/Rulers/Fit)
- `SuiCanvasErrorBanner.cs` — banner overlay struct
- `SuiFlexLayout.cs` — Solve (flex) + SolveGrid (wrapped grid)

### `Editor/Commands/` (11 commands, all implementing ISuiCommand)
- Add/Delete/Rename/Reorder/Reparent/Duplicate/Move/Resize/Anchor/Paste/SetProperty<T>

### `Editor/Widgets/`
- `SuiPaletteWidget.cs` — palette (drag enabled via SuiPaletteButton subclass)
- `SuiHierarchyWidget.cs` — TreeView with custom selection paint
- `SuiCanvasWidget.cs` — wraps viewport + toolbar + status bar
- `SuiDetailsWidget.cs` — sectioned property editor (refresh gated by selection ID)
- `SuiCompileResultsWidget.cs` — categorized sections (Generated/Skipped/Preserved/Conflict/Obsolete)
- `SuiAnimationsWidget.cs`, `SuiCollapsibleSection.cs` — UI helpers
- `SuiColorPickerPopup.cs` — custom color picker (HSV state, SV square, hue/alpha sliders, hex/RGB)
- `SuiColorSwatchField.cs` — full-width color field (replaces LineEdit + button)

### Root
- `STATUS.md` — milestone progress + batch summaries
- `ISSUES.md` — known issues (001/002/003 all resolved)
- `MILESTONE_M14_REDESIGN.md` — M14 plan (5-7 dias estimado, 4-6h real)
- `AUTONOMOUS_PLAN_2026-05-08.md` — Batch 1-3 plan
- `OVERNIGHT_REPORT.md`, `M6-M8_REPORT.md` — historical
- `README.md` — usage instructions
- `samples/ui/simple_panel.sui` — JSON reference sample (others via Tools menu)
- `docs/prd/00_overview.md` … `15_canvas_redesign.md` — PRDs originais

### Mirror (auto-synced)
`<estudonovo>/Libraries/kikozl.sbox_ui_designer/...` — runtime sync, idêntico ao source exceto `obj/`, `Properties/`, `*.csproj`.

---

## 6. Pendências honestas pré-M14

3 itens cosméticos do PRD literal que NÃO bloqueiam workflow:

1. **Show Anchors overlay** — `Settings.ShowAnchors` toggle existe, paint code não. Visual: linhas indicando anchor point dos elementos.
2. **Show Safe Area overlay** — `Settings.ShowSafeArea` + `SuiSafeArea` rect existem, paint code não.
3. **Document state indicators ricos** — só dirty `*` no título. PRD pede "Preview Updating", "Compile Needed", "Compile Error", "Output Missing".

**Decisão alinhada com user:** absorver no M14 (paint overlays se beneficiam do redesign visual; state indicators ficam parte da toolbar nova).

---

## 7. Memórias persistentes (`~/.claude/.../memory/`)

Memos gravados pra orientar sessões futuras:
- `feedback_language.md` — toda comunicação em PT-BR
- `feedback_sbox_sandbox_apis.md` — não usar `string.AsSpan`, `Span<T>`, `MemoryMarshal`
- `feedback_time_estimates.md` — dividir dev-day estimates por ~6-8 (AI velocity)
- `reference_sbox_paint_image_api.md` — usar `Paint.Draw(rect, pixmap)`, não SetBrush+DrawRect
- `reference_sbox_ui_designer_paths.md` — addon vive em 2 paths (source + runtime mirror)
- `project_sui_canvas_redesign.md` — overview do M11 + bugs resolvidos
- `reference_sbox_drag_mousepos.md` — `Sandbox.Mouse.Position` pra ghost icons (drag)
- `reference_sbox_editor_scene_panel_lifecycle.md` — editor scenes não rodam lifecycle, reflection workaround

---

## 8. Compile state at save point

- `get_compile_errors`: 0 diagnostics
- Source ↔ runtime mirror sync: clean (zero diff fora de obj/Properties/csproj)
- Self-tests M9: 17/17 verde

---

## 9. Próximo: M14

**Goal:** chegar 1:1 com o mockup do user (UMG-like, mantendo identidade s&box).

**Estimativa real:** 4-6h (não 5-7 dias do plano).

**Pré-requisitos cumpridos:** Batch 1 + 2 + 3 (ISSUEs 001/002/003) todos validados pelo user.

**Pacote de mudanças (do `MILESTONE_M14_REDESIGN.md`):**

A. Reorganização estrutural
- Tabs Designer / Preview / Code no centro
- Toolbar superior simplificada
- Bottom panel multi-tab (Animations/Bindings/Compile Results/Logs) recolhível
- Search bars na Palette/Hierarchy/Details

B. Visual polish
- Border radius 4px em widgets
- Section headers leves (texto + chevron)
- Selection chrome canvas mais sutil
- Rulers já feitos
- 3 itens cosméticos absorvidos: Show Anchors paint, Show Safe Area paint, document state indicators

C. Funcionalidades novas
- Components favoritos na Palette
- Lock/hide ícones na Hierarchy
- Breadcrumb do selecionado
- Reorder Details: Common → Transform → Appearance → Events → Advanced

**Ainda V2 mesmo dentro do M14:** Animations real, Bindings real, Code tab editável, Designer/Graph mode switch.

**Mockup:** user salvou imagem de referência durante a conversa. M14 começa pedindo screenshot/mais detalhes visuais pra match.

---

End of save point. **Re-leia esse doc no início da próxima sessão pra reconstruir contexto sem varrer códigos.**
