using System;
using System.Collections.Generic;
using System.Linq;
using TranslationAssistant.TranslationServices.Core;
using TranslationAssistant.Business;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ADFConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Start to execute custom activity V2");

            // Parse activity and reference objects info from input files
            dynamic activity = JsonConvert.DeserializeObject(File.ReadAllText("activity.json"));
            dynamic linkedServices = JsonConvert.DeserializeObject(File.ReadAllText("linkedServices.json"));

            // Extract Connection String from LinkedService
            dynamic storageLinkedService = ((JArray)linkedServices).First(_ => "BatchStorageLinkedService".Equals(((dynamic)_).name.ToString()));
            string connectionString = storageLinkedService.properties.typeProperties.connectionString.value;

            // Extract InputFilePath & OutputFilePath from ExtendedProperties
            // In ADFv2, Input & Output Datasets are not required for Custom Activity. In this sample the folderName and 
            // fileName properties are stored in ExtendedProperty of the Custom Activity like below. You are not required
            // to get the information from Datasets. 

            //"extendedProperties": {
            //    "InputContainer": "incoming",
            //            "OutputFolder": "translated",
            //                      "TranslateServiceKey": "key goes here"
            //        }                

            string azureKey = activity.typeProperties.extendedProperties.TranslateServiceKey;//"cognitive services key";
            string outputPath = activity.typeProperties.extendedProperties.OutputFolder;//"translated"; 
            string inputContainer = activity.typeProperties.extendedProperties.InputContainer; //"incoming";

            //V1 Logger is no longer required as your executable can directly write to STDOUT
            Console.WriteLine(string.Format("InputContainer: {0}, OutputFolderPath: {1}", inputContainer, outputPath));

            // Extract Input & Output Dataset
            // If you would like to continue using Datasets, pass the Datasets in referenceObjects of the Custom Activity JSON payload like below: 

            //"referenceObjects": {
            //    "linkedServices": [
            //                {
            //                    "referenceName": "BatchStorageLinkedService",
            //                    "type": "LinkedServiceReference"
            //                }
            //            ],
            //            "datasets": [
            //                {
            //                    "referenceName": "InputDataset",
            //                    "type": "DatasetReference"
            //                },
            //                {
            //                    "referenceName": "OutputDataset",
            //                    "type": "DatasetReference"
            //                }
            //            ]
            //        }

            // Then you can use following code to get the folder and file info instead:  
            //dynamic datasets = JsonConvert.DeserializeObject(File.ReadAllText("datasets.json"));
            //dynamic inputDataset = ((JArray)datasets).First(_ => ((dynamic)_).name.ToString().StartsWith("InputDataset"));
            //dynamic outputDataset = ((JArray)datasets).First(_ => ((dynamic)_).name.ToString().StartsWith("OutputDataset"));
            //string inputFolderPath = inputDataset.properties.typeProperties.folderPath; 
            //string outputFolderPath = outputDataset.properties.typeProperties.folderPath; 
            //string outputFile = outputDataset.properties.typeProperties.fileName;
            //string outputFilePath = outputFolderPath + "/" + outputFile; 


            //Once needed info is prepared, core business logic down below remains the same. 

            Console.WriteLine("initializing blob services...");

            //Blob Storage References
            // create storage client for output. Pass the connection string.
            CloudStorageAccount outputStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient outputClient = outputStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer cbc = outputClient.GetContainerReference(inputContainer);

            Console.WriteLine("success...");

            Console.WriteLine("initializing cortana intelligence services...");

            //Cognitive Services Intialization
            TranslationServiceFacade.AzureKey = azureKey;
            TranslationServiceFacade.SaveCredentials();
            TranslationServiceFacade.Initialize();

            Console.WriteLine("success...");

            Console.WriteLine("finding blobs...");
            foreach (IListBlobItem item in cbc.ListBlobs(null, true))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    Console.WriteLine("found file {0}", blob.Name);
                    ProcessTextDocument(blob, string.Empty, "en", outputPath);
                }
            }

        }




        private static void ProcessTextDocument(CloudBlockBlob fullNameForDocumentToProcess, string sourceLanguage, string targetLanguage, string strPath)
        {
            Console.WriteLine("reading blob...");
            var document = fullNameForDocumentToProcess.OpenRead();
            List<string> lstTexts = new List<string>();
            using (var sr = new StreamReader(document, System.Text.Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    lstTexts.Add(line);
                }
            }

            string result = string.Empty;

            var batches = DocumentTranslationManager.SplitList(lstTexts, 99, 9000);

            foreach (var batch in batches)
            {
                string[] translated = TranslationServiceFacade.TranslateArray(batch.ToArray(), sourceLanguage, targetLanguage);
                result = string.Concat(translated);

            }
            string blobfilename = DocumentTranslationManager.GetOutputDocumentFullName(fullNameForDocumentToProcess.Name, targetLanguage);
            CloudBlockBlob outputBlob = CreateBlockBlob(fullNameForDocumentToProcess.Container, strPath, blobfilename);
            outputBlob.UploadText(result, System.Text.Encoding.UTF8);
            return;
        }


        private static CloudBlockBlob CreateBlockBlob(CloudBlobContainer container, string strDirectoryName, string strFileName)
        {
            CloudBlobDirectory directory = container.GetDirectoryReference(strDirectoryName);
            CloudBlockBlob blockblob = directory.GetBlockBlobReference(strFileName);
            return blockblob;
        }
   
    }

}
