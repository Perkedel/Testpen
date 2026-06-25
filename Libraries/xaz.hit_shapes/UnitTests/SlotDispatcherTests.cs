#nullable enable
using System.Collections.Generic;
using HitShapes;
using Sandbox;
using Xunit;

namespace HitShapes.Tests;

public class SlotDispatcherTests
{
    // 2-slot horizontal split: left half = slot 0, right half = slot 1, outside rect = null.
    static IHitShape TwoSlot()
        => HitShape.CustomRaw(slotCount: 2, (pos, size) =>
        {
            if (pos.x < 0 || pos.y < 0 || pos.x >= size.x || pos.y >= size.y) return null;
            return pos.x < size.x * 0.5f ? 0 : 1;
        });

    [Fact]
    public void Null_to_A_fires_OnSlotEnter_only()
    {
        var events = new List<string>();
        var disp = new SlotDispatcher(TwoSlot())
        {
            OnSlotEnter = s => events.Add($"enter:{s}"),
            OnSlotLeave = s => events.Add($"leave:{s}"),
        };

        disp.UpdateHoverAt(new Vector2(20, 50), new Vector2(100, 100));

        Assert.Equal(new[] { "enter:0" }, events);
        Assert.Equal(0, disp.CurrentSlot);
    }

    [Fact]
    public void A_to_A_fires_nothing()
    {
        var events = new List<string>();
        var disp = new SlotDispatcher(TwoSlot())
        {
            OnSlotEnter = s => events.Add($"enter:{s}"),
            OnSlotLeave = s => events.Add($"leave:{s}"),
        };

        disp.UpdateHoverAt(new Vector2(20, 50), new Vector2(100, 100));
        events.Clear();
        disp.UpdateHoverAt(new Vector2(30, 50), new Vector2(100, 100));

        Assert.Empty(events);
    }

    [Fact]
    public void A_to_B_fires_leave_then_enter_in_order()
    {
        var events = new List<string>();
        var disp = new SlotDispatcher(TwoSlot())
        {
            OnSlotEnter = s => events.Add($"enter:{s}"),
            OnSlotLeave = s => events.Add($"leave:{s}"),
        };

        disp.UpdateHoverAt(new Vector2(20, 50), new Vector2(100, 100));
        events.Clear();
        disp.UpdateHoverAt(new Vector2(80, 50), new Vector2(100, 100));

        Assert.Equal(new[] { "leave:0", "enter:1" }, events);
        Assert.Equal(1, disp.CurrentSlot);
    }

    [Fact]
    public void B_to_null_fires_leave_only()
    {
        var events = new List<string>();
        var disp = new SlotDispatcher(TwoSlot())
        {
            OnSlotEnter = s => events.Add($"enter:{s}"),
            OnSlotLeave = s => events.Add($"leave:{s}"),
        };

        disp.UpdateHoverAt(new Vector2(80, 50), new Vector2(100, 100));
        events.Clear();
        disp.UpdateHoverAt(new Vector2(-10, 50), new Vector2(100, 100));

        Assert.Equal(new[] { "leave:1" }, events);
        Assert.Null(disp.CurrentSlot);
    }

    [Fact]
    public void Reset_clears_state_without_firing_leave()
    {
        var events = new List<string>();
        var disp = new SlotDispatcher(TwoSlot())
        {
            OnSlotEnter = s => events.Add($"enter:{s}"),
            OnSlotLeave = s => events.Add($"leave:{s}"),
        };

        disp.UpdateHoverAt(new Vector2(20, 50), new Vector2(100, 100));
        events.Clear();
        disp.Reset();

        Assert.Empty(events);
        Assert.Null(disp.CurrentSlot);
    }

    [Fact]
    public void Null_shape_throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => new SlotDispatcher(null!));
    }
}
