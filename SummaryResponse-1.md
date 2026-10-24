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
| `BookImporter` | در `sln` رجیستر شده اما **در فایل‌سیستم وجود ندارد** | ناموجود/حذف‌شده | به‌احتمال قوی قرار بوده پل بین خروجی `WordReader` و دامنهٔ `BookOnline.Domain` باشد |

### ۱.۳. معماری کلی
- الگوی کلی، تفکیک **Domain-per-bounded-context** است: هر حوزهٔ کسب‌وکار (کتابخانه، مالی، مدیریت ادمین، هستهٔ کتاب) در یک پروژهٔ Domain مجزا مدل شده — رگه‌ای از الهام‌گیری از DDD (Domain-Driven Design) با Value Objectها (`record`هایی مثل `PageId`, `BookId`, `UserId`) و Aggregateهای اولیه.
- هیچ لایهٔ Application/Infrastructure (مثل EF Core DbContext، Repository، API واقعی) بین این Domainها و پروژه‌های اجرایی (`BookOnline`, `LandingSite`, `WordReader`) هنوز پیاده‌سازی نشده؛ فقط ارجاعات پروژه‌ای در `sln` تعریف شده‌اند ولی به‌جز `WordReader`، بقیه اجرا/استفادهٔ واقعی ندارند.
- `LandingSite` تنها بخشی‌ست که با دنیای بیرون (ایمیل/پیامک برای فرم تماس) یکپارچه شده و مستقل از بقیهٔ Domainهاست.
- `WordReader` به هیچ‌یک از پروژه‌های Domain ارجاع مستقیم ندارد (در ادامه توضیح داده می‌شود) و عملاً یک **سرویس/ابزار مستقل استخراج داده** است که خروجی آن (JSON ساختاریافته) قاعدتاً باید توسط پروژه‌ای مثل `BookImporter` (که موجود نیست) به مدل `BookOnline.Domain.Books` نگاشت شود.

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
- `ParagraphSpan`: واحد اصلی متن — چون یک پاراگراف Word می‌تواند بین چند صفحه «شکسته» شود، این کلاس با `ParagraphIndex` (شمارهٔ پاراگراف اصلی) و `FragmentIndex` (شمارهٔ تکه) امکان بازسازی پاراگراف‌های چندصفحه‌ای را فراهم می‌کند. فیلدهای دامنه‌محور و اختصاصی محتوا (`Date`, `Ravi`, `Story`, `Fehrest`) نیز به آن اضافه شده — این فیلدها **مخصوص ساختار محتوایی کتاب «سیره نبوی»** هستند (تاریخ رویداد، راوی، عنوان داستان، عنوان فهرست) و در `Program.cs` بر اساس نام سبک پاراگراف پر می‌شوند؛ یعنی مدل عمومی به‌صورت غیرعمومی (hardcoded) به یک کتاب خاص گره خورده.
- `ImageContent`, `TableContent`/`TableRowContent`/`TableCellContent`: مدل‌های تصویر و جدول.
- `FootnoteReferenceContext`: زمینهٔ ارجاع پاورقی (شماره ارجاع، کلمهٔ میزبان و دو کلمهٔ پیش از آن) که برای اتصال دقیق‌تر پاورقی به متن اصلی به کار می‌رود.
- `HistoricalDate` / `HistoricalDatePart` / `StandardDate`: مدلی برای تاریخ‌های تاریخی (هجری/شمسی/میلادی) — تعریف شده اما در کد فعلی استفاده‌ای از آن دیده نشد؛ احتمالاً برای پردازش آتی فیلد `Date` در نظر گرفته شده.
- `CssClassName`: یک Utility استاتیک که نام سبک Word را نرمال‌سازی کرده و به کلاس CSS معتبر (`style-xxx`) تبدیل می‌کند.

#### `Style.cs`
مدلی غنی و مستقل از Word برای نمایش کامل یک سبک (Style): نوع (`Paragraph`/`Character`/`Table`/`Numbering`)، وراثت سبک (`BasedOnStyleId`)، اطلاعات فونت (`FontStyleInfo`)، پاراگراف (`ParagraphStyleInfo` شامل تراز، تورفتگی، فاصله‌گذاری خط، جهت متن RTL/LTR) و حاشیه (`BorderStyleInfo`). این مدل کاملاً مستقل از Interop/OpenXML طراحی شده تا لایهٔ ارائه (رندر HTML/CSS) بتواند بدون وابستگی به Word با آن کار کند.

