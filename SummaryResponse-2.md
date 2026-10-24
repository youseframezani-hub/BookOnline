# تحلیل معماری سورس‌کد BookOnline

> این سند حاصل بررسی کامل کدبیس، بدون اعمال هیچ تغییری در کد، است. هدف آشنایی عمیق با معماری کلی راه‌حل (Solution) و به‌طور ویژه پروژه `WordReader` است.

---

## ۱. معرفی اجمالی کل پروژه و معماری کلی

### ۱.۱. ماهیت پروژه
`BookOnline` یک **Solution چندپروژه‌ای (.NET 8)** است که هدف نهایی آن ساخت یک **پلتفرم کتابخانه/کتاب‌فروشی آنلاین** به نظر می‌رسد؛ پلتفرمی که کاربران می‌توانند کتاب‌ها (احتمالاً با محوریت متون فارسی/دینی مانند «سیره نبوی») را مطالعه، جستجو، هایلایت، یادداشت‌گذاری و حتی با محتوای کتاب «گفت‌وگو» (چت‌بات مبتنی بر هوش مصنوعی) کنند. بخش قابل‌توجهی از پروژه هنوز در مرحلهٔ **اسکلت‌بندی دامنه (Domain Skeleton)** است و پیاده‌سازی واقعی محدود به یک بخش مشخص (`WordReader`) شده است.

### ۱.۲. نقشهٔ پروژه‌ها (بر اساس `BookOnline.sln`)

| پروژه | نوع | وضعیت پیاده‌سازی | نقش احتمالی |
|---|---|---|---|
| `BookOnline` | ASP.NET Core Web API | اسکلت خام (تمپلیت پیش‌فرض `WeatherForecastController`) | API اصلی/Backend for Frontend سرویس‌های اصلی |
| `Core/Domain/BookOnline.Domain` | Class Library (Domain) | مدل‌سازی دامنه، بدون منطق اجرایی واقعی | هستهٔ دامنه: کتاب، کاربر، هایلایت/یادداشت/بوکمارک، جستجو، چت‌بات |
| `LibraryOnline.Domain` | Class Library (Domain) | اسکلت خام | مدل فروشگاه/کتابخانه، دسته‌بندی، صفحهٔ فرود فروشگاه |
| `FinancialBookStore.Domain` | Class Library (Domain) | اسکلت خام | اشتراک‌ها، سفارش‌ها، قیمت‌گذاری، پرداخت |
| `AdminLibraryOnlins.Domain` | Class Library (Domain) | اسکلت خام | نقش‌ها و کاربران مدیریتی (ادمین، صاحب فروشگاه، مشتری) |
| `LandingSite` (`LandingPage`) | ASP.NET Core Razor Pages | نسبتاً کامل‌تر (فرم تماس، ایمیل/پیامک) | سایت فرود/معرفی (صفحات Contact، Rules، PublicQuestions و ...) |
| `WordReader` | ASP.NET Core Razor Pages + Console-style runner | **تنها پروژه با منطق پردازشی واقعی و کامل** | استخراج ساختاریافتهٔ محتوای فایل‌های Word به مدل داده و JSON |
| `BookImporter` | در `sln` رجیستر شده اما **در فایل‌سیستم وجود ندارد** | ناموجود/حذف‌شده — در حال طراحی/پیاده‌سازی توسط این مکالمه | پل بین خروجی `WordReader` و دامنهٔ `BookOnline.Domain` + موتور Full-Text Search |

