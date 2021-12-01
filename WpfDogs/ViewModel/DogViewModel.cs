using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WpfDogs.Annotations;
using WpfDogs.DataModels;
using WpfDogs.DisplayClasses;
using WpfDogs.ViewModel;
using WpfDogs.Web;

namespace WpfDogs
{
	public class DogViewModel: INotifyPropertyChanged
	{
		#region Fields

		private ICommand searchAllBreeds;
		private ICommand sortCommand;
		private ICommand sortTypeCommand;
		private ICommand openAdoptWindow;

		private ObservableCollection<Dog> filteredDogList;
		private ObservableCollection<Dog> dogList;
		
		private Dog selectedDogBreed;

		private string imageUrl;
		private string breed;
		private string searchText;
		private string sortType;


		#endregion

		public DogViewModel()
		{
			DogList = new ObservableCollection<Dog>();
			FilteredDogList = new ObservableCollection<Dog>();

			//I'm not sure how WPF MVVM works with inject so i'm instantiating and setting here.
			//property is designed for injection though.
			DogService = new DogBreedService();
		}

		/// <summary>
		/// Property for service to be injected.  But unsure if this works with MVVM in this way.
		/// </summary>
		public IDogServiceInterface DogService { get; set; }

		/// <summary>
		/// Triggers command that calls API and updates List view with Dog Breeds
		/// </summary>
		public ICommand SearchAllBreeds
		{
			get
			{
				if (searchAllBreeds == null)
					searchAllBreeds = new RelayCommand((p) => GetAllBreeds());

				return searchAllBreeds;
			}
			set => searchAllBreeds = value;
		}

		
		public ICommand OpenAdoptWindow
		{
			get
			{
				if (openAdoptWindow == null)
					openAdoptWindow = new RelayCommand((p) => OpenAdoptWindowAction());

				return openAdoptWindow;
			}
			set => openAdoptWindow = value;
		}

		public ObservableCollection<Dog> FilteredDogList
		{
			get { return filteredDogList; }
			set
			{
				filteredDogList = value;
				OnPropertyChanged(nameof(FilteredDogList));
			}
		}


		public ObservableCollection<Dog> DogList
		{
			get { return dogList; }
			set
			{
				dogList = value;
				OnPropertyChanged(nameof(DogList));
			}
		}

		/// <summary>
		/// Triggers the sorting o the displayed breeds in the ListView.
		/// </summary>
		public ICommand SortCommand
		{
			get
			{
				if (sortCommand == null)
					sortCommand = new RelayCommand(p => SortDogList(FilteredDogList));
				return sortCommand;
			}
		}

		public Dog SelectedDogBreed
		{
			get { return selectedDogBreed; }
			set
			{
				selectedDogBreed = value;
				GetImagesForSelectedBreed();
				OnPropertyChanged("SelectedDogBreed");
			}
		}

		public string ImageUrl
		{
			get { return imageUrl; }
			set
			{
				imageUrl = value;
				OnPropertyChanged("ImageUrl");
			}
		}

		//public string Breed
		//{
		//	get { return breed; }
		//	set
		//	{
		//		breed = value;
		//		OnPropertyChanged("Breed");
		//	}
		//}

		//public string SubBreed { get; set; }

		public string SearchText
		{
			get { return searchText; }
			set
			{
				searchText = value;

				OnPropertyChanged("SearchText");
				string equalizedSearchText = searchText.ToUpper();

				FilteredDogList = new ObservableCollection<Dog>(DogList.Where(dog=>dog.Breed.ToUpper().Contains(equalizedSearchText) || dog.SubBreed.ToUpper().Contains(equalizedSearchText)));
				//Sorting again so that when you remove text, its still sorted according to Filter.
				SortDogList(FilteredDogList);
			}
		}

		public ICommand SortTypeCommand
		{
			get
			{
				if (sortTypeCommand == null)
					sortTypeCommand = new RelayCommand(SetSortType);
				return sortTypeCommand;
			}
		}

		#region Private Methods

		private void OpenAdoptWindowAction()
		{
			new AdoptionWindow().Show();
		}

		private void SetSortType(object parameter)
		{
			sortType = (string)parameter;
			SortDogList(FilteredDogList);
		}

