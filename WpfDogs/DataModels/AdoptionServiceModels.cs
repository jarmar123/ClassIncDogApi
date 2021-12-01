using System.Collections.Generic;

namespace WpfDogs.DataModels
{
	//Normally i'd json property everything here but give me a break.
	public class AccessTokenResponse
	{
		public string token_type { get; set; }
		public int expires_in { get; set; }
		public string access_token { get; set; }
	}

	public class AnimalsMessage
	{
		public Animal[] animals { get; set; }
		public Pagination pagination { get; set; }
	}

	public class Animal
	{
		public int id { get; set; }
		public string type { get; set; }
		public string species { get; set; }
		public string gender { get; set; }
		public string name { get; set; }
		public string description { get; set; }
		public Photo[] photos { get; set; }
		public Photo primary_photo_cropped { get; set; }
		public ContactInfo contact { get; set; }

		public string DisplayPhotoUrl
		{
			get
			{
				//Get the first photo we can find.
				string url;
				if (TryGetPhoto(primary_photo_cropped, out url))
					return url;

				foreach (var photo in photos)
				{
					if (TryGetPhoto(photo, out url))
						return url;
				}

				return null;
			}
		}

		private bool TryGetPhoto(Photo photo, out string url)
		{
			//Get the first photo that exists.  Priority -> Full, Large, Medium, Small
			url = null;
			if (photo == null)
				return false;

			if (photo.full != null)
			{
				url = photo.full;
				return true;
			}

			if (photo.large != null)
			{
				url = photo.large;
				return true;
			}

			if (photo.medium != null)
			{
				url = photo.medium;
				return true;
			}

			if (photo.small != null)
			{
				url = photo.small;
				return true;
			}

			return false;
		}
	}

	public class Photo
	{
		public string small { get; set; }
		public string medium { get; set; }
		public string large { get; set; }
		public string full { get; set; }
	}

	public class ContactInfo
	{
		public string email { get; set; }
		public string phone { get; set; }
	}

	public class Pagination
	{
		public int count_per_page { get; set; }
		public int total_count { get; set; }
		public int current_page { get; set; }
		public int total_pages { get; set; }
		public PaginationLinks _links { get; set; }

		public class PaginationLinks
		{
			public Link previous { get; set; }
			public Link next { get; set; }
		}

		public class Link
		{
			public string href { get; set; }
		}
	}
}
