# Milestone M14 — UI/UX Redesign + Layout Rebrand

**Status:** planejado, **não iniciar até** Batches 2-3 + ISSUEs 001/002/003 estarem fechados.

**Goal:** chegar a 1:1 com o mockup do usuário (referência: screenshot anexado em `docs/mockups/redesign_target.png` — quando salvo). Visual estilo Unreal Widget Blueprint mas mantendo identidade s&box (tema escuro, acentos azuis, tipografia técnica).

**Estimativa total:** 5-7 dias focados.

**Pré-requisitos (devem estar fechados antes de começar):**
- [ ] Batch 2 (M13 polish + samples + Phase 4-5 canvas) entregue
- [ ] ISSUE-001 (color picker) resolvido — bloqueia porque vai tocar no Details
- [ ] ISSUE-002 (text auto-size) resolvido — bloqueia porque vai mudar Details + canvas
- [ ] ISSUE-003 (color picker custom) resolvido — superseded ISSUE-001

---

## 1. Categorias de mudança

### Categoria A — Reorganização estrutural (~2 dias)

#### A.1 Tabs Designer / Preview / Code no centro

- Canvas widget vira filho de um `Editor.TabWidget` com 3 pages.
- **Designer** (default) — o canvas paint atual. Sem mudança de comportamento.
- **Preview** — embeda o `SuiPreviewWindow` host atual (que hoje é modal). Quando a tab é ativada: roda `SuiCompileWriter` em modo Preview + monta o type + renderiza num `SceneRenderingWidget`. **Não atualiza live.** Botão "Refresh" inline pra forçar regen.
- **Code** — `TextEdit` read-only com syntax highlight (se Editor API permitir; senão plain). Atualiza só quando user clica Compile (ou quando tab é ativada e tem output cached).

**Files:**
- `Editor/Widgets/SuiCanvasWidget.cs` — wrap em TabWidget
- `Editor/Widgets/SuiPreviewTab.cs` (novo) — extrai lógica de `SuiPreviewWindow` pra widget reutilizável
- `Editor/Widgets/SuiCodeTab.cs` (novo) — TextEdit + populate via `SuiGenerationPipeline.Run`
- `SuiDesignerWindow` — passar TabWidget como dock central em vez de SuiCanvasWidget direto

#### A.2 Toolbar inline do canvas

- Linha extra dentro de SuiCanvasWidget (acima do viewport):
  - **Screen size** dropdown (1920×1080, 1280×720, 2560×1440, custom)
  - **Zoom** dropdown (25/50/75/100/150/200%, fit, custom)
  - **Snap** dropdown (off/4/8/16/32px)
  - **Alignment buttons** (left/right/top/bottom/center horizontal/center vertical)
  - **Lock toggle** (impede edição sem trocar pra outro tool)
- Settings persistem no `SuiDocumentSettings` (já tem `SnapToGrid`, `GridSize`; falta `PreviewWidth`, `PreviewHeight`)

**Files:**
- `Editor/Canvas/SuiCanvasToolbar.cs` (novo)
- `Code/Runtime/SuiCanvasSettings.cs` — adicionar `PreviewWidth`, `PreviewHeight` (com defaults 1920/1080)
- `Editor/Widgets/SuiCanvasWidget.cs` — Layout.Column com toolbar + viewport

#### A.3 Toolbar superior simplificada

Hoje: `Save | Compile | Preview | Undo | Redo`.

Manter mas remover qualquer responsabilidade de zoom/snap/grid (vai pro canvas toolbar). Adicionar `Settings` à direita com gear icon que abre dialog de preferências do editor.

**Files:**
- `Editor/SuiDesignerWindow.cs` — `BuildToolBar()` enxuto

#### A.4 Bottom panel multi-tab recolhível

Hoje: dois docks separados (Compile Results, Animations) na bottom outer.

Substituir por **um único dock** `SuiBottomTabsWidget` com `TabWidget` interno:
- **Animations** (placeholder, V2)
- **Bindings** (placeholder, V2)
- **Compile Results** (existente, refator pro novo container)
- **Logs** (novo — captura `Log.Info/Warning/Error` com prefixo `[Sui]` e mostra timeline)

Recolhível via DockManager close button (já suportado). Reabre via View menu novo item "Show Bottom Panel".

**Files:**
- `Editor/Widgets/SuiBottomTabsWidget.cs` (novo)
- `Editor/Widgets/SuiCompileResultsWidget.cs` — vira tab dentro do bottom widget, perde standalone status
- `Editor/Widgets/SuiAnimationsWidget.cs` — idem
- `Editor/Widgets/SuiLogsWidget.cs` (novo) — listener de `Log.*` filtrando por `[Sui]`

#### A.5 Search bars

