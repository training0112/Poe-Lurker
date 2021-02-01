﻿//-----------------------------------------------------------------------
// <copyright file="BuildManagerViewModel.cs" company="Wohs Inc.">
//     Copyright © Wohs Inc.
// </copyright>
//-----------------------------------------------------------------------

namespace Lurker.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Caliburn.Micro;
    using Lurker.Helpers;
    using Lurker.Models;
    using Lurker.Services;
    using Lurker.UI.Models;
    using MahApps.Metro.Controls.Dialogs;

    /// <summary>
    /// Class BuildManagerViewModel.
    /// Implements the <see cref="Caliburn.Micro.PropertyChangedBase" />.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    public class BuildManagerViewModel : Caliburn.Micro.PropertyChangedBase
    {
        #region Fields

        private ObservableCollection<BuildConfigurationViewModel> _configurations;
        private Func<string, string, MessageDialogStyle?, Task<MessageDialogResult>> _showMessage;
        private BuildManagerContext _context;
        private bool _skipOpen;
        private bool _isFlyoutOpen;
        private BuildConfigurationViewModel _selectedConfiguration;
        private BuildService _buildService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildManagerViewModel" /> class.
        /// </summary>
        /// <param name="showMessage">The show message.</param>
        public BuildManagerViewModel(Func<string, string, MessageDialogStyle?, Task<MessageDialogResult>> showMessage)
        {
            this._buildService = IoC.Get<BuildService>();
            this._showMessage = showMessage;
            this._configurations = new ObservableCollection<BuildConfigurationViewModel>();
            this._context = new BuildManagerContext(this.Remove, this.Open);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this instance is flyout open.
        /// </summary>
        public bool IsFlyoutOpen
        {
            get
            {
                return this._isFlyoutOpen;
            }

            set
            {
                this._isFlyoutOpen = value;
                this.NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Gets or sets the selected configuration.
        /// </summary>
        public BuildConfigurationViewModel SelectedConfiguration
        {
            get
            {
                return this._selectedConfiguration;
            }

            set
            {
                this._selectedConfiguration = value;
                this.NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Gets or sets the path of building code.
        /// </summary>
        /// <value>The path of building code.</value>
        public string PathOfBuildingCode { get; set; }

        /// <summary>
        /// Gets the c onfigurations.
        /// </summary>
        /// <value>The configurations.</value>
        public ObservableCollection<BuildConfigurationViewModel> Configurations
        {
            get
            {
                return this._configurations;
            }

            private set
            {
                this._configurations = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Synchronizes this instance.
        /// </summary>
        public void Sync()
        {
            Execute.OnUIThread(async () =>
            {
                await this.PopulateBuilds();
            });
        }

        /// <summary>
        /// Adds this instance.
        /// </summary>
        public async void Add()
        {
            var text = ClipboardHelper.GetClipboardText();
            if (Uri.TryCreate(text, UriKind.Absolute, out Uri url))
            {
                var rawUri = new Uri($"https://pastebin.com/raw{url.AbsolutePath}");
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, rawUri);
                    var response = await client.SendAsync(request);
                    text = await response.Content.ReadAsStringAsync();
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                await this.ShowError();
                return;
            }

            using (var service = new PathOfBuildingService())
            {
                await service.InitializeAsync();
                try
                {
                    var build = service.Decode(text);
                    var simpleBuild = this._buildService.AddBuild(build);
                    this._buildService.Save();
                    this.Configurations.Add(new BuildConfigurationViewModel(simpleBuild));
                }
                catch
                {
                    await this.ShowError();
                }
            }
        }

        /// <summary>
        /// Populates the builds.
        /// </summary>
        private async Task PopulateBuilds()
        {
            // Sync with Path of Building
            await this._buildService.Sync();
            foreach (var build in this._buildService.Builds)
            {
                this._configurations.Add(new BuildConfigurationViewModel(build));
            }
        }

        /// <summary>
        /// Shows the error.
        /// </summary>
        /// <returns>Task.</returns>
        private Task ShowError()
        {
            return this._showMessage("Oups!", "You need to have a POB code in the clipboard.", MessageDialogStyle.Affirmative);
        }

        /// <summary>
        /// Opens the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public void Open(BuildConfigurationViewModel configuration)
        {
            if (this._skipOpen)
            {
                this._skipOpen = false;
                return;
            }

            this.IsFlyoutOpen = true;
            this.SelectedConfiguration = configuration;
        }

        /// <summary>
        /// Raises the Close event.
        /// </summary>
        public void OnClose()
        {
            this._buildService.Save();
        }

        /// <summary>
        /// Removes the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public async void Remove(BuildConfigurationViewModel configuration)
        {
            this._skipOpen = true;
            var result = await this._showMessage("Are you sure?", $"You are about to delete {configuration.BuildName}", MessageDialogStyle.AffirmativeAndNegative);

            if (result == MessageDialogResult.Affirmative)
            {
                this.Configurations.Remove(configuration);
                this._buildService.RemoveBuild(configuration.Id);
                this._buildService.Save();
            }
        }

        #endregion
    }
}