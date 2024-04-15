using System.Text.Json.Serialization;


namespace BiliBiliWBISign;

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