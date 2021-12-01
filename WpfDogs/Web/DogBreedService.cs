using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WpfDogs.DataModels;

namespace WpfDogs.Web
{
	public interface IDogServiceInterface
	{
		//event EventHandler<BreedsResponse> BreedsListUpdated;
		//event EventHandler<BreedImagesResponse> ImageUrlUpdated;

		Task<DataModels.BreedsResponse> GetAllBreeds();
		Task<BreedImagesResponse> GetImagesForBreed(string breedName);
	}

	public class DogBreedService : IDogServiceInterface
	{
		//I'm using a single HttpClient so that i don't keep opening sockets on the machine.
		//Don't quote me but I believe i've seen issues in the past leaving sockets open, using the standard using/dispose.
		private readonly HttpClient client = new HttpClient();

		//public event EventHandler<BreedsResponse> BreedsListUpdated;
		//public event EventHandler<BreedImagesResponse> ImageUrlUpdated;

		public async Task<DataModels.BreedsResponse> GetAllBreeds()
		{
			const string allBreedsUrl = "https://dog.ceo/api/breeds/list/all";
			string responseBody = await MakeApiRequest(allBreedsUrl);
			BreedsResponse m = JsonConvert.DeserializeObject<BreedsResponse>(responseBody);
			return m;
		}

		public  async Task<BreedImagesResponse> GetImagesForBreed(string breedName)
		{
			string imagesUrl = $"https://dog.ceo/api/breed/{breedName}/images";
			string json = await MakeApiRequest(imagesUrl);
			BreedImagesResponse response = JsonConvert.DeserializeObject<BreedImagesResponse>(json);
			return response;
		}

		private async Task<string> MakeApiRequest(string url)
		{
			//I considered using a lock here, but i don't think it's necessary as HttpClient is built to handle
			//multiple requests completely seperately.
			HttpResponseMessage response = await client.GetAsync(url);
			response.EnsureSuccessStatusCode();
			string responseBody = await response.Content.ReadAsStringAsync();
			return responseBody;
		}
	}
}
