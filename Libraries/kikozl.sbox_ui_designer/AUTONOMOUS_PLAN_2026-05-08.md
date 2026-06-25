# Autonomous Plan — Sbox UI Designer MVP closure

**Criado:** 2026-05-08
**Goal:** fechar o "MVP DoD honesto" da PRD doc 13/14. Após esse plano, o designer vira ferramenta entregável: você desenha, compila, e os arquivos `.razor`/`.razor.scss` aparecem no projeto-alvo.

## Estrutura

- **Batch 1** (autonomous run, então valida): tasks 1-4 abaixo.
- **Batch 2** (autonomous run, então valida): M13 polish + samples + Phase 4-5 canvas.
- **Batch 3** (paralelo após batch 2): ISSUE-001 / 002 / 003.

Cada task tem:
- Files a tocar
- Sub-passos
- Acceptance criteria
- Riscos / blockers (que podem pausar o run)
- Estimativa

Convenção: ✅ done, 🔄 em progresso, ⏸ paused esperando user, ❌ blocked.

---

# BATCH 1 — Tasks 1-4

## Task 1 — M12 Compile-to-disk + manifest + safe-overwrite + backup folder

**Estimativa:** 1.5 dia
**Status:** ⏳ pending

### Why
M12 inteiro pendente. Hoje `Compile()` em [SuiDesignerWindow.cs:365](Editor/SuiDesignerWindow.cs#L365) só roda pipeline em memória e loga "M12 will write to disk". Sem M12, o ciclo "designer → projeto do usuário" está quebrado.

### Files

| Arquivo | Mudança |
|---|---|
| `Editor/SuiCompileWriter.cs` | **novo** — orquestra write + backup + manifest |
| `Code/Generation/SuiGenerationResult.cs` | adicionar `SkippedFiles`, `PreservedFiles`, `Conflicts`, `Obsolete` |
| `Editor/SuiDesignerWindow.cs` | `Compile()` chama `SuiCompileWriter.Run`. `ChangeOutputFolder()` abre folder picker. `OpenGeneratedFolder()` abre explorer no folder do output. |
| `Editor/Widgets/SuiCompileResultsWidget.cs` | render seções Generated / Skipped / Preserved / Conflicts / Obsolete |
| `Code/Runtime/SuiOutputSettings.cs` | confirmar campos: RootFolder, Namespace, ClassName, GenerateRazor, GenerateScss |

### Sub-passos

1. **Folder picker** — primeiro precisa de uma forma de o user escolher `OutputSettings.RootFolder`. API candidata: `Editor.FileDialog.SelectFolder()` ou similar.
   - Confirmar API via `gh search code "FileDialog" --repo Facepunch/sbox-public` antes de codar.
   - Se inexistente, fallback: textbox manual no Details (canvas settings) — não ideal mas funciona.
2. **`SuiCompileWriter.Run(SuiGenerationResult, outputFolder, manifestPath)`**:
   - Carrega manifest existente do disco (se houver), se inválido começa do zero.
   - Pra cada arquivo do result:
     - Calcula path absoluto = `outputFolder / file.RelativePath`
     - Se arquivo não existe → grava direto (Generated)
     - Se existe → lê + parseia header via `SuiHeaderEmitter.Parse`
       - Sem header / header de outro doc → ❌ Conflict (não toca, registra erro)
       - Header nosso, hash igual → Skipped (no-op)
       - Header nosso, hash diff → Backup + overwrite (Preserved)
   - Backup path: `<outputFolder>/sui-generated-backups/<DocumentName>/<UTC-timestamp>/<relativePath>`
   - Atualiza manifest com sha256 + version + timestamp por arquivo.
   - Detecta Obsolete: arquivos no manifest velho que não estão no result novo.
3. **Compile Results UX** — adicionar 4 seções colapsáveis (Generated, Skipped, Preserved/Backed-up, Conflicts, Obsolete) com count por seção e lista de paths clicáveis.
4. **Concurrent guard** — flag `_compileRunning` no Window, button disabled durante run.
5. **First compile UX** — se `OutputSettings.RootFolder` vazio, mostra toast "Choose output folder" e abre folder picker automaticamente.

### Acceptance criteria

- [ ] First compile sem folder configurado: prompt → folder picker → folder salvo no doc → compile roda
- [ ] Folder novo: todos arquivos gravados como Generated
- [ ] Recompile sem mudanças: todos Skipped
- [ ] Recompile com mudança: backup criado em `sui-generated-backups/<doc>/<timestamp>/`, original sobrescrito
- [ ] Conflito (arquivo sem header / outro doc): erro mostrado, arquivo NÃO tocado
- [ ] Manifest é gravado/lido corretamente, formato JSON
- [ ] Compile button disabled durante run

### Riscos / blockers

- 🚧 **Folder picker API desconhecida** — pode pausar pra eu pesquisar. Fallback se necessário: manual path entry. **Confirmar antes de começar.**
- 🚧 **Path manipulation cross-platform** — usar `System.IO.Path.Combine` + normalizar separators consistentemente.
- 🚧 **Manifest corruption recovery** — se JSON quebra, começa do zero (logged warning), não trava.

---

## Task 2 — Wire dos no-ops dos menus

**Estimativa:** 0.5 dia
**Status:** ⏳ pending
**Dependencies:** nenhuma

### Why
PRD doc 06 prescreve menus completos. Hoje várias opções são `() => { }` em [SuiDesignerWindow.cs:213-235](Editor/SuiDesignerWindow.cs#L213). Discrepância silenciosa que confunde user.

### Files

| Arquivo | Mudança |
|---|---|
| `Editor/SuiDesignerWindow.cs` | wire menus Edit/View/Tools |
| `Editor/SuiDesignerController.cs` | adicionar `Cut/Copy/Paste` methods + `_clipboard` static field |
| `Editor/Commands/SuiPasteElementCommand.cs` | **novo** — paste subtree com IDs novos |

### Sub-passos

1. **Edit menu**:
   - `Cut`: copy → delete (chama Copy + DeleteElement existentes)
   - `Copy`: serializa subtree do `Selected` em string JSON, guarda em `_clipboard` static
   - `Paste`: deserializa, regenera IDs, executa `SuiPasteElementCommand` com parent = Selected (se container) ou Selected.ParentId
   - `Duplicate` / `Delete` / `Rename`: já existem via shortcut, só wire menu actions pros mesmos handlers
2. **View menu**:
   - `Zoom In`: `_canvas.GetViewport().SetZoom(zoom * 1.1)`
   - `Zoom Out`: `_canvas.GetViewport().SetZoom(zoom / 1.1)`
   - `Fit to Screen`: `_canvas.GetViewport().FitCanvas()`
   - Expor `Viewport` getter público no `SuiCanvasWidget`
3. **Tools menu**:
   - `Regenerate Preview`: deleta `<projectRoot>/Code/_sui_preview/<className>/`, fecha modal preview se aberto
   - `Clean Preview Cache`: deleta `<projectRoot>/Code/_sui_preview/` inteiro
   - `Validate Document`: já existe wire OK
4. **Atalhos novos**: `Ctrl+C`, `Ctrl+V`, `Ctrl+X` (Copy/Paste/Cut), `Ctrl+0` (Fit to Screen), `Ctrl+B` (Compile)

### Acceptance criteria

- [ ] Edit > Copy + Edit > Paste: duplica subtree no parent atual com IDs novos
- [ ] Edit > Cut + Edit > Paste: move subtree pra novo parent
- [ ] View > Zoom In/Out funcional, persistido via `Settings.CanvasZoom`
- [ ] View > Fit to Screen centraliza+escala canvas inteiro na widget
- [ ] Tools > Clean Preview Cache: pasta deletada, console log
- [ ] Atalhos Ctrl+C/V/X funcionam (anywhere no window)

### Riscos

- ✅ Baixo risco — todos os primitives já existem.

---

## Task 3 — Banner de erro de compile sobre canvas/preview

**Estimativa:** 0.5 dia
**Status:** ⏳ pending
**Dependencies:** Task 1 (pra capturar erros do compile)

### Why
PRD doc 09 § Hotload considerations: "never blank canvas on typo, keep last-known-good frame and overlay banner". Hoje erros silenciam ou só aparecem no Console.

### Files

| Arquivo | Mudança |
|---|---|
| `Editor/Canvas/SuiCanvasViewport.cs` | adicionar `ErrorBanner` property + paint overlay |
| `Editor/SuiDesignerWindow.cs` | propagar erros do Compile pro viewport |
| `Editor/SuiPreviewWindow.cs` | mesma coisa pro preview modal |

### Sub-passos

1. Property `ErrorBanner` no viewport: `string Title; string Detail; Action OnDismiss;`
2. Paint pass adicional após chrome: rect colorido (vermelho 80% alpha) com ícone + título + detail truncado, no topo do viewport
3. Click no banner abre Compile Results dock; X fecha
4. `SuiDesignerWindow.Compile()`: se result.Errors.Count > 0, set `_canvas.ErrorBanner = new(...)`. Se sucesso, clear banner.
5. `SuiPreviewWindow`: mesma coisa, banner sobre o preview render

### Acceptance criteria

- [ ] Compile com erro de schema → banner aparece no canvas
- [ ] Compile success → banner some
- [ ] Click no banner abre Compile Results e expande seção Errors
- [ ] X no banner fecha (mas erro persiste no Compile Results)
- [ ] Preview modal: erro de C# build mostra banner no preview

### Riscos

- 🚧 Captura de erro de C# build no preview cache: precisa interceptar `Compiler.Build` callbacks. Confirmar ponto de hook.

---

## Task 4 — Drag-drop do Palette → canvas

**Estimativa:** 1 dia
**Status:** ⏳ pending
**Dependencies:** nenhuma

### Why
PRD doc 06 § 4 e doc 03 Flow 2 prescrevem drag. Hoje só click-to-add (sempre adiciona em Root).

### Files

| Arquivo | Mudança |
|---|---|
| `Editor/Widgets/SuiPaletteWidget.cs` | drag start nos buttons |
| `Editor/Widgets/SuiCanvasWidget.cs` | accept drop, adiciona elemento na posição correta |
| `Editor/Canvas/SuiCanvasViewport.cs` | visual feedback durante drag (drop indicator) |

### Sub-passos

1. **Palette OnDragStart**: cada palette button override `OnDragStart` retornando true, criando `Drag` com `Data.Object = SuiElementType` (enum value).
2. **Canvas accept drops**: `_viewport.AcceptDrops = true`. Override `OnDragEnter` checa se payload é `SuiElementType` aceita.
3. **Visual feedback**:
   - Durante drag: paint outline azul tracejado sobre o container hit-tested no cursor
   - Cursor logical → solver → element under cursor (deepest container, fallback root)
4. **OnDrop**:
   - Hit-test pra container parent: se cursor sobre elemento container → child desse; senão → child de Root
   - `controller.AddElement(type, parentResolved)`
   - Set `Layout.X` / `Y` derivado da posição do drop relativa ao parent (uses `RectToLayoutValues` se anchor TopLeft)
5. **Existing click-to-add stays** — não remove, só complementa.

### Acceptance criteria

- [ ] Drag de palette button mostra ghost cursor com nome do tipo
- [ ] Hover sobre Panel container durante drag → outline tracejado azul no Panel
- [ ] Drop sobre Panel: novo elemento é child do Panel
- [ ] Drop em área vazia: novo elemento é child do Root
- [ ] Posição do elemento corresponde ao ponto de drop (logical coords convertidos)
- [ ] Click-to-add continua funcionando como antes

### Riscos

- 🚧 Editor `Drag` API + accept drops: confirmar via `gh search code` no sbox-public. Modelo: `SuiHierarchyWidget` já usa `Drag` pra reparent, então API confirmadamente existe.
- 🚧 Hit-test durante drag pode ser pesado se canvas tem muitos elementos — mas solver é cheap.

---

# BATCH 1 — Definition of done

Para encerrar Batch 1 e voltar pra você validar:

1. ✅ Todos os 4 tasks compilando verde (`get_compile_errors` zero)
2. ✅ STATUS.md atualizado refletindo M12 ✅, M4 menus ✅
3. ✅ ISSUES.md sem novas entradas (issues conhecidos podem permanecer)
4. ✅ Source ↔ runtime mirror sincronizados (zero diff fora de obj/Properties/csproj)
5. ✅ Self-tests do M9 ainda passando
6. ✅ Memória atualizada com lições aprendidas (se houver)

**Você valida**: testa fluxo end-to-end (cria UI no canvas → Compile → arquivos no projeto-alvo → Preview), bate em casos edge (cancel folder picker, conflict overwrite), reporta bugs.

---

# BATCH 2 — Após validação Batch 1

## Task 5 — M13 polish + 4 sample .sui files

**Estimativa:** 0.5 dia
- Improved error messages com contexto (linha, elemento, sugestão)
- 4 samples gerados in-code: `simple_panel.sui`, `inventory_basic.sui`, `hotbar_basic.sui`, `hud_survival.sui`
- README usage instructions atualizado

## Task 6 — Phase 4-5 do canvas redesign

**Estimativa:** 1.5 dia
- **Phase 4 toolbar inline**: zoom dropdown, resolution preset, snap toggle, fit-canvas button
- **Phase 5 design aids**: alignment guides (snap a sibling/parent edges), rulers + grid overlay, distance labels durante selection
- Schema: adicionar `SuiCanvasSettings.PreviewWidth/PreviewHeight` pros presets de resolução

---

# BATCH 3 — ISSUEs em paralelo

| ISSUE | Effort |
|---|---|
| ISSUE-003 (color picker custom) | 1.5 dia |
| ISSUE-002 (text auto-size) | 1 dia |
| ISSUE-001 (SV box stale) | resolvido pelo ISSUE-003 |

Podem ser feitos em paralelo (3 agentes) ou sequenciados.

---

# Riscos transversais que podem pausar a run

| Risco | Mitigação |
|---|---|
| API editor desconhecida (FileDialog, Drag, AcceptDrops em ScrollArea) | Pesquisar via `gh search code --repo Facepunch/sbox-public` ANTES de cada task. Se API não confirmada, pausa e pergunta. |
| Build break por API errada | Sync runtime mirror + `get_compile_errors` após cada arquivo significativo. Se quebrar, fix imediato antes de avançar. |
| `string.AsSpan` ou outros sandbox-blocked APIs | Memória já registra essa armadilha. |
| Crash do editor durante teste | Reabrir, ler console, fix root cause antes de retomar. Não fazer "tente de novo". |
| Arquivos removidos / renomeados que quebram referências cruzadas | Glob/Grep antes de remover qualquer coisa. |

---

# Tracking durante a run

Cada task começa com:
1. Read PRD/doc relevante (1 minuto)
2. Confirm API se houver dúvida
3. Implementar
4. Sync runtime mirror
5. Compile check
6. Update status no plan (✅ ou 🚧 com nota)

Cada task termina com:
- Update STATUS.md com nota de done
- Commit suggestion (mensagem de commit pronta)

User pode interromper a qualquer momento — task em progresso fica 🔄 com last-known-state documentado.

---

# Estimativa total

| Batch | Tempo |
|---|---|
| Batch 1 | 3.5 dias |
| Batch 2 | 2 dias |
| Batch 3 | 2.5 dias (paralelizável pra ~1.5) |
| **Total** | **8 dias** |

Realista. Se algum task estourar 50%+, paro e re-planejo.
