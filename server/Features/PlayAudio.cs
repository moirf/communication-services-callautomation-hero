using Azure.Communication.CallAutomation;
using Azure.Communication;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace CallAutomationHero.Server
{




    public class PlayAudio
    {
       public static CommunicationIdentifierKind GetIdentifierKind(string participantnumber)
        {
            //checks the identity type returns as string
            return Regex.Match(participantnumber, Constants.userIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.UserIdentity :
         Regex.Match(participantnumber, Constants.phoneIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.PhoneIdentity :
         CommunicationIdentifierKind.UnknownIdentity;
        }


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

                //var addParticipantOptions = new AddParticipantsOptions(new List<CommunicationIdentifier>()
                //        {
                //        new PhoneNumberIdentifier(configuration["ParticipantToAdd"])
                //        })
                //{
                //    SourceCallerId = new PhoneNumberIdentifier(configuration["ACSAlternatePhoneNumber"])
                //};
                var AddParticipant = configuration["ParticipantToAdd"];
                

                var identifierKind = GetIdentifierKind(AddParticipant);
                CallInvite? callInvite = null;

                if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                    {
                        callInvite = new CallInvite(new PhoneNumberIdentifier(AddParticipant), new PhoneNumberIdentifier(configuration["ACSAlternatePhoneNumber"]));
                    }
                    if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                    {
                        callInvite = new CallInvite(new CommunicationUserIdentifier(AddParticipant));

                    }

                if (callInvite != null)
                {
                    var addParticipantOptions = new AddParticipantOptions(callInvite);
                    _ = await callConnection.AddParticipantAsync(addParticipantOptions);
                }
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
    public enum CommunicationIdentifierKind
    {
        PhoneIdentity,
        UserIdentity,
        UnknownIdentity



    }
    public class Constants
    {
        public const string userIdentityRegex = @"8:acs:[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}_[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
        public const string phoneIdentityRegex = @"^\+\d{10,14}$";



    }
}
