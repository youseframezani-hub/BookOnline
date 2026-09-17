using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookOnline.Domain.Services;
/// <summary>
/// این سرویس جهت چت و گفتگو با کتاب بر اساس متون و محتوای کتاب متدهایی ارایه میده
/// </summary>
internal interface ChatBotWithBookService
{
    /// <summary>
    /// جهت ارتباط با سرویس های هوش مصنوعی
    /// </summary>
    /// <param name="question"></param>
    /// <returns></returns>
    Task<Answare> TalkAsync(Question question);
}

public class Question
{
}

public class Answare
{
}