### ۱.۳. معماری کلی
- الگوی کلی، تفکیک **Domain-per-bounded-context** است: هر حوزهٔ کسب‌وکار (کتابخانه، مالی، مدیریت ادمین، هستهٔ کتاب) در یک پروژهٔ Domain مجزا مدل شده — رگه‌ای از الهام‌گیری از DDD (Domain-Driven Design) با Value Objectها (`record`هایی مثل `PageId`, `BookId`, `UserId`) و Aggregateهای اولیه.
- هیچ لایهٔ Application/Infrastructure (مثل EF Core DbContext، Repository، API واقعی) بین این Domainها و پروژه‌های اجرایی (`BookOnline`, `LandingSite`, `WordReader`) هنوز پیاده‌سازی نشده؛ فقط ارجاعات پروژه‌ای در `sln` تعریف شده‌اند ولی به‌جز `WordReader`، بقیه اجرا/استفادهٔ واقعی ندارند.
- `LandingSite` تنها بخشی‌ست که با دنیای بیرون (ایمیل/پیامک برای فرم تماس) یکپارچه شده و مستقل از بقیهٔ Domainهاست.
- `WordReader` به هیچ‌یک از پروژه‌های Domain ارجاع مستقیم ندارد و عملاً یک **سرویس/ابزار مستقل استخراج داده** است که خروجی آن (JSON ساختاریافته/شیء زنده `DocumentContent`) قاعدتاً باید توسط پروژهٔ `BookImporter` به مدل `BookOnline.Domain.Books` نگاشت شود.

---

## ۲. آنالیز تفصیلی پروژه WordReader

### ۲.۱. هدف پروژه
`WordReader` یک اپلیکیشن ASP.NET Core (Razor Pages) با **هدف واقعی یک ابزار ETL/Extractor** است: یک فایل Word (`.docx`) را باز می‌کند، صفحه‌به‌صفحه محتوای آن (متن، استایل، جدول، تصویر، پاورقی، هدر/فوتر) را استخراج و در یک مدل دادهٔ ساختاریافته (`DocumentContent`) بازسازی می‌کند، سپس آن را به‌صورت JSON سریالایز و در فایل `output.txt` ذخیره می‌کند. رابط کاربری Razor Pages آن (`Index.cshtml`) هنوز تمپلیت پیش‌فرض است و در عمل استفاده نمی‌شود — منطق واقعی در `Program.cs` به‌صورت یک اجرای همزمان با بالا آمدن اپلیکیشن (`RunWordExtractor`) فراخوانی می‌شود.

### ۲.۲. وابستگی‌های کلیدی (`WordReader.csproj`)
```xml
<TargetFramework>net8.0</TargetFramework>
<COMReference Include="Microsoft.Office.Interop.Word" .../>
<PackageReference Include="DocumentFormat.OpenXml" Version="3.5.1" />
<PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" .../>
```
دو مسیر کاملاً متفاوت برای خواندن فایل Word هم‌زمان استفاده می‌شود:
1. **COM Interop با `Microsoft.Office.Interop.Word`** (نیازمند نصب واقعی Microsoft Word روی سیستم اجرا‌کننده، صرفاً Windows) — مسئول استخراج متن، صفحه‌بندی (pagination واقعی Word)، جدول، هدر/فوتر و پاورقی.
2. **`DocumentFormat.OpenXml`** (کتابخانهٔ مدیریت‌شدهٔ خالص، Cross-platform) — صرفاً برای استخراج بایت‌های واقعی تصاویر (`Blip` در XML) و همچنین در `ExtractStyleService` برای استخراج دقیق استایل‌ها از `styles.xml`.

این ترکیب نشان می‌دهد نویسنده به دلیل محدودیت‌های OpenXML (نداشتن صفحه‌بندی واقعی همانند رندر Word) از Interop برای «صفحه» استفاده کرده، اما برای داده‌های ساختاریافته (تصاویر، استایل‌ها) به OpenXML رجوع کرده که پایدارتر و بدون وابستگی به نصب Word است.

### ۲.۳. مدل‌های داده (`Services/Models`)

