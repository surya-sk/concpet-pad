﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ConceptPad.Models;
using muxc = Microsoft.UI.Xaml.Controls;
using ConceptPad.Saving;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.Storage;
using Windows.UI.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.System.Profile;
using System.Diagnostics;
using Microsoft.Graph;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConceptPad.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Concept> concepts;
        private string type;
        StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
        string fileName = "concepts.txt";
        GraphServiceClient graphServiceClient;

        public MainPage()
        {
            this.InitializeComponent();
            graphServiceClient = Profile.GetInstance().GetGraphServiceClient();
            Task.Run(async () => { await Profile.GetInstance().ReadProfileAsync(); }).Wait();
            ObservableCollection<Concept> readConcepts = Profile.GetInstance().GetConcepts();
            concepts = new ObservableCollection<Concept>(readConcepts.OrderByDescending(c => c.DateCreated)); // sort by last created

            InitUIPrefs();

            UpdateNotificationQueue();
        }

        private async void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            ProgRing.IsActive = true;
            await DownloadConceptsAsync();
            Frame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Download concepts and save them locally
        /// </summary>
        /// <returns></returns>
        private async Task DownloadConceptsAsync()
        {
            var search = await graphServiceClient.Me.Drive.Root.Search(fileName).Request().GetAsync();
            if (search.Count == 0)
            {
                return;
            }
            StorageFile storageFile = await roamingFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (Stream stream = await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Content.Request().GetAsync())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    await FileIO.WriteTextAsync(storageFile, sr.ReadToEnd());
                }
            }
        }

        /// <summary>
        /// Upload concepts to OneDrive
        /// </summary>
        /// <returns></returns>
        public async Task UploadConceptsAsync()
        {
            if (graphServiceClient is null)
            {
                return;
            }
            StorageFile storageFile = await roamingFolder.GetFileAsync(fileName);
            using (var stream = await storageFile.OpenStreamForWriteAsync())
            {
                await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(stream);
            }
        }

        private void UpdateNotificationQueue()
        {
            // Set tile notification queue
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
            string showLiveTile = ApplicationData.Current.LocalSettings.Values["LiveTileOn"]?.ToString();
            if (showLiveTile == null || showLiveTile == "True")
            {
                foreach (Concept c in concepts)
                {
                    UpdateLiveTile(c);
                }
            }
        }

        private void InitUIPrefs()
        {
            string theme = GetTheme();
            foreach (Concept c in concepts)
            {
                c.ImagePath = $@"ms-appx:///Assets/{c.Type.ToLower()}-{theme}.png";
            }

            string cmdLabelPref = (string)ApplicationData.Current.LocalSettings.Values["CmdBarLabels"];
            if (cmdLabelPref == null || cmdLabelPref == "No")
            {
                CmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom;
            }
            else
            {
                CmdBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
            }

            // disable back button
            var view = SystemNavigationManager.GetForCurrentView();
            view.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Disabled;


            if (concepts.Count == 0)
            {
                EmtpyListText.Visibility = Visibility.Visible;
            }
        }


        /// <summary>
        /// Get the current app theme. Used for choosing the right icons
        /// </summary>
        /// <returns>a string representing current app theme</returns>
        private static string GetTheme()
        {
            string savedTheme = ApplicationData.Current.LocalSettings.Values["SelectedAppTheme"]?.ToString();
            string theme = string.Empty;
            if (savedTheme == null || savedTheme == "Default")
            {
                var defautlTheme = new Windows.UI.ViewManagement.UISettings();
                var uiTheme = defautlTheme.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background).ToString();
                if (uiTheme.Equals("#FF000000"))
                {
                    savedTheme = "Dark";
                }
                else
                {
                    savedTheme = "Light";
                }
            }
            theme = savedTheme;

            return theme.ToLower();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(TypeButtons != null && sender is muxc.RadioButtons rb)
            {
                type = rb.SelectedItem as string;
                switch(type)
                {
                    case "Game":
                        ToolsText.Text = "Engine";
                        break;
                    case "App":
                        ToolsText.Text = "Framework";
                        break;
                }
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(NameInput.Text) || string.IsNullOrEmpty(DescriptionInput.Text) || string.IsNullOrEmpty(ToolsInput.Text) || string.IsNullOrEmpty(GenresInput.Text) || string.IsNullOrEmpty(PlatformsInput.Text))
            {
                ShowInvalidParamDialog();
            }
            else
            {
                CreateAndAddConcept();
                Frame.Navigate(typeof(MainPage));
            }
        }

        /// <summary>
        /// Create a concept and add it to the list, then write it to the save file
        /// </summary>
        private async void CreateAndAddConcept()
        {
            Concept concept = new Concept()
            {
                Id = Guid.NewGuid(),
                Name = NameInput.Text,
                Summary = SummaryInput.Text,
                Description = DescriptionInput.Text,
                Type = type,
                Tools = ToolsInput.Text,
                Genres = GenresInput.Text,
                Platforms = PlatformsInput.Text,
                DateCreated = DateTime.Now.ToString("D")
            };
            concepts.Add(concept);
            Profile.GetInstance().SaveSettings(concepts);
            ProgRing.IsActive = true;
            Task.Run(async () => { await Profile.GetInstance().WriteProfileAsync(); }).Wait();
            await UploadConceptsAsync();
            ProgRing.IsActive = false;
        }

        /// <summary>
        /// Show content dialog if all the fields are not filled in
        /// </summary>
        private async void ShowInvalidParamDialog()
        {
            ContentDialog contentDialog = new ContentDialog
            {
                Title = "Invalid Parameters",
                Content = "Please fill in all the fields",
                CloseButtonText = "Ok"
            };
            ContentDialogResult result = await contentDialog.ShowAsync();
        }

        private void ConceptView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedConcept = (Concept)e.ClickedItem;
            if(AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Frame.Navigate(typeof(ConceptPage), selectedConcept.Id, new DrillInNavigationTransitionInfo());
            }
            else
            {
                Frame.Navigate(typeof(ConceptPage), selectedConcept.Id, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
        }

        /// <summary>
        /// Update the live tile to show concepts based on the notification queue
        /// </summary>
        /// <param name="c"></param>
        private void UpdateLiveTile(Concept c)
        {
            var tileContent = new TileContent()
            {
                Visual = new TileVisual()
                {

                    TileMedium = new TileBinding()
                    {
                        Branding = TileBranding.Name,
                        DisplayName = "Concept Pad",
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = $@"ms-appx:///Assets/{c.Type.ToLower()}-light.png",
                                HintOverlay = 80
                            },
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = c.Name,
                                    HintWrap = true,
                                    HintMaxLines = 2
                                },
                                new AdaptiveText()
                                {
                                    Text = c.Type,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },
                                new AdaptiveText()
                                {
                                    Text = c.Genres,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    },
                    TileWide = new TileBinding()
                    {
                        Branding = TileBranding.NameAndLogo,
                        DisplayName = "Concept Pad",
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = $@"ms-appx:///Assets/{c.Type.ToLower()}-light.png",
                                HintOverlay = 80
                            },
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = c.Name
                                },
                                new AdaptiveText()
                                {
                                    Text = $"{c.Tools} - {c.Genres}",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                },
                                new AdaptiveText()
                                {
                                    Text = $"{c.Summary}",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                }
                            }
                        }
                    },
                }
            };

            // Create the tile notification
            var tileNotif = new TileNotification(tileContent.GetXml());

            // And send the notification to the primary tile
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
        }

        private async void RateButton_Click(object sender, RoutedEventArgs e)
        {
            var ratingUri = new Uri(@"ms-windows-store://review/?ProductId=9N9CV4TS3VB1");
            await Windows.System.Launcher.LaunchUriAsync(ratingUri);
        }
    }
}
