using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Frame.Core
{
    /// <summary>
    /// 字符串类的扩展方法
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 确保字符串以指定字符结尾（如果不是则添加）
        /// </summary>
        public static string EnsureEndsWith(this string str, char c, StringComparison comparisonType = StringComparison.Ordinal)
        {
            Check.NotNull(str, nameof(str));

            if (str.EndsWith(c.ToString(), comparisonType))
            {
                return str;
            }

            return str + c;
        }

        /// <summary>
        /// 确保字符串以指定字符开头（如果不是则添加）
        /// </summary>
        public static string EnsureStartsWith(this string str, char c, StringComparison comparisonType = StringComparison.Ordinal)
        {
            Check.NotNull(str, nameof(str));

            if (str.StartsWith(c.ToString(), comparisonType))
            {
                return str;
            }

            return c + str;
        }

        /// <summary>
        /// 判断字符串是否为null或空字符串
        /// </summary>
        public static bool IsNullOrEmpty([NotNullWhen(false)] this string? str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// 判断字符串是否为null、空字符串或仅包含空白字符
        /// </summary>
        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// 从字符串开头获取指定长度的子字符串
        /// </summary>
        /// <exception cref="ArgumentNullException">当str为null时抛出</exception>
        /// <exception cref="ArgumentException">当len大于字符串长度时抛出</exception>
        public static string Left(this string str, int len)
        {
            Check.NotNull(str, nameof(str));

            if (str.Length < len)
            {
                throw new ArgumentException("len argument can not be bigger than given string's length!");
            }

            return str.Substring(0, len);
        }

        /// <summary>
        /// 将字符串中的换行符统一转换为<see cref="Environment.NewLine"/>.
        /// </summary>
        public static string NormalizeLineEndings(this string str)
        {
            return str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
        }


        /// <summary>
        /// 获取字符串中第n次出现指定字符的索引位置
        /// </summary>
        /// <param name="str">要搜索的源字符串</param>
        /// <param name="c">要在字符串中搜索的字符</param>
        /// <param name="n">出现次数</param>
        public static int NthIndexOf(this string str, char c, int n)
        {
            Check.NotNull(str, nameof(str));

            var count = 0;
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] != c)
                {
                    continue;
                }

                if (++count == n)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 从字符串末尾移除第一个匹配的后缀
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="postFixes">一个或多个后缀</param>
        /// <returns>修改后的字符串，如果没有匹配则返回原字符串</returns>
        public static string RemovePostFix(this string str, params string[] postFixes)
        {
            return str.RemovePostFix(StringComparison.Ordinal, postFixes);
        }

        /// <summary>
        /// 从字符串末尾移除第一个匹配的后缀
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="comparisonType">字符串比较类型</param>
        /// <param name="postFixes">一个或多个后缀</param>
        /// <returns>修改后的字符串，如果没有匹配则返回原字符串</returns>
        public static string RemovePostFix(this string str, StringComparison comparisonType, params string[] postFixes)
        {
            if (str.IsNullOrEmpty())
            {
                return str;
            }

            if (postFixes.IsNullOrEmpty())
            {
                return str;
            }

            foreach (var postFix in postFixes)
            {
                if (str.EndsWith(postFix, comparisonType))
                {
                    return str.Left(str.Length - postFix.Length);
                }
            }

            return str;
        }

        /// <summary>
        /// 从字符串开头移除第一个匹配的前缀
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="preFixes">一个或多个前缀</param>
        /// <returns>修改后的字符串，如果没有匹配则返回原字符串</returns>
        public static string RemovePreFix(this string str, params string[] preFixes)
        {
            return str.RemovePreFix(StringComparison.Ordinal, preFixes);
        }


        /// <summary>
        /// 从字符串开头移除第一个匹配的前缀
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="comparisonType">字符串比较类型</param>
        /// <param name="preFixes">一个或多个前缀</param>
        /// <returns>修改后的字符串，如果没有匹配则返回原字符串</returns>
        public static string RemovePreFix(this string str, StringComparison comparisonType, params string[] preFixes)
        {
            if (str.IsNullOrEmpty())
            {
                return str;
            }

            if (preFixes.IsNullOrEmpty())
            {
                return str;
            }

            foreach (var preFix in preFixes)
            {
                if (str.StartsWith(preFix, comparisonType))
                {
                    return str.Right(str.Length - preFix.Length);
                }
            }

            return str;
        }

        /// <summary>
        /// 替换字符串中第一个匹配的子串
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <param name="search">要查找的子串</param>
        /// <param name="replace">替换内容</param>
        /// <param name="comparisonType">字符串比较类型</param>
        public static string ReplaceFirst(this string str, string search, string replace, StringComparison comparisonType = StringComparison.Ordinal)
        {
            Check.NotNull(str, nameof(str));

            var pos = str.IndexOf(search, comparisonType);
            if (pos < 0)
            {
                return str;
            }

            var searchLength = search.Length;
            var replaceLength = replace.Length;
            var newLength = str.Length - searchLength + replaceLength;

            Span<char> buffer = newLength <= 1024 ? stackalloc char[newLength] : new char[newLength];

            // 复制搜索词前的原始字符串部分
            str.AsSpan(0, pos).CopyTo(buffer);

            // 复制替换文本
            replace.AsSpan().CopyTo(buffer.Slice(pos));

            // 复制原始字符串的剩余部分
            str.AsSpan(pos + searchLength).CopyTo(buffer.Slice(pos + replaceLength));

            return buffer.ToString();
        }

        /// <summary>
        /// 从字符串末尾获取指定长度的子字符串
        /// </summary>
        /// <exception cref="ArgumentNullException">当<paramref name="str"/>为null时抛出</exception>
        /// <exception cref="ArgumentException">当<paramref name="len"/>大于字符串长度时抛出</exception>
        public static string Right(this string str, int len)
        {
            Check.NotNull(str, nameof(str));

            if (str.Length < len)
            {
                throw new ArgumentException("len argument can not be bigger than given string's length！");
            }

            return str.Substring(str.Length - len, len);
        }

        /// <summary>
        /// 使用指定分隔符分割字符串
        /// </summary>
        public static string[] Split(this string str, string separator)
        {
            return str.Split(new[] { separator }, StringSplitOptions.None);
        }

        /// <summary>
        /// 使用指定分隔符和选项分割字符串
        /// </summary>
        public static string[] Split(this string str, string separator, StringSplitOptions options)
        {
            return str.Split(new[] { separator }, options);
        }

        /// <summary>
        /// 按换行符<see cref="Environment.NewLine"/>分割字符串
        /// </summary>
        public static string[] SplitToLines(this string str)
        {
            return str.Split(Environment.NewLine);
        }


        /// <summary>
        /// 使用 string.Split 方法按 <see cref="Environment.NewLine"/> 分割给定字符串。
        /// </summary>
        /// <param name="str">要分割的字符串</param>
        /// <param name="options">StringSplitOptions 枚举值，指定在空条目应包含在结果数组中还是应被忽略</param>
        /// <returns>分割后的字符串数组</returns>
        public static string[] SplitToLines(this string str, StringSplitOptions options)
        {
            return str.Split(Environment.NewLine, options);
        }

        /// <summary>
        /// 将 PascalCase 字符串转换为 camelCase 字符串。
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <param name="useCurrentCulture">设置为 true 使用当前文化，否则使用不变文化</param>
        /// <param name="handleAbbreviations">设置为 true 将全大写字符串（如缩写）转换为小写</param>
        /// <returns>转换后的 camelCase 字符串</returns>
        public static string ToCamelCase(this string str, bool useCurrentCulture = false, bool handleAbbreviations = false)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return useCurrentCulture ? str.ToLower() : str.ToLowerInvariant();
            }

            if (handleAbbreviations && IsAllUpperCase(str))
            {
                return useCurrentCulture ? str.ToLower() : str.ToLowerInvariant();
            }

            return (useCurrentCulture ? char.ToLower(str[0]) : char.ToLowerInvariant(str[0])) + str.Substring(1);
        }

        /// <summary>
        /// 将给定的 PascalCase/camelCase 字符串转换为句子形式（通过空格分割单词）。
        /// 例如："ThisIsSampleSentence" 转换为 "This is a sample sentence"。
        /// 注意：此方法未处理单词间的自动添加空格逻辑（如示例中的"a"），需根据实际需求完善。
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <param name="useCurrentCulture">设置为 true 使用当前文化，否则使用不变文化</param>
        /// <returns>转换后的句子形式字符串</returns>
        public static string ToSentenceCase(this string str, bool useCurrentCulture = false)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            return useCurrentCulture
                ? Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]))
                : Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLowerInvariant(m.Value[1]));
        }

        /// <summary>
        /// 将给定的 PascalCase/camelCase 字符串转换为 kebab-case。
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <param name="useCurrentCulture">设置为 true 使用当前文化，否则使用不变文化</param>
        /// <returns>转换后的 kebab-case 字符串</returns>
        public static string ToKebabCase(this string str, bool useCurrentCulture = false)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            str = str.ToCamelCase();

            return useCurrentCulture
                ? Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLower(m.Value[1]))
                : Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLowerInvariant(m.Value[1]));
        }

        /// <summary>
        /// 将给定的 PascalCase/camelCase 字符串转换为 snake_case。
        /// 例如："ThisIsSampleSentence" 转换为 "this_is_sample_sentence"（注意：实际转换可能需根据具体实现调整）。
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <returns>转换后的 snake_case 字符串</returns>
        public static string ToSnakeCase(this string str)
        {
            return str.IsNullOrWhiteSpace() ? str : JsonNamingPolicy.SnakeCaseLower.ConvertName(str);
        }


        /// <summary>
        /// 将字符串转换为枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">要转换的字符串值</param>
        /// <returns>返回枚举对象</returns>
        public static T ToEnum<T>(this string value)
            where T : struct
        {
            Check.NotNull(value, nameof(value));
            return (T)Enum.Parse(typeof(T), value);
        }

        /// <summary>
        /// 将字符串转换为枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">要转换的字符串值</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns>返回枚举对象</returns>
        public static T ToEnum<T>(this string value, bool ignoreCase)
            where T : struct
        {
            Check.NotNull(value, nameof(value));
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }


        public static string ToMd5(this string str)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(str);
                var hashBytes = md5.ComputeHash(inputBytes);

                var sb = new StringBuilder();
                foreach (var hashByte in hashBytes)
                {
                    sb.Append(hashByte.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        public static string ToSha256(this string str)
        {
            using (var sha = SHA256.Create())
            {
                var data = sha.ComputeHash(Encoding.UTF8.GetBytes(str));

                var sb = new StringBuilder();
                foreach (var d in data)
                {
                    sb.Append(d.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static string ToSha512(this string str)
        {
            using (var sha = SHA512.Create())
            {
                var data = sha.ComputeHash(Encoding.UTF8.GetBytes(str));

                var sb = new StringBuilder();
                foreach (var d in data)
                {
                    sb.Append(d.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// 将camelCase字符串转换为PascalCase格式
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <param name="useCurrentCulture">设为true使用当前区域设置，否则使用不变区域设置</param>
        /// <returns>PascalCase格式的字符串</returns>
        public static string ToPascalCase(this string str, bool useCurrentCulture = false)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return useCurrentCulture ? str.ToUpper() : str.ToUpperInvariant();
            }

            return (useCurrentCulture ? char.ToUpper(str[0]) : char.ToUpperInvariant(str[0])) + str.Substring(1);
        }

        /// <summary>
        /// 当字符串超过最大长度时，从开头截取子字符串
        /// </summary>
        public static string? Truncate(this string? str, int maxLength)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            return str.Left(maxLength);
        }

        /// <summary>
        /// 当字符串超过最大长度时，从末尾截取子字符串
        /// </summary>
        public static string? TruncateFromBeginning(this string? str, int maxLength)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            return str.Right(maxLength);
        }

        /// <summary>
        /// 当字符串超过最大长度时，从开头截取并添加"..."后缀
        /// 返回的字符串长度不会超过maxLength
        /// </summary>
        /// <exception cref="ArgumentNullException">当str为null时抛出</exception>
        public static string? TruncateWithPostfix(this string? str, int maxLength)
        {
            return str.TruncateWithPostfix(maxLength, "...");
        }

        /// <summary>
        /// 当字符串超过最大长度时，从开头截取并添加指定后缀
        /// 返回的字符串长度不会超过maxLength
        /// </summary>
        /// <exception cref="ArgumentNullException">当str为null时抛出</exception>
        public static string? TruncateWithPostfix(this string? str, int maxLength, string postfix)
        {
            if (str == null)
            {
                return null;
            }

            if (str == string.Empty || maxLength == 0)
            {
                return string.Empty;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            if (maxLength <= postfix.Length)
            {
                return postfix.Left(maxLength);
            }

            return str.Left(maxLength - postfix.Length) + postfix;
        }

        /// <summary>
        /// 使用UTF8编码将字符串转换为字节数组
        /// </summary>
        public static byte[] GetBytes(this string str)
        {
            return str.GetBytes(Encoding.UTF8);
        }

        /// <summary>
        /// 使用指定编码将字符串转换为字节数组
        /// </summary>
        public static byte[] GetBytes([NotNull] this string str, [NotNull] Encoding encoding)
        {
            Check.NotNull(str, nameof(str));
            Check.NotNull(encoding, nameof(encoding));

            return encoding.GetBytes(str);
        }

        private static bool IsAllUpperCase(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsLetter(input[i]) && !char.IsUpper(input[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}