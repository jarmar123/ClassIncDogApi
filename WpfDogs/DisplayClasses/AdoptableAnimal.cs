using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDogs.DisplayClasses
{
	public class AdoptableAnimal
	{
		public AdoptableAnimal(string animalType, string name, string photoUrl, string contactEmail, string contactPhone)
		{
			Type = animalType;
			Name = name;
			PhotoUrl = photoUrl;
			Email = contactEmail;
			Phone = contactPhone;
		}

		public string Name { get; private set; }
		public string Type { get; private set; }
		public string PhotoUrl { get; private set; }
		public string Email { get; private set; }
		public string Phone{ get; private set; }
	}
}
