namespace Goo;

// Hand-written partial: the Style bundle property is not part of the generated facade.
public readonly partial record struct Text
{
    /// <summary>Applies a font preset (FontFamily, FontSize, FontWeight, FontColor) in one property. First-declared wins: a per-field override must precede Style in the initializer; fields set after Style are ignored where the preset already set them.</summary>
    public TextStyle Style { init => _style = value.ApplyTo(_style); }
}
