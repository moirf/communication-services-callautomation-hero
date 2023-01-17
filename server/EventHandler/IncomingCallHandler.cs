// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Newtonsoft.Json;
using System.Text.Json.Nodes;

namespace CallAutomationHero.Server
{
    /// <summary>
    /// Handling different callback events
    /// and perform operations
    /// </summary>

    public class IncomingCallHandler
    {
        private readonly CallAutomationClient _callAutomationClient;
        private readonly IConfiguration _configuration;
        private CallConnection _callConnection;
        private PlayAudio playAudioFeature;
        private RecognizeDtmf recognizeDtmfFeaure;
        private RecordAudio recordAudioFeature;

        private TaskCompletionSource<bool> callEstablishedTask;

        public IncomingCallHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            _callAutomationClient = new CallAutomationClient(_configuration["ConnectionString"]);
        }

        public async Task<IResult> HandleIncomingCall(EventGridEvent[] eventGridEvents)
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
                        var responseData = new SubscriptionValidationResponse
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };
                        return Results.Ok(responseData);
                    }
                }
                else
                {
                    Logger.LogInformation($"AnswerCall - {JsonNode.Parse(eventGridEvent.Data)}");
                    var jsonObject = JsonNode.Parse(eventGridEvent.Data)!.AsObject();
                    return await AnswerCall(jsonObject);
                }
            }
            return Results.Ok();
        }

        public async Task<IResult> HandleCallback(CloudEvent[] cloudEvents, string callerId)
        {
            foreach (var cloudEvent in cloudEvents)
            {
                await callEstablishedTask.Task.ConfigureAwait(false);

                var @event = CallAutomationEventParser.Parse(cloudEvent);
                Logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event)}");

                if (@event is CallConnected)
                {
                    // Start Call recording
                    recordAudioFeature.StartCallRecording(@event.CallConnectionId);

                    //Start recognizing Dtmf
                    await recognizeDtmfFeaure.StartRecognizingDtmf(callerId);
                }
                if (@event is RecognizeCompleted { OperationContext: "MainMenu" })
                {
                    //Perform operation as per DTMF tone recieved
                    var recognizeCompleted = (RecognizeCompleted)@event;
                    await playAudioFeature.PlayAudioOperation(recognizeCompleted.CollectTonesResult.Tones[0]);
                }
                if (@event is RecognizeFailed { OperationContext: "MainMenu" })
                {
                    // play invalid audio
                    await playAudioFeature.PlayAudioToAll(new PlayOptions() { Loop = false }, PlayAudio.PlayAudioType.InvalidAudio);
                    _ = await _callConnection.HangUpAsync(true);
                }
                if (@event is PlayCompleted { OperationContext: "SimpleIVR" })
                {
                    _ = await _callConnection.HangUpAsync(true);
                }
                if (@event is PlayFailed { OperationContext: "SimpleIVR" })
                {
                    _ = await _callConnection.HangUpAsync(true);
                }
                if(@event is AddParticipantsSucceeded)
                {
                    Logger.LogInformation("Successfully added Agent participant");
                }
                if(@event is AddParticipantsFailed)
                {
                    Logger.LogError("Failed to add Agent participant");
                    _ = await _callConnection.HangUpAsync(true);
                }
            }
            return Results.Ok();
        }

        public async Task<IResult> AnswerCall(JsonObject jsonObject)
        {
            if (jsonObject != null && _callAutomationClient != null)
            {
                callEstablishedTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                var callerId = jsonObject["from"]!["rawId"]!.ToString();
                var incomingCallContext = jsonObject["incomingCallContext"]!.ToString();
                var callbackUri = new Uri(_configuration["AppBaseUri"] + $"/api/calls/{Guid.NewGuid()}?callerId={callerId}");

                // Answer Call
                var response = await _callAutomationClient.AnswerCallAsync(incomingCallContext, callbackUri);
                _callConnection = response.Value.CallConnection;

                //Initializing all the feature objects
                playAudioFeature = new PlayAudio(_configuration, _callConnection);
                recordAudioFeature = new RecordAudio(_configuration, _callAutomationClient);
                recognizeDtmfFeaure = new RecognizeDtmf(_configuration, _callConnection);

                Logger.LogInformation($"AnswerCallAsync Response -----> {response.GetRawResponse()}");

                callEstablishedTask.TrySetResult(true);

                return Results.Ok();
            }
            return Results.Problem("Answer Call failed.");

        }
    }
}
