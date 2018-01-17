using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using DP.Helpers.Models;
using DP.Helpers.RSAPI;
using DP.Populator.Interfaces;
using kCura.Relativity.Client;
using kCura.Relativity.ImportAPI;
using Relativity.API;

namespace DP.Populator
{
    public class DocumentPopulator : IPopulator
    {
        public IRSAPIClient RsapiClient { get; set; }
        public int WorkspaceArtifactID { get; set; }
        public IImportAPI ImportApi { get; set; }
        public IArtifactQueries ArtifactQueries { get; set; }
        public IAPILog Logger { get; set; }
        public DocumentImportSettings Settings { get; }

        public readonly string NativeFileLocation;
        public readonly string ImageFileLocation;
        public readonly string ProductionImageFileLocation;

        public DocumentPopulator(IRSAPIClient rsapiClient, int workspaceArtifactID, IImportAPI importApi, IArtifactQueries artifactQueries, IAPILog logger, DocumentImportSettings settings)
        {
            RsapiClient = rsapiClient;
            WorkspaceArtifactID = workspaceArtifactID;
            ImportApi = importApi;
            ArtifactQueries = artifactQueries;
            Logger = logger;
            Settings = settings;
            NativeFileLocation = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), Helpers.Constants.FileNames.NativeName);
            ImageFileLocation = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), Helpers.Constants.FileNames.ImageName);
            ProductionImageFileLocation = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), Helpers.Constants.FileNames.ProductionImageName);
        }

        public async Task PopulateDataAsync()
        {
            try
            {
                var docIdentifiers = GenerateIdentifiers(Settings.NumberOfDocuments);
                var documentIdentifyingField = await ArtifactQueries.GetIdentityFieldAsync(RsapiClient, WorkspaceArtifactID, Helpers.Constants.ArtifactTypeIDs.Document);

                if (documentIdentifyingField != null)
                {
                    await ImportDocumentsAsync(documentIdentifyingField, docIdentifiers);
                    if (Settings.ImportImagesWithDocuments)
                    {
                        await ImportImagesAsync(documentIdentifyingField, docIdentifiers);
                    }

                    if (Settings.ImportProductionImagesWithDocuments)
                    {
                        var productionIdentifyingField = await ArtifactQueries.GetIdentityFieldAsync(RsapiClient, WorkspaceArtifactID, Helpers.Constants.ArtifactTypeIDs.Production);
                        if (productionIdentifyingField != null)
                        {
                            var nameOfProductionSet = Guid.NewGuid().ToString();
                            await CreateProductionSetAsync(productionIdentifyingField, nameOfProductionSet);
                            var productionSetArtifactID = await ArtifactQueries.QueryProductionSetArtifactID(RsapiClient, WorkspaceArtifactID, nameOfProductionSet);
                            if (productionSetArtifactID > 0)
                            {
                                await ImportProductionAsync(documentIdentifyingField, productionSetArtifactID, docIdentifiers);
                            }
                            else
                            {
                                throw new Exception($"Unable to find Production set: {nameOfProductionSet}");
                            }
                        }
                        else
                        {
                            throw new Exception("Unable to find Production Identifying field");
                        }
                    }
                }
                else
                {
                    throw new Exception("Unable to find document identifying field");
                }

            }
            catch (Exception ex)
            {
                Logger.LogError("Error populating document test data: " + ex.ToString());
                throw;
            }

        }
        private IEnumerable<String> GenerateIdentifiers(int numberOfDocuments)
        {
            var identifiers = new String[numberOfDocuments];
            for (var i = 0; i < numberOfDocuments; i++)
            {
                identifiers[i] = Guid.NewGuid().ToString();
            }
            return identifiers;
        }

        private async Task ImportDocumentsAsync(kCura.Relativity.Client.DTOs.Field identifyingField, IEnumerable<String> docIdentifiers)
        {
            try
            {
                var importJob = ImportApi.NewNativeDocumentImportJob();

                importJob.OnMessage += OnMessage;
                importJob.OnComplete += OnComplete;
                importJob.OnFatalException += OnError;

                importJob.Settings.CaseArtifactId = WorkspaceArtifactID;
                importJob.Settings.ExtractedTextFieldContainsFilePath = false;
                importJob.Settings.NativeFilePathSourceFieldName = "Native File";
                importJob.Settings.ParentObjectIdSourceFieldName = "Parent Document ID";
                importJob.Settings.SelectedIdentifierFieldName = identifyingField.Name;

                importJob.Settings.NativeFileCopyMode = kCura.Relativity.DataReaderClient.NativeFileCopyModeEnum.CopyFiles;
                importJob.Settings.OverwriteMode = kCura.Relativity.DataReaderClient.OverwriteModeEnum.Append;
                importJob.Settings.IdentityFieldId = identifyingField.ArtifactID;

                importJob.SourceData.SourceData = GetDocumentDataTable(identifyingField, docIdentifiers).CreateDataReader();

                await Task.Run(() => importJob.Execute());
            }
            catch (Exception ex)
            {
                Logger.LogError("Error occured while creating documents: " + ex.ToString());
                throw;
            }

        }

        private async Task ImportImagesAsync(kCura.Relativity.Client.DTOs.Field identifyingField, IEnumerable<String> docIdentifiers)
        {
            try
            {
                var importJob = ImportApi.NewImageImportJob();

                importJob.OnMessage += OnMessage;
                importJob.OnComplete += OnComplete;
                importJob.OnFatalException += OnError;

                importJob.Settings.CaseArtifactId = WorkspaceArtifactID;
                importJob.Settings.AutoNumberImages = false;
                importJob.Settings.BatesNumberField = "Bates";

                importJob.Settings.DocumentIdentifierField = "Control Number";
                importJob.Settings.FileLocationField = "File";
                importJob.Settings.CopyFilesToDocumentRepository = true;

                importJob.Settings.IdentityFieldId = identifyingField.ArtifactID;
                importJob.Settings.OverwriteMode = kCura.Relativity.DataReaderClient.OverwriteModeEnum.Overlay;
                importJob.SourceData.SourceData = GetImageDataTable(identifyingField, docIdentifiers);

                await Task.Run(() => importJob.Execute());
            }
            catch (Exception ex)
            {
                Logger.LogError("Error occured while importing images: " + ex.ToString());
                throw;
            }
        }

        public async Task CreateProductionSetAsync(kCura.Relativity.Client.DTOs.Field identifyingField, string nameOfProductionSet)
        {
            var importJob = ImportApi.NewObjectImportJob(17);

            importJob.OnMessage += OnMessage;
            importJob.OnComplete += OnComplete;
            importJob.OnFatalException += OnError;

            importJob.Settings.SelectedIdentifierFieldName = identifyingField.Name;

            importJob.Settings.IdentityFieldId = identifyingField.ArtifactID;

            importJob.Settings.CaseArtifactId = WorkspaceArtifactID;
            importJob.Settings.OverwriteMode = kCura.Relativity.DataReaderClient.OverwriteModeEnum.Append;
            importJob.SourceData.SourceData = GetProductionSetDataTable(nameOfProductionSet).CreateDataReader();

            Console.WriteLine("Executing import...");

            await Task.Run(() => importJob.Execute());
        }

        public async Task ImportProductionAsync(kCura.Relativity.Client.DTOs.Field identifyingField, int productionArtifactID, IEnumerable<String> docIdentifiers)
        {
            try
            {
                var importJob = ImportApi.NewProductionImportJob(productionArtifactID);

                importJob.OnMessage += OnMessage;
                importJob.OnComplete += OnComplete;
                importJob.OnFatalException += OnError;

                importJob.Settings.IdentityFieldId = identifyingField.ArtifactID;
                importJob.Settings.AutoNumberImages = false;

                importJob.Settings.BatesNumberField = "Bates";
                importJob.Settings.CaseArtifactId = WorkspaceArtifactID;
                importJob.Settings.DocumentIdentifierField = "Doc";
                importJob.Settings.ExtractedTextFieldContainsFilePath = false;

                importJob.Settings.FileLocationField = "FileLoc";
                importJob.Settings.NativeFileCopyMode = kCura.Relativity.DataReaderClient.NativeFileCopyModeEnum.CopyFiles;
                importJob.Settings.OverwriteMode = kCura.Relativity.DataReaderClient.OverwriteModeEnum.Overlay;

                importJob.SourceData.SourceData = GetProductionDataTable(docIdentifiers);

                Console.WriteLine("Executing import...");

                await Task.Run(() => importJob.Execute());
            }
            catch (Exception ex)
            {
                Logger.LogError("Error occured while importing production: " + ex.ToString());
                throw;
            }
        }

        private DataTable GetDocumentDataTable(kCura.Relativity.Client.DTOs.Field identifyingField, IEnumerable<String> docIdentifiers)
        {
            DataTable table = new DataTable();

            // The document identifer column name must match the field name in the workspace.
            table.Columns.Add(identifyingField.Name, typeof(string));
            table.Columns.Add("Native File", typeof(string));
            table.Columns.Add("Parent Document ID", typeof(string));
            foreach (var identifier in docIdentifiers)
            {
                table.Rows.Add(identifier, NativeFileLocation, String.Empty);
            }

            return table;
        }

        private DataTable GetImageDataTable(kCura.Relativity.Client.DTOs.Field identifyingField, IEnumerable<String> docIdentifiers)
        {
            DataTable table = new DataTable();

            // The document identifer column name must match the field name in the workspace.
            table.Columns.Add(identifyingField.Name, typeof(string));
            table.Columns.Add("Bates", typeof(string));
            table.Columns.Add("File", typeof(string));
            foreach (var identifier in docIdentifiers)
            {
                table.Rows.Add(identifier, identifier, ImageFileLocation);
            }

            return table;
        }

        public static DataTable GetProductionSetDataTable(string nameOfProductionSet)
        {
            DataTable table = new DataTable();

            // Column names must match the object field name in the workspace.
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Branding Font Size", typeof(int));
            table.Columns.Add("Date Produced", typeof(DateTime));
            table.Columns.Add("Scale Branding Font for Viewer", typeof(bool));
            table.Columns.Add("Prefix", typeof(string));
            table.Columns.Add("Start Number", typeof(int));
            table.Columns.Add("Copy Production On Workspace Create", typeof(bool));

            table.Rows.Add(nameOfProductionSet, "10", DateTime.Now, true, "iapiT", 1, true);

            return table;
        }

        public DataTable GetProductionDataTable(IEnumerable<String> docIdentifiers)
        {
            DataTable table = new DataTable();

            // The document identifer column name must match the field name in the workspace.
            table.Columns.Add("Bates");
            table.Columns.Add("Doc", typeof(string));
            table.Columns.Add("FileLoc", typeof(string));
            foreach (var identifier in docIdentifiers)
            {
                table.Rows.Add(identifier, identifier, ProductionImageFileLocation);
            }

            return table;
        }

        private void OnMessage(kCura.Relativity.DataReaderClient.Status status)
        {
            Logger.LogDebug(status.Message);
        }
        private void OnComplete(JobReport jobReport)
        {
            Logger.LogDebug("Import Completed with the following number of errors: " + jobReport.ErrorRowCount);
        }
        private void OnError(JobReport jobReport)
        {
            Logger.LogError(jobReport.FatalException.ToString());
            throw new Exception(jobReport.FatalException.ToString());
        }

    }
}
