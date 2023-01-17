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
                    var recordingDownloadUri = new Uri(acsRecordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation);
                    var downloadRespose = await _callAutomationClient.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);

                    string filePath = $".\\recording\\{acsRecordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].DocumentId}.mp4";
                    using Stream readFromStream = downloadRespose.Value;
                    using Stream writeToStream = System.IO.File.Open(filePath, FileMode.Create);
                    await readFromStream.CopyToAsync(writeToStream);
                    await writeToStream.FlushAsync();
                }
            }
            return Results.Ok();
        }
    }
}
