using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class TranslationService
{
    private const string EnvAppIdName = "20251108002493267";
    private const string EnvSecretName = "5huyyKaOSjIldJM683aD";

    private static readonly string AppId = Environment.GetEnvironmentVariable(EnvAppIdName) ?? "20251108002493267";
    private static readonly string SecretKey = Environment.GetEnvironmentVariable(EnvSecretName) ?? "5huyyKaOSjIldJM683aD";

    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

    // 百度官方语言代码集合（常用）
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto","zh","en","yue","wyw","jp","kor","fra","spa","th","ara","ru","pt","de","it",
        "el","nl","pl","bul","est","dan","fin","cs","rom","slo","swe","hu","cht","vie","id","ms","tr","uk","hi"
    };

    // 常见别名→百度代码
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // 中文
        ["zh-cn"] = "zh",
        ["zh_cn"] = "zh",
        ["cn"] = "zh",
        ["zh-hans"] = "zh",
        ["zh-tw"] = "cht",
        ["zh_tw"] = "cht",
        ["tw"] = "cht",
        ["zh-hant"] = "cht",
        ["cht"] = "cht",
        // 英文
        ["en-us"] = "en",
        ["en_gb"] = "en",
        ["en-uk"] = "en",
        // 日/韩/法/西 等
        ["ja"] = "jp",
        ["jpn"] = "jp",
        ["ko"] = "kor",
        ["kr"] = "kor",
        ["kor"] = "kor",
        ["fr"] = "fra",
        ["fre"] = "fra",
        ["es"] = "spa",
        ["spa"] = "spa",
        ["pt-br"] = "pt",
        ["pt-pt"] = "pt",
        ["deu"] = "de",
        ["it-it"] = "it",
        ["vi"] = "vie",
        ["ms-my"] = "ms",
        ["tr-tr"] = "tr"
    };

    public async Task<string> TranslateAsync(string query, string from, string to)
    {
        if (string.IsNullOrWhiteSpace(query)) return "要翻译的文本为空";
        if (AppId.StartsWith("请配置") || SecretKey.StartsWith("请配置"))
            return $"请配置百度API密钥（在环境变量 {EnvAppIdName} / {EnvSecretName} 中设置 AppId 与 Secret）";

        // 规范化语言代码
        string fromCode = NormalizeFrom(from);
        string toCode = NormalizeTo(to);

        if (!Supported.Contains(toCode) || toCode.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return $"目标语言不合法：{to}. 请选择：{string.Join(",", Supported)}（除 auto 外）";

        try
        {
            string salt = new Random().Next(100000, 999999).ToString();
            string sign = GetMd5Hash(AppId + query + salt + SecretKey);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("q", query),        // 注意：签名用原文，提交表单即可
                new KeyValuePair<string,string>("from", fromCode),
                new KeyValuePair<string,string>("to", toCode),
                new KeyValuePair<string,string>("appid", AppId),
                new KeyValuePair<string,string>("salt", salt),
                new KeyValuePair<string,string>("sign", sign)
            });

            var response = await _httpClient.PostAsync("https://api.fanyi.baidu.com/api/trans/vip/translate", content);
            var jsonResult = await response.Content.ReadAsStringAsync();
            Debug.WriteLine("百度翻译原始返回: " + jsonResult);

            var jsonResponse = JObject.Parse(jsonResult);
            if (jsonResponse["error_code"] != null)
            {
                string code = jsonResponse["error_code"]?.ToString();
                string msg = jsonResponse["error_msg"]?.ToString();

                if (code == "58001") // 你的当前报错
                    return $"百度翻译错误 (代码: {code})：{msg}。已将 to={to} 规范化为 {toCode}，请确认在支持列表内。";

                if (code == "52003")
                    return $"百度翻译错误 (代码: {code})：{msg}。请在百度开放平台检查该 App 的服务授权、来源限制和账号状态。";

                if (code == "58000" || code == "54003")
                {
                    string clientIp = jsonResponse["data"]?["client_ip"]?.ToString()
                                   ?? jsonResponse["client_ip"]?.ToString();
                    return $"百度翻译错误 (代码: {code})：{msg}。client_ip={clientIp ?? "未知"}。";
                }

                return $"百度翻译错误 (代码: {code})：{msg}";
            }

            return jsonResponse["trans_result"]?[0]?["dst"]?.ToString() ?? "解析翻译结果失败";
        }
        catch (HttpRequestException hx) { return "网络请求失败: " + hx.Message; }
        catch (Exception ex) { return "翻译接口调用失败: " + ex.Message; }
    }

    private static string NormalizeFrom(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "auto";
        s = s.Trim();
        if (Map.TryGetValue(s, out var v)) return v;
        return Supported.Contains(s) ? s.ToLowerInvariant() : "auto";
    }

    private static string NormalizeTo(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "zh"; // 缺省翻译成中文
        s = s.Trim();
        if (Map.TryGetValue(s, out var v)) return v;
        return Supported.Contains(s) ? s.ToLowerInvariant() : s.ToLowerInvariant(); // 让上层校验报错
    }

    private static string GetMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        var sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("x2"));
        return sb.ToString();
    }
}
