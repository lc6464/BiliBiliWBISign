Dictionary<string, string> query = new() {
	{ "foo", "114" },
	{ "bar", "514" },
	{ "baz", "1919810" }
};

var keys = await WbiSign.GetKeysAsync();

var signedParams = await WbiSign.EncryptAsync(query, keys);

var queryString = await WbiSign.CreateQueryStringAsync(signedParams);

Console.WriteLine(queryString);