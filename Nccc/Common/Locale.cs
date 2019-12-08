using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nccc.Common
{
    public class Locale
    {
        public string Language { get; set; }

        public string L(string s)
        {
            if (TryGetFromLocales(_locales, s, out var text))
            {
                return text;
            }
            if (TryGetFromLocales(globalLocales, s, out text))
            {
                return text;
            }
            return s;
        }

        public void Set(string lang, IDictionary<string, string> entries)
        {
            if (_locales.TryGetValue(lang, out var dict))
            {
                foreach (var e in entries)
                {
                    dict[e.Key] = e.Value;
                }
            }
            else
            {
                _locales[lang] = entries;
            }
        }

        private bool TryGetFromLocales(IDictionary<string, IDictionary<string, string>> locales, string s, out string content)
        {
            if (Language != null && locales.TryGetValue(Language, out var dict))
            {
                if (dict.TryGetValue(s, out content))
                {
                    return true;
                }
            }
            content = null;
            return false;
        }

        private readonly IDictionary<string, IDictionary<string, string>> _locales = new Dictionary<string, IDictionary<string, string>>();

        private static readonly IDictionary<string, IDictionary<string, string>> globalLocales = new Dictionary<string, IDictionary<string, string>>
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
