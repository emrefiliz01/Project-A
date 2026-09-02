using System;

public static class CurrencyFormatter
{
    /// <summary>
    /// Para miktarını K, M, B formatında kısaltarak döndürür.
    /// Örn: 50000 -> "$50K", -500000 -> "-$500K", 2500000 -> "$2.5M"
    /// </summary>
    public static string FormatMoney(long amount)
    {
        long absAmount = Math.Abs(amount);
        string sign = amount < 0 ? "-" : "";

        if (absAmount >= 1_000_000_000)
        {
            float val = absAmount / 1_000_000_000f;
            return $"{sign}${val:0.##}B";
        }
        if (absAmount >= 1_000_000)
        {
            float val = absAmount / 1_000_000f;
            return $"{sign}${val:0.##}M";
        }
        if (absAmount >= 1_000)
        {
            float val = absAmount / 1_000f;
            return $"{sign}${val:0.##}K";
        }

        return $"{sign}${absAmount}";
    }

    /// <summary>
    /// int türündeki veriler için aşırı yükleme (Overload).
    /// </summary>
    public static string FormatMoney(int amount)
    {
        return FormatMoney((long)amount);
    }
}