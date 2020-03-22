﻿//-----------------------------------------------------------------------
// <copyright file="UpdateViewModel.cs" company="Wohs">
//     Missing Copyright information from a valid stylecop.json file.
// </copyright>
//-----------------------------------------------------------------------

namespace Lurker.UI.ViewModels
{
    using Lurker.UI.Models;
    using System;
    using System.IO;
    using System.Reflection;

    public class UpdateViewModel : Caliburn.Micro.PropertyChangedBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateViewModel"/> class.
        /// </summary>
        public UpdateViewModel(UpdateState state)
        {
            if (!File.Exists(this.NeedUpdateFilePath))
            {
                File.WriteAllText(this.NeedUpdateFilePath, this.GetResourceContent(this.NeedUpdateFileName));
            }

            if (!File.Exists(this.UpdateSuccessFilePath))
            {
                File.WriteAllText(this.UpdateSuccessFilePath, this.GetResourceContent(this.UpdateSuccessFileName));
            }

            if (!File.Exists(this.UpdateWorkingFilePath))
            {
                File.WriteAllText(this.UpdateWorkingFilePath, this.GetResourceContent(this.UpdateWorkingFileName));
            }

            switch (state)
            {
                case UpdateState.NeedUpdate:
                    this.AnimationFilePath = this.NeedUpdateFilePath;
                    break;
                case UpdateState.Success:
                    this.AnimationFilePath = this.UpdateSuccessFilePath;
                    break;
                case UpdateState.Working:
                    this.AnimationFilePath = this.UpdateWorkingFilePath;
                    break;
                default:
                    throw new System.NotSupportedException();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the settings file path.
        /// </summary>
        public string AnimationFilePath { get; private set; }

        /// <summary>
        /// Gets the need update file path.
        /// </summary>
        public string NeedUpdateFilePath => Path.Combine(this.SettingsFolderPath, this.NeedUpdateFileName);

        /// <summary>
        /// Gets the update success file path.
        /// </summary>
        public string UpdateSuccessFilePath => Path.Combine(this.SettingsFolderPath, this.UpdateSuccessFileName);

        /// <summary>
        /// Gets the update success file path.
        /// </summary>
        public string UpdateWorkingFilePath => Path.Combine(this.SettingsFolderPath, this.UpdateWorkingFileName);

        /// <summary>
        /// Gets the name of the folder.
        /// </summary>
        private string FolderName => "PoeLurker";

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        private string NeedUpdateFileName => "UpdateAnimation.json";

        /// <summary>
        /// Gets the name of the update success file.
        /// </summary>
        private string UpdateSuccessFileName => "UpdateSuccessAnimation.json";

        /// <summary>
        /// Gets the name of the update working file.
        /// </summary>
        private string UpdateWorkingFileName => "UpdateWorkingAnimation.json";

        /// <summary>
        /// Gets the application data folder path.
        /// </summary>
        private string AppDataFolderPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// Gets the settings folder path.
        /// </summary>
        private string SettingsFolderPath => Path.Combine(AppDataFolderPath, FolderName);

        #endregion

        #region Methods

        /// <summary>
        /// Gets the content of the resource.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns>The animation text.</returns>
        private string GetResourceContent(string fileName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Lurker.UI.Assets.{fileName}"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        #endregion
    }
}