#### `DocumentContent.cs`
مدل ریشه (Root Model) خروجی است:
- `DocumentContent`: شامل `FilePath`, `Pages` (لیست `PageContent`), `Styles` (دیکشنری نام سبک‌ها), `Index` (فهرست مطالب/`IndexItem`), `Title`.
- `PageContent`: نمایانگر یک صفحه با `Text`, `HtmlContent`, `Paragraphs`, `Images`, `Tables`, `Header`, `Footer`, `Footnotes`. متد `RebuildHtml()` یک HTML ساده (تگ `<p>` با کلاس CSS مشتق از نام سبک) از پاراگراف‌ها و پاورقی‌ها می‌سازد.
- `ParagraphSpan`: واحد اصلی متن — چون یک پاراگراف Word می‌تواند بین چند صفحه «شکسته» شود، این کلاس با `ParagraphIndex` (شمارهٔ پاراگراف اصلی) و `FragmentIndex` (شمارهٔ تکه) امکان بازسازی پاراگراف‌های چندصفحه‌ای را فراهم می‌کند. فیلدهای دامنه‌محور و اختصاصی محتوا (`Date`, `Ravi`, `Story`, `Fehrest`) نیز به آن اضافه شده — این فیلدها **مخصوص ساختار محتوایی کتاب «سیره نبوی»** هستند و در `Program.cs` بر اساس نام سبک پاراگراف پر می‌شوند.
- `ImageContent`, `TableContent`/`TableRowContent`/`TableCellContent`: مدل‌های تصویر و جدول.
- `FootnoteReferenceContext`: زمینهٔ ارجاع پاورقی (شماره ارجاع، کلمهٔ میزبان و دو کلمهٔ پیش از آن).
- `HistoricalDate` / `HistoricalDatePart` / `StandardDate`: مدلی برای تاریخ‌های تاریخی (هجری/شمسی/میلادی) — تعریف شده اما در کد فعلی استفاده‌ای از آن دیده نشد.
- `CssClassName`: یک Utility استاتیک که نام سبک Word را نرمال‌سازی کرده و به کلاس CSS معتبر (`style-xxx`) تبدیل می‌کند.

#### `Style.cs`
مدلی غنی و مستقل از Word برای نمایش کامل یک سبک: نوع (`Paragraph`/`Character`/`Table`/`Numbering`)، وراثت سبک (`BasedOnStyleId`)، اطلاعات فونت (`FontStyleInfo`)، پاراگراف (`ParagraphStyleInfo`) و حاشیه (`BorderStyleInfo`).

### ۲.۴. منطق پردازشی و کلاس‌های کلیدی (`Services/WordExtractor`)

#### `ITextExtractor` / `InteropTextExtractor`
قلب پروژه. یک پیاده‌سازی حجیم (۱۵۰۰+ خط):
1. **مدیریت چرخهٔ عمر COM**: `Word.Application` نامرئی ساخته می‌شود؛ تمام اشیاء COM در بلوک‌های `try/finally` با `ReleaseComObject` (`Marshal.FinalReleaseComObject`) آزاد می‌شوند تا از نشت حافظه و پروسه‌های پنهانِ WINWORD.EXE جلوگیری شود.
2. **جمع‌آوری اطلاعات خام سطح سند**: `ReadParagraphInfos`, `ReadTableInfos`, `ReadImageInfos` (ترکیب Interop برای موقعیت و OpenXML برای بایت تصویر واقعی).
3. **پیمایش صفحه‌به‌صفحه**: `GetPageRange` (با `doc.GoTo`)، سپس `AddParagraphFragmentsForPage`/`AddTablesForPage`/`AddImagesForPage`/`AddHeaderAndFooterForPage`/`AddFootnotesForPage` با الگوی تقاطع بازهٔ کاراکتری (`RangesIntersect`).
4. **پاکسازی متن**: متدهای `Clean*Text` برای نرمال‌سازی کاراکترهای کنترلی Word.
5. **مدیریت خطا**: بخش‌های حساس در `try/catch` محافظت شده‌اند تا خطای یک المان کل استخراج را متوقف نکند.