- **Palette**: LineEdit acima das categorias. Filter live: substring match em ElementType.ToString().
- **Hierarchy**: LineEdit acima da tree. Filter recursivo: matching elements expandidos + ancestors visíveis, não-matching colapsam.
- **Details**: LineEdit acima das sections. Filter recursivo: rows com label matching ficam, sections sem matches colapsam automaticamente.

**Files:**
- `Editor/Widgets/SuiPaletteWidget.cs` — adiciona search + filter logic
- `Editor/Widgets/SuiHierarchyWidget.cs` — search + tree filter (TreeView API permite?)
- `Editor/Widgets/SuiDetailsWidget.cs` — search + row visibility

---

### Categoria B — Visual polish ("menos quadrado, menos grosso") (~1-2 dias)

#### B.1 Audit do Editor.Theme

Antes de começar polish, **investigar** o que o `Editor.Theme` permite customizar via `SetStyles` ou `Theme.*` overrides.

Opções esperadas (a verificar):
- `Theme.WidgetBackground`, `Theme.ButtonBackground`, `Theme.RowHeight`, `Theme.Primary`
- `SetStyles( "border-radius: 4px; padding: 6px 8px; ..." )` — CSS-like nos widgets

Trabalho concreto vai depender do que descobrir.

#### B.2 Border radius + padding consistentes

- Buttons: 4px radius em vez de 0
- Section headers: 4px top radius, 0 bottom (encaixe no container)
- Input rows (LineEdit, dropdowns): 3px radius, padding interno 6px horizontal
- DockManager corners: arredondar onde possível

#### B.3 Hover/selection states sutis

- Palette items: hover bg `rgba(255,255,255,0.04)`, selection bg `rgba(0,140,255,0.15)`
- Hierarchy items: mesmo padrão (já criamos seleção via callback custom no Batch 1 — só ajustar cor/alpha)
- Section headers: hover muda só o ícone de expand pra mais brilho

#### B.4 Section headers mais leves

Hoje: blocos com background. Trocar por:
- Texto + chevron (▾/▸)
- Linha divisória abaixo (1px `Theme.WidgetBackground.Lighten(0.05)`)
- Sem background fill

#### B.5 Selection chrome do canvas mais sutil

Hoje: 1.5px border + 8 handles 8×8 + label `1344×752 @ -32,-24`.

Mockup-style:
- Border 1px (mais fino)
- Handles 6×6 (menores)
- Label fora do objeto, em status bar do canvas, não em cima

#### B.6 Rulers no canvas (top + left)

PRD doc 06 § View menu lista `Show Rulers`. Settings já existe (`Settings.ShowRulers`). Implementar paint:
- Faixa 24px top + 24px left
- Marcas a cada 50px logical (mostra label "100", "200", ...)
- Cor: `Theme.WidgetBackground.Darken(0.1)` com texto `Theme.Text.Dim`
- Toggle via View menu

**Files:**
- `Editor/Canvas/SuiCanvasViewport.cs` — método `PaintRulers()` no chrome pass

---

### Categoria C — Funcionalidades novas (~1-2 dias)

#### C.1 Search funcional na Palette/Hierarchy/Details

(Já listado em A.5 mas é trabalho funcional, não só layout — destacando aqui.)

#### C.2 Components favoritos na Palette

- Section "FAVORITES" no topo da palette quando há ≥1 favorito
- Right-click no palette item → "Add to Favorites" / "Remove from Favorites"
- Persistência: `SuiDocumentSettings.PaletteFavorites: List<SuiElementType>` ou per-user `Editor.Cookie`
- Decisão: per-document ou per-user? **Per-user** faz mais sentido (favoritos do designer, não do projeto)

**Files:**
- Cookie storage via `Editor.Cookie.Set/Get`
- `Editor/Widgets/SuiPaletteWidget.cs` — render Favorites section + context menu

#### C.3 Lock/hide toggles na Hierarchy

- Ícones clicáveis 14×14 ao lado do nome do elemento na tree
- 👁 / 👁‍🗨 (visibility) — toggle `Element.Flags.HiddenInDesigner`
- 🔒 / 🔓 (lock) — toggle `Element.Flags.Locked`
- Click no ícone faz toggle (sem selecionar o elemento)
- Schema: `SuiElementFlags.HiddenInDesigner` e `Locked` já existem

**Files:**
- `Editor/Widgets/SuiHierarchyWidget.cs` — `SuiElementTreeNode.OnPaint` adiciona ícones clicáveis (provavelmente via `OnMousePress` na area da node)

#### C.4 Breadcrumb do selecionado

- Topo do Details panel (acima de Search): mostra `Root > Canvas > Panel > Image_Health`
- Cada segmento clicável → seleciona aquele ancestor
- Atualiza on `SelectionChanged`

