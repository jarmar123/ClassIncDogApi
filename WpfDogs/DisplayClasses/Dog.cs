using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDogs.DisplayClasses
{
	public class Dog
	{
		public Dog(string breed, string subBreed = "")
		{
			Breed = breed;
			if (!String.IsNullOrEmpty(subBreed))
			{
				HasSubBreed = true;
			}

			SubBreed = subBreed;
		}

		public string Breed { get; private set; }
		public string SubBreed { get; private set; }
		public bool HasSubBreed { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public string DisplayName
		{
			get
			{
				if (HasSubBreed)
				{
					return $"{CapitalizeFirstLetter(SubBreed)} {CapitalizeFirstLetter(Breed)}";
				}

				return CapitalizeFirstLetter(Breed);
			}
		}

		private string CapitalizeFirstLetter(string word)
		{
			if (string.IsNullOrEmpty(word))
				return "";

			return word[0].ToString().ToUpper() + word.Substring(1);
		}
	}
}
