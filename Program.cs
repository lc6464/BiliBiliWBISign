using BiliBiliWBISign;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;


Dictionary<string, string> query = new() {
	// { "foo", "114" },
	// { "bar", "514" },
	// { "baz", "1919810" }
	{ "mid", "52618445" }
};

var (imgKey, subKey) = await WbiSign.GetKeysAsync();

var signedParams = await WbiSign.EncryptAsync(query, imgKey, subKey);

var queryString = await WbiSign.CreateQueryStringAsync(signedParams);

Console.WriteLine(queryString);


namespace BiliBiliWBISign {
	internal static class WbiSign {
		private static readonly HttpClient httpClient = new() {
			BaseAddress = new Uri("https://api.bilibili.com/"),
			Timeout = TimeSpan.FromSeconds(5)
		};

		private static readonly int[] MixinKeyEncTab = [
			46, 47, 18, 2, 53, 8, 23, 32, 15, 50, 10, 31, 58, 3, 45, 35, 27, 43, 5, 49, 33, 9, 42, 19, 29, 28, 14, 39,
		12, 38, 41, 13, 37, 48, 7, 16, 24, 55, 40, 61, 26, 17, 0, 1, 60, 51, 30, 4, 22, 25, 54, 21, 56, 59, 6, 63,
		57, 62, 11, 36, 20, 34, 44, 52
		];

		private static readonly Regex valueFilter = GeneratedRegex.ValueFilter();

		private static bool initialized = false;

		private static string GetMixinKey(string origin) => MixinKeyEncTab.Aggregate("", (s, i) => s + origin[i])[..32];

		/// <summary>
		/// 初始化 HttpClient，可以不执行，在第一次请求时会自动初始化。
		/// </summary>
		public static void Initialize() {
			if (initialized) {
				return;
			}

			httpClient.DefaultRequestHeaders.UserAgent.Clear();
			httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");

			initialized = true;
		}

		/// <summary>
		/// 获取最新的 img_key 和 sub_key，有效期为一个自然日。
		/// </summary>
		/// <returns>(img_key, sub_key)</returns>
		public static async Task<(string, string)> GetKeysAsync() {
			Initialize();

			var wbiImg = (await httpClient.GetFromJsonAsync<APIRoot>("/x/web-interface/nav").ConfigureAwait(false)).Data.WbiImg;

			var imgKey = Path.GetFileNameWithoutExtension(wbiImg.ImgUrl.Segments.Last());
			var subKey = Path.GetFileNameWithoutExtension(wbiImg.SubUrl.Segments.Last());

			return (imgKey, subKey);
		}

		/// <summary>
		/// 对参数进行 WBI 签名。
		/// </summary>
		/// <param name="parameters">参数</param>
		/// <param name="imgKey">img_key</param>
		/// <param name="subKey">sub_key</param>
		/// <returns>签名后的参数</returns>
		public static async Task<Dictionary<string, string>> EncryptAsync(Dictionary<string, string> parameters, string imgKey, string subKey) {
			var mixinKey = GetMixinKey(imgKey + subKey);
			var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

			parameters["wts"] = timestamp; // 添加 wts 字段

			parameters = parameters.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value); // 按照 key 重排参数

			parameters = parameters.ToDictionary(
				p => p.Key,
				p => valueFilter.Replace(p.Value, "")
			); // 过滤 value 中的 "!'()*" 字符

			var query = await CreateQueryStringAsync(parameters); // 序列化参数

			// 计算 w_rid
			var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(query + mixinKey));

			StringBuilder stringBuilder = new();
			foreach (var b in hashBytes) {
				stringBuilder.Append(b.ToString("x2"));
			}

			parameters["w_rid"] = stringBuilder.ToString();
			stringBuilder.Clear();

			return parameters;
		}

		/// <summary>
		/// 从 Dictionary 创建查询字符串。
		/// </summary>
		/// <param name="parameters">参数字典</param>
		/// <returns>查询字符串</returns>
		public static async Task<string> CreateQueryStringAsync(Dictionary<string, string> parameters) {
			using FormUrlEncodedContent content = new(parameters);
			return await content.ReadAsStringAsync().ConfigureAwait(false);
		}
	}

	internal readonly struct APIRoot {
		public APIData Data { get; init; }
	}

	internal readonly struct APIData {
		[JsonPropertyName("wbi_img")]
		public APIDataWbiImg WbiImg { get; init; }
	}

	internal readonly struct APIDataWbiImg {
		[JsonPropertyName("img_url")]
		public Uri ImgUrl { get; init; }

		[JsonPropertyName("sub_url")]
		public Uri SubUrl { get; init; }
	}

	internal partial class GeneratedRegex {
		[GeneratedRegex(@"[!'()*]")]
		public static partial Regex ValueFilter();
	}
}