**Files:**
- `Editor/Widgets/SuiDetailsWidget.cs` — header com breadcrumb antes de Search

#### C.5 Status bar do canvas

- Linha fixa no rodapé do `SuiCanvasWidget` (abaixo do viewport)
- Conteúdo: `Selected: Image_Health · 1344×752 · X:-32 Y:-24 · Anchor:MiddleCenter` (ou "Nothing selected" / "5 selected")
- Atualiza on `SelectionChanged`

**Files:**
- `Editor/Widgets/SuiCanvasWidget.cs` — Layout adiciona status bar Label

#### C.6 Reorder Details: Common → Transform → Appearance → Events → Advanced

Mover blocos no `BuildElementSections`:

```csharp
BuildCommonSection( el );      // Name, Is Variable, Tooltip Text
BuildLayoutSection( el );      // Anchors, Position, Size, Alignment, Z, Pivot, Margin, Padding
BuildAppearanceSection( el );  // BG, Border, Opacity, Visibility, PointerEvents
BuildEventsSection( el );      // OnClicked, OnHovered (V1.5 placeholder, mas reservar slot)
BuildPropsSection( el );       // Type-specific (TextProps, ImageProps, etc) — fica em Appearance ou Advanced?
BuildAdvancedSection( el );    // Overflow, ClipToBounds, RenderOpacity (advanced flags)
BuildNotesSection( el );       // Notes
```

Decisão de UX: type-specific props (Text font, Image path) ficam num grupo separado **Appearance** ou caem em **Advanced**? **Recomendo Appearance** — são visualmente impactantes.

---

## 2. Acceptance Criteria

### Visual (mockup-driven)

1. ☐ Layout final bate visualmente com a screenshot mockup salva em `docs/mockups/redesign_target.png` (placeholder até salvar)
2. ☐ Tabs Designer/Preview/Code visíveis no centro
3. ☐ Toolbar superior simplificada (Save | Compile | Preview | Undo | Redo + Settings à direita)
4. ☐ Toolbar inline no canvas com Screen/Zoom/Snap/Alignment/Lock
5. ☐ Bottom panel com 4 tabs (Animations | Bindings | Compile Results | Logs)
6. ☐ Search bars na Palette, Hierarchy, Details
7. ☐ Visual: bordas arredondadas (4px), section headers leves (texto + chevron, sem background), hover/selection sutis
8. ☐ Rulers ativos no canvas (top + left, 24px, escala em px logical)

### Funcional

9. ☐ Tab Preview: ao clicar, roda compile + monta type + renderiza no SceneRenderingWidget. NÃO atualiza live. Tem botão Refresh.
10. ☐ Tab Code: read-only, mostra output do `SuiGenerationPipeline.Run` da última Compile.
11. ☐ Search filter live em Palette/Hierarchy/Details — funciona em < 16ms (≤ 1 frame)
12. ☐ Lock/hide ícones na Hierarchy clicáveis, toggle persiste
13. ☐ Breadcrumb do selecionado clicável (segmentos = ancestors)
14. ☐ Status bar do canvas mostra info da seleção
15. ☐ Components favoritos persistem entre sessões
16. ☐ Details groups na ordem Common → Transform → Appearance → Events → Advanced

### Compatibilidade

17. ☐ Documentos `.sui` antigos abrem sem migração necessária (schema compatível)
18. ☐ Os 4 sample `.sui` files (do M13) renderizam sem regressão
19. ☐ Compile + Preview + Undo/Redo funcionam idênticos ao comportamento pré-redesign

---

## 3. Não-objetivos (V2 explícitos dentro do redesign)

- **Animations panel real** — continua placeholder. Schema `SuiAnimationData` já existe.
- **Bindings panel real** — continua placeholder. Schema `SuiEventBinding` já existe.
- **Code tab editável** — read-only somente. Editar abre worms (parser, save-back, conflito gerador).
- **Real-time preview** — modal/tab opt-in apenas. Live preview tem hotload churn (foi razão do canvas redesign do M11).
- **Designer | Graph mode switch** (PRD doc 06) — V2 de v2, fora de M14.
- **Cut/Copy/Paste cross-document** — clipboard process-local (já feito no Batch 1).
- **Theme customization pelo usuário** (light mode, custom colors) — V3.

---

## 4. Riscos técnicos

### R1 — Editor.Theme/SetStyles limitations
**Severidade:** Alta
**Mitigação:** primeiro passo do M14 = audit do que Theme permite. Se for muito limitado, parte da Categoria B (visual polish) fica reduzida.

### R2 — TabWidget no centro vs DockManager
**Severidade:** Média
**Mitigação:** confirmar que `DockManager.AddDock` aceita um `TabWidget` como dock. Modelo: `SuiBottomTabsWidget` (do A.4) é exatamente isso, posso testar A.4 primeiro pra validar antes de fazer A.1.