### ۲.۴. منطق پردازشی و کلاس‌های کلیدی (`Services/WordExtractor`)

#### `ITextExtractor` / `InteropTextExtractor`
قلب پروژه. یک پیاده‌سازی حجیم (۱۵۰۰+ خط) با الگوی زیر:

1. **مدیریت چرخهٔ عمر COM**: یک نمونه از `Word.Application` (نامرئی، `ScreenUpdating=false`) ساخته و سند را Read-Only باز می‌کند. تمام اشیاء COM (`Range`, `Paragraph`, `Style`, `Shape`, ...) با دقت در بلوک‌های `try/finally` با متد استاتیک `ReleaseComObject` (`Marshal.FinalReleaseComObject`) آزاد می‌شوند تا از **نشت حافظه و باقی‌ماندن پروسه‌های پنهانِ WINWORD.EXE** جلوگیری شود — الگوی صحیح و شناخته‌شده برای کار با COM Interop در دات‌نت.
2. **جمع‌آوری اطلاعات خام سطح سند** (پیش از تفکیک صفحه‌به‌صفحه):
   - `ReadParagraphInfos`: برای هر پاراگراف سند، محدودهٔ کاراکتری (`Start`/`End`) و نام سبک را می‌خواند.
   - `ReadTableInfos`: محدودهٔ هر جدول و سلول‌های هر ردیف را می‌خواند.
   - `ReadImageInfos`: هم `InlineShapes` و هم `Shapes` (تصاویر شناور) را پیمایش کرده و محدودهٔ آن‌ها را ثبت می‌کند؛ برای گرفتن بایت واقعی تصویر به `ReadOpenXmlImages` (OpenXML، مستقل از Interop) رجوع می‌کند که با پیمایش `Blip` درون XML سند، `ImagePart` مرتبط را باز و بایت‌ها را می‌خواند.
3. **پیمایش صفحه‌به‌صفحه** (`GetTotalPages` سپس حلقه از ۱ تا totalPages):
   - `GetPageRange`: با استفاده از `doc.GoTo(wdGoToPage)` شروع هر صفحه و صفحهٔ بعد را پیدا کرده و بازهٔ کاراکتری صفحه را می‌سازد (تکنیک تفاضل ابتدای صفحهٔ فعلی و بعدی).
   - `AddParagraphFragmentsForPage`: با تقاطع بازهٔ پاراگراف‌ها و بازهٔ صفحه (`RangesIntersect`)، تکه (Fragment) متعلق به آن صفحه را استخراج و پرچم `IsSplitFromMultiPageParagraph` را برای پاراگراف‌های چندصفحه‌ای تنظیم می‌کند؛ `CalculateFragmentIndex` با استفاده از `WdInformation.wdActiveEndAdjustedPageNumber` شمارهٔ تکه را محاسبه می‌کند.
   - `AddTablesForPage` و `AddImagesForPage`: همان منطق تقاطع بازه برای جدول و تصویر.
   - `AddHeaderAndFooterForPage`: بر اساس شمارهٔ Section سند، هدر/فوتر مناسب (اول صفحه/زوج/فرد/عادی) را با `GetHeaderForPage`/`GetFooterForPage` پیدا کرده و متن آن را (شامل متن مستقیم، فیلدهایی مثل شمارهٔ صفحه، یا متن داخل Shape/TextBox) استخراج می‌کند.
   - `AddFootnotesForPage`: برای هر پاورقی سند بررسی می‌کند که آیا مرجع آن در صفحهٔ جاری است؛ در صورت تطابق، متن پاورقی به همراه `FootnoteReferenceContext` (با `GetWordBeforeReference` که با `MoveStart/MoveEnd` بر مبنای واحد کلمه، کلمات پیش از ارجاع را می‌گیرد) استخراج می‌شود.
   - در پایان `page.RebuildHtml()` صدا زده می‌شود تا HTML اولیهٔ صفحه ساخته شود.
