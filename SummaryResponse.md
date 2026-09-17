# تحلیل معماری سورس‌کد BookOnline

> این سند حاصل بررسی کامل کدبیس، بدون اعمال هیچ تغییری در کد، است. هدف آشنایی عمیق با معماری کلی راه‌حل (Solution) و به‌طور ویژه پروژه `WordReader` است.

---

## ۱. معرفی اجمالی کل پروژه و معماری کلی

### ۱.۱. ماهیت پروژه
`BookOnline` یک **Solution چندپروژه‌ای (.NET 8)** است که هدف نهایی آن ساخت یک **پلتفرم کتابخانه/کتاب‌فروشی آنلاین** به نظر می‌رسد؛ پلتفرمی که کاربران می‌توانند کتاب‌ها (احتمالاً با محوریت متون فارسی/دینی مانند «سیره نبوی») را مطالعه، جستجو، هایلایت، یادداشت‌گذاری و حتی با محتوای کتاب «گفت‌وگو» (چت‌بات مبتنی بر هوش مصنوعی) کنند.

### ۱.۲. نقشهٔ پروژه‌ها (بر اساس `BookOnline.sln`، پس از این مکالمه)

| پروژه | نوع | وضعیت |
|---|---|---|
| `BookOnline` | ASP.NET Core Web API | اسکلت خام (تمپلیت پیش‌فرض) |
| `Core/Domain/BookOnline.Domain` | Class Library (Domain) | **اصلاح‌شده در این مکالمه** — قابل‌ساخت (Instantiable) شد |
| `LibraryOnline.Domain` | Class Library (Domain) | اسکلت خام (بدون تغییر) |
| `FinancialBookStore.Domain` | Class Library (Domain) | اسکلت خام (بدون تغییر) |
| `AdminLibraryOnlins.Domain` | Class Library (Domain) | اسکلت خام (بدون تغییر) |
| `LandingSite` (`LandingPage`) | ASP.NET Core Razor Pages | بدون تغییر |
| `WordReader` | ASP.NET Core Razor Pages + Console-style runner | بدون تغییر — تنها ابزار استخراج واقعی |
| `SearchEngine` | **Class Library جدید** | موتور Full-Text Search مستقل (این مکالمه) |
| `BookImporter` | **Console App جدید** | پل WordReader ↔ Domain + Persistence + Indexing (این مکالمه) |

---

## ۲. آنالیز تفصیلی پروژه WordReader (بدون تغییر در این مکالمه)

`WordReader` یک ابزار ETL است: فایل `.docx` را با **COM Interop به Microsoft Word** (`InteropTextExtractor`) صفحه‌به‌صفحه می‌خواند و در `DocumentContent` (شامل `Pages`→`ParagraphSpan`، جدول، تصویر، پاورقی، هدر/فوتر) بازسازی می‌کند؛ به‌موازات آن `ExtractStyleService` با **OpenXML خالص** سبک‌ها را با جزئیات کامل (وراثت سبک، رنگ Theme) استخراج می‌کند. منطق کسب‌وکار مخصوص کتاب «سیره نبوی» (نام‌های سبک فارسی، پارس TOC) در `WordReader/Program.cs` بود، نه در خود Extractor.

**وابستگی حیاتی:** `WordReader.csproj` یک `<COMReference>` به `Microsoft.Office.Interop.Word` دارد. این باعث می‌شود:
- فقط با **MSBuild کامل (.NET Framework)**، نه CLI دات‌نت (`dotnet build`)، قابل بیلد باشد (جزئیات در بخش ۷.۳).
- فقط روی ویندوزی با Word واقعاً نصب‌شده قابل اجرا باشد.

---

## ۳و۴. ارتباط با سایر بخش‌ها و چالش‌های معماری (خلاصه)

پیش از این مکالمه، `WordReader` کاملاً ایزوله بود؛ خروجی‌اش فقط یک فایل JSON روی دیسک (`output.txt`) بود و هیچ پروژه‌ای آن را مصرف نمی‌کرد. `BookOnline.Domain.Books` (`IBook`/`IPage`/`PragraphSection`/قدیمی `AtomicBlockSection`) از نظر ساختاری شبیه خروجی `WordReader` بود اما نه constructible بود (تمام کلاس‌ها فقط property های get-only بدون سازنده داشتند) و نه هیچ Mapper‌ای بین این دو وجود داشت. پروژهٔ `BookImporter` در `.sln` ثبت شده بود ولی در دیسک نبود — دقیقاً همان «حلقهٔ گمشده».

