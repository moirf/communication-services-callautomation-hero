using Azure.Storage.Blobs;

namespace CallAutomationHero.Server
{
    public static class BlobStorageHelper
    {
        /// <summary>
        /// Method to upload a file to Blob storage
        /// </summary>
        /// <param name="connectionString">Connection String details for Azure Blob Storage</param>
        /// <param name="containerName">Container Name to upload files</param>
        /// <param name="filePath">File path of the file to upload</param>
        /// <returns>BlobStorageHelperInfo</returns>
        public static async Task<(bool IsSuccess, string Message)> UploadFileAsync(string connectionString, string containerName, string filePath)
        {
            try
            {
                //checking if container is available
                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                if (!await blobContainerClient.ExistsAsync())
                {
                    return (false, $"Container {containerName} is not available");
                }

                //upload the file, if file already exists overwrite it
                string fileName = Path.GetFileName(filePath);
                BlobClient blobClient = blobContainerClient.GetBlobClient(fileName);
                var uploadStatus = await blobClient.UploadAsync(filePath, overwrite: true);
                
                return (true, $"File uploaded successfully. Uri : {blobClient.Uri}");
            }
            catch (Exception ex)
            {
                return (false, $"The file upload was not successful. Exception: {ex.Message}");
            }
        }
    }
}
