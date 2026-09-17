using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookOnline.Domain.Books;

public interface IExtendedDate
{
    int CompareTo(ExtendedDate other);
    string ToAncientFormatString();
    (long Year, int Month, int Day) ToHijriLunar();
    long ToInt64();
    long ToJulianDayNumber();
    (long Year, int Month, int Day) ToShamsi();
    (string AncientFormat, long DbKey) ToStorageOutput();
    string ToString();
}

public struct ExtendedDate : IComparable<ExtendedDate>, IExtendedDate
{
    // ─── فیلدها ───────────────────────────────────────────────────────────────
    public readonly long GregorianYear;
    public readonly int Month;
    public readonly int Day;

    // ─── سازنده ───────────────────────────────────────────────────────────────
    public ExtendedDate(long gregorianYear, int month, int day)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month));
        if (day < 1 || day > DaysInMonth(gregorianYear, month))
            throw new ArgumentOutOfRangeException(nameof(day));
        GregorianYear = gregorianYear;
        Month = month;
        Day = day;
    }

    // ─── کمکی‌های میلادی ──────────────────────────────────────────────────────
    public static bool IsLeapGregorian(long year)
    {
        long astro = year <= 0 ? year + 1 : year;
        return (astro % 4 == 0) && (astro % 100 != 0 || astro % 400 == 0);
    }

    public static int DaysInMonth(long year, int month)
    {
        int[] days = { 31, IsLeapGregorian(year) ? 29 : 28, 31, 30, 31, 30,
                        31, 31, 30, 31, 30, 31 };
        return days[month - 1];
    }

    // ─── Julian Day Number ────────────────────────────────────────────────────
    public long ToJulianDayNumber()
    {
        long y = GregorianYear <= 0 ? GregorianYear - 1 : GregorianYear;
        int m = Month;
        int d = Day;
        if (m <= 2) { y -= 1; m += 12; }
        long A = y / 400 - y / 100 + y / 4;
        return 365L * y - 679004L + A + (int)(30.6001 * (m + 1)) + d;
    }

    // ─── تقویم شمسی ───────────────────────────────────────────────────────────
    /// <summary>تبدیل به تقویم هجری شمسی بر پایه چرخه ۲۸۲۰ ساله</summary>
    public (long Year, int Month, int Day) ToShamsi()
    {
        const long ShamsiEpoch = 1948319L;
        long jdn = ToJulianDayNumber();
        long daysSinceEpoch = jdn - ShamsiEpoch;
        long yearCycle = daysSinceEpoch >= 0
                               ? daysSinceEpoch / 365
                               : (daysSinceEpoch - 364) / 365;
        long year = yearCycle + 1;
        long startJdn = ShamsiYearStartJdn(year);

        if (startJdn > jdn) { year--; startJdn = ShamsiYearStartJdn(year); }
        while (ShamsiYearStartJdn(year + 1) <= jdn) year++;

        long dayOfYear = jdn - startJdn;
        int month, day;

        if (dayOfYear < 6 * 31)
        {
            month = (int)(dayOfYear / 31) + 1;
            day = (int)(dayOfYear % 31) + 1;
        }
        else
        {
            long remaining = dayOfYear - 6 * 31;
            month = (int)(remaining / 30) + 7;
            day = (int)(remaining % 30) + 1;
            if (month > 12) { month = 12; day = (int)remaining - 5 * 30 + 1; }
        }
        return (year, month, day);
    }

    private static long ShamsiYearStartJdn(long year)
    {
        const long Epoch = 1948319L;
        const long Cycle = 2820L;
        const long CycleDays = 1029983L;
        long y = year - 1;
        long c = y / Cycle;
        long r = y % Cycle;
        if (r < 0) { c--; r += Cycle; }
        return Epoch + c * CycleDays + r * 365L + (r * 683L) / 2820L;
    }

    // ─── تقویم قمری ───────────────────────────────────────────────────────────
    /// <summary>تبدیل به تقویم هجری قمری</summary>
    public (long Year, int Month, int Day) ToHijriLunar()
    {
        const long HijriEpoch = 1948438L;
        long jdn = ToJulianDayNumber();
        long daysSinceEpoch = jdn - HijriEpoch;
        long cycle = daysSinceEpoch >= 0
                               ? daysSinceEpoch / 10631
                               : (daysSinceEpoch - 10630) / 10631;
        long remaining = daysSinceEpoch - cycle * 10631L;
        long yearInCycle = 0;
        long accDays = 0;

        for (int i = 1; i <= 30; i++)
        {
            long yDays = IsLeapHijri(i) ? 355 : 354;
            if (accDays + yDays > remaining) { yearInCycle = i; remaining -= accDays; break; }
            accDays += yDays;
        }
        if (yearInCycle == 0) yearInCycle = 30;

        long year = cycle * 30L + yearInCycle;
        int month = 1;
        for (int m = 1; m <= 12; m++)
        {
            int mDays = HijriDaysInMonth(year, m);
            if (remaining < mDays) { month = m; break; }
            remaining -= mDays;
        }
        return (year, month, (int)remaining + 1);
    }

    private static bool IsLeapHijri(long yearInCycle) =>
        (((yearInCycle - 1) % 30 + 30) % 30 + 1) switch
        {
            2 or 5 or 7 or 10 or 13 or 16 or 18 or 21 or 24 or 26 or 29 => true,
            _ => false
        };

    private static int HijriDaysInMonth(long year, int month) =>
        month % 2 == 1 ? 30
        : month == 12 ? (IsLeapHijri(((year - 1) % 30 + 30) % 30 + 1) ? 30 : 29)
        : 29;

    // ─── فرمت‌دهی ─────────────────────────────────────────────────────────────
    /// <summary>
    /// فرمت نمایش باستانی:
    /// <c>]15 محرمِ 3000-/ 20 آذرِ 2912-/ 12 دسامبرِ 2290-]</c>
    /// </summary>
    public string ToAncientFormatString()
    {
        // میلادی
        string[] gregorianMonths = {
            "ژانویه","فوریه","مارس","آوریل","مه","ژوئن",
            "ژوئیه","اوت","سپتامبر","اکتبر","نوامبر","دسامبر"
        };
        string gPart = FormatAncientPart(Day, gregorianMonths[Month - 1], GregorianYear);

        // شمسی
        string[] shamsiMonths = {
            "فروردین","اردیبهشت","خرداد","تیر","مرداد","شهریور",
            "مهر","آبان","آذر","دی","بهمن","اسفند"
        };
        var (sy, sm, sd) = ToShamsi();
        string sPart = FormatAncientPart(sd, shamsiMonths[sm - 1], sy);

        // قمری
        string[] hijriMonths = {
            "محرم","صفر","ربیع‌الاول","ربیع‌الثانی","جمادی‌الاول","جمادی‌الثانی",
            "رجب","شعبان","رمضان","شوال","ذی‌القعده","ذی‌الحجه"
        };
        var (hy, hm, hd) = ToHijriLunar();
        string hPart = FormatAncientPart(hd, hijriMonths[hm - 1], hy);

        return $"]{hPart}/ {sPart}/ {gPart}]";
    }

    /// <summary>
    /// فرمت یک بخش از تاریخ باستانی.
    /// سال منفی: عدد سال + علامت منفی (مثلاً <c>2290-</c>).
    /// کسره (\u0650) زیر آخرین حرف نام ماه اضافه می‌شود.
    /// </summary>
    private static string FormatAncientPart(int day, string monthName, long year)
    {
        string yearStr = year < 0 ? $"{Math.Abs(year)}-" : $"{year}";
        return $"{day} {monthName}\u0650 {yearStr}";
    }

    // ─── ToString پیش‌فرض ─────────────────────────────────────────────────────
    public override string ToString() =>
        $"{(GregorianYear < 0 ? $"{Math.Abs(GregorianYear)}-" : $"{GregorianYear}")}" +
        $"/{Month:D2}/{Day:D2}";

    // ─── مقایسه ───────────────────────────────────────────────────────────────
    public int CompareTo(ExtendedDate other)
    {
        int c = GregorianYear.CompareTo(other.GregorianYear);
        if (c != 0) return c;
        c = Month.CompareTo(other.Month);
        return c != 0 ? c : Day.CompareTo(other.Day);
    }
    #region Factory

    // ─── تبدیل به عدد برای دیتابیس ────────────────────────────────────────────
    /// <summary>
    /// تاریخ را به یک عدد int64 تبدیل می‌کند.
    /// فرمت: SYYYYMMDD
    ///   S    = علامت (0 = مثبت/صفر، 1 = منفی)
    ///   YYYY = عدد مطلق سال با padding (حداکثر 9 رقم)
    ///   MM   = ماه (01-12)
    ///   DD   = روز (01-31)
    /// مثال: 2290/12/12 => 0229012 12  => 2_2290_12_12 => 222901212
    ///        2290-/12/12 => 1229012 12 => 1_2290_12_12 => 122901212
    /// </summary>
    public long ToInt64()
    {
        long absYear = Math.Abs(GregorianYear);
        long sign = GregorianYear < 0 ? 1L : 0L;
        // sign * 10^11 + absYear * 10^4 + month * 10^2 + day
        return sign * 1_000_000_000_00L
             + absYear * 10_000L
             + Month * 100L
             + Day;
    }

    /// <summary>
    /// از عدد int64 (خروجی ToInt64) یک ExtendedDate می‌سازد.
    /// </summary>
    public static ExtendedDate FromInt64(long value)
    {
        long sign = value / 1_000_000_000_00L;         // 0 یا 1
        long rest = value % 1_000_000_000_00L;
        long absYear = rest / 10_000L;
        int month = (int)(rest % 10_000L / 100L);
        int day = (int)(rest % 100L);
        long year = sign == 1 ? -absYear : absYear;
        return new ExtendedDate(year, month, day);
    }

    // ─── Parse از رشته ToAncientFormatString ──────────────────────────────────
    /// <summary>
    /// رشته‌ای با فرمت <c>]hd hm\u0650 hy/ sd sm\u0650 sy/ gd gm\u0650 gy]</c>
    /// را می‌خواند و ExtendedDate میلادی را برمی‌گرداند.
    /// نمونه: ]15 محرمِ 3000-/ 20 آذرِ 2912-/ 12 دسامبرِ 2290-]
    /// </summary>
    public static ExtendedDate Parse(string ancientFormatString)
    {
        if (string.IsNullOrWhiteSpace(ancientFormatString))
            throw new ArgumentNullException(nameof(ancientFormatString));

        // حذف کروشه‌های ابتدا و انتها
        string s = ancientFormatString.Trim().TrimStart(']').TrimEnd(']').Trim();

        // سه بخش با جداکننده "/ "
        string[] parts = s.Split(new[] { "/ " }, StringSplitOptions.None);
        if (parts.Length != 3)
            throw new FormatException("فرمت رشته صحیح نیست؛ سه بخش انتظار می‌رود.");

        // آخرین بخش = میلادی
        string gregorianPart = parts[2].Trim();
        var (gDay, gMonthName, gYear) = ParseAncientPart(gregorianPart);

        string[] gMonths = {
        "ژانویه","فوریه","مارس","آوریل","مه","ژوئن",
        "ژوئیه","اوت","سپتامبر","اکتبر","نوامبر","دسامبر"
    };
        int gMonth = Array.IndexOf(gMonths, gMonthName) + 1;
        if (gMonth == 0)
            throw new FormatException($"نام ماه میلادی شناخته‌شده نیست: {gMonthName}");

        return new ExtendedDate(gYear, gMonth, gDay);
    }

    /// <summary>
    /// یک بخش از فرمت باستانی مثل "12 دسامبرِ 2290-" را تجزیه می‌کند.
    /// </summary>
    private static (int Day, string MonthName, long Year) ParseAncientPart(string part)
    {
        // جدا کردن بر اساس فاصله
        // فرمت: "{day} {monthName}\u0650 {yearStr}"
        // کسره (\u0650) به انتهای نام ماه چسبیده است
        string[] tokens = part.Split(' ');
        if (tokens.Length < 3)
            throw new FormatException($"بخش ناقص: {part}");

        int day = int.Parse(tokens[0]);
        // حذف کسره از انتهای نام ماه
        string monthName = tokens[1].TrimEnd('\u0650');
        string yearToken = tokens[2];

        // تشخیص منفی: عدد + "-" در انتها  مثلاً "2290-"
        bool negative = yearToken.EndsWith("-");
        string yearStr = negative ? yearToken.TrimEnd('-') : yearToken;
        long year = long.Parse(yearStr);
        if (negative) year = -year;

        return (day, monthName, year);
    }

    // ─── خروجی ترکیبی (رشته + عدد) ───────────────────────────────────────────
    /// <summary>
    /// هر دو خروجی را با هم برمی‌گرداند: رشته باستانی و کد عددی دیتابیس.
    /// </summary>
    public (string AncientFormat, long DbKey) ToStorageOutput()
        => (ToAncientFormatString(), ToInt64());
    #endregion

}