		private void GetImagesForSelectedBreed()
		{
			//SelectedDogBreed update comes from Ui via main thread.
			//Kicking off a new thread to handle the calling of the API.
			//After response is received, pass update back to UI via main thread using Dispatcher.
			//Ui update is not awaited, giving it back as soon as the update is done.
			Task.Run(async () =>
			{
				var resp = await DogService.GetImagesForBreed(selectedDogBreed.Breed);

				if (resp.status != "success")
					return;

				if (resp.message.Length < 1)
					return;

				Application.Current.Dispatcher.BeginInvoke(
					new Action(() =>
					{
						ImageUrl = GetRandomImageUrl(resp.message);

					}), DispatcherPriority.Background);
			});
		}

		private void GetAllBreeds()
		{
			//Kicking off task on work thread to not lock up UI.
			Task.Run(async () =>
			{
				var e = await DogService.GetAllBreeds();

				if (e.status != "success")
					return;

				Application.Current.Dispatcher.BeginInvoke(
					new Action(() =>
					{
						DogList.Clear();

						//convert to ObservableCollection<Dog>
						foreach (var breedName in e.Message.Keys)
						{
							string[] subBreeds = e.Message[breedName];
							if (subBreeds.Length < 1)
							{
								DogList.Add(new Dog(breedName));
							}
							else
							{
								foreach (var subBreed in subBreeds)
								{
									DogList.Add(new Dog(breedName, subBreed));
								}
							}
						}
						FilteredDogList = new ObservableCollection<Dog>(DogList);

					}), DispatcherPriority.Background);

			});
		}

		//private void SortDogList()
		//{
		//	switch (SortType)
		//	{
		//		case "Breed":
		//			FilteredDogList = new ObservableCollection<Dog>(FilteredDogList.OrderBy(dog => dog.Breed)
		//				.ThenBy(dog => dog.SubBreed).ToList());
		//			break;
		//		case "ZA":
		//			FilteredDogList = new ObservableCollection<Dog>(FilteredDogList.OrderByDescending(dog => dog.DisplayName).ToList());
		//			break;
		//		default:
		//			FilteredDogList = new ObservableCollection<Dog>(FilteredDogList.OrderBy(dog => dog.DisplayName).ToList());
		//			break;
		//	}
		//}

		private void SortDogList(ObservableCollection<Dog> dogList)
		{
			switch (sortType)
			{
				case "Breed":
					FilteredDogList = new ObservableCollection<Dog>(dogList.OrderBy(dog => dog.Breed)
						.ThenBy(dog => dog.SubBreed).ToList());
					break;
				case "ZA":
					FilteredDogList = new ObservableCollection<Dog>(dogList.OrderByDescending(dog => dog.DisplayName).ToList());
					break;
				default:
					FilteredDogList = new ObservableCollection<Dog>(dogList.OrderBy(dog => dog.DisplayName).ToList());
					break;
			}
		}


		//Originally, i planned to update the UI via events in a non-dependent pattern.  For example if the service ever decided to send unproprted images/updates
		//I was unsure if this would come across or make sense, so i scrapped it.
		//I left the code commented out so you could see the thought process if you want, although the inner logic is pretty much the same as what exists.
		//private void CloudInterface_BreedsListUpdated(object sender, BreedsResponse e)
		//{
		//	if (e.status != "success")
		//		return;

		//	Application.Current.Dispatcher.BeginInvoke(
		//		new Action(() =>
		//		{
		//			DogList.Clear();

		//			//convert to ObservableCollection<Dog>
		//			foreach (var breedName in e.Message.Keys)
		//			{
		//				string[] subBreeds = e.Message[breedName];
		//				if (subBreeds.Length < 1)
		//				{
		//					DogList.Add(new Dog(breedName));
		//				}
		//				else
		//				{
		//					foreach (var subBreed in subBreeds)
		//					{
		//						DogList.Add(new Dog(breedName, subBreed));
		//					}
		//				}
		//			}
		//			FilteredDogList = new ObservableCollection<Dog>(DogList);

		//		}), DispatcherPriority.Background);
		//}

		//private void CloudInterface_ImageUrlUpdated(object sender, BreedImagesResponse e)
		//{
		//	if (e.status != "success")
		//		return;

		//	if (e.Message.Length < 1)
		//		return;

		//	Application.Current.Dispatcher.BeginInvoke(
		//		new Action(() =>
		//		{
		//			ImageUrl = GetRandomImageUrl(e.Message);

		//		}), DispatcherPriority.Background);
		//}

		private Random r;
		private string GetRandomImageUrl(string[] imageUrls)
		{
			if (r == null)
				r = new Random();

			int index = r.Next(0, imageUrls.Length);
			return imageUrls[index];
		}
		#endregion

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
