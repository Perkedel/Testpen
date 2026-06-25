using System;
using System.Linq;
using Sandbox.UI;

namespace Goo.Internal;

internal sealed class StatefulTextEntry : Sandbox.UI.TextEntry, IStatefulEventHost
{
    internal Action<string>? _onChange;
    internal Action<string>? _onSubmit;
    internal Action? _onFocus;
    internal Action<string>? _onBlur;
    internal Action? _onCancel;

    internal Func<char, bool>?   _canEnterChar;
    internal Func<string, bool>? _validate;
    internal Action<bool>?       _onValidationChanged;
    bool _lastInvalid;

    internal Action<MousePanelEvent>? _onClick;
    internal Action<MousePanelEvent>? _onRightClick;
    internal Action<MousePanelEvent>? _onMiddleClick;
    internal Action<MousePanelEvent>? _onMouseEnter;
    internal Action<MousePanelEvent>? _onMouseLeave;
    internal Action<MousePanelEvent>? _onMouseDown;
    internal Action<MousePanelEvent>? _onMouseUp;
    internal Action<MousePanelEvent>? _onMouseMove;
    internal bool _userSetPointerEvents;
    internal Action? _requestRebuild;
    public Action? RequestRebuild { set => _requestRebuild = value; }

    public StatefulTextEntry()
    {
        // Wire the engine's OnTextEdited through _onChange for per-keystroke notifications.
        OnTextEdited = newValue => { _onChange?.Invoke(newValue); if (_onChange != null) _requestRebuild?.Invoke(); };
    }

    // AND-compose the Goo predicate after the engine's rules (CharacterRegex / Numeric / Multiline).
    public override bool CanEnterCharacter(char c)
        => base.CanEnterCharacter(c) && (_canEnterChar?.Invoke(c) ?? true);

    // Engine OnPaste has two MaxLength bugs (negative-length crash + under-limit bypass); reimplemented with a correct room clamp.
    public override void OnPaste(string text)
    {
        if (Label.HasSelection())
            Label.ReplaceSelection("");

        // Filter per-character through CanEnterCharacter (also runs the Goo CanEnterChar predicate).
        var pasteResult = new string(text.Where(CanEnterCharacter).ToArray());

        if (MaxLength.HasValue)
        {
            // TextLength here reflects the post-selection-removal length, matching engine ordering.
            int room = MaxLength.Value - TextLength;
            if (room <= 0)
                return;
            if (pasteResult.Length > room)
                pasteResult = pasteResult.Substring(0, room);
        }

        Text ??= "";
        Label.InsertText(pasteResult, CaretPosition);
        Label.MoveCaretPos(pasteResult.Length);

        OnValueChanged();
    }

    // Engine runs UpdateValidation() + OnTextEdited() here; merge the Goo Validate predicate and fire OnValidationChanged on a flip.
    public override void OnValueChanged()
    {
        base.OnValueChanged();
        ApplyPredicateAndNotify();
    }

    // Tighten HasValidationErrors with the Goo predicate, then fire OnValidationChanged (and rebuild) only on a validity transition.
    internal void ApplyPredicateAndNotify()
    {
        if (_validate != null && !_validate(Text ?? string.Empty))
        {
            HasValidationErrors = true;
            SetClass("invalid", true);
        }

        if (HasValidationErrors != _lastInvalid)
        {
            _lastInvalid = HasValidationErrors;
            _onValidationChanged?.Invoke(HasValidationErrors);
            if (_onValidationChanged != null) _requestRebuild?.Invoke();
        }
    }

    // Recompute validity (engine rules + predicate) without an edit event; the Applier calls this after props change.
    internal void RecomputeValidation()
    {
        UpdateValidation();
        ApplyPredicateAndNotify();
    }

    // Engine fires "onsubmit" itself on Enter (no Submit method); hook OnEvent to react.
    protected override void OnEvent(PanelEvent e)
    {
        base.OnEvent(e);
        if (e.Name == "onsubmit") { _onSubmit?.Invoke(Text ?? string.Empty); if (_onSubmit != null) _requestRebuild?.Invoke(); }
        // Escape fires "oncancel" via the engine's Cancel(); value-less, same path as onsubmit.
        if (e.Name == "oncancel") { _onCancel?.Invoke(); if (_onCancel != null) _requestRebuild?.Invoke(); }
    }

    // Call base first so the engine's focus/blur work runs before we observe the committed Text.
    protected override void OnFocus(PanelEvent e)
    {
        base.OnFocus(e);
        if (_onFocus != null) { _onFocus.Invoke(); _requestRebuild?.Invoke(); }
    }

    protected override void OnBlur(PanelEvent e)
    {
        base.OnBlur(e);
        if (_onBlur != null) { _onBlur.Invoke(Text ?? string.Empty); _requestRebuild?.Invoke(); }
    }

    public void ApplyEvents(in BlobEvents events)
    {
        _onClick      = events.OnClick;
        _onRightClick = events.OnRightClick;
        _onMiddleClick = events.OnMiddleClick;
        _onMouseEnter = events.OnMouseEnter;
        _onMouseLeave = events.OnMouseLeave;
        _onMouseDown  = events.OnMouseDown;
        _onMouseUp    = events.OnMouseUp;
        _onMouseMove  = events.OnMouseMove;
    }

    public bool HasEventHandlers =>
        _onClick != null || _onRightClick != null || _onMiddleClick != null || _onMouseEnter != null || _onMouseLeave != null ||
        _onMouseDown != null || _onMouseUp != null || _onMouseMove != null ||
        _onChange != null || _onSubmit != null ||
        _onFocus != null || _onBlur != null || _onCancel != null;

    public bool UserSetPointerEvents
    {
        get => _userSetPointerEvents;
        set => _userSetPointerEvents = value;
    }

    protected override void OnClick(MousePanelEvent e)       { base.OnClick(e);       EventDispatch.Fire(_onClick, e, _requestRebuild); }
    protected override void OnRightClick(MousePanelEvent e)  { base.OnRightClick(e);  EventDispatch.Fire(_onRightClick, e, _requestRebuild); }
    protected override void OnMiddleClick(MousePanelEvent e) { base.OnMiddleClick(e); EventDispatch.Fire(_onMiddleClick, e, _requestRebuild); }
    protected override void OnMouseOver(MousePanelEvent e)   { base.OnMouseOver(e);   EventDispatch.Fire(_onMouseEnter, e, _requestRebuild); }
    protected override void OnMouseOut(MousePanelEvent e)    { base.OnMouseOut(e);    EventDispatch.Fire(_onMouseLeave, e, _requestRebuild); }
    protected override void OnMouseDown(MousePanelEvent e)   { base.OnMouseDown(e);   EventDispatch.Fire(_onMouseDown, e, _requestRebuild); }
    protected override void OnMouseUp(MousePanelEvent e)     { base.OnMouseUp(e);     EventDispatch.Fire(_onMouseUp, e, _requestRebuild); }
    protected override void OnMouseMove(MousePanelEvent e)   { base.OnMouseMove(e);   EventDispatch.Fire(_onMouseMove, e, _requestRebuild); }
}