4. **پاکسازی متن (Text cleaning)**: چند متد اختصاصی (`CleanPageText`, `CleanParagraphFragmentText`, `CleanCellText`, `CleanHeaderFooterText`) کاراکترهای کنترلی خاص Word (`\r` پایان پاراگراف، `\a` جداکنندهٔ سلول/گرافیک، `\f` شکست صفحه، `\v` شکست خط عمودی) را به فضای خالی/خط جدید تبدیل و فاصله‌های تکراری را نرمال می‌کنند.
5. **مدیریت خطا**: بخش‌های حساس (پاورقی، هدر/فوتر، Shapeها) در `try/catch` محافظت شده‌اند تا خطای یک المان (مثلاً جدول با سلول‌های ادغام‌شده که با اندیس‌گذاری Word مشکل ایجاد می‌کند) کل فرآیند استخراج سند را متوقف نکند.

#### `ExtractStyleService` (در فایل `StyleExtractor.cs`)
مسیر مستقل و **کاملاً OpenXML-محور** (بدون نیاز به Word نصب‌شده) برای استخراج دقیق سبک‌ها:
- سند را با `WordprocessingDocument.Open` باز کرده و `StyleDefinitionsPart` را پیمایش می‌کند.
- `ReadThemeColors`: رنگ‌های Theme سند (از `ThemePart`) را می‌خواند تا بتواند رنگ‌های نسبی (`ThemeColor`/`ThemeTint`/`ThemeShade`) را به Hex واقعی حل کند (`ResolveColor`, `ApplyTint`, `ApplyShade`).
- `ExtractStyle`/`ExtractFont`/`ExtractParagraph`/`ExtractBorder`: نگاشت مستقیم عناصر XML به مدل `Style` (تبدیل واحد: نصف‌پوینت به پوینت برای فونت، Twip به پوینت برای تورفتگی/فاصله‌گذاری).
- **حل وراثت سبک‌ها (`ResolveInheritance`/`ResolveStyle`/`Merge*`)**: چون در Word یک سبک می‌تواند بر پایهٔ سبک دیگری (`BasedOnStyleId`) تعریف شود، این متدها به‌صورت بازگشتی (با یک `HashSet` برای جلوگیری از حلقهٔ بی‌نهایت در صورت وراثت چرخه‌ای) زنجیرهٔ والد را resolve و merge می‌کنند تا مقدار نهایی مؤثر هر ویژگی به دست آید (الگوی cascading مشابه CSS).

نکته: این سرویس (`ExtractStyleService`) و `InteropTextExtractor.ReadParagraphInfos` هر دو به‌طور مستقل «سبک‌ها» را جمع‌آوری می‌کنند اما از دو مسیر متفاوت (Interop برای نام سبک هر پاراگراف در `document.Styles` که در واقع صرفاً دیکشنری نام←نام است؛ و `ExtractStyleService` برای جزئیات کامل بصری سبک). این دو مسیر هنوز به هم متصل نشده‌اند (`ExtractStyleService` در `Program.cs` استفاده نمی‌شود).

#### `IHtlmRenderer`
```csharp
public interface IHtlmRenderer
{
}
```
یک Interface کاملاً خالی (حتی بدون امضای متد) — به‌وضوح **Placeholder** برای یک سرویس رندر HTML آینده که هنوز طراحی نشده (نام آن هم دارای غلط تایپی «Htlm» به‌جای «Html» است).

### ۲.۵. جریان کار کامل (`Program.cs`)
`Program.cs` به‌جای آنکه صرفاً یک وب‌سرور راه‌اندازی کند، در پایان یک تابع محلی (`RunWordExtractor`) را همگام (synchronous) در زمان استارت‌آپ اجرا می‌کند:

