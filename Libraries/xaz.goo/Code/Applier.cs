using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Sandbox;
using Sandbox.Rendering;
using Sandbox.UI;
using Goo.Internal;
using EnginePanelTransform = Sandbox.UI.PanelTransform;

namespace Goo;

internal static class Applier
{
    // Fast path takes the concrete List<Op> via CollectionsMarshal.AsSpan so the JIT
    // emits an indexed-span loop, avoiding the boxed IEnumerator<Op> and per-element
    // 64B copy that an interface-typed foreach over a value-type Op would incur.
    public static void Apply(Panel root, IReadOnlyList<Op> ops)
    {
        if (ops is List<Op> list)
        {
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < span.Length; i++)
                ApplyOne(root, in span[i]);
            return;
        }

        foreach (var op in ops)
            ApplyOne(root, in op);
    }

    static void ApplyOne(Panel root, in Op op)
    {
        var host = WalkPath(root, op.HostPath);
        switch (op.Kind)
        {
            case OpKind.CreateText:
                {
                    var label = host.AddChild(new StatefulLabel { Text = op.StringPayload! });
                    host.SetChildIndex(label, op.Index);
                    break;
                }
            case OpKind.UpdateText:
                {
                    if (host.GetChild(op.Index, false) is Label label)
                        label.Text = op.StringPayload!;
                    break;
                }
            case OpKind.CreateContainer:
                {
                    var panel = host.AddChild(new StatefulDrawPanel());
                    // Default FlexDirection; SetStyle overrides if the StyleList carries one.
                    panel.Style.FlexDirection = FlexDirection.Row;
                    host.SetChildIndex(panel, op.Index);
                    break;
                }
            case OpKind.CreateImage:
                {
                    var img = host.AddChild(new StatefulImage());
                    if (op.StringPayload is not null)
                        img.SetTexture(op.StringPayload);
                    else if (op.Texture is not null)
                        img.Texture = op.Texture;
                    host.SetChildIndex(img, op.Index);
                    break;
                }
            case OpKind.UpdateImage:
                {
                    if (host.GetChild(op.Index, false) is Sandbox.UI.Image img)
                    {
                        if (op.StringPayload is not null)
                            img.SetTexture(op.StringPayload);
                        else if (op.Texture is not null)
                            img.Texture = op.Texture;
                    }
                    break;
                }
            case OpKind.CreateScenePanel:
                {
                    // ScenePath ctor path triggers engine's RenderScene.Load(SceneLoadOptions) and engine owns the scene.
                    // Scene-ref path uses the parameterless ctor and assigns RenderScene, which flips _ownsScene = false.
                    var scenePanel = op.StringPayload is not null
                        ? new StatefulScenePanel(op.StringPayload)
                        : new StatefulScenePanel();
                    if (op.StringPayload is null && op.Scene is not null)
                        scenePanel.RenderScene = op.Scene;
                    scenePanel.RenderOnce = op.RenderOnce;
                    host.AddChild(scenePanel);
                    host.SetChildIndex(scenePanel, op.Index);
                    break;
                }
            case OpKind.UpdateScenePanel:
                {
                    if (host.GetChild(op.Index, false) is Sandbox.UI.ScenePanel scenePanel)
                    {
                        // ScenePath hot-swap: re-load via SceneLoadOptions on the existing RenderScene.
                        // Scene-ref hot-swap: assign RenderScene (disposes prior owned scene if any).
                        if (op.StringPayload is not null)
                        {
                            var options = new SceneLoadOptions { ShowLoadingScreen = false };
                            if (options.SetScene(op.StringPayload))
                                scenePanel.RenderScene.Load(options);
                        }
                        else if (op.Scene is not null)
                        {
                            scenePanel.RenderScene = op.Scene;
                        }
                        scenePanel.RenderOnce = op.RenderOnce;
                    }
                    break;
                }
            case OpKind.CreateSvgPanel:
                {
                    var svg = new StatefulSvgPanel();
                    if (op.StringPayload is not null)
                        svg.Src = op.StringPayload;
                    if (op.Color is not null)
                        svg.Color = op.Color;
                    host.AddChild(svg);
                    host.SetChildIndex(svg, op.Index);
                    break;
                }
            case OpKind.UpdateSvgPanel:
                {
                    if (host.GetChild(op.Index, false) is Sandbox.UI.SvgPanel svg)
                    {
                        svg.Src = op.StringPayload;
                        svg.Color = op.Color;
                    }
                    break;
                }
            case OpKind.CreateSector:
                {
                    var p = new StatefulShapePanel();
                    // Shapes with no explicit size collapse to 0 under the engine's
                    // default Yoga flex layout. Fill the parent unless the user
                    // declared a Width/Height (SetStyle path preserves this default).
                    p.Style.Width  = Length.Percent(100);
                    p.Style.Height = Length.Percent(100);
                    host.AddChild(p);
                    p.ApplyShape(BlobKind.Sector, in op.Shape);
                    host.SetChildIndex(p, op.Index);
                    break;
                }
            case OpKind.UpdateSector:
                {
                    if (host.GetChild(op.Index, false) is StatefulShapePanel p)
                        p.ApplyShape(BlobKind.Sector, in op.Shape);
                    break;
                }
            case OpKind.CreateArc:
                {
                    var p = new StatefulShapePanel();
                    p.Style.Width  = Length.Percent(100);
                    p.Style.Height = Length.Percent(100);
                    host.AddChild(p);
                    p.ApplyShape(BlobKind.Arc, in op.Shape);
                    host.SetChildIndex(p, op.Index);
                    break;
                }
            case OpKind.UpdateArc:
                {
                    if (host.GetChild(op.Index, false) is StatefulShapePanel p)
                        p.ApplyShape(BlobKind.Arc, in op.Shape);
                    break;
                }
            case OpKind.CreatePolygon:
                {
                    var p = new StatefulShapePanel();
                    p.Style.Width  = Length.Percent(100);
                    p.Style.Height = Length.Percent(100);
                    host.AddChild(p);
                    if (op.Points is not null && op.Points.Length >= 3)
                        p.ApplyPolygon(op.Points);
                    host.SetChildIndex(p, op.Index);
                    break;
                }
            case OpKind.UpdatePolygon:
                {
                    if (host.GetChild(op.Index, false) is StatefulShapePanel p)
                    {
                        if (op.Points is not null && op.Points.Length >= 3)
                            p.ApplyPolygon(op.Points);
                        else
                            p.Style.BackgroundImage = null;
                    }
                    break;
                }
            case OpKind.CreateWebPanel:
                {
                    var wp = new StatefulWebPanel();
                    // WebPanel needs PointerEvents to receive mouse/wheel/key events; default to All so a bare one is interactive (preserved unless the user overrides).
                    wp.Style.PointerEvents = PointerEvents.All;
                    // Engine writes the webview texture to BackgroundImage without a size; force 100% so it fills the panel instead of centering with black bars.
                    wp.Style.BackgroundSizeX = Length.Percent(100);
                    wp.Style.BackgroundSizeY = Length.Percent(100);
                    if (op.StringPayload is not null)
                        wp.Url = op.StringPayload;
                    if (op.Paused)
                        wp.Surface.InBackgroundMode = true;
                    host.AddChild(wp);
                    host.SetChildIndex(wp, op.Index);
                    break;
                }
            case OpKind.UpdateWebPanel:
                {
                    if (host.GetChild(op.Index, false) is Sandbox.UI.WebPanel wp)
                    {
                        wp.Url = op.StringPayload;
                        wp.Surface.InBackgroundMode = op.Paused;
                    }
                    break;
                }
            case OpKind.CreateTextEntry:
                {
                    var te = new StatefulTextEntry();
                    // TextEntry needs PointerEvents to receive focus on click; default to All so a bare one is interactive (preserved unless the user overrides).
                    te.Style.PointerEvents = PointerEvents.All;
                    // Create-time uses the Text setter (safe: not focused yet, so the Value-setter HasFocus guard is irrelevant; the Update arm uses Value for controlled mode).
                    if (op.StringPayload is not null)
                        te.Text = op.StringPayload;
                    if (op.Placeholder is not null)
                        te.Placeholder = op.Placeholder;
                    if (op.MaxLength.HasValue)
                        te.MaxLength = op.MaxLength.Value;
                    te.Disabled = op.Disabled;
                    te.Numeric = op.Numeric;
                    te.MinLength = op.MinLength;
                    te.CharacterRegex = op.CharacterRegex;
                    te.StringRegex = op.StringRegex;
                    if (op.MinValue.HasValue)
                        te.MinValue = op.MinValue.Value;
                    if (op.MaxValue.HasValue)
                        te.MaxValue = op.MaxValue.Value;
                    if (op.NumberFormat is not null)
                        te.NumberFormat = op.NumberFormat;
                    te.Multiline = op.Multiline;
                    te._onChange = op.OnChange;
                    te._onSubmit = op.OnSubmit;
                    te._onFocus = op.OnFocus;
                    te._onBlur = op.OnBlur;
                    te._onCancel = op.OnCancel;
                    te._canEnterChar = op.CanEnterChar;
                    te._validate = op.Validate;
                    te._onValidationChanged = op.OnValidationChanged;
                    // OnChange/OnSubmit live outside BlobEvents, so a text-only entry (no mouse
                    // handlers) never gets a SetEvents op. Wire RequestRebuild here too so typing
                    // auto-rebuilds. Same policy the SetEvents arm reads from BuildContext.
                    var teCtx = BuildContext._current;
                    te.RequestRebuild = (teCtx != null && teCtx.AutoRebuildOnEvents) ? teCtx.RootRebuild : null;
                    // Compute initial validity now so the 'invalid' class / callback reflect the starting text.
                    te.RecomputeValidation();
                    host.AddChild(te);
                    host.SetChildIndex(te, op.Index);
                    break;
                }
            case OpKind.UpdateTextEntry:
                {
                    if (host.GetChild(op.Index, false) is Sandbox.UI.TextEntry te)
                    {
                        var ste = te as Goo.Internal.StatefulTextEntry;

                        te.Placeholder = op.Placeholder;
                        te.MaxLength = op.MaxLength;
                        te.Disabled = op.Disabled;
                        te.Numeric = op.Numeric;
                        te.MinLength = op.MinLength;
                        te.CharacterRegex = op.CharacterRegex;
                        te.StringRegex = op.StringRegex;
                        te.MinValue = op.MinValue;
                        te.MaxValue = op.MaxValue;
                        te.NumberFormat = op.NumberFormat;
                        te.Multiline = op.Multiline;

                        if (ste != null)
                        {
                            ste._onChange = op.OnChange;
                            ste._onSubmit = op.OnSubmit;
                            ste._onFocus = op.OnFocus;
                            ste._onBlur = op.OnBlur;
                            ste._onCancel = op.OnCancel;
                            ste._canEnterChar = op.CanEnterChar;
                            ste._validate = op.Validate;
                            ste._onValidationChanged = op.OnValidationChanged;
                            // See CreateTextEntry: keep RequestRebuild wired in case this entry
                            // never receives a SetEvents op (no mouse handlers in BlobEvents).
                            var steCtx = BuildContext._current;
                            ste.RequestRebuild = (steCtx != null && steCtx.AutoRebuildOnEvents) ? steCtx.RootRebuild : null;
                        }

                        // Controlled mode re-emits Value every render (the engine's HasFocus guard
                        // no-ops mid-typing). Done AFTER all validation config is applied so the
                        // OnValueChanged it triggers validates against the current rules and predicate;
                        // uncontrolled mode never touches text (engine owns it after InitialText).
                        if (op.IsControlled)
                            te.Value = op.StringPayload ?? "";

                        // Recompute for the config-only-change case where no controlled Value set
                        // fired OnValueChanged; flip-gated, so a redundant call is a harmless no-op.
                        ste?.RecomputeValidation();
                    }
                    break;
                }
            case OpKind.RemoveAt:
                {
                    host.GetChild(op.Index, false)?.Delete(true);
                    break;
                }
            case OpKind.MoveAt:
                {
                    var child = host.GetChild(op.Index, false);
                    if (child is not null)
                        host.SetChildIndex(child, op.ToIndex);
                    break;
                }
            case OpKind.SetStyle:
                {
                    ApplyAllDeclaredFields(host, host.Style, op.Style!);
                    break;
                }
            case OpKind.SetEvents:
                {
                    // HostPath for SetEvents resolves directly to the target panel
                    // (not its parent), so `host` here is the target.
                    if (host is IStatefulEventHost evHost)
                    {
                        evHost.ApplyEvents(op.Events);
                        // Auto-rebuild-after-dispatch: wire the panel's rebuild request from the
                        // owning GooPanel, unless it opted out. BuildContext._current is the
                        // owning panel's context for the whole Apply pass.
                        var ctx = BuildContext._current;
                        evHost.RequestRebuild = (ctx != null && ctx.AutoRebuildOnEvents) ? ctx.RootRebuild : null;
                        // Apply-or-clear: a one-way All write would leave a panel that lost its
                        // handlers hittable forever, so the engine never hands the mouse back to the
                        // game (Panel.WantsMouseInput scans for All). Clear to None when handlers go.
                        if (!evHost.UserSetPointerEvents)
                        {
                            if (op.Events.AnyNonNull)
                                host.Style.PointerEvents = PointerEvents.All;
                            else if (host is not Sandbox.UI.WebPanel and not Sandbox.UI.TextEntry
                                && (host is not IStatefulHost sh || !sh.HasActiveStateVariants))
                                host.Style.PointerEvents = PointerEvents.None;
                        }
                    }
                    break;
                }
            case OpKind.SetDraw:
                {
                    if (host is StatefulDrawPanel p)
                        p._draw = op.DrawCallback;
                    break;
                }
            case OpKind.SetEffect:
                {
                    if (host is StatefulDrawPanel p)
                    {
                        p._effect = op.Effect;
                        // Create the Material here on the main thread; Draw (render thread) cannot.
                        op.Effect?.Warm();
                    }
                    break;
                }
            case OpKind.SetLayoutTransition:
                {
                    if (host is StatefulDrawPanel p)
                        p.SetLayoutTransition(op.LayoutTransition);
                    break;
                }
        }
    }

    static void ApplyAllDeclaredFields(Panel host, PanelStyle target, StyleList style)
    {
        // Track whether the user declared PointerEvents in their style; the SetEvents handler
        // uses this flag to decide whether to auto-gate to PointerEvents.All when any handler is set.
        if (host is IStatefulEventHost evHost)
            evHost.UserSetPointerEvents = style.TryGet(StyleField.PointerEvents, out _);


        Length? padding       = TryLen(style, StyleField.Padding);
        Length? paddingLeft   = TryLen(style, StyleField.PaddingLeft)   ?? padding;
        Length? paddingTop    = TryLen(style, StyleField.PaddingTop)    ?? padding;
        Length? paddingRight  = TryLen(style, StyleField.PaddingRight)  ?? padding;
        Length? paddingBottom = TryLen(style, StyleField.PaddingBottom) ?? padding;

        Length? margin       = TryLen(style, StyleField.Margin);
        Length? marginLeft   = TryLen(style, StyleField.MarginLeft)   ?? margin;
        Length? marginTop    = TryLen(style, StyleField.MarginTop)    ?? margin;
        Length? marginRight  = TryLen(style, StyleField.MarginRight)  ?? margin;
        Length? marginBottom = TryLen(style, StyleField.MarginBottom) ?? margin;

        Length? gap       = TryLen(style, StyleField.Gap);
        Length? rowGap    = TryLen(style, StyleField.RowGap)    ?? gap;
        Length? columnGap = TryLen(style, StyleField.ColumnGap) ?? gap;

        Length? borderRadius            = TryLen(style, StyleField.BorderRadius);
        Length? borderTopLeftRadius     = TryLen(style, StyleField.BorderTopLeftRadius)     ?? borderRadius;
        Length? borderTopRightRadius    = TryLen(style, StyleField.BorderTopRightRadius)    ?? borderRadius;
        Length? borderBottomRightRadius = TryLen(style, StyleField.BorderBottomRightRadius) ?? borderRadius;
        Length? borderBottomLeftRadius  = TryLen(style, StyleField.BorderBottomLeftRadius)  ?? borderRadius;

        // BorderColor fan-out: shorthand default applied to per-edge reads.
        Color? borderColor       = TryColor(style, StyleField.BorderColor);
        Color? borderLeftColor   = TryColor(style, StyleField.BorderLeftColor)   ?? borderColor;
        Color? borderTopColor    = TryColor(style, StyleField.BorderTopColor)    ?? borderColor;
        Color? borderRightColor  = TryColor(style, StyleField.BorderRightColor)  ?? borderColor;
        Color? borderBottomColor = TryColor(style, StyleField.BorderBottomColor) ?? borderColor;

        // BorderWidth fan-out: shorthand default applied to per-edge reads.
        Length? borderWidth       = TryLen(style, StyleField.BorderWidth);
        Length? borderLeftWidth   = TryLen(style, StyleField.BorderLeftWidth)   ?? borderWidth;
        Length? borderTopWidth    = TryLen(style, StyleField.BorderTopWidth)    ?? borderWidth;
        Length? borderRightWidth  = TryLen(style, StyleField.BorderRightWidth)  ?? borderWidth;
        Length? borderBottomWidth = TryLen(style, StyleField.BorderBottomWidth) ?? borderWidth;

        // FlexDirection is non-nullable on PanelStyle; default to Row when undeclared.
        target.FlexDirection   = TryFlex(style, StyleField.FlexDirection) ?? FlexDirection.Row;
        target.JustifyContent  = TryJustify(style, StyleField.JustifyContent);
        target.AlignItems      = TryAlign(style, StyleField.AlignItems);
        target.Display         = TryDisplay(style, StyleField.Display);
        // Shape panels need to fill their parent by default so they don't collapse
        // to 0 under flex layout; explicit user Width/Height still wins.
        target.Width           = TryLen(style, StyleField.Width)
                              ?? (host is Goo.Internal.StatefulShapePanel ? Length.Percent(100) : (Length?)null);
        target.Height          = TryLen(style, StyleField.Height)
                              ?? (host is Goo.Internal.StatefulShapePanel ? Length.Percent(100) : (Length?)null);
        // On shape panels BackgroundColor is redirected to BackgroundTint (multiplies into the alpha mask) rather than written directly, which would render a solid rect behind the silhouette. Handled with BackgroundTint below.
        bool _isShape     = host is Goo.Internal.StatefulShapePanel;
        bool _isWebPanel  = host is Goo.Internal.StatefulWebPanel;
        bool _isTextEntry = host is Goo.Internal.StatefulTextEntry;
        bool _isImage     = host is Goo.Internal.StatefulImage;

        // State variants (hover/active/focus). Read up here because the inline base
        // colors depend on them: the engine cascade merges inline styles after all
        // stylesheet rules, so a channel with a variant must move its declared base
        // into the variant sheet and leave the inline slot null, or the variant
        // never visibly flips. The pointer-events policy below also needs them.
        Color? hoverBg  = TryColor(style, StyleField.HoverBackgroundColor);
        Color? activeBg = TryColor(style, StyleField.ActiveBackgroundColor);
        Color? focusBg  = TryColor(style, StyleField.FocusBackgroundColor);
        Color? hoverFg  = TryColor(style, StyleField.HoverFontColor);
        Color? activeFg = TryColor(style, StyleField.ActiveFontColor);
        Color? focusFg  = TryColor(style, StyleField.FocusFontColor);

        bool hasBgVariant = hoverBg.HasValue || activeBg.HasValue || focusBg.HasValue;
        bool hasFgVariant = hoverFg.HasValue || activeFg.HasValue || focusFg.HasValue;
        bool hasAnyVariant = hasBgVariant || hasFgVariant;

        StateController.SplitBaseColors(
            TryColor(style, StyleField.BackgroundColor),
            TryColor(style, StyleField.FontColor),
            hasBgVariant, hasFgVariant, _isShape,
            out var inlineBg, out var inlineFg,
            out var sheetBg, out var sheetFg);

        target.BackgroundColor = inlineBg;

        target.Padding = padding;
        target.PaddingLeft  = paddingLeft;     target.PaddingTop     = paddingTop;
        target.PaddingRight = paddingRight;    target.PaddingBottom  = paddingBottom;

        target.Margin = margin;
        target.MarginLeft  = marginLeft;       target.MarginTop      = marginTop;
        target.MarginRight = marginRight;      target.MarginBottom   = marginBottom;

        target.RowGap = rowGap;                target.ColumnGap      = columnGap;

        target.BorderTopLeftRadius     = borderTopLeftRadius;
        target.BorderTopRightRadius    = borderTopRightRadius;
        target.BorderBottomRightRadius = borderBottomRightRadius;
        target.BorderBottomLeftRadius  = borderBottomLeftRadius;

        // Align properties.
        target.AlignContent = TryAlign(style, StyleField.AlignContent);
        target.AlignSelf    = TryAlign(style, StyleField.AlignSelf);

        // Aspect ratio.
        target.AspectRatio = TrySingle(style, StyleField.AspectRatio);

        // Backdrop filter properties.
        target.BackdropFilterBlur       = TryLen(style, StyleField.BackdropFilterBlur);
        target.BackdropFilterBrightness = TryLen(style, StyleField.BackdropFilterBrightness);
        target.BackdropFilterContrast   = TryLen(style, StyleField.BackdropFilterContrast);
        target.BackdropFilterHueRotate  = TryLen(style, StyleField.BackdropFilterHueRotate);
        target.BackdropFilterInvert     = TryLen(style, StyleField.BackdropFilterInvert);
        target.BackdropFilterSaturate   = TryLen(style, StyleField.BackdropFilterSaturate);
        target.BackdropFilterSepia      = TryLen(style, StyleField.BackdropFilterSepia);

        // Background properties.
        target.BackgroundAngle          = TryLen(style, StyleField.BackgroundAngle);
        target.BackgroundBlendMode      = TryString(style, StyleField.BackgroundBlendMode);
        // On shape panels, BackgroundImage/Size are owned by ApplyShape (the baked mask); skip them here so SetStyle does not clobber the mask. Other panels pass through (undeclared clears).
        if (!_isShape)
            target.BackgroundImage      = TryTexture(style, StyleField.BackgroundImage);
        target.BackgroundPlaybackPaused = TryBool(style, StyleField.BackgroundPlaybackPaused);
        target.BackgroundPositionX      = TryLen(style, StyleField.BackgroundPositionX);
        target.BackgroundPositionY      = TryLen(style, StyleField.BackgroundPositionY);
        // Image defaults to NoRepeat so ObjectFit.Contain's aspect-preserved empty bars
        // don't get tiled by the engine's Repeat default (engine-fact-objectfit-contain-tiles:
        // Panel.Draw.cs DrawBackgroundTexture defaults BackgroundRepeat to Repeat when null).
        // Explicit user value still wins. Mirrors the WebPanel BackgroundSize default below.
        target.BackgroundRepeat         = TryBackgroundRepeat(style, StyleField.BackgroundRepeat)
                                       ?? (_isImage ? BackgroundRepeat.NoRepeat : (BackgroundRepeat?)null);
        if (!_isShape)
        {
            // WebPanel needs BackgroundSize=100% so the engine-managed webview texture
            // fills the panel instead of being centered at native size with black bars
            // around it. Explicit user value still wins.
            target.BackgroundSizeX      = TryLen(style, StyleField.BackgroundSizeX)
                                       ?? (_isWebPanel ? Length.Percent(100) : (Length?)null);
            target.BackgroundSizeY      = TryLen(style, StyleField.BackgroundSizeY)
                                       ?? (_isWebPanel ? Length.Percent(100) : (Length?)null);
        }
        // BackgroundTint: on shape panels BackgroundColor multiplies into the alpha mask via this property (explicit BackgroundTint overrides); elsewhere only an explicit value is honored.
        if (_isShape)
            target.BackgroundTint       = TryColor(style, StyleField.BackgroundTint)
                                       ?? TryColor(style, StyleField.BackgroundColor);
        else
            target.BackgroundTint       = TryColor(style, StyleField.BackgroundTint);

        // Border color: shorthand first, per-edge override.
        target.BorderColor       = borderColor;
        target.BorderLeftColor   = borderLeftColor;   target.BorderTopColor    = borderTopColor;
        target.BorderRightColor  = borderRightColor;  target.BorderBottomColor = borderBottomColor;

        // Border image properties.
        target.BorderImageFill        = TryBorderImageFill(style, StyleField.BorderImageFill);
        target.BorderImageRepeat      = TryBorderImageRepeat(style, StyleField.BorderImageRepeat);
        target.BorderImageSource      = TryTexture(style, StyleField.BorderImageSource);
        target.BorderImageTint        = TryColor(style, StyleField.BorderImageTint);
        target.BorderImageWidthBottom = TryLen(style, StyleField.BorderImageWidthBottom);
        target.BorderImageWidthLeft   = TryLen(style, StyleField.BorderImageWidthLeft);
        target.BorderImageWidthRight  = TryLen(style, StyleField.BorderImageWidthRight);
        target.BorderImageWidthTop    = TryLen(style, StyleField.BorderImageWidthTop);

        // Border width: shorthand first, per-edge override.
        target.BorderWidth       = borderWidth;
        target.BorderLeftWidth   = borderLeftWidth;   target.BorderTopWidth    = borderTopWidth;
        target.BorderRightWidth  = borderRightWidth;  target.BorderBottomWidth = borderBottomWidth;

        // Position edges.
        target.Bottom = TryLen(style, StyleField.Bottom);
        target.Left   = TryLen(style, StyleField.Left);
        target.Right  = TryLen(style, StyleField.Right);
        target.Top    = TryLen(style, StyleField.Top);

        // Caret.
        target.CaretColor = TryColor(style, StyleField.CaretColor);

        // Cursor. TextEntry defaults to "text" so consumers don't have to opt in
        // to the I-beam; explicit user value wins.
        target.Cursor = TryString(style, StyleField.Cursor)
                     ?? (_isTextEntry ? "text" : null);

        // Filter properties.
        target.FilterBlur        = TryLen(style, StyleField.FilterBlur);
        target.FilterBorderColor = TryColor(style, StyleField.FilterBorderColor);
        target.FilterBorderWidth = TryLen(style, StyleField.FilterBorderWidth);
        target.FilterBrightness  = TryLen(style, StyleField.FilterBrightness);
        target.FilterContrast    = TryLen(style, StyleField.FilterContrast);
        target.FilterHueRotate   = TryLen(style, StyleField.FilterHueRotate);
        target.FilterInvert      = TryLen(style, StyleField.FilterInvert);
        target.FilterSaturate    = TryLen(style, StyleField.FilterSaturate);
        target.FilterSepia       = TryLen(style, StyleField.FilterSepia);
        target.FilterTint        = TryColor(style, StyleField.FilterTint);

        // Flex properties.
        target.FlexBasis  = TryLen(style, StyleField.FlexBasis);
        target.FlexGrow   = TrySingle(style, StyleField.FlexGrow);
        target.FlexShrink = TrySingle(style, StyleField.FlexShrink);
        target.FlexWrap   = TryWrap(style, StyleField.FlexWrap);

        // Font properties.
        target.FontColor          = inlineFg;
        target.FontFamily         = TryString(style, StyleField.FontFamily);
        target.FontSize           = TryLen(style, StyleField.FontSize);
        target.FontSmooth         = TryFontSmooth(style, StyleField.FontSmooth);
        target.FontStyle          = TryFontStyle(style, StyleField.FontStyle);
        target.FontVariantNumeric = TryFontVariantNumeric(style, StyleField.FontVariantNumeric);
        target.FontWeight         = TryInt32(style, StyleField.FontWeight);

        // Image rendering.
        target.ImageRendering = TryImageRendering(style, StyleField.ImageRendering);

        // Letter and line spacing.
        target.LetterSpacing = TryLen(style, StyleField.LetterSpacing);
        target.LineHeight    = TryLen(style, StyleField.LineHeight);

        // Mask properties.
        target.MaskAngle     = TryLen(style, StyleField.MaskAngle);
        target.MaskImage     = TryTexture(style, StyleField.MaskImage);
        target.MaskMode      = TryMaskMode(style, StyleField.MaskMode);
        target.MaskPositionX = TryLen(style, StyleField.MaskPositionX);
        target.MaskPositionY = TryLen(style, StyleField.MaskPositionY);
        target.MaskRepeat    = TryBackgroundRepeat(style, StyleField.MaskRepeat);
        target.MaskScope     = TryMaskScope(style, StyleField.MaskScope);
        target.MaskSizeX     = TryLen(style, StyleField.MaskSizeX);
        target.MaskSizeY     = TryLen(style, StyleField.MaskSizeY);

        // Min/max size.
        target.MaxHeight = TryLen(style, StyleField.MaxHeight);
        target.MaxWidth  = TryLen(style, StyleField.MaxWidth);
        target.MinHeight = TryLen(style, StyleField.MinHeight);
        target.MinWidth  = TryLen(style, StyleField.MinWidth);

        // Mix blend mode.
        target.MixBlendMode = TryString(style, StyleField.MixBlendMode);

        // Object fit.
        target.ObjectFit = TryObjectFit(style, StyleField.ObjectFit);

        // Opacity.
        target.Opacity = TrySingle(style, StyleField.Opacity);

        // Order.
        target.Order = TryInt32(style, StyleField.Order);

        // Outline properties.
        target.OutlineColor  = TryColor(style, StyleField.OutlineColor);
        target.OutlineOffset = TryLen(style, StyleField.OutlineOffset);
        target.OutlineWidth  = TryLen(style, StyleField.OutlineWidth);

        // Overflow properties.
        target.Overflow  = TryOverflowMode(style, StyleField.Overflow);
        target.OverflowX = TryOverflowMode(style, StyleField.OverflowX);
        target.OverflowY = TryOverflowMode(style, StyleField.OverflowY);

        // Perspective origin.
        target.PerspectiveOriginX = TryLen(style, StyleField.PerspectiveOriginX);
        target.PerspectiveOriginY = TryLen(style, StyleField.PerspectiveOriginY);

        // Pointer events: WebPanel and TextEntry default to All (intrinsically interactive); also gate to All when handlers exist so a style-only rebuild does not clobber the events-driven gate. A handler-less panel with a state variant stays capturing (unset); a fully inert panel resolves to None so it is pointer-transparent. Explicit value wins.
        bool hasHandlers = host is IStatefulEventHost peHost && peHost.HasEventHandlers;
        target.PointerEvents = PointerEventsPolicy.Resolve(
            TryPointerEvents(style, StyleField.PointerEvents), _isWebPanel, hasHandlers, hasStateVariant: hasAnyVariant, isTextEntry: _isTextEntry);
        target.Position      = TryPositionMode(style, StyleField.Position);

        // Sound.
        target.SoundIn  = TryString(style, StyleField.SoundIn);
        target.SoundOut = TryString(style, StyleField.SoundOut);

        // Text properties.
        target.TextAlign               = TryTextAlign(style, StyleField.TextAlign);
        target.TextBackgroundAngle     = TryLen(style, StyleField.TextBackgroundAngle);
        target.TextDecorationColor     = TryColor(style, StyleField.TextDecorationColor);
        target.TextDecorationLine      = TryTextDecoration(style, StyleField.TextDecorationLine);
        target.TextDecorationSkipInk   = TryTextSkipInk(style, StyleField.TextDecorationSkipInk);
        target.TextDecorationStyle     = TryTextDecorationStyle(style, StyleField.TextDecorationStyle);
        target.TextDecorationThickness = TryLen(style, StyleField.TextDecorationThickness);
        target.TextFilter              = TryFilterMode(style, StyleField.TextFilter);
        target.TextLineThroughOffset   = TryLen(style, StyleField.TextLineThroughOffset);
        target.TextOverflow            = TryTextOverflow(style, StyleField.TextOverflow);
        target.TextOverlineOffset      = TryLen(style, StyleField.TextOverlineOffset);
        target.TextStrokeColor         = TryColor(style, StyleField.TextStrokeColor);
        target.TextStrokeWidth         = TryLen(style, StyleField.TextStrokeWidth);
        target.TextTransform           = TryTextTransform(style, StyleField.TextTransform);
        target.TextUnderlineOffset     = TryLen(style, StyleField.TextUnderlineOffset);

        // Transform.
        target.Transform = TryPanelTransform(style, StyleField.Transform);

        // Transform origin.
        target.TransformOriginX = TryLen(style, StyleField.TransformOriginX);
        target.TransformOriginY = TryLen(style, StyleField.TransformOriginY);

        // White space and word properties.
        target.WhiteSpace  = TryWhiteSpace(style, StyleField.WhiteSpace);
        target.WordBreak   = TryWordBreak(style, StyleField.WordBreak);
        target.WordSpacing = TryLen(style, StyleField.WordSpacing);

        // Z-index, scaled so a declared value dominates sibling document order.
        target.ZIndex = ZIndexScaling.Scale(TryInt32(style, StyleField.ZIndex));

        int? transMs = TryInt32(style, StyleField.TransitionMs);

        if (host is IStatefulHost stateful)
        {
            if (hasAnyVariant)
                stateful.ApplyStateVariants(
                    baseBg: sheetBg,
                    baseFg: sheetFg,
                    hoverBg: hoverBg, activeBg: activeBg, focusBg: focusBg,
                    hoverFg: hoverFg, activeFg: activeFg, focusFg: focusFg,
                    transitionMs: transMs);
            else
                // Reused panels can carry a prior occupant's variant sheet; shed it
                // so its stale base/:hover background does not bleed through.
                stateful.ClearStateVariants();
        }

        // Disabled is the final word: state variants force PointerEvents.All for hover delivery, so this must run after them.
        bool disabled = TryBool(style, StyleField.Disabled) == true;
        if (disabled)
        {
            target.PointerEvents = PointerEvents.None;
            target.Opacity = Controls.ResolveDisabledOpacity(target.Opacity);
        }
    }

    // Each Try* ends with (T?)null not null: a bare null lets the ternary resolve through engine types' implicit string operator and NPE. See engine-fact memories.

    static Length? TryLen(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.Length ? v.LengthVal : (Length?)null;

    static Color? TryColor(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.Color ? v.ColorVal : (Color?)null;

    static FlexDirection? TryFlex(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.FlexDirection ? ((FlexDirection)v.EnumVal) : (FlexDirection?)null;

    static Justify? TryJustify(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.Justify ? ((Justify)v.EnumVal) : (Justify?)null;

    static Align? TryAlign(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.Align ? ((Align)v.EnumVal) : (Align?)null;

    static DisplayMode? TryDisplay(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.DisplayMode ? ((DisplayMode)v.EnumVal) : (DisplayMode?)null;

    static string? TryString(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.String ? (string?)v.RefVal : null;

    static float? TrySingle(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.Single ? System.BitConverter.Int32BitsToSingle(v.RefSlot) : (float?)null;

    static bool? TryBool(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.Boolean ? v.RefSlot != 0 : (bool?)null;

    static int? TryInt32(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.Int32 ? v.RefSlot : (int?)null;

    static Texture? TryTexture(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.Texture ? (Texture?)v.RefVal : null;

    static EnginePanelTransform? TryPanelTransform(StyleList s, StyleField f)
    {
        if (!s.TryGet(f, out var v)) return null;
        if (v.Kind != StyleValueKind.PanelTransform) return null;
        if (v.RefVal is not ImmutableList<EnginePanelTransform.Entry> list) return null;
        return new EnginePanelTransform { List = list };
    }

    static Wrap? TryWrap(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.Wrap ? ((Wrap)v.EnumVal) : (Wrap?)null;

    static BackgroundRepeat? TryBackgroundRepeat(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.BackgroundRepeat ? ((BackgroundRepeat)v.EnumVal) : (BackgroundRepeat?)null;

    static BorderImageFill? TryBorderImageFill(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.BorderImageFill ? ((BorderImageFill)v.EnumVal) : (BorderImageFill?)null;

    static BorderImageRepeat? TryBorderImageRepeat(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.BorderImageRepeat ? ((BorderImageRepeat)v.EnumVal) : (BorderImageRepeat?)null;

    static FontSmooth? TryFontSmooth(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.FontSmooth ? ((FontSmooth)v.EnumVal) : (FontSmooth?)null;

    static FontStyle? TryFontStyle(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.FontStyle ? ((FontStyle)v.EnumVal) : (FontStyle?)null;

    static FontVariantNumeric? TryFontVariantNumeric(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.FontVariantNumeric ? ((FontVariantNumeric)v.EnumVal) : (FontVariantNumeric?)null;

    static ImageRendering? TryImageRendering(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.ImageRendering ? ((ImageRendering)v.EnumVal) : (ImageRendering?)null;

    static MaskMode? TryMaskMode(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.MaskMode ? ((MaskMode)v.EnumVal) : (MaskMode?)null;

    static MaskScope? TryMaskScope(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.MaskScope ? ((MaskScope)v.EnumVal) : (MaskScope?)null;

    static ObjectFit? TryObjectFit(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.ObjectFit ? ((ObjectFit)v.EnumVal) : (ObjectFit?)null;

    static OverflowMode? TryOverflowMode(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.OverflowMode ? ((OverflowMode)v.EnumVal) : (OverflowMode?)null;

    static PointerEvents? TryPointerEvents(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.PointerEvents ? ((PointerEvents)v.EnumVal) : (PointerEvents?)null;

    static PositionMode? TryPositionMode(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.PositionMode ? ((PositionMode)v.EnumVal) : (PositionMode?)null;

    static TextAlign? TryTextAlign(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.TextAlign ? ((TextAlign)v.EnumVal) : (TextAlign?)null;

    static TextDecoration? TryTextDecoration(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.TextDecoration ? ((TextDecoration)v.EnumVal) : (TextDecoration?)null;

    static TextDecorationStyle? TryTextDecorationStyle(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.TextDecorationStyle ? ((TextDecorationStyle)v.EnumVal) : (TextDecorationStyle?)null;

    static TextSkipInk? TryTextSkipInk(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.TextSkipInk ? ((TextSkipInk)v.EnumVal) : (TextSkipInk?)null;

    static FilterMode? TryFilterMode(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.FilterMode ? ((FilterMode)v.EnumVal) : (FilterMode?)null;

    static TextOverflow? TryTextOverflow(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.TextOverflow ? ((TextOverflow)v.EnumVal) : (TextOverflow?)null;

    static TextTransform? TryTextTransform(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.TextTransform ? ((TextTransform)v.EnumVal) : (TextTransform?)null;

    static WhiteSpace? TryWhiteSpace(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.WhiteSpace ? ((WhiteSpace)v.EnumVal) : (WhiteSpace?)null;

    static WordBreak? TryWordBreak(StyleList s, StyleField f)
        => s.TryGet(f, out var v) && v.Kind == StyleValueKind.WordBreak ? ((WordBreak)v.EnumVal) : (WordBreak?)null;

    static Panel WalkPath(Panel root, int[] path)
    {
        var cur = root;
        foreach (var idx in path)
        {
            cur = cur.GetChild(idx, false);
            if (cur is null)
                throw new InvalidOperationException(
                    $"Applier.WalkPath: child index {idx} is null while traversing HostPath=[{string.Join(",", path)}].");
        }
        return cur;
    }
}
