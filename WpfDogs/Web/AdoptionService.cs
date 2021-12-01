﻿using System;
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

		string GetToken();
		AnimalsMessage GetAdoptableAnimals();
		AnimalsMessage GetPageFlip(string pageDirectionSuffix);

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
		public string GetToken()
		{
			var postData = new List<KeyValuePair<string, string>>();
			postData.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
			postData.Add(new KeyValuePair<string, string>("client_id", clientId));
			postData.Add(new KeyValuePair<string, string>("client_secret", clientSecret));

			HttpContent content = new FormUrlEncodedContent(postData);
			content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

			//The result property call forces thread to wait.
			var responseResult = client.PostAsync(tokenUrl, content).Result;

			var statuscode = responseResult.StatusCode;
			var jsonResult = responseResult.Content.ReadAsStringAsync().Result;

			var atr = JsonConvert.DeserializeObject<AccessTokenResponse>(jsonResult);
			token = atr.access_token;

			if (token != null)
			{
				HasToken = true;
				TokenExpireTime = DateTime.Now.AddSeconds(atr.expires_in);
			}

			return atr.access_token;
		}

		public AnimalsMessage GetAdoptableAnimals()
		{
			const string animalsUrl = "https://api.petfinder.com/v2/animals?status=adopted";
			return MakeAnimalsRequest(animalsUrl);
		}

		public AnimalsMessage GetPageFlip(string pageDirectionSuffix)
		{
			const string baseAnimalsUrl = "https://api.petfinder.com";

			string animalsUrl = baseAnimalsUrl + pageDirectionSuffix;
			return MakeAnimalsRequest(animalsUrl);
		}

		public Task TriggerAdoptableAnimalsGetAsync()
		{
			const string animalsUrl = "https://api.petfinder.com/v2/animals?status=adopted";
			return Task.Run(async () =>
			{
				var animalsMessage = MakeAnimalsRequest(animalsUrl);
				RaiseAdoptableAnimalsUpdate(animalsMessage);
			});
		}

		public Task TriggerPageFlipAsync(string pageDirectionSuffix)
		{
			const string baseAnimalsUrl = "https://api.petfinder.com";
			
			return Task.Run(async () =>
			{
				string animalsUrl = baseAnimalsUrl + pageDirectionSuffix;
				var animalsMessage = MakeAnimalsRequest(animalsUrl);
				RaiseAdoptableAnimalsUpdate(animalsMessage);
			});
		}

		private AnimalsMessage MakeAnimalsRequest(string animalsUrl)
		{
			if (NeedsNewToken())
				GetToken();

			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			HttpResponseMessage result = client.GetAsync(animalsUrl).Result;

			if (result.StatusCode == HttpStatusCode.Unauthorized)
			{
				//RefreshToken(); /* TODO Implement Refresh Token  don't know if there is a separate way to refresh.  just reuse the client id and secret
				Debug.WriteLine($"Error in method: {nameof(GetAdoptableAnimals)}. Url: {animalsUrl} Error Code: {result.StatusCode}");
			}

			Task<string> infoTask = result.Content.ReadAsStringAsync();
			infoTask.Wait();

			AnimalsMessage response = JsonConvert.DeserializeObject<AnimalsMessage>(infoTask.Result);

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