#### `ExtractStyleService` (در `StyleExtractor.cs`)
مسیر مستقل و کاملاً OpenXML-محور برای استخراج دقیق سبک‌ها، شامل حل رنگ‌های Theme و **حل وراثت سبک‌ها** (`ResolveInheritance`/`Merge*`) به‌صورت بازگشتی، مشابه cascading در CSS.

#### `IHtlmRenderer`
Interface کاملاً خالی — Placeholder برای رندر HTML آینده (با غلط تایپی «Htlm»).

### ۲.۵. جریان کار کامل (`Program.cs`)
منطق واقعی در تابع محلی `RunWordExtractor` هنگام Startup اجرا می‌شود: مسیر فایل هارد‌کدشده (`C:\raha_part 02_08-02-1404-mini.docx`) استخراج می‌شود، بر اساس نام سبک‌های فارسی (`فهرست`, `تاریخ`, `داستان`, `راوی`, `TOC 1`) فیلدهای دامنه‌محور و فهرست مطالب پر می‌شوند، و در پایان با `System.Text.Json` (با `UnsafeRelaxedJsonEscaping`) در `output.txt` ذخیره می‌شود.

### ۲.۶. الگوهای طراحی مشاهده‌شده
- Interface segregation ساده (`ITextExtractor`, `IExtractStyleService`, `IHtlmRenderer`).
- Resource management با `try/finally` + COM release.
- Range-intersection pattern برای تفکیک per-page.
- مدل غنی مستقل از فناوری منبع (`Style.cs`, `DocumentContent.cs`).
- Style inheritance resolution در `ExtractStyleService`.

---

## ۳. نحوهٔ ارتباط WordReader با سایر بخش‌ها

- **`WordReader` به هیچ پروژهٔ دیگری در Solution ارجاع مستقیم ندارد** — پروژه‌ای کاملاً ایزوله.
- خروجی آن یک فایل JSON مسطح روی دیسک (`output.txt`) است؛ در حال حاضر هیچ مکانیزم یکپارچه‌سازی خودکاری بین `WordReader` و بقیهٔ سیستم وجود ندارد.
- ارتباط مفهومی قوی با `Core/Domain/BookOnline.Domain/Books/Book.cs` وجود دارد: `IPage`, `PragraphSection`, `AtomicBlockSection`, `BookIndex`, `BookIndexItem` به‌شدت شبیه ساختار خروجی `WordReader` (`PageContent`, `ParagraphSpan`, `IndexItem`) هستند، با دانه‌بندی ریزتر در سطح کلمه.
- پروژهٔ `BookImporter` که در `.sln` ثبت شده اما در دیسک نبود، دقیقاً همان «حلقهٔ گمشده»ای است که باید این نگاشت را انجام دهد؛ ساخت آن موضوع مکالمهٔ فعلی است.

**جمع‌بندی رابطه:** `WordReader` امروز یک ابزار offline/manual برای تبدیل یک فایل Word مشخص به JSON/شیء زنده است، نه یک میکروسرویس یکپارچه.

---

## ۴. نکات کلیدی و چالش‌های معماری شناسایی‌شده

