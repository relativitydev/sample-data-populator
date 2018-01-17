using System;
using System.IO;
using Newtonsoft.Json;
using Relativity.API;

namespace DP.Helpers.Models
{
    public class DocumentImportSettings
    {
        private int _numberOfDocuments;
        public string RelativityUsername { get; set; }
        public string RelativityPassword { get; set; }
        public string RelativityLibraryFolder { get; set; }

        public int NumberOfDocuments
        {
            get { return _numberOfDocuments; }
            set
            {
                if (value > Constants.MaximumNumberOfDocuments)
                {
                    _numberOfDocuments = Constants.MaximumNumberOfDocuments;
                }
                else
                {
                    _numberOfDocuments = value;
                }
            }
        }
        public bool ImportImagesWithDocuments { get; set; }
        public bool ImportProductionImagesWithDocuments { get; set; }

        public IAPILog Logger { get; set; }

        public DocumentImportSettings()
        {
            
        }
        public DocumentImportSettings(string importSettingsFileLocation)
        {
            if (File.Exists(importSettingsFileLocation))
            {
                using (var fStream = File.Open(importSettingsFileLocation, FileMode.Open))
                {
                    ParseSettings(fStream);
                }
            }
            else
            {
                LogError("File does not exist" + importSettingsFileLocation, new FileNotFoundException());
                throw new FileNotFoundException();
            }
        }
        public DocumentImportSettings(Stream documentImportSettingsStream)
        {
            ParseSettings(documentImportSettingsStream);
        }

        private void ParseSettings(Stream documentImportSettings)
        {
            try
            {
                var serializer = new JsonSerializer();

                using (var sr = new StreamReader(documentImportSettings))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    var tempSettings = serializer.Deserialize<DocumentImportSettings>(jsonTextReader);
                    RelativityUsername = tempSettings.RelativityUsername;
                    RelativityPassword = tempSettings.RelativityPassword;
                    RelativityLibraryFolder = tempSettings.RelativityLibraryFolder;
                    NumberOfDocuments = tempSettings.NumberOfDocuments;
                    ImportImagesWithDocuments = tempSettings.ImportImagesWithDocuments;
                    ImportProductionImagesWithDocuments = tempSettings.ImportProductionImagesWithDocuments;
                }

            }
            catch (Exception ex)
            {
                var error = $"Error parsing settings configuration settings:";
                LogError(error, ex);
                throw new Exception(error, ex);
            }
        }

        private void LogError(string message, Exception ex)
        {
            if (Logger != null)
            {
                Logger.LogError(message, ex);
            }
        }
    }
}
