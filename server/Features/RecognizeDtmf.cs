using Azure.Communication.CallAutomation;
using Azure.Communication;

namespace CallAutomationHero.Server
{
    public class RecognizeDtmf
    {
        public static async Task StartRecognizingDtmf(string callerId, IConfiguration configuration,
        CallConnection callConnection)
        {
            var appBaseUri = configuration["AppBaseUri"];
            // Start recognize prompt - play audio and recognize 1-digit DTMF input
            var recognizeOptions = 
                new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 1)
                {
                    InterruptPrompt = true,
                    InterToneTimeout = TimeSpan.FromSeconds(10),
                    InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                    Prompt = new FileSource(new Uri(appBaseUri + configuration[PlayAudio.PlayAudioMessages.MainMenuAudio.ToString()])),
                    OperationContext = "MainMenu"
                };
            _ = await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions);
        }
    }
}