1. **وابستگی حیاتی به Microsoft Word نصب‌شده روی Windows (COM Interop)**: `InteropTextExtractor` برای اجرا به نصب واقعی Word نیاز دارد؛ این با `Dockerfile` مبتنی بر ایمیج لینوکسی `WordReader` ناسازگار است — تصویر Docker در Runtime با خطا متوقف می‌شود.
2. **مسیر فایل و منطق کسب‌وکار Hardcoded** در `Program.cs` (مسیر فایل و نگاشت نام سبک‌های فارسی).
3. **اجرای منطق سنگین در بدنهٔ Startup** به‌جای یک Endpoint/Job مجزا.
4. **دو مسیر موازی و ناهماهنگ برای استخراج سبک** (`document.Styles` ساده در Interop، و `ExtractStyleService` غنی در OpenXML) که هنوز به هم متصل نشده‌اند.
5. **حلقهٔ گمشدهٔ `BookImporter`** — موضوع این مکالمه.
6. **کیفیت/ثبات مدل دامنه در پروژه‌های دیگر**: بسیاری از سرویس‌ها/مدل‌ها (`BookContentService`, `SearchBookContentService`, `ChatBotWithBookService`, و چند کلاس در `LibraryOnline.Domain`/`FinancialBookStore.Domain`/`AdminLibraryOnlins.Domain`) صرفاً Placeholder هستند و چند غلط تایپی (`Subsciription`, `BookUploeded`, `Worde`/`Wodrs`, `Htlm`) و باگ نحوی (`AtomicBlockSectionHtmlRender.HtmlRender`) در آن‌ها دیده می‌شود.
7. **عدم وجود تست و لایهٔ Persistence** در کل Solution.
8. **مقیاس‌پذیری و عملکرد Interop**: باز کردن Word و پیمایش `doc.GoTo` برای هر صفحه، برای کتاب‌های حجیم کند است و Word Interop thread-affinity دارد.

---

## ۵. درک من از تسک: پیاده‌سازی BookImporter

### ۵.۱. هدف
ساخت پروژهٔ `BookImporter` با دو مسئولیت اصلی:
1. **Mapping/ETL**: تبدیل خروجی `WordReader` (`DocumentContent` → `PageContent` → `ParagraphSpan`) به مدل دامنهٔ `Core/Domain/BookOnline.Domain/Books/Book.cs` (`IBook`, `BookIndex`/`BookIndexItem`, `IPage`, `PragraphSection`, `AtomicBlockSection` و مدل‌های کمکی مثل `PageId`/`BookId` که فعلاً Placeholder خالی هستند).
2. **غنی‌سازی دامنه با Full-Text Search**: فراتر از منطق ابتدایی فعلی `AtomicBlockSection.Contains`/`MatchiurityScoreFactory` (که بدون ایندکس واقعی، صرفاً بر پایهٔ چک کلمات مجاور کار می‌کند).

### ۵.۲. روش کار توافق‌شده
کاربر قطعه‌کدهای پراکندهٔ آماده‌شده در مرورگر را مرحله‌به‌مرحله ارسال می‌کند؛ تا اطلاع بعدی **کدی نوشته/تغییر داده نمی‌شود** — فقط دریافت، تحلیل و مستندسازی در همین فایل انجام می‌گیرد.

### ۵.۳. قطعه‌کد اول دریافت‌شده: «موتور جستجوی متنی» (پیش از ورود LLM)
کاربر یک بستهٔ کامل کد برای یک **موتور Full-Text Search سبک و کاملاً کدنویسی‌شده (بدون LLM/Embedding)** ارائه داد، با معماری زیر:

- **معماری پایه**: Positional Inverted Index. «منبع حقیقت» متن خام (`RawText`) است؛ موقعیت دقیق هر واژه (`Position`) و آفست کاراکتری (`Offset`) برای هر واژه در هر پاراگراف نگه‌داری می‌شود تا جستجوی عبارتی (Phrase) و مجاورتی (Proximity) ممکن شود.
- **واحد مبنای مجاورت**: پاراگراف — مجاورت هرگز از مرز پاراگراف عبور نمی‌کند؛ مجاورت‌ها در ساختار داده ذخیره نمی‌شوند، بلکه در زمان جستجو با **الگوریتم Two-pointer** روی آرایه‌های `Positions` (که باید همیشه صعودی/مرتب بمانند) محاسبه می‌شوند.
- **ساختار فایل‌های پیشنهادی** (namespace ریشه `SearchEngine`):
  - `Models/AtomicBlockSection.cs` — نسخهٔ **جدید و ساده‌شدهٔ** این کلاس (فقط `Term`, `Position`, `Offset`, `ParagraphId`) — **متفاوت و در تداخل نام با** `AtomicBlockSection` موجود در `BookOnline.Domain.Books` (`Book.cs`) که ساختار غنی‌تری دارد (`Worde`, `NextWodrs`, `PreviousWodrs`, ...).
  - `Models/PostingEntry.cs` — واحد Posting List: `ParagraphId`, `BookId` و `PageNumber` (به‌عنوان متادیتای Facet)، و دو لیست موازی و صعودی `Positions`/`Offsets`.
  - `Models/SearchResult.cs` — نتیجهٔ نهایی جستجو با `Score` و لیست `Highlights`.
  - `Analysis/PersianNormalizer.cs` — نرمال‌سازی حروف عربی/فارسی (`ك→ک`, `ي→ی`, حذف نیم‌فاصله و اعراب)، حذف Stop Words فارسی، و ریشه‌یابی سبک (Stemming) به روش Lovins-style (حذف پسوندهای رایج مثل `ها`, `تر`, `هایتان` ...) بدون وابستگی به کتابخانه‌های سنگین (مثل Hazm). متد `Tokenize` خروجی `(Term, Position, Offset)` تولید می‌کند.
  - `Indexing/PositionalInvertedIndex.cs` — نگهداری `ConcurrentDictionary<string, List<PostingEntry>>` برای ایندکس معکوس و `ConcurrentDictionary<int, string>` برای متن خام هر پاراگراف؛ متد `AddParagraph` هر پاراگراف را توکنایز و پوسه‌های آن را می‌سازد.
  - `Indexing/IndexBuilderService.cs` — **در طراحی اولیهٔ ارسالی**، یک `BackgroundService` (ASP.NET Core Hosted Service) بود که با `IServiceScopeFactory` یک `AppDbContext` می‌ساخت و پاراگراف‌ها را از جدول `Paragraphs` (با EF Core، `AsAsyncEnumerable`) در زمان Startup می‌خواند و ایندکس می‌کرد. *(این بخش طبق تصمیم جدید کاربر در بخش ۶ بازطراحی می‌شود — به بخش ۶.۳ مراجعه شود.)*
  - `Scoring/MatchiurityScoreFactory.cs` — نسخهٔ **جدید و اصلاح‌شدهٔ** این کلاس (که در `Book.cs` موجود هم با همین نام اما منطق متفاوت/باگ‌دار وجود دارد): `enum MatchKind {Exact=60, PreviousAdjacent=25, NextAdjacent=15}` و وزن‌دهی ۶۰٪/۲۵٪/۱۵٪؛ با `HashSet<MatchKind>` از بازشمارش/بازنویسی امتیاز جلوگیری می‌کند (رفع باگ نسخهٔ قبلی که با `List` و انتساب‌های پیاپی `score = ...` عملاً فقط آخرین شرط را نگه می‌داشت).
  - `Search/SearchService.cs` — `PhraseSearch` (تقاطع Posting Listها + بررسی توالی دقیق مواضع با `BinarySearch`) و `ProximitySearch` (Two-pointer روی `ParagraphId`های دو Posting List، سپس Two-pointer دوم روی `Positions` داخل هر پاراگراف مشترک با `HasClosePositions`).
  - `Search/FacetService.cs` — گروه‌بندی نتایج بر اساس `BookId` با شمارش تعداد و مجموعهٔ یکتای شماره صفحات (`HashSet<int> PageNumbers`)؛ توصیه شده ابتدا مرتب‌سازی بر اساس `Score` و سپس Facet روی نتایج برتر انجام شود.
- **تخمین کارایی**: برای ۱۵ جلد کتاب، زمان ایندکس‌سازی کامل زیر ۲ دقیقه تخمین زده شده است (در طراحی اولیهٔ Web/BackgroundService).

