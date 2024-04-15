using BiliBiliWBISign;


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