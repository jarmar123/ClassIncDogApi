using System.Collections.Generic;
using Newtonsoft.Json;

namespace WpfDogs.DataModels
{
	public class BreedsResponse
	{
		// Doing this JsonProperty at the very end.  Normally i'd do it with every property in a json class.
		// But it works right now and wpf isn't my strong suit.  I don't think changing it will mess anything up. But it's just a homework demo.
		[JsonProperty("message")]
		public Dictionary<string, string[]> Message { get; set; }
		public string status { get; set; }
	}

	public class BreedImagesResponse
	{
		public string[] message { get; set; }
		public string status { get; set; }
	}
}