---

## ۵ و ۶. تصمیمات معماری توافق‌شده (پیش از پیاده‌سازی)

طی مکالمه، تصمیمات زیر گرفته شد:
- کاربر یک بستهٔ کامل کد برای موتور جستجوی متنی (Positional Inverted Index، نرمال‌ساز فارسی، Two-pointer Phrase/Proximity Search، Faceting) ارائه داد.
- `BookImporter` باید یک **Console App** باشد که لاگ را هم‌زمان در کنسول و یک فایل **Markdown فارسی** بنویسد.
- ورودی باید از طریق **فراخوانی مستقیم `ITextExtractor`** (ProjectReference به `WordReader`) باشد، نه از فایل `output.txt`.
- ایندکس‌سازی باید **کاملاً در حافظه** باشد، بدون خواندن از دیتابیس در Startup.

سپس کاربر دستور اجرای نهایی را با ۴ الزام کلیدی داد: (۱) ذخیرهٔ `IBook` در دیتابیس کاملاً خالص/بدون ABP و به‌صورت متن خام، (۲) رفع خودکار تداخل نام‌گذاری، (۳) طراحی سادهٔ ساختار لاگ Markdown، (۴) موتور جستجو در یک Class Library کاملاً مجزا — و خواست بیلد را تا ۰ خطا پیش ببرم.

---

## ۷. گزارش پیاده‌سازی نهایی (این اجرا)

### ۷.۱. فایل‌های جدید

**پروژهٔ `SearchEngine`** (Class Library مستقل، بدون هیچ وابستگی NuGet یا Project Reference — طبق الزام ۴):
- `Models/AtomicBlockSection.cs`, `Models/PostingEntry.cs`, `Models/SearchResult.cs`
- `Analysis/PersianNormalizer.cs` (نرمال‌سازی + Stemmer سبک Lovins-style)
- `Indexing/PositionalInvertedIndex.cs`
- `Scoring/MatchiurityScoreFactory.cs`
- `Search/SearchService.cs` (Phrase/Proximity با Two-pointer), `Search/FacetService.cs`
- به `BookOnline.sln` اضافه شد (`dotnet sln add`).

**پروژهٔ `BookImporter`** (Console App):
- `Program.cs` — ارکستراسیون کامل: Extract → Map → Persist → Index → Search Demo → لاگ خلاصه
- `Mapping/DocumentToBookMapper.cs` — نگاشت `DocumentContent` → `Book`/`IPage`/`PragraphSection`/`BookIndex` (شامل بازتولید منطق پارس TOC که در `WordReader/Program.cs` بود، چون دیگر از آن مسیر عبور نمی‌کنیم)
- `Logging/MarkdownConsoleLogger.cs` — لاگر ساده: `Section`/`Info`/`Success`/`Warning`/`Error`/`Table`؛ هر پیام هم‌زمان در کنسول (رنگی) و در فایل `.md` (تیتر H2 برای هر مرحله، بولت برای پیام‌ها، جدول برای آمار پایانی) نوشته می‌شود.
- `Persistence/Entities/{Book,Page,Paragraph}Entity.cs` — POCO خالص، فقط متن خام
- `Persistence/ImportDbContext.cs` — **EF Core خالص روی SQLite**، بدون هیچ اثری از ABP (نه `AggregateRoot`، نه `IRepository<T>`، نه Unit-of-Work اضافه)؛ `EnsureCreated()` به‌جای Migration برای سادگی.
- `Persistence/BookRepository.cs` — تبدیل مستقیم `IBook` → Entityهای متن خام.

### ۷.۲. رفع تداخل نام‌گذاری (الزام ۲)

