using System;
using Sandbox.UI;

namespace Goo.Internal;

internal sealed class StatefulWebPanel : Sandbox.UI.WebPanel, IStatefulEventHost
{
    internal Action<MousePanelEvent>? _onClick;
    internal Action<MousePanelEvent>? _onRightClick;
    internal Action<MousePanelEvent>? _onMiddleClick;
    internal Action<MousePanelEvent>? _onMouseEnter;
    internal Action<MousePanelEvent>? _onMouseLeave;
    internal Action<MousePanelEvent>? _onMouseDown;
    internal Action<MousePanelEvent>? _onMouseUp;
    internal Action<MousePanelEvent>? _onMouseMove;
    internal bool    _userSetPointerEvents;
    internal Action? _requestRebuild;
    public Action? RequestRebuild { set => _requestRebuild = value; }

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
        _onMouseDown != null || _onMouseUp != null || _onMouseMove != null;

    public bool UserSetPointerEvents
    {
        get => _userSetPointerEvents;
        set => _userSetPointerEvents = value;
    }

    // (Engine #11055 corrected the WebPanel/Steam-HTML mousewheel sign; the former OnMouseWheel
    //  override is gone - keeping it would now double-invert. See bd engine-fact-webpanel-scroll-inverted.)

    protected override void OnClick(MousePanelEvent e)       { base.OnClick(e);       EventDispatch.Fire(_onClick, e, _requestRebuild); }
    protected override void OnRightClick(MousePanelEvent e)  { base.OnRightClick(e);  EventDispatch.Fire(_onRightClick, e, _requestRebuild); }
    protected override void OnMiddleClick(MousePanelEvent e) { base.OnMiddleClick(e); EventDispatch.Fire(_onMiddleClick, e, _requestRebuild); }
    protected override void OnMouseOver(MousePanelEvent e)   { base.OnMouseOver(e);   EventDispatch.Fire(_onMouseEnter, e, _requestRebuild); }
    protected override void OnMouseOut(MousePanelEvent e)    { base.OnMouseOut(e);    EventDispatch.Fire(_onMouseLeave, e, _requestRebuild); }
    protected override void OnMouseDown(MousePanelEvent e)   { base.OnMouseDown(e);   EventDispatch.Fire(_onMouseDown, e, _requestRebuild); }
    protected override void OnMouseUp(MousePanelEvent e)     { base.OnMouseUp(e);     EventDispatch.Fire(_onMouseUp, e, _requestRebuild); }
    protected override void OnMouseMove(MousePanelEvent e)   { base.OnMouseMove(e);   EventDispatch.Fire(_onMouseMove, e, _requestRebuild); }
}
