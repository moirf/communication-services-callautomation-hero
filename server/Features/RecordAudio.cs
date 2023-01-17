﻿using Azure.Communication.CallAutomation;

namespace CallAutomationHero.Server
{
    public class RecordAudio
    {
        IConfiguration _configuration;
        CallAutomationClient client;
        public RecordAudio(IConfiguration configuration, CallAutomationClient client)
        {
            _configuration = configuration;
            this.client = client;
        }

        public void StartCallRecording(string callConnectionId)
        {
            try
            {
                // Start call recording
                var serverCallId = client.GetCallConnection(callConnectionId)
                    .GetCallConnectionProperties().Value.ServerCallId;
                var startRecordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));

                _ = Task.Run(async () => await client.GetCallRecording().StartRecordingAsync(startRecordingOptions));
                Logger.LogInformation("Successfully started recording");
            }
            catch(Exception ex)
            {
                Logger.LogError("Failed to start recording.  error message: " + ex.Message);
            }
        }
    }
}
