using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nccc
{
    public class Locale
    {
        public string Language { get; set; }

        public string L(string s)
        {
            if (Language == null)
            {
                return s;
            }
            if (locales.TryGetValue(Language, out var dict))
            {
                if (dict.TryGetValue(s, out var content))
                {
                    return content;
                }
            }
            return s;
        }

        private static readonly IDictionary<string, IDictionary<string, string>> locales = new Dictionary<string, IDictionary<string, string>>
        {
            { "zh-cn", new Dictionary<string, string>{
                { "expect <<EOF>>", "分析已结束，但文本还有内容" },
                { "expect", "应为" },
                { "not match regex", "无法匹配正则" },
                { "reach eof", "文本已读到末尾" },
                { "is undefined", "未定义" },
                { "block comment match error", "注释结束标记缺失" },
                { "string match error", "字符串结束标记缺失" },
                { "regex match error", "正则结束标记缺失" },
            } },
        };
    }
}