### R3 — Preview embed (não-modal) reintroduz hotload churn
**Severidade:** Média
**Mitigação:** Preview tab só compila/regenera quando o user **explicitamente ativa a tab ou clica Refresh**. Não escuta DocumentChanged. Confirmar que SceneRenderingWidget embedado funciona com isso.

### R4 — Search com filter recursivo na Hierarchy
**Severidade:** Baixa
**Mitigação:** TreeView pode ou não suportar filter de items. Se não, rebuild da tree on every search keystroke (debounced 100ms).

### R5 — Persistência de favorites cross-session
**Severidade:** Baixa
**Mitigação:** `Editor.Cookie` é o storage padrão; já usado em outros contextos (StateCookie do DockManager).

### R6 — Performance do filter em Details
**Severidade:** Baixa
**Mitigação:** filter por substring é O(n) em rows. Doc típico tem ~50 rows visíveis. Trivial.

---

## 5. Ordem de ataque sugerida

Quando começar o M14:

**Fase 1 — Audit (0.5 dia)**
- Investigar Editor.Theme limits
- Verificar DockManager + TabWidget compat
- Salvar mockup em `docs/mockups/redesign_target.png`

**Fase 2 — Estrutural (Categoria A) (~2 dias)**
1. A.4 Bottom panel multi-tab (testa TabWidget em dock)
2. A.1 Designer/Preview/Code tabs no centro
3. A.2 Toolbar inline do canvas
4. A.3 Toolbar superior simplificada
5. A.5 Search bars

**Fase 3 — Visual (Categoria B) (~1.5 dia)**
6. B.1 Confirmar findings do audit
7. B.2 Border radius + padding
8. B.3 Hover/selection states
9. B.4 Section headers
10. B.5 Selection chrome canvas
11. B.6 Rulers

**Fase 4 — Functional (Categoria C) (~1.5 dia)**
12. C.6 Reorder Details (trivial, fazer junto com B)
13. C.5 Status bar canvas
14. C.4 Breadcrumb
15. C.3 Lock/hide ícones na Hierarchy
16. C.2 Components favoritos
17. C.1 já feito em A.5

**Fase 5 — Validation (0.5 dia)**
- Side-by-side mockup vs implementação
- Reabrir os 4 samples do M13 e verificar regressão zero
- Snapshot final de screenshots

---

## 6. Files que serão criados ou tocados

### Novos
- `Editor/Widgets/SuiPreviewTab.cs`
- `Editor/Widgets/SuiCodeTab.cs`
- `Editor/Canvas/SuiCanvasToolbar.cs`
- `Editor/Widgets/SuiBottomTabsWidget.cs`
- `Editor/Widgets/SuiLogsWidget.cs`

### Modificados (refator significativo)
- `Editor/SuiDesignerWindow.cs` — toolbar enxuta + dock layout novo
- `Editor/Widgets/SuiCanvasWidget.cs` — TabWidget wrap + toolbar inline + status bar
- `Editor/Widgets/SuiPaletteWidget.cs` — search + favorites + visual
- `Editor/Widgets/SuiHierarchyWidget.cs` — search + lock/hide ícones + visual
- `Editor/Widgets/SuiDetailsWidget.cs` — search + breadcrumb + reorder + visual
- `Editor/Widgets/SuiCompileResultsWidget.cs` — vira tab dentro de bottom
- `Editor/Widgets/SuiAnimationsWidget.cs` — vira tab dentro de bottom
- `Editor/Canvas/SuiCanvasViewport.cs` — rulers + selection chrome refinada

### Schema additions
- `Code/Runtime/SuiCanvasSettings.cs` — `PreviewWidth`, `PreviewHeight`

### Cookies (per-user)
- `SuiDesigner.PaletteFavorites` — JSON list of `SuiElementType` strings

---

## 7. Open questions resolvidas (2026-05-08)

| Pergunta | Decisão |
|---|---|
| Rulers entram no milestone? | ✅ Sim |
| Components favoritos? | ✅ Sim, no milestone |
| Animations panel real? | ❌ Continua placeholder |
| Bindings panel real? | ❌ Continua placeholder |
| Code tab editável? | ❌ Read-only |

---

## 8. Decisões pendentes pra abrir quando iniciar M14

- [ ] Type-specific props ficam em Appearance ou Advanced no Details?
- [ ] Status bar do canvas: bottom-fixed ou floating overlay no canto?
- [ ] Tabs labels: "Designer / Preview / Code" ou icons-only com tooltip?
- [ ] Bottom panel default state: aberto ou recolhido?
- [ ] Breadcrumb localização: topo Details ou canvas top toolbar?
- [ ] Search bars: keyboard shortcut pra focus (Ctrl+F)?
