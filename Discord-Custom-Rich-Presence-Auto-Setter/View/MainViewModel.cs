﻿#region
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows;
using Discord_Custom_Rich_Presence_Auto_Setter.Models;
using Discord_Custom_Rich_Presence_Auto_Setter.Models.Interfaces;
using Discord_Custom_Rich_Presence_Auto_Setter.Models.Metadata;
using Discord_Custom_Rich_Presence_Auto_Setter.Models.Requirements;
using Discord_Custom_Rich_Presence_Auto_Setter.Service;
using Discord_Custom_Rich_Presence_Auto_Setter.Utils;
using GameSDK.GameSDK;
using Activity = Discord_Custom_Rich_Presence_Auto_Setter.Models.Activity;
using ICloneable = Discord_Custom_Rich_Presence_Auto_Setter.Models.Interfaces.ICloneable;
using Lobby = Discord_Custom_Rich_Presence_Auto_Setter.Models.Lobby;
#endregion

namespace Discord_Custom_Rich_Presence_Auto_Setter.View {
	public class MainViewModel : INotifyPropertyChanged {
		private ObservableCollection<IListable> _list;
		private IListable _selected;
		private string _status = "Application started";

		public RelayCommand ActivitiesClick => new(() => { List = Data.Activities; }, () => List != Data.Activities);
		public Visibility ActivityVisibility => SelectedActivity == null ? Visibility.Hidden : Visibility.Visible;
		public RelayCommand AddClick => new(() => {
			if (List == Data.Configs) {
				Data.Configs.Add(new Config());
			} else if (List == Data.Activities) {
				Data.Activities.Add(new Activity());
			} else if (List == Data.Lobbies) {
				Data.Lobbies.Add(new Lobby());
			}
		}, () => List != App);
		public RelayCommand AddMetaData => new(() => { (Selected as Lobby)?.Metadata?.Add(new Metadata()); });

		public RelayCommand AddRequirement => new(() => { (Selected as Config)?.Requirements?.Add(new DayRequirement()); });
		private ObservableCollection<IListable> App { get; } = new();

		public RelayCommand AppClick => new(() => { List = App; }, () => List != App);
		public ApplicationSettings ApplicationSettings => Discord_Custom_Rich_Presence_Auto_Setter.App.Settings;
		public Visibility AppVisibility => List == App ? Visibility.Visible : Visibility.Hidden;

		public RelayCommand ConfigsClick => new(() => { List = Data.Configs; }, () => List != Data.Configs);
		public Visibility ConfigVisibility => SelectedConfig == null ? Visibility.Hidden : Visibility.Visible;
		public FileSyncedConfigs Data { get; }
		public RelayCommand DeleteClick => new(() => { List.Remove(Selected); }, () => Selected != null);

		public RelayCommand DeleteMetaData => new(obj => {
			Metadata metadata = obj as Metadata;
			(Selected as Lobby)?.Metadata?.Remove(metadata);
		});
		public RelayCommand DownClick => new(() => { List.Move(SelectedIndex, SelectedIndex + 1); }, () => Selected != null && SelectedIndex < List.Count - 1);
		public RelayCommand DuplicateClick => new(() => { List.Insert(SelectedIndex + 1, ICloneable.Clone(Selected)); }, () => Selected != null);
		public ViewHelperModel HelperModel { get; } = new(Enum.GetValues<DayRequirement.NumberEquality>(), Enum.GetValues<DayOfWeek>(),
			Enum.GetValues<ActivityType>(), Enum.GetValues<LobbyType>(), new[] { false, true }, Enum.GetValues<Requirement.RequirementType>());
		public RelayCommand LobbiesClick => new(() => { List = Data.Lobbies; }, () => List != Data.Lobbies);

		public Visibility LobbyVisibility => SelectedLobby == null ? Visibility.Hidden : Visibility.Visible;

		public RelayCommand OpenDataDirectory => new(() => { Process.Start("explorer.exe", FileService.ApplicationFolderPath); });

		private RichPresenceManager RichPresenceManager { get; }

		public Activity SelectedActivity => Selected as Activity;

		public Config SelectedConfig => Selected as Config;
		public Lobby SelectedLobby => Selected as Lobby;

		public string TotalFileSize => $"{FileService.Instance.TotalFileBytes / 1024} kB";
		public RelayCommand UpClick => new(() => { List.Move(SelectedIndex, SelectedIndex - 1); }, () => Selected != null && SelectedIndex > 0);

		public ObservableCollection<IListable> List {
			get => _list;
			private set {
				_list = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof (AppVisibility));
			}
		}
		public IListable Selected {
			get => _selected;
			set {
				_selected = value;
				OnPropertyChanged(nameof (Selected));
				OnPropertyChanged(nameof (SelectedLobby));
				OnPropertyChanged(nameof (SelectedActivity));
				OnPropertyChanged(nameof (SelectedConfig));
				OnPropertyChanged(nameof (LobbyVisibility));
				OnPropertyChanged(nameof (ActivityVisibility));
				OnPropertyChanged(nameof (ConfigVisibility));
			}
		}
		public int SelectedIndex { get; set; }

		public string Status {
			get => _status;
			set {
				_status = value;
				OnPropertyChanged();
				FileService.Instance.Log($"Status: {value}");
			}
		}

		public MainViewModel() {
			RichPresenceManager = new RichPresenceManager(ApplicationSettings.DefaultApplicationId);
			Data = RichPresenceManager.Configs;
			List = App;
			RichPresenceManager.CurrentlyUsedConfigChanged += RichPresenceChanged;
			RichPresenceManager.ExceptionOccurred += ManagerExceptionOccurred;
			RichPresenceManager.Start();
			FileService.Instance.PropertyChanged += TotalFileSizeChanged;
		}

		private void ManagerExceptionOccurred(Exception exception) {
			Status = $"Error: '{exception.Message}'";
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void RichPresenceChanged(Config config) {
			Status = config != null ? $"{config.Name} is currently set as your rich presence" : "No rich presence is currently set";
		}

		private void TotalFileSizeChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof (FileService.Instance.TotalFileBytes)) {
				OnPropertyChanged(nameof (TotalFileSize));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
		public record ViewHelperModel (DayRequirement.NumberEquality[] NumberEquality, DayOfWeek[] WeekDay, ActivityType[] ActivityType, LobbyType[] LobbyType,
			bool[] Boolean, Requirement.RequirementType[] RequirementType);
	}
}
