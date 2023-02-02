using Azure.Communication.CallAutomation;
using Azure.Communication;

namespace CallAutomationHero.Server
{
    public class PlayAudio
    {
        public enum PlayAudioMessages
        {
            MainMenuAudio,
            SalesAudio,
            MarketingAudio,
            CustomerCareAudio,
            AgentAudio,
            InvalidAudio
        }

        public static async Task PlayAudioOperation(DtmfTone toneReceived, IConfiguration configuration, 
            CallConnection callConnection)
        {
            var audioPlayOptions = new PlayOptions() { OperationContext = "SimpleIVR", Loop = false };

            if (toneReceived == DtmfTone.One)
            {
                await PlayAudioToAll(audioPlayOptions, PlayAudioMessages.SalesAudio, configuration, callConnection);
            }
            else if (toneReceived == DtmfTone.Two)
            {
                await PlayAudioToAll(audioPlayOptions, PlayAudioMessages.MarketingAudio, configuration, callConnection);
            }
            else if (toneReceived == DtmfTone.Three)
            {
                await PlayAudioToAll(audioPlayOptions, PlayAudioMessages.CustomerCareAudio, configuration, callConnection);
            }
            else if (toneReceived == DtmfTone.Four)
            {
                audioPlayOptions.OperationContext = "AgentConnect";
                await PlayAudioToAll(audioPlayOptions, PlayAudioMessages.AgentAudio, configuration, callConnection);

                var addParticipantOptions = new AddParticipantsOptions(new List<CommunicationIdentifier>()
                        {
                        new PhoneNumberIdentifier(configuration["ParticipantToAdd"])
                        })
                {
                    SourceCallerId = new PhoneNumberIdentifier(configuration["ACSAlternatePhoneNumber"])
                };

                _ = await callConnection.AddParticipantsAsync(addParticipantOptions);
            }
            else if (toneReceived == DtmfTone.Five)
            {
                // Hangup for everyone
                _ = await callConnection.HangUpAsync(true);
            }
            else
            {
                await PlayAudioToAll(audioPlayOptions, PlayAudioMessages.InvalidAudio, configuration, callConnection);
            }
        }

        public static async Task PlayAudioToAll(PlayOptions audioPlayOptions, PlayAudioMessages audioType, 
            IConfiguration configuration, CallConnection callConnection)
        {
            var appBaseUri = configuration["AppBaseUri"];
            PlaySource audioSource = new FileSource(new Uri(appBaseUri + configuration[audioType.ToString()]));
            _ = await callConnection.GetCallMedia().PlayToAllAsync(audioSource, audioPlayOptions);
        }
    }
}