1. DI: تنها `ITextExtractor → InteropTextExtractor` به‌صورت Scoped ثبت شده.
2. یک Scope دستی ساخته و `Extract` را روی **مسیر فایل هارد‌کدشدهٔ** `C:\raha_part 02_08-02-1404-mini.docx` فراخوانی می‌کند (مسیر مطلق ویندوزی — پروژه در عمل فقط روی ماشین توسعه‌دهنده قابل اجراست).
3. روی تمام پاراگراف‌های تمام صفحات پیمایش می‌کند و بر اساس **نام سبک پاراگراف** (`فهرست`, `تاریخ`, `داستان`, `راوی`, `TOC 1`) فیلدهای دامنه‌محور کتاب (`Fehrest`, `Date`, `Story`, `Ravi`) و آیتم‌های فهرست مطالب (`IndexItem` با پارس رشتهٔ TOC بر مبنای `\t` و `/`) را پر می‌کند. این بخش کاملاً **Business logic مخصوص یک کتاب خاص (سیرهٔ نبوی)** است، نه یک Extractor عمومی.
4. سند نهایی (`DocumentContent`) با `System.Text.Json` (با `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` برای جلوگیری از escape شدن کاراکترهای فارسی) سریالایز و در `output.txt` (بدون BOM) ذخیره می‌شود.

نکتهٔ مهم: چون این کد مستقیماً در بدنهٔ `Program.cs` (خارج از هر Endpoint) اجرا می‌شود، **هر بار اجرای اپلیکیشن** (`dotnet run`) این عملیات سنگین COM را اجرا می‌کند، نه با یک درخواست HTTP. یعنی Razor Pages در این پروژه صرفاً اسکلتی حاصل از تمپلیت است و نقشی در جریان اصلی کار ندارد.

### ۲.۶. الگوهای طراحی مشاهده‌شده
- **Interface segregation ساده**: `ITextExtractor`, `IExtractStyleService`, `IHtlmRenderer` جدا از هم تعریف شده‌اند تا مسئولیت‌ها (استخراج متن/صفحه، استخراج سبک، رندر HTML) در آینده مستقل قابل تعویض/تست باشند؛ گرچه فعلاً فقط یکی (`ITextExtractor`) واقعاً در DI ثبت و استفاده می‌شود.
- **Resource management با `try/finally` + COM release**: الگوی تکرارشونده در تمام متدهای `InteropTextExtractor` برای هر شیء COM — نشان‌دهندهٔ آگاهی نویسنده به ریسک نشت حافظه COM.
- **Range-intersection pattern**: به‌جای استخراج مستقیم per-page از Word (که API مستقیم آسانی ندارد)، ابتدا اطلاعات کل سند (پاراگراف/جدول/تصویر) با بازهٔ کاراکتری خوانده می‌شود، سپس با تقاطع بازه‌ها (`RangesIntersect`) به هر صفحه نسبت داده می‌شود — یک راه‌حل مهندسی هوشمندانه برای محدودیت مدل شیءِ Word در تفکیک صفحه‌ای.
- **مدل غنی مستقل از فناوری منبع (`Style.cs`, `DocumentContent.cs`)**: مدل‌های خروجی کاملاً POCO و بدون وابستگی به Interop/OpenXML هستند تا مصرف‌کنندگان پایین‌دستی (مثلاً یک API یا رندرر) به این وابستگی‌های سنگین نیازی نداشته باشند.
- **Style inheritance resolution** در `ExtractStyleService`: الگوی بازگشتی merge مشابه cascading در CSS، با محافظت در برابر چرخه.

---

## ۳. نحوهٔ ارتباط WordReader با سایر بخش‌ها

بررسی دقیق ارجاعات پروژه‌ای (`ProjectReference`) در `WordReader.csproj` و کد نشان می‌دهد:

