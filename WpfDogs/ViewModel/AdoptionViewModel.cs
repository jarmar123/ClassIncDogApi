using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfDogs.DataModels;
using WpfDogs.DisplayClasses;
using WpfDogs.Web;
using System.Windows;
using System.Windows.Threading;

namespace WpfDogs.ViewModel
{
	public class AdoptViewModel : INotifyPropertyChanged
	{
		private IAdoptionService AdoptionService;
		//private bool hasNextPage;
		//private bool hasPreviousPage;
		private string prevousLink;
		private string nextLink;

		private ObservableCollection<AdoptableAnimal> displayAnimalsList;
		private ICommand getAdoptablePets;
		private ICommand getNextPage;
		private ICommand getPreviousPage;

		public AdoptViewModel()
		{
			AdoptionService = new AdoptionService();
		}

		public ICommand GetAdoptablePets
		{
			get
			{
				if (getAdoptablePets == null)
					getAdoptablePets = new RelayCommand((p) => GetPets());

				return getAdoptablePets;
			}
			set => getAdoptablePets = value;
		}

		public ICommand GetNextPage
		{
			get
			{
				if (getNextPage == null)
					getNextPage = new RelayCommand((p) => GetNextPageAction(), o => CheckNextPageButtonEnabled());

				return getNextPage;
			}
			set => getNextPage = value;
		}

		public ICommand GetPreviousPage
		{
			get
			{
				if (getPreviousPage == null)
					getPreviousPage = new RelayCommand((p) => GetPreviousPageAction(), o => CheckPreviousPageButtonEnabled());

				return getPreviousPage;
			}
			set => getPreviousPage = value;
		}

		private string imageUrl;
		public string ImageUrl
		{
			get { return imageUrl; }
			set
			{
				imageUrl = value;
				OnPropertyChanged("ImageUrl");
			}
		}

		public ObservableCollection<AdoptableAnimal> DisplayAnimalsList
		{
			get { return displayAnimalsList; }
			set
			{
				displayAnimalsList = value;
				OnPropertyChanged(nameof(DisplayAnimalsList));
			}
		}

		private AdoptableAnimal selectedAnimal;
		public AdoptableAnimal SelectedAnimal {
			get { return selectedAnimal; }
			set
			{
				selectedAnimal = value;
				if (selectedAnimal?.PhotoUrl != null)
				{
					ImageUrl = selectedAnimal.PhotoUrl;
				}
				else
				{
					ImageUrl = string.Empty;
				}
				OnPropertyChanged(nameof(SelectedAnimal));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#region Private

		private bool CheckNextPageButtonEnabled()
		{
			return nextLink != null;
		}

		private void GetNextPageAction()
		{
			PageFlipAction(nextLink);
		}

		private void GetPreviousPageAction()
		{
			PageFlipAction(prevousLink);
		}

		private void PageFlipAction(string linkSuffix)
		{
			Task.Run(async () =>
			{
				AnimalsMessage adoptableAnimalsMessage = AdoptionService.GetPageFlip(linkSuffix);

				Application.Current.Dispatcher.BeginInvoke(
					new Action(() =>
					{
						PopulateAdoptableAnimalsCollection(adoptableAnimalsMessage);
						UpdateArrowButtons(adoptableAnimalsMessage);

					}), DispatcherPriority.Background);
			});
		}

		private bool CheckPreviousPageButtonEnabled()
		{
			return prevousLink != null;
		}

		private void GetPets()
		{
			Task.Run(async () =>
			{
				AnimalsMessage adoptableAnimalsMessage = AdoptionService.GetAdoptableAnimals();

				Application.Current.Dispatcher.BeginInvoke(
						new Action(() =>
						{
							PopulateAdoptableAnimalsCollection(adoptableAnimalsMessage);
							UpdateArrowButtons(adoptableAnimalsMessage);

						}), DispatcherPriority.Background);
			});
		}

		private void UpdateArrowButtons(AnimalsMessage message)
		{
			prevousLink = message.pagination?._links?.previous?.href;
			nextLink = message.pagination?._links?.next?.href;
		}

		private void PopulateAdoptableAnimalsCollection(AnimalsMessage message)
		{
			DisplayAnimalsList = new ObservableCollection<AdoptableAnimal>();
			foreach (var animal in message.animals)
			{
				AdoptableAnimal displayAnimal = new AdoptableAnimal(animal.type, animal.name, animal.DisplayPhotoUrl, animal.contact?.email, animal.contact?.phone);
				DisplayAnimalsList.Add(displayAnimal);
			}
		}

		#endregion

		#region INotifyPropertyChanged

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
