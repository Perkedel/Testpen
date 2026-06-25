#nullable enable
using System;
using HitShapes;
using Sandbox;
using Xunit;

namespace HitShapes.Tests;

public class ShapeMathTests
{
    [Fact]
    public void Radial_top_position_resolves_to_slot_0()
    {
        var shape = HitShape.Radial(slots: 8);
        // y=5 keeps the point inside the inscribed circle (radius=160).
        var slot = shape.Resolve(new Vector2(160, 5), new Vector2(320, 320));
        Assert.Equal(0, slot);
    }

    [Theory]
    [InlineData(160, 5,   0)]   // 12 o'clock (top)
    [InlineData(315, 160, 2)]   // 3 o'clock (right, inside inscribed circle)
    [InlineData(160, 315, 4)]   // 6 o'clock (bottom)
    [InlineData(5,   160, 6)]   // 9 o'clock (left)
    public void Radial_8_slots_resolves_cardinals(float x, float y, int expectedSlot)
    {
        var shape = HitShape.Radial(slots: 8);
        var slot = shape.Resolve(new Vector2(x, y), new Vector2(320, 320));
        Assert.Equal(expectedSlot, slot);
    }

    [Fact]
    public void Radial_with_inner_ratio_rejects_center()
    {
        var shape = HitShape.Radial(slots: 8, innerRatio: 0.4f);
        var slot = shape.Resolve(new Vector2(160, 160), new Vector2(320, 320));
        Assert.Null(slot);
    }

    [Fact]
    public void Radial_rejects_corners_outside_inscribed_circle()
    {
        var shape = HitShape.Radial(slots: 8);
        Assert.Null(shape.Resolve(new Vector2(0, 0), new Vector2(320, 320)));
        Assert.Null(shape.Resolve(new Vector2(320, 0), new Vector2(320, 320)));
        Assert.Null(shape.Resolve(new Vector2(0, 320), new Vector2(320, 320)));
        Assert.Null(shape.Resolve(new Vector2(320, 320), new Vector2(320, 320)));
    }

    [Theory]
    [InlineData(50,   50,  0)]    // first cell (col=0, row=0)
    [InlineData(150,  50,  1)]    // col=1, row=0
    [InlineData(50,   150, 4)]    // col=0, row=1 -> 1*4+0 = 4
    [InlineData(350,  250, 11)]   // last cell (col=3, row=2) -> 2*4+3 = 11
    public void RectGrid_4x3_resolves_cells(float x, float y, int expectedSlot)
    {
        var shape = HitShape.RectGrid(cols: 4, rows: 3);
        var slot = shape.Resolve(new Vector2(x, y), new Vector2(400, 300));
        Assert.Equal(expectedSlot, slot);
    }

    [Fact]
    public void RectGrid_out_of_bounds_returns_null()
    {
        var shape = HitShape.RectGrid(cols: 4, rows: 3);
        Assert.Null(shape.Resolve(new Vector2(-1, 50), new Vector2(400, 300)));
        Assert.Null(shape.Resolve(new Vector2(401, 50), new Vector2(400, 300)));
        Assert.Null(shape.Resolve(new Vector2(50, -1), new Vector2(400, 300)));
        Assert.Null(shape.Resolve(new Vector2(50, 301), new Vector2(400, 300)));
    }

    [Fact]
    public void RectGrid_boundary_clamps_to_last_cell()
    {
        var shape = HitShape.RectGrid(cols: 4, rows: 3);
        var slot = shape.Resolve(new Vector2(399.99f, 50), new Vector2(400, 300));
        Assert.Equal(3, slot);
    }

    [Fact]
    public void RectGrid_slot_count_is_cols_times_rows()
    {
        var shape = HitShape.RectGrid(cols: 4, rows: 3);
        Assert.Equal(12, shape.SlotCount);
    }

    [Fact]
    public void CustomRaw_resolver_is_invoked_with_position_and_size()
    {
        Vector2 capturedPos = default;
        Vector2 capturedSize = default;
        var shape = HitShape.CustomRaw(slotCount: 3, (pos, size) =>
        {
            capturedPos = pos;
            capturedSize = size;
            return 1;
        });

        var slot = shape.Resolve(new Vector2(42, 17), new Vector2(200, 100));

        Assert.Equal(1, slot);
        Assert.Equal(new Vector2(42, 17), capturedPos);
        Assert.Equal(new Vector2(200, 100), capturedSize);
    }

    [Fact]
    public void CustomRaw_slot_count_is_the_provided_value()
    {
        var shape = HitShape.CustomRaw(slotCount: 7, (_, _) => null);
        Assert.Equal(7, shape.SlotCount);
    }