`AtomicBlockSection` و `MatchiurityScoreFactory` قدیمی در `Book.cs` **حذف شدند** (به‌همراه سه کلاس رندر HTML داخلی‌شان که اساساً باگ‌دار و مرده بودند: تبدیل رشتهٔ ضمنی یک `List<string>` به‌جای join کردن، و ارجاع به فیلد ناموجود `atomicBlock.Value`). این تصمیم چون:
1. آن کد اصلاً هیچ‌جای دیگر Solution استفاده نمی‌شد (بررسی شد).
2. `MatchiurityScoreFactory` قدیمی باگ واقعی داشت (پشت‌سرهم بازنویسی `score` در چند خط، که عملاً فقط آخرین شرط اثر داشت) — همان چیزی که نسخهٔ جدید در `SearchEngine.Scoring` با `HashSet` درست کرده.
3. نسخهٔ جدید و درست همین دو مفهوم اکنون در `SearchEngine.Models`/`SearchEngine.Scoring` (اسمبلی کاملاً جدا) زندگی می‌کند.

برای اینکه دامنه بدون این کلاس‌ها هم منسجم بماند، `PragraphSection` اکنون یک فیلد `RawText` مستقیم دارد (به‌جای لیستی از `AtomicBlockSection`) — دقیقاً همان «منبع حقیقت متن خام» که معماری `SearchEngine` هم بر پایهٔ آن است. یک تداخل مشابه در `Users/User.cs` هم بود (`HiLightsMark` به `AtomicBlockSection` ارجاع می‌داد)؛ به `HiLightsMark(int CharacterOffset)` تغییر یافت — هم‌راستا با مفهوم `Offset` در `PostingEntry`.

**فیکس جانبی که برای Build واقعاً لازم بود:** یک باگ واقعی در کد ارسالی موتور جستجو پیدا و رفع شد — `SearchResult` سه پراپرتی `required` دارد (`ParagraphId`, `BookId`, `PageNumber`) اما متد `BuildResult` در `SearchService` فقط دو تای اول را ست می‌کرد؛ کامپایل با خطای `CS9035` رد می‌شد. راه‌حل: افزودن `GetParagraphMeta(paragraphId)` به `PositionalInvertedIndex` که BookId/PageNumber هر پاراگراف را نگه می‌دارد.

**فیکس دفاعی کوچک دیگر:** `PhraseSearch` با یک واژهٔ تکی (`terms.Count == 1`) قبلاً `IndexOutOfRangeException` می‌داد (چون بدون بررسی مستقیم به `lists[1]` دسترسی پیدا می‌کرد)؛ یک مسیر جداگانه برایش اضافه شد.

### ۷.۳. الزام «۰ خطای بیلد» و یک محدودیت واقعی محیط (نه از کد این مکالمه)

قبل از هر تغییری، به‌عنوان خط پایه، `WordReader.csproj` را جدا با `dotnet build` تست کردم:
```
error MSB4803: The task "ResolveComReference" is not supported on the .NET Core version of MSBuild.
```
یعنی **این پروژه از همان ابتدا، مستقل از هر تغییری در این مکالمه، هرگز با CLI دات‌نت (`dotnet build`) قابل بیلد نبوده** — چون `ResolveComReference` (لازم برای `<COMReference>` به Word) فقط در MSBuild کامل (.NET Framework، همان چیزی که ویژوال استودیو داخلی استفاده می‌کند) پیاده‌سازی شده. چون `BookImporter` طبق تصمیم شما مستقیماً به `WordReader` ارجاع می‌دهد، همین محدودیت به آن هم منتقل می‌شود.

روی این ماشین Visual Studio 2022 Enterprise نصب است، پس از همان MSBuild کامل آن استفاده کردم:
```
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" BookOnline.sln -t:Build
```
**نتیجه: کل Solution (۹ پروژه، از جمله `BookImporter` و `SearchEngine` جدید) با ۰ خطا بیلد شد** (فقط Warningهای preexisting nullable در پروژه‌های دیگر که به این تسک ربطی ندارند). برای شفافیت، دوباره `dotnet build BookOnline.sln` را هم اجرا کردم: دقیقاً همان یک خطای MSB4803 (فقط از `WordReader.csproj`) ظاهر شد و **هیچ خطای دیگری**، که ثابت می‌کند این خطا کاملاً مستقل از کدهای این مکالمه و صرفاً یک محدودیت شناخته‌شدهٔ ابزار دات‌نت CLI با COM Interop است.