### ۵.۴. نکات باز که طی همین قطعه‌کد شناسایی شدند (پیش از تصمیمات بخش ۶)
- تداخل نام‌گذاری `AtomicBlockSection` و `MatchiurityScoreFactory` بین `Book.cs` موجود و بستهٔ جدید `SearchEngine.*`.
- وابستگی طراحی اولیه به `AppDbContext`/جدول `Paragraphs` که در هیچ‌جای فعلی Solution تعریف نشده (اولین لایهٔ Persistence واقعی).
- این بسته هنوز شامل بخش Mapping اصلی (خروجی `WordReader` → `IBook`/`IPage`) نبود؛ صرفاً موتور جستجو بود.
- محل قرارگیری پروژه در Solution (مستقل/داخل `BookImporter`/داخل `BookOnline.Domain`).

---

## ۶. تصمیمات معماری تثبیت‌شدهٔ BookImporter (به‌روزرسانی پس از پاسخ کاربر)

کاربر سه مورد از نکات باز بخش ۵.۴ را صراحتاً مشخص کرد. **این بخش صرفاً مستندسازی تصمیمات است؛ هنوز هیچ کدی نوشته یا فایلی در پروژه ایجاد/تغییر داده نشده.**

### ۶.۱. نوع پروژه: Console Application
`BookImporter` یک **پروژهٔ کنسولی (.NET Console App)** خواهد بود، نه Web/Worker Service/Class Library. خروجی لاگِ اجرا باید **هم‌زمان** در دو مقصد نوشته شود:
1. کنسول (خروجی استاندارد، برای مشاهدهٔ زنده حین اجرا)
2. یک **فایل Markdown (`.md`)** — و متن لاگ باید **به زبان فارسی** نوشته شود.

این یعنی باید یک Logger/Writer سفارشی (نه صرفاً `Console.WriteLine`) طراحی شود که هر پیام را هم‌زمان به کنسول و هم به‌صورت append به یک فایل `.md` (با فرمت‌بندی خوانا، مثلاً تیتر برای هر مرحله، لیست/جدول برای خلاصهٔ آمار import) بنویسد.

### ۶.۲. منبع ورودی: فراخوانی مستقیم `ITextExtractor`
`BookImporter` به‌جای خواندن فایل از پیش تولیدشدهٔ `output.txt`، **مستقیماً به پروژهٔ `WordReader` ارجاع (ProjectReference) می‌دهد** و از طریق اینترفیس `ITextExtractor` (پیاده‌سازی `InteropTextExtractor` در `WordReader/Services/WordExtractor/ITextExtractor.cs`) متد `Extract(filePath)` را فراخوانی می‌کند تا مستقیماً یک نمونهٔ زندهٔ `DocumentContent` (نه JSON سریالایز‌شده) دریافت کند و آن را به مدل دامنهٔ `Book.cs` نگاشت دهد.

**پیامد مهم و قابل‌توجه (طبق بخش ۴، بند ۱ همین سند):** چون `WordReader.csproj` یک `COMReference` به `Microsoft.Office.Interop.Word` دارد، هر پروژه‌ای که مستقیماً به `WordReader` ارجاع بدهد — از جمله `BookImporter` — **فقط روی ویندوزی که Microsoft Word واقعی روی آن نصب است** قابل اجراست. این محدودیت از `WordReader` به `BookImporter` هم منتقل می‌شود؛ یعنی `BookImporter` را نمی‌توان در یک کانتینر Linux اجرا کرد.