- **`WordReader` به هیچ پروژهٔ دیگری در Solution ارجاع مستقیم ندارد** (نه به `BookOnline.Domain`، نه به `BookOnline` API، نه به `LandingSite`). این یک پروژهٔ کاملاً ایزوله (standalone tool) است.
- خروجی آن یک **فایل JSON مسطح روی دیسک** (`output.txt`) است، نه یک API، صف پیام، یا دیتابیس مشترک. یعنی در حال حاضر **هیچ مکانیزم یکپارچه‌سازی خودکاری بین WordReader و بقیهٔ سیستم وجود ندارد**.
- ارتباط مفهومی (نه کدی) با `Core/Domain/BookOnline.Domain/Books/Book.cs` وجود دارد: مدل‌های آن پروژه (`IPage`, `PragraphSection`, `AtomicBlockSection`, `BookIndex`, `BookIndexItem`) به‌شدت شبیه به ساختار خروجی `WordReader` (`PageContent`, `ParagraphSpan`, `IndexItem`) هستند — با این تفاوت که `Book.cs` سطح دانه‌بندی ریزتری (`AtomicBlockSection` در سطح کلمه، برای جستجوی فازی/context-aware) دارد. این شباهت قوی نشان می‌دهد **قرار بوده خروجی `WordReader` منبع دادهٔ اولیهٔ ساخت Aggregate `IBook` در دامنه باشد**، اما این نگاشت (Mapping) هنوز پیاده‌سازی نشده.
- پروژهٔ `BookImporter` که در `BookOnline.sln` ثبت شده اما پوشه/فایل آن در ریپازیتوری موجود نیست، از نظر نام‌گذاری دقیقاً همان «حلقهٔ گمشده»ای است که باید خروجی `WordReader` را می‌خواند و به مدل دامنهٔ `BookOnline.Domain.Books` تبدیل و شاید در یک پایگاه‌داده ذخیره می‌کرد. نبود این پروژه در فایل‌سیستم (در حالی که در Solution رجیستر شده) نشان می‌دهد یا هنوز ساخته نشده یا به‌اشتباه/عمداً از کنترل نسخه حذف شده است.
- به لحاظ زیرساخت (`Dockerfile`)، `WordReader` مانند سایر پروژه‌های وب (`BookOnline`, `LandingSite`) یک Dockerfile مستقل چندمرحله‌ای (multi-stage) بر پایهٔ `mcr.microsoft.com/dotnet/aspnet:8.0` و `sdk:8.0` دارد و در Solution به همان شکل به‌عنوان یک سرویس مجزا دیده می‌شود؛ اما همان‌طور که در بخش چالش‌ها می‌آید، این Dockerfile در عمل قابل اجرا نیست.

جمع‌بندی رابطه: **`WordReader` امروز یک ابزار offline/manual برای تبدیل یک فایل Word مشخص به JSON است، نه یک میکروسرویس یکپارچه در معماری کلی.** پل ارتباطی طراحی‌شده (احتمالاً `BookImporter`) وجود ندارد و مصرف‌کنندهٔ خروجی آن در کد فعلی هیچ پروژهٔ دیگری نیست.

---

## ۴. نکات کلیدی و چالش‌های معماری شناسایی‌شده

