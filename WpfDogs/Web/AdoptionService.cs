using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WpfDogs.DataModels;

namespace WpfDogs.Web
{
	public interface IAdoptionService
	{
		/// <summary>
		/// Fires when new adoptable animals page comes in TriggerAdoptableAnimalsGetAsync has finished
		/// </summary>
		event EventHandler<AnimalsMessage> AdoptableAnimalsUpdate;

		Task<string> GetToken();
		Task<AnimalsMessage> GetAdoptableAnimals();
		Task<AnimalsMessage> GetPageFlip(string pageDirectionSuffix);

		Task TriggerAdoptableAnimalsGetAsync();
		Task TriggerPageFlipAsync(string linkSuffix);
	}

	public class AdoptionService: IAdoptionService
	{
		private HttpClient client;
		public string tokenUrl = $"https://api.petfinder.com/v2/oauth2/token";
		public string clientId = $"v6TlmxcYcI0jYeo1JB5P7YCKZn455IuLbYnGUAYovKhYwlFOC0";
		public string clientSecret = $"WiGBVldqVEszUglEIsqpEfhdSeWnSDPf5jARagJr";

		public string token = null;

		public event EventHandler<AnimalsMessage> AdoptableAnimalsUpdate;

		public bool HasToken { get; private set; }
		public DateTime TokenExpireTime { get; private set; }

		public AdoptionService()
		{
			client = new HttpClient();
		}

		//Leaving this method non async for getting
		public async Task<string> GetToken()
		{
			var postData = new List<KeyValuePair<string, string>>();
			postData.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
			postData.Add(new KeyValuePair<string, string>("client_id", clientId));
			postData.Add(new KeyValuePair<string, string>("client_secret", clientSecret));

			HttpContent content = new FormUrlEncodedContent(postData);
			content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

			//The result property call forces thread to wait.
			var responseResult = await client.PostAsync(tokenUrl, content);

			var statuscode = responseResult.StatusCode;
			var jsonResult = await responseResult.Content.ReadAsStringAsync();

			var atr = JsonConvert.DeserializeObject<AccessTokenResponse>(jsonResult);
			token = atr.access_token;

			if (token != null)
			{
				HasToken = true;
				TokenExpireTime = DateTime.Now.AddSeconds(atr.expires_in);
			}

			return atr.access_token;
		}

		public Task<AnimalsMessage> GetAdoptableAnimals()
		{
			const string animalsUrl = "https://api.petfinder.com/v2/animals?status=adopted";
			return MakeAnimalsRequest(animalsUrl);
		}

		public Task<AnimalsMessage> GetPageFlip(string pageDirectionSuffix)
		{
			const string baseAnimalsUrl = "https://api.petfinder.com";

			string animalsUrl = baseAnimalsUrl + pageDirectionSuffix;
			return MakeAnimalsRequest(animalsUrl);
		}


		//These trigger methods are not async because they are not to be awaited.  THey are set and forget, updates come through the events.
		public Task TriggerAdoptableAnimalsGetAsync()
		{
			const string animalsUrl = "https://api.petfinder.com/v2/animals?status=adopted";
			return Task.Run(async () =>
			{
				var animalsMessage = await  MakeAnimalsRequest(animalsUrl);
				RaiseAdoptableAnimalsUpdate(animalsMessage);
			});
		}

		public Task TriggerPageFlipAsync(string pageDirectionSuffix)
		{
			const string baseAnimalsUrl = "https://api.petfinder.com";
			
			return Task.Run(async () =>
			{
				string animalsUrl = baseAnimalsUrl + pageDirectionSuffix;
				var animalsMessage = await MakeAnimalsRequest(animalsUrl);
				RaiseAdoptableAnimalsUpdate(animalsMessage);
			});
		}

		private async Task<AnimalsMessage> MakeAnimalsRequest(string animalsUrl)
		{
			if (NeedsNewToken())
				await GetToken();

			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			HttpResponseMessage result = client.GetAsync(animalsUrl).Result;

			if (result.StatusCode == HttpStatusCode.Unauthorized)
			{
				//RefreshToken(); /* TODO Implement Refresh Token  don't know if there is a separate way to refresh.  just reuse the client id and secret
				Debug.WriteLine($"Error in method: {nameof(GetAdoptableAnimals)}. Url: {animalsUrl} Error Code: {result.StatusCode}");
			}

			string json = await result.Content.ReadAsStringAsync();

			AnimalsMessage response = JsonConvert.DeserializeObject<AnimalsMessage>(json);

			return response;
		}

		private void RaiseAdoptableAnimalsUpdate(AnimalsMessage message)
		{
			AdoptableAnimalsUpdate?.Invoke(this, message);
		}

		private bool NeedsNewToken()
		{
			if (!HasToken)
				return true;

			if (DateTime.Now >= TokenExpireTime)
				return true;

			return false;
		}
	}
}