### ۶.۳. ایندکس: کاملاً در حافظه (In-Memory)، بدون دیتابیس
برخلاف طراحی اولیهٔ بستهٔ «موتور جستجو»ی ارسال‌شده در بخش ۵.۳ — که فرض می‌کرد متن خام (`RawText`) در یک دیتابیس (`AppDbContext` با جدول `Paragraphs`) ذخیره شده و `IndexBuilderService` (یک `BackgroundService` در یک اپلیکیشن Web/Host) آن را در زمان بوت‌آپ از دیتابیس می‌خواند و ایندکس می‌سازد — تصمیم نهایی این است که:
- **هیچ لایهٔ Persistence/EF Core/`AppDbContext`ای در این مرحله ساخته نمی‌شود.**
- ایندکس‌سازی (`PositionalInvertedIndex`) مستقیماً از خروجی زندهٔ `ITextExtractor.Extract(...)` (پس از نگاشت به `IBook`/`IPage`/`PragraphSection`) در حافظهٔ همان پردازش کنسولی ساخته می‌شود، نه از یک منبع دیتابیسی.
- بنابراین طراحی `IndexBuilderService` (که در بخش ۵.۳ به‌عنوان `BackgroundService` با وابستگی به `IServiceScopeFactory` و `AppDbContext` ارائه شد) باید **بازطراحی شود** تا به‌جای خواندن از دیتابیس، مستقیماً پاراگراف‌های حاصل از Mapping (خروجی `ITextExtractor` → مدل دامنه) را دریافت و ایندکس کند؛ منطق و ساختار خودِ `PositionalInvertedIndex`, `PostingEntry`, `PersianNormalizer`, `MatchiurityScoreFactory` (نسخهٔ جدید در `SearchEngine.Scoring`)، و الگوریتم Two-pointer در `SearchService`/`FacetService` بدون تغییر معنایی قابل استفاده باقی می‌مانند — فقط منبع تغذیهٔ ایندکس عوض می‌شود.

### ۶.۴. جمع‌بندی جریان کار نهایی `BookImporter` (بر اساس تصمیمات فوق)
```
[ورودی: مسیر فایل .docx]
        ↓
ITextExtractor.Extract(filePath)   ← ProjectReference مستقیم به WordReader (نیازمند Word نصب‌شده روی Windows)
        ↓
DocumentContent (Pages/Paragraphs/Styles/Index)
        ↓
Mapper (بخش هنوز دریافت‌نشده) → IBook / IPage / PragraphSection / AtomicBlockSection (مدل BookOnline.Domain.Books)
        ↓
PositionalInvertedIndex.AddParagraph(...)   ← کاملاً در حافظه، بدون DbContext
        ↓
لاگ فارسی هم‌زمان در Console و یک فایل .md   (خلاصهٔ آمار: تعداد صفحات/پاراگراف‌ها/واژه‌های ایندکس‌شده، خطاها و ...)
```

### ۶.۵. نکات همچنان باز
- محل ذخیرهٔ نمونهٔ ساخته‌شدهٔ `IBook`/ایندکس پس از پایان اجرای Console App: آیا صرفاً برای همان اجرا در حافظه می‌ماند (Import یک‌باره برای تست/دمو)، یا باید در جایی (فایل/سریالایز) هم Persist شود؟ هنوز مشخص نشده.
- رفع تداخل نام‌گذاری `AtomicBlockSection`/`MatchiurityScoreFactory` بین `Book.cs` موجود و بستهٔ جدید `SearchEngine.*` (طبق بخش ۵.۴) هنوز پاسخ داده نشده.
- ساختار دقیق فایل لاگ Markdown (قالب دقیق تیتربندی/جدول‌ها) هنوز مشخص نشده و در قطعه‌کد بعدی روشن خواهد شد.
- محل قرارگیری خودِ کلاس‌های موتور جستجو (`SearchEngine.*`) نسبت به پروژهٔ کنسولی `BookImporter`: پوشه/namespace داخل همان پروژه یا Class Library جدا؟ هنوز مشخص نشده.

---

*این سند صرفاً حاصل مشاهده، تحلیل و مستندسازی کد/تصمیمات ارائه‌شده توسط کاربر است؛ هیچ کدی نوشته و هیچ فایلی در پروژه ایجاد/تغییر داده نشده است.*