1. **وابستگی حیاتی به Microsoft Word نصب‌شده روی Windows (COM Interop)**: `InteropTextExtractor` — یعنی تنها بخش کاملاً پیاده‌سازی‌شدهٔ کل Solution — برای اجرا به نصب واقعی Microsoft Word نیاز دارد. این با `Dockerfile` پروژه که بر پایهٔ ایمیج رسمی و سبک لینوکسی `mcr.microsoft.com/dotnet/aspnet:8.0` است **کاملاً ناسازگار است**: این ایمیج نه Windows است و نه Word روی آن نصب می‌شود، پس تصویر Docker ساخته‌شده در Runtime روی خط `extractor.Extract(...)` با خطا متوقف خواهد شد. این مهم‌ترین ریسک معماری فعلی پروژه است.
2. **مسیر فایل و منطق کسب‌وکار Hardcoded**: مسیر ورودی (`C:\raha_part 02_08-02-1404-mini.docx`) و نگاشت نام‌های سبک فارسی (`فهرست`, `تاریخ`, `داستان`, `راوی`, `TOC 1`) مستقیماً در `Program.cs` هارد‌کد شده‌اند. یعنی `WordReader` در حال حاضر یک ابزار عمومی نیست بلکه **مخصوص پردازش یک کتاب خاص** است و برای استفادهٔ عمومی نیاز به پارامتری‌سازی (ورودی از طریق آپلود/API) و جداسازی این منطق دامنه‌محور از هستهٔ Extractor دارد.
3. **اجرای منطق سنگین در بدنهٔ Startup به‌جای یک Endpoint/Job**: عملیات `RunWordExtractor` هر بار در زمان بالا آمدن اپلیکیشن اجرا می‌شود، نه از طریق یک HTTP endpoint یا صف پردازش پس‌زمینه. این هم برای مقیاس‌پذیری و هم برای استفادهٔ آن به‌عنوان یک سرویس واقعی وب مشکل‌ساز است.
4. **دو مسیر موازی و ناهماهنگ برای استخراج سبک**: `document.Styles` (که در Interop صرفاً نگاشت نام←نام است، بدون جزئیات بصری) و `ExtractStyleService`/`Style.cs` (که جزئیات کامل بصری سبک را از OpenXML می‌گیرد) هنوز به هم متصل نشده‌اند. برای تولید HTML/CSS نهایی صفحات، این دو باید ادغام شوند تا `RebuildHtml()` بتواند از کلاس‌های CSS واقعی (نه فقط نام نرمال‌شده) بهره ببرد.
5. **حلقهٔ گمشده در یکپارچه‌سازی (`BookImporter`)**: پروژه‌ای که باید خروجی JSON `WordReader` را به مدل دامنهٔ `BookOnline.Domain.Books` (`IBook`, `IPage`, `PragraphSection`, `AtomicBlockSection`) تبدیل کند، در `sln` وجود دارد اما در دیسک نیست. بدون آن، Extractor یک جزیرهٔ مجزا باقی می‌ماند و ارزش دادهٔ استخراج‌شده (شامل تصاویر Base64، جداول، پاورقی‌ها) در معماری کلی مصرف نمی‌شود.
6. **کیفیت/ثبات مدل دامنه در پروژه‌های دیگر**: اکثر پروژه‌های Domain (`LibraryOnline.Domain`, `FinancialBookStore.Domain`, `AdminLibraryOnlins.Domain`, و بخش زیادی از `BookOnline.Domain`) صرفاً کلاس‌ها/اینترفیس‌های خالی یا نیمه‌کاره هستند (مثل `BookContentService`, `SearchBookContentService`, `ChatBotWithBookService` که فقط امضا دارند). همچنین چند خطای تایپی در نام‌گذاری دیده می‌شود (`Subsciription` به‌جای `Subscription`, `BookUploeded` به‌جای `BookUploaded`, `Worde`/`Wodrs` به‌جای `Word`/`Words`, `Htlm` به‌جای `Html`, `Contain`/`AtomicBlockSectionHtmlRender` با باگ نحوی در `HtmlRender` — رشته‌ای که `List<string>` را مستقیم داخل `$""` قرار می‌دهد و خروجی معنی‌داری تولید نمی‌کند). این‌ها نشانهٔ مرحلهٔ اولیهٔ توسعه (پروتوتایپ/Draft) هستند، نه نقص‌های بحرانی، اما پیش از ساخت لایه‌های بالادستی باید پاکسازی شوند.
7. **عدم وجود تست و لایهٔ Persistence**: در کل Solution هیچ پروژهٔ تست (Unit/Integration) و هیچ DbContext/Repository واقعی دیده نشد؛ داده‌ها فعلاً یا در حافظه‌اند یا (در `WordReader`) روی فایل دیسک serialize می‌شوند.
8. **مقیاس‌پذیری و عملکرد Interop**: باز کردن Word برای هر فایل و پیمایش صفحه‌به‌صفحه با چندین `doc.GoTo` برای هر صفحه (که خود عملیات نسبتاً کند COM است) برای کتاب‌های حجیم (صدها صفحه) می‌تواند کند باشد؛ برای استفادهٔ Production بهتر است این مسیر به یک Worker/Background Job مجزا (و نه یک ASP.NET Core Request Thread) منتقل شود، به‌خصوص چون Word Interop **thread-affinity** دارد و برای اجرای همزمان (چند کاربر) نیاز به صف‌بندی/Single-instance محافظت‌شده دارد.

---

## ۵. درک من از تسک بعدی: پیاده‌سازی BookImporter (پروژهٔ جدید)

این بخش خلاصه‌ای از فهم من از درخواست فعلی کاربر است، پیش از دریافت قطعه‌کدهای پراکنده‌ای که قرار است مرحله‌به‌مرحله ارسال شوند. **هنوز هیچ کدی نوشته یا تغییر داده نشده است.**

### ۵.۱. هدف
ساخت پروژهٔ `BookImporter` که در `BookOnline.sln` ثبت شده اما در فایل‌سیستم وجود نداشت (طبق بخش ۳ و ۴ همین سند، «حلقهٔ گمشده» نامیده شد). این پروژه دو مسئولیت اصلی خواهد داشت:

