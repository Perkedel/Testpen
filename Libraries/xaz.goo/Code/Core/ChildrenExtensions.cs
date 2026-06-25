using System;
using System.Collections.Generic;

namespace Goo;

// Auto-keying loop helper: AddRange runs the builder once per item and keys by index when the builder leaves Key == null. User-supplied keys win.
public static class ChildrenExtensions
{
    // TBlob is generic over all blob kinds (Polygon, Text, ... not just Container). A `with`
    // expression cannot key a generic struct, so the key is written into the Frame slot after
    // Add — same assembly, same effect, no boxing.
    public static void AddRange<TItem, TBlob>(
        this Children                children,
        IReadOnlyList<TItem>         items,
        Func<int, TItem, TBlob>      builder ) where TBlob : struct, IBlob
    {
        for ( int i = 0; i < items.Count; i++ )
        {
            var child = builder( i, items[i] );
            children.Add( in child );
            ref Frame slot = ref children[children.Count - 1];
            slot.Key ??= $"_idx:{i}";
        }
    }

    // Child argument is always evaluated; pass default(T) or a prebuilt local when condition is false (default skips the Container ctor).
    public static void AddIf<T>( this Children children, bool condition, in T child ) where T : struct, IBlob
    {
        if ( condition ) children.Add( in child );
    }
}