**جمع‌بندی صادقانه:** «۰ خطا با `dotnet build`» برای کل Solution ممکن نیست تا زمانی که `WordReader` به `Microsoft.Office.Interop.Word` COM-reference داشته باشد — این مستقل از هر پیاده‌سازی من است. معیار واقعی که استفاده شد: بیلد با MSBuild کامل (همان موتوری که Visual Studio خودتان هم زیر پوستهٔ F5/Build از آن استفاده می‌کند) با ۰ خطا.

### ۷.۴. تست اجرای واقعی (فراتر از بیلد)

چون فقط بیلد کافی نبود تا مطمئن شوم منطق واقعاً کار می‌کند، `BookImporter.dll` ساخته‌شده را مستقیم (`dotnet BookImporter.dll`، بدون دوباره از مسیر `dotnet build` رد شدن) روی همان فایل نمونهٔ موجود در دیسک (`C:\raha_part 02_08-02-1404-mini.docx`) اجرا کردم. نتیجهٔ واقعی:

| مورد | مقدار |
|---|---|
| تعداد صفحات استخراج‌شده | 50 |
| تعداد پاراگراف‌های نگاشت‌شده | 976 |
| تعداد آیتم‌های فهرست مطالب | 247 |
| تعداد پاراگراف‌های ایندکس‌شده | 976 |
| عنوان کتاب (خودکار از صفحهٔ اول) | «روزشمار سیره نبوی، عنوان جلد۲: از مهاجرت هاجر به مکه تا بغض رسول خدا از بت و بت‌پرستی» |
| دیتابیس | `BookImporter/Data/books.db` (SQLite، ~640KB) |
| لاگ | `BookImporter/Logs/import-*.md` (فارسی، فرمت‌بندی‌شده) |
| جستجوی آزمایشی | موفق (۱ نتیجه برای عبارت «روزشمار سیره») |
| مدت اجرا | ~۴٫۳ دقیقه (عمدتاً کندی ذاتی COM Automation در پیمایش ۵۰ صفحه با `doc.GoTo`) |

هیچ استثنایی رخ نداد؛ فایل لاگ Markdown و دیتابیس SQLite واقعاً و با محتوای فارسی صحیح تولید شدند. `Data/` و `Logs/` داخل `BookImporter` به `.gitignore` اضافه شدند تا این خروجی‌های اجرا (دیتابیس/لاگ) به‌اشتباه commit نشوند.

### ۷.۵. نکات باز/محدودیت‌های شناخته‌شده برای آینده
- `BookImporter` (و `WordReader`) فقط با MSBuild کامل/داخل Visual Studio قابل بیلد و اجراست، نه با CLI خالص دات‌نت — این یک محدودیت ساختاری preexisting در `WordReader.csproj` است، نه چیزی که در این مکالمه ایجاد شده باشد.
- شناسهٔ دامنهٔ کتاب (`BookId`) در حال حاضر ثابت (`1`) است چون این ابزار برای واردسازی یک فایل در هر اجرا طراحی شده؛ اگر قرار شود چند کتاب پیاپی وارد شوند، باید این مقدار پارامتری/خودکار شود.
- `SearchEngine.Analysis.PersianNormalizer.Tokenize` آفست هر واژه را با `rawText.IndexOf(token)` (اولین وقوع در کل متن) محاسبه می‌کند؛ برای واژه‌های تکراری در یک پاراگراف این آفست دقیق نیست. طبق دستور شما فقط خطاهای بیلد/کرش برطرف شدند، نه منطق‌های موجود در بستهٔ ارسالی؛ این مورد اگر برای Highlight دقیق لازم شد باید بازبینی شود.
- `FacetService` هنوز در مسیر Demo استفاده نشده (چون فقط برای اثبات ساخت پروژه لازم نبود)؛ آمادهٔ استفاده در گام بعدی (مثلاً یک لایهٔ API روی `BookImporter`/`SearchEngine`) است.

---

*تمام مراحل این بخش (۷) واقعاً روی این ماشین اجرا و تأیید شدند: بیلد کامل Solution با ۰ خطا (MSBuild کامل)، و یک اجرای واقعی end-to-end با تولید دیتابیس و لاگ.*