    [Fact]
    public void CustomRaw_null_resolver_throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => HitShape.CustomRaw(slotCount: 1, resolver: null!));
    }

    [Fact]
    public void Custom_native_frame_passes_position_unchanged_at_matching_size()
    {
        Vector2 captured = default;
        var shape = HitShape.Custom(slotCount: 1, nativeSize: new Vector2(400, 200),
            pos => { captured = pos; return 0; });

        shape.Resolve(new Vector2(100, 50), new Vector2(400, 200));

        Assert.Equal(new Vector2(100, 50), captured);
    }

    [Fact]
    public void Custom_native_frame_descales_when_panel_is_larger_than_native()
    {
        Vector2 captured = default;
        var shape = HitShape.Custom(slotCount: 1, nativeSize: new Vector2(100, 50),
            pos => { captured = pos; return 0; });

        shape.Resolve(new Vector2(200, 150), new Vector2(200, 150));

        Assert.Equal(new Vector2(100, 50), captured);
    }

    [Fact]
    public void Custom_native_frame_descales_independently_on_each_axis()
    {
        Vector2 captured = default;
        var shape = HitShape.Custom(slotCount: 1, nativeSize: new Vector2(100, 100),
            pos => { captured = pos; return 0; });

        shape.Resolve(new Vector2(150, 50), new Vector2(300, 200));

        Assert.Equal(new Vector2(50, 25), captured);
    }

    [Fact]
    public void Custom_native_frame_returns_null_for_degenerate_size()
    {
        var shape = HitShape.Custom(slotCount: 1, nativeSize: new Vector2(100, 100), _ => 0);

        Assert.Null(shape.Resolve(new Vector2(50, 50), new Vector2(0, 100)));
        Assert.Null(shape.Resolve(new Vector2(50, 50), new Vector2(100, 0)));
    }

    [Fact]
    public void Custom_native_frame_slot_count_is_the_provided_value()
    {
        var shape = HitShape.Custom(slotCount: 9, nativeSize: new Vector2(100, 100), _ => null);
        Assert.Equal(9, shape.SlotCount);
    }

    [Fact]
    public void Custom_native_frame_null_resolver_throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => HitShape.Custom(slotCount: 1, nativeSize: new Vector2(100, 100),
                resolver: (Func<Vector2, int?>)null!));
    }

    [Fact]
    public void Custom_native_frame_throws_when_resolver_returns_out_of_range_slot()
    {
        var shape = HitShape.Custom(slotCount: 4, nativeSize: new Vector2(100, 100), _ => 9);
        Assert.Throws<InvalidOperationException>(
            () => shape.Resolve(new Vector2(50, 50), new Vector2(100, 100)));
    }

    [Fact]
    public void Custom_native_frame_throws_when_resolver_returns_negative_slot()
    {
        var shape = HitShape.Custom(slotCount: 4, nativeSize: new Vector2(100, 100), _ => -1);
        Assert.Throws<InvalidOperationException>(
            () => shape.Resolve(new Vector2(50, 50), new Vector2(100, 100)));
    }

    [Fact]
    public void CustomRaw_throws_when_resolver_returns_out_of_range_slot()
    {
        var shape = HitShape.CustomRaw(slotCount: 4, (_, _) => 9);
        Assert.Throws<InvalidOperationException>(
            () => shape.Resolve(new Vector2(50, 50), new Vector2(100, 100)));
    }

    [Fact]
    public void CustomRaw_throws_when_resolver_returns_negative_slot()
    {
        var shape = HitShape.CustomRaw(slotCount: 4, (_, _) => -1);
        Assert.Throws<InvalidOperationException>(
            () => shape.Resolve(new Vector2(50, 50), new Vector2(100, 100)));
    }

    [Fact]
    public void Polygon_triangle_interior_resolves_to_slot_0()
    {
        var shape = HitShape.Polygon(
            new Vector2(0.5f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f));
        var slot = shape.Resolve(new Vector2(200, 200), new Vector2(400, 400));
        Assert.Equal(0, slot);
    }

    [Fact]
    public void Polygon_above_triangle_returns_null()
    {
        var shape = HitShape.Polygon(
            new Vector2(0.5f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f));
        Assert.Null(shape.Resolve(new Vector2(20, 20), new Vector2(400, 400)));
        Assert.Null(shape.Resolve(new Vector2(380, 20), new Vector2(400, 400)));
    }

    [Fact]
    public void Polygon_concave_L_rejects_notch_accepts_arm()
    {
        // L-shape vertices in unit coords (clockwise from top-left):
        //  (0,0) -> (0.5,0) -> (0.5,0.5) -> (1,0.5) -> (1,1) -> (0,1)
        var shape = HitShape.Polygon(
            new Vector2(0f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f));
        Assert.Equal(0, shape.Resolve(new Vector2(100, 200), new Vector2(400, 400)));
        Assert.Equal(0, shape.Resolve(new Vector2(300, 300), new Vector2(400, 400)));
        Assert.Null(shape.Resolve(new Vector2(300, 100), new Vector2(400, 400)));
    }

    [Fact]
    public void Polygon_slot_count_is_one()
    {
        var shape = HitShape.Polygon(
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 1f));
        Assert.Equal(1, shape.SlotCount);
    }

    [Fact]
    public void Polygon_null_verts_throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => HitShape.Polygon(verts: null!));
    }

    [Fact]
    public void Polygon_with_fewer_than_3_verts_throws()
    {
        Assert.Throws<System.ArgumentException>(
            () => HitShape.Polygon(new Vector2(0f, 0f), new Vector2(1f, 1f)));
    }

    [Fact]
    public void Polygons_non_overlapping_routes_to_correct_slot()
    {
        // Two triangles. Left triangle occupies x in [0, 0.4]. Right occupies x in [0.6, 1].
        var left = new[]
        {
            new Vector2(0f, 0f), new Vector2(0.4f, 0f), new Vector2(0.4f, 1f), new Vector2(0f, 1f),
        };
        var right = new[]
        {
            new Vector2(0.6f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0.6f, 1f),
        };
        var shape = HitShape.Polygons(left, right);

        Assert.Equal(0, shape.Resolve(new Vector2(80, 200), new Vector2(400, 400)));   // x=0.2 -> left
        Assert.Equal(1, shape.Resolve(new Vector2(320, 200), new Vector2(400, 400)));  // x=0.8 -> right
        Assert.Null(shape.Resolve(new Vector2(200, 200), new Vector2(400, 400)));      // x=0.5 -> neither
    }

    [Fact]
    public void Polygons_overlap_returns_first_match()
    {
        // Two squares overlapping in the middle.
        var a = new[]
        {
            new Vector2(0f, 0f), new Vector2(0.6f, 0f), new Vector2(0.6f, 0.6f), new Vector2(0f, 0.6f),
        };
        var b = new[]
        {
            new Vector2(0.4f, 0.4f), new Vector2(1f, 0.4f), new Vector2(1f, 1f), new Vector2(0.4f, 1f),
        };
        var shape = HitShape.Polygons(a, b);

        // Point in overlap region (0.5, 0.5):
        Assert.Equal(0, shape.Resolve(new Vector2(200, 200), new Vector2(400, 400)));
    }

    [Fact]
    public void Polygons_slot_count_matches_polygon_count()
    {
        var p1 = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f) };
        var p2 = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f) };
        var p3 = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f) };
        Assert.Equal(3, HitShape.Polygons(p1, p2, p3).SlotCount);
    }

    [Fact]
    public void Polygons_null_array_throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => HitShape.Polygons(polys: null!));
    }

    [Fact]
    public void Polygons_null_sub_array_throws()
    {
        Vector2[] valid = { new(0f, 0f), new(1f, 0f), new(0.5f, 1f) };
        Assert.Throws<System.ArgumentNullException>(
            () => HitShape.Polygons(valid, null!));
    }

    [Fact]
    public void Polygons_sub_array_with_fewer_than_3_verts_throws()
    {
        Vector2[] tooFew = { new(0f, 0f), new(1f, 0f) };
        Assert.Throws<System.ArgumentException>(
            () => HitShape.Polygons(tooFew));
    }

    [Fact]
    public void Union_routes_to_A_when_only_A_resolves()
    {
        var grid = HitShape.RectGrid(2, 2);                                // SlotCount 4
        var circle = HitShape.Radial(slots: 1, outerRatio: 0.2f);          // SlotCount 1
        var combo = HitShape.Union(grid, circle);

        // Point near top-left corner: inside grid (slot 0), outside small circle.
        var slot = combo.Resolve(new Vector2(20, 20), new Vector2(400, 400));
        Assert.Equal(0, slot);
    }

    [Fact]
    public void Union_offsets_B_slots_by_A_slot_count()
    {
        var aOnly = HitShape.Radial(slots: 1, outerRatio: 0.0f);           // never resolves
        var b = HitShape.RectGrid(2, 2);                                    // SlotCount 4, slot 0..3
        var combo = HitShape.Union(aOnly, b);                               // SlotCount 5

        // aOnly never resolves; b at (20, 20) on 400x400 = top-left grid cell -> slot 0 in B.
        // After offset by A.SlotCount (1), expect slot 1.
        var slot = combo.Resolve(new Vector2(20, 20), new Vector2(400, 400));
        Assert.Equal(1, slot);
    }

    [Fact]
    public void Union_A_wins_on_overlap()
    {
        var a = HitShape.RectGrid(1, 1);            // covers whole panel, slot 0
        var b = HitShape.RectGrid(1, 1);            // also covers whole panel
        var combo = HitShape.Union(a, b);           // SlotCount 2

        var slot = combo.Resolve(new Vector2(50, 50), new Vector2(100, 100));
        Assert.Equal(0, slot);                       // A wins
    }

    [Fact]
    public void Union_slot_count_is_sum_of_inputs()
    {
        var combo = HitShape.Union(HitShape.Radial(8), HitShape.RectGrid(2, 2));
        Assert.Equal(12, combo.SlotCount);
    }

    [Fact]
    public void Union_null_args_throw()
    {
        var ok = HitShape.Radial(4);
        Assert.Throws<System.ArgumentNullException>(() => HitShape.Union(null!, ok));
        Assert.Throws<System.ArgumentNullException>(() => HitShape.Union(ok, null!));
    }

    [Fact]
    public void Intersect_returns_null_when_A_alone_resolves()
    {
        var bigBox = HitShape.RectGrid(1, 1);                              // whole panel = slot 0
        var smallCircle = HitShape.Radial(slots: 1, outerRatio: 0.2f);     // tiny center disc
        var combo = HitShape.Intersect(bigBox, smallCircle);

        // Corner: A resolves (0), B does not.
        Assert.Null(combo.Resolve(new Vector2(10, 10), new Vector2(400, 400)));
    }

    [Fact]
    public void Intersect_returns_A_slot_when_both_resolve()
    {
        var bigBox = HitShape.RectGrid(1, 1);
        var smallCircle = HitShape.Radial(slots: 1, outerRatio: 0.2f);
        var combo = HitShape.Intersect(bigBox, smallCircle);

        // Center: both A and B resolve. Expect A's slot (0).
        Assert.Equal(0, combo.Resolve(new Vector2(200, 200), new Vector2(400, 400)));
    }

    [Fact]
    public void Intersect_returns_null_when_B_alone_resolves()
    {
        var smallCircle = HitShape.Radial(slots: 1, outerRatio: 0.2f);
        var bigBox = HitShape.RectGrid(1, 1);
        var combo = HitShape.Intersect(smallCircle, bigBox);

        // Corner: A (small circle) does not resolve. Result should be null even though B does.
        Assert.Null(combo.Resolve(new Vector2(10, 10), new Vector2(400, 400)));
    }

    [Fact]
    public void Intersect_slot_count_is_A_slot_count()
    {
        var combo = HitShape.Intersect(HitShape.Radial(8), HitShape.RectGrid(2, 2));
        Assert.Equal(8, combo.SlotCount);
    }

    [Fact]
    public void Intersect_null_args_throw()
    {
        var ok = HitShape.Radial(4);
        Assert.Throws<System.ArgumentNullException>(() => HitShape.Intersect(null!, ok));
        Assert.Throws<System.ArgumentNullException>(() => HitShape.Intersect(ok, null!));
    }

    [Fact]
    public void Difference_returns_A_slot_when_only_A_resolves()
    {
        var bigBox = HitShape.RectGrid(1, 1);                              // whole panel = slot 0
        var smallCircle = HitShape.Radial(slots: 1, outerRatio: 0.2f);     // tiny center disc
        var combo = HitShape.Difference(bigBox, smallCircle);

        // Corner: A resolves (0), B does not -> A's slot.
        Assert.Equal(0, combo.Resolve(new Vector2(10, 10), new Vector2(400, 400)));
    }

    [Fact]
    public void Difference_returns_null_when_both_resolve()
    {
        var bigBox = HitShape.RectGrid(1, 1);
        var smallCircle = HitShape.Radial(slots: 1, outerRatio: 0.2f);
        var combo = HitShape.Difference(bigBox, smallCircle);

        // Center: both resolve. Difference punches the hole -> null.
        Assert.Null(combo.Resolve(new Vector2(200, 200), new Vector2(400, 400)));
    }

    [Fact]
    public void Difference_returns_null_when_only_B_resolves()
    {
        var smallCircle = HitShape.Radial(slots: 1, outerRatio: 0.2f);
        var bigBox = HitShape.RectGrid(1, 1);
        var combo = HitShape.Difference(smallCircle, bigBox);

        // Corner: A (small circle) does not resolve.
        Assert.Null(combo.Resolve(new Vector2(10, 10), new Vector2(400, 400)));
    }

    [Fact]
    public void Difference_slot_count_is_A_slot_count()
    {
        var combo = HitShape.Difference(HitShape.Radial(8), HitShape.Radial(1, outerRatio: 0.3f));
        Assert.Equal(8, combo.SlotCount);
    }

    [Fact]
    public void Difference_null_args_throw()
    {
        var ok = HitShape.Radial(4);
        Assert.Throws<System.ArgumentNullException>(() => HitShape.Difference(null!, ok));
        Assert.Throws<System.ArgumentNullException>(() => HitShape.Difference(ok, null!));
    }
}