1. **Mapping/ETL**: خواندن خروجی JSON پروژهٔ `WordReader` (ساختار `DocumentContent` → `PageContent` → `ParagraphSpan` و غیره، طبق بخش ۲.۳ همین سند) و تبدیل/نگاشت آن به مدل دامنهٔ `Core/Domain/BookOnline.Domain/Books/Book.cs`، یعنی ساخت نمونه‌های واقعی از:
   - `IBook` / پیاده‌سازی آن
   - `BookIndex` و `BookIndexItem` (از `DocumentContent.Index`)
   - `IPage` (از `PageContent`)
   - `PragraphSection` (از `ParagraphSpan`، به همراه `SectionHmlStyle`/`PragraphSectionType`)
   - `AtomicBlockSection` (سطح دانه‌بندی ریزتر — کلمه‌به‌کلمه — که در خروجی فعلی `WordReader` وجود ندارد و باید از متن `ParagraphSpan.Text` توسط منطق Tokenize مشابه آنچه در `AtomicBlockSection.Contains` هست ساخته شود)
   - سایر مدل‌های کمکی دامنه (`PageId`, `BookId`, `PageMark` و مشابه) که فعلاً در `Book.cs` به‌صورت Placeholder/خالی هستند (مثلاً `class BookId {}`) و احتمالاً باید تکمیل/سازگار شوند.

2. **غنی‌سازی دامنه با Full-Text Search**: اضافه کردن قابلیت جستجوی متنی کامل (Full-Text Search) به دامنهٔ کتاب، فراتر از منطق فعلی و بسیار ابتدایی `AtomicBlockSection.Contains`/`MatchiurityScoreFactory` (که صرفاً بر پایهٔ چک کردن کلمات مجاور و بدون ایندکس واقعی کار می‌کند). این بخش به‌احتمال زیاد نیازمند طراحی یک سرویس/ایندکسِ جستجو (و شاید اتصال به `SearchBookContentService` که در حال حاضر در `BookOnline.Domain` فقط یک Interface خالی است — طبق بخش ۱.۲ سند) خواهد بود.

### ۵.۲. روش کار توافق‌شده
- کاربر قطعه‌کدها و نمونه‌های پیاده‌سازی پراکنده‌ای را که از قبل در مرورگر آماده کرده، **مرحله‌به‌مرحله و تکه‌تکه** ارسال می‌کند.
- من در این مرحله **هیچ کدی نمی‌نویسم و هیچ فایلی را تغییر نمی‌دهم**؛ صرفاً آماده دریافت و بررسی قطعه‌کدها هستم.
- پس از دریافت و جمع‌بندی همهٔ قطعه‌کدها و درک کامل نیت طراحی کاربر، پیاده‌سازی واقعی (ساخت پروژهٔ `BookImporter`، کلاس‌های Mapper، و افزودن قابلیت Full-Text Search به دامنه) در مرحلهٔ بعد و پس از هماهنگی انجام خواهد شد.

### ۵.۳. نکات باز (باید طی دریافت کدها روشن شود)
- ساختار دقیق پروژهٔ `BookImporter` (نوع پروژه: Console/Worker Service/Class Library، نحوهٔ اجرا: دستی/زمان‌بندی‌شده/تریگر بر اساس فایل).
- محل و فرمت دقیق ورودی (همان `output.txt` تولیدی `WordReader`، یا فراخوانی مستقیم `ITextExtractor`؟).
- انتخاب فناوری Full-Text Search (مثلاً ایندکس داخلی در حافظه، Lucene.NET/Elasticsearch/SQL Server Full-Text Search یا چیز دیگر) — که در کدهای ارسالی کاربر مشخص خواهد شد.
- نحوهٔ تکمیل مدل‌های خالی/ناقص فعلی در `Book.cs` (مثل `BookId`, `PageId` به‌عنوان کلاس‌های بدون پراپرتی) برای اینکه Mapper بتواند واقعاً نمونه بسازد.

---

*این سند صرفاً حاصل مشاهده و تحلیل کد موجود است؛ هیچ تغییری در فایل‌های سورس اعمال نشده است.*
