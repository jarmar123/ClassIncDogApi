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
		event EventHandler<BreedsResponse> BreedsListUpdated;
		event EventHandler<BreedImagesResponse> ImageUrlUpdated;

		/// <summary>
		/// You'll Get results via ImageUrlUpdated event.  Kicks off new thread.  no need to await it.
		/// </summary>
		/// <param name="breedName"></param>
		/// <returns></returns>
		Task GetImagesForBreedAsync(string breedName);

		/// <summary>
		/// Get updates via BreedsListUpdated Event
		/// </summary>
		/// <returns></returns>
		Task GetAllBreedsAsync();

		BreedsResponse GetAllBreeds();
		BreedImagesResponse GetImagesForBreed(string breedName);
	}

	public class DogBreedService : IDogServiceInterface
	{
		//I'm using a single HttpClient so that i don't keep opening sockets on the machine.
		//Don't quote me but I believe i've seen issues in the past leaving sockets open, using the standard using/dispose.
		private readonly HttpClient client = new HttpClient();

		public event EventHandler<BreedsResponse> BreedsListUpdated;
		public event EventHandler<BreedImagesResponse> ImageUrlUpdated;

		public BreedsResponse GetAllBreeds()
		{
			const string allBreedsUrl = "https://dog.ceo/api/breeds/list/all";
			string responseBody = MakeApiRequest(allBreedsUrl);
			BreedsResponse m = JsonConvert.DeserializeObject<BreedsResponse>(responseBody);
			return m;
		}

		public BreedImagesResponse GetImagesForBreed(string breedName)
		{
			string imagesUrl = $"https://dog.ceo/api/breed/{breedName}/images";
			string json = MakeApiRequest(imagesUrl);
			BreedImagesResponse response = JsonConvert.DeserializeObject<BreedImagesResponse>(json);
			return response;
		}
		public async Task GetImagesForBreedAsync(string breedName)
		{
			string imagesUrl = $"https://dog.ceo/api/breed/{breedName}/images";
			Task.Run(async () =>
			{
				string json = MakeApiRequest(imagesUrl);
				BreedImagesResponse response = JsonConvert.DeserializeObject<BreedImagesResponse>(json);
				RaiseImageUrlUpdate(response);
			});
		}

		public async Task GetAllBreedsAsync()
		{
			const string allBreedsUrl = "https://dog.ceo/api/breeds/list/all";
			Task.Run(async () =>
			{
				string responseBody = MakeApiRequest(allBreedsUrl);
				BreedsResponse m = JsonConvert.DeserializeObject<BreedsResponse>(responseBody);
				RaiseBreedListUpdate(m);
			});
		}

		private void RaiseBreedListUpdate(BreedsResponse response)
		{
			BreedsListUpdated?.Invoke(this, response);
		}

		private void RaiseImageUrlUpdate(BreedImagesResponse response)
		{
			ImageUrlUpdated?.Invoke(this, response);
		}

		private string MakeApiRequest(string url)
		{
			//I considered using a lock here, but i don't think it's necessary as HttpClient is built to handle
			//multiple requests completely separately.
			HttpResponseMessage response = client.GetAsync(url).Result;
			response.EnsureSuccessStatusCode();
			string responseBody = response.Content.ReadAsStringAsync().Result;
			return responseBody;
		}
	}
}
