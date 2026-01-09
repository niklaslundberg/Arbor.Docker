namespace Arbor.Docker;

internal static class StringExtensions
{
    public static string Wrap(this string text, string wrapper) => wrapper + text + wrapper;
}