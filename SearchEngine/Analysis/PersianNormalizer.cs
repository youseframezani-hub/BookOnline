using System.Text;

namespace SearchEngine.Analysis;

public sealed class PersianNormalizer
{
    private static readonly string[] Suffixes =
        ["هایتان", "ترین", "هایی", "های", "شان", "تان", "مان", "تر", "ها"];

    private static readonly string[] StopWords =
        ["و", "در", "به", "از", "که", "این", "آن", "با", "برای", "است", "را", "تا", "هم"];

    public string NormalizeText(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            var c = ch switch
            {
                'ك' => 'ک',
                'ي' => 'ی',
                '‌' => ' ',   // نیم‌فاصله → فاصله
                >= 'ً' and <= 'ْ' => (char)0, // حذف اعراب
                _ => ch
            };
            if (c != 0) sb.Append(c);
        }
        return sb.ToString();
    }

    public string Stem(string term)
    {
        if (term.Length < 4) return term;
        foreach (var suffix in Suffixes)
            if (term.EndsWith(suffix, StringComparison.Ordinal) &&
                term.Length - suffix.Length >= 3)
                return term[..^suffix.Length];
        return term;
    }

    public bool IsStopWord(string term) => StopWords.Contains(term);

    public IEnumerable<(string Term, int Position, int Offset)> Tokenize(string rawText)
    {
        var normalized = NormalizeText(rawText);
        int position = 0;
        foreach (var token in normalized.Split(' ',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var offset = rawText.IndexOf(token, StringComparison.Ordinal);
            if (IsStopWord(token)) { position++; continue; }
            yield return (Stem(token), position++, offset);
        }
    }
}
