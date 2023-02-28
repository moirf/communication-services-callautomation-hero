using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Newtonsoft.Json;

namespace CallAutomationHero.Server
{
    public class RecordingHandler
    {
        private readonly CallAutomationClient _callAutomationClient;
        private readonly IConfiguration _configuration;
        private const string CallRecodingActiveErrorCode = "8553";
        private const string CallRecodingActiveError = "Recording is already in progress, one recording can be active at one time.";

        public RecordingHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            _callAutomationClient = new CallAutomationClient(_configuration["ConnectionString"]);
        }

        public async Task<IResult> HandleRecording(EventGridEvent[] eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                Logger.LogInformation("Event " + JsonConvert.SerializeObject(eventGridEvent));

                // Handle system events
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the subscription validation event.
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        var responseData = new SubscriptionValidationResponse()
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };
                        return Results.Ok(responseData);
                    }
                }
                else if (eventData is AcsRecordingFileStatusUpdatedEventData acsRecordingFileStatusUpdatedEventData)
                {
                    try
                    {
                        var recordingDownloadUri = new Uri(acsRecordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation);
                        var downloadRespose = await _callAutomationClient.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);
                        var containerName = _configuration["BlobContainerName"];

                        string filePath = $".\\recording\\{acsRecordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].DocumentId}.mp4";
                        using Stream readFromStream = downloadRespose.Value;
                        using Stream writeToStream = System.IO.File.Open(filePath, FileMode.Create);
                        await readFromStream.CopyToAsync(writeToStream);
                        await writeToStream.FlushAsync();

                        Logger.LogInformation($"Starting to upload .mp4 to BlobStorage into container -- > {containerName}");

                        var blobStorageHelperInfo = await BlobStorageHelper.UploadFileAsync(_configuration["BlobStorageConnectionString"], containerName, filePath, filePath);
                        if (blobStorageHelperInfo.Status)
                        {
                            Logger.LogInformation(blobStorageHelperInfo.Message);
                            Logger.LogInformation($"Deleting temporary .mp4 file being created");
                            System.IO.File.Delete(filePath);
                        }
                        else
                        {
                            Logger.LogError($".mp4 file was not uploaded,{blobStorageHelperInfo.Message}");
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.LogError($"Failed to upload the file. error message,{ex.Message}");
                    }
                    
                }
            }
            return Results.Ok();
        }

        /// <summary>
        /// Method to start call recording
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        public async Task<IResult> StartRecordingAsync(string serverCallId)
        {
            try
            {
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    //Passing RecordingContent initiates recording in specific format. audio/audiovideo
                    //RecordingChannel is used to pass the channel type. mixed/unmixed
                    //RecordingFormat is used to pass the format of the recording. mp4/mp3/wav
                    StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
                    var startRecordingResponse = await _callAutomationClient.GetCallRecording()
                        .StartRecordingAsync(recordingOptions).ConfigureAwait(false);

                    Logger.LogInformation($"StartRecordingAsync response -- >  {startRecordingResponse.GetRawResponse()}, Recording Id: {startRecordingResponse.Value.RecordingId}");

                    return Results.Json(startRecordingResponse.Value);
                }
                else
                {
                    return Results.Json(new { Message = "serverCallId is invalid" });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(CallRecodingActiveErrorCode))
                {
                    return Results.Json(new { Message = CallRecodingActiveError });
                }
                return Results.Json(new { Exception = ex });
            }
        }


        /// <summary>
        /// Method to stop call recording
        /// </summary>
        /// <param name="recordingId">Recording id of the call</param>
        /// <returns></returns>
        public async Task<IResult> StopRecordingAsync(string recordingId)
        {
            try
            {
                if (!string.IsNullOrEmpty(recordingId))
                {
                    var stopRecording = await _callAutomationClient.GetCallRecording().StopRecordingAsync(recordingId).ConfigureAwait(false);
                    Logger.LogInformation($"StopRecordingAsync response -- > {stopRecording}");

                    return Results.Ok();
                }
                else
                {
                    return Results.Json(new { Message = "recordingId is invalid" });
                }
            }
            catch (Exception ex)
            {
                return Results.Json(new { Exception = ex });
            }
        }
    }
}
