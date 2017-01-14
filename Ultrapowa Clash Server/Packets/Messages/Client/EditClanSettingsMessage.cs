﻿using System;
using System.IO;
using System.Text;
using UCS.Core;
using UCS.Core.Network;
using UCS.Helpers;
using UCS.Logic;
using UCS.Logic.StreamEntry;
using UCS.Packets.Messages.Server;
using  UCS.Packets.Commands.Server;
using System.Threading.Tasks;

namespace UCS.Packets.Messages.Client
{
    // Packet 14316
    internal class EditClanSettingsMessage : Message
    {
        public EditClanSettingsMessage(Packets.Client client, PacketReader br) : base(client, br)
        {
        }

        int m_vAllianceBadgeData;
        string m_vAllianceDescription;
        int m_vAllianceOrigin;
        int m_vAllianceType;
        int m_vRequiredScore;
        int m_vWarFrequency;
        byte m_vWarAndFriendlyStatus;      
        int Unknown;

        public override void Decode()
        {
            using (PacketReader br = new PacketReader (new MemoryStream(GetData())))
            {
                m_vAllianceDescription = br.ReadString();
                Unknown = br.ReadInt32();
                m_vAllianceBadgeData = br.ReadInt32();
                m_vAllianceType = br.ReadInt32();
                m_vRequiredScore = br.ReadInt32();
                m_vWarFrequency = br.ReadInt32();
                m_vAllianceOrigin = br.ReadInt32();
                m_vWarAndFriendlyStatus = br.ReadByte();
            }
        }

        public override void Process(Level level)
        {
            Alliance alliance = ObjectManager.GetAlliance(level.GetPlayerAvatar().GetAllianceId());
            if (alliance != null)
            {
                alliance.SetAllianceDescription(m_vAllianceDescription);
                alliance.SetAllianceBadgeData(m_vAllianceBadgeData);
                alliance.SetAllianceType(m_vAllianceType);
                alliance.SetRequiredScore(m_vRequiredScore);
                alliance.SetWarFrequency(m_vWarFrequency);
                alliance.SetAllianceOrigin(m_vAllianceOrigin);
                alliance.SetWarAndFriendlytStatus(m_vWarAndFriendlyStatus);

                ClientAvatar avatar = level.GetPlayerAvatar();
                long allianceId = avatar.GetAllianceId();
                AllianceEventStreamEntry eventStreamEntry = new AllianceEventStreamEntry();
                eventStreamEntry.SetId((int) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                eventStreamEntry.SetSender(avatar);
                eventStreamEntry.SetEventType(10);
                eventStreamEntry.SetAvatarId(avatar.GetId());
                eventStreamEntry.SetAvatarName(avatar.GetAvatarName());
                eventStreamEntry.SetSenderId(avatar.GetId());
                eventStreamEntry.SetSenderName(avatar.GetAvatarName());
                alliance.AddChatMessage(eventStreamEntry);

                AllianceSettingChangedCommand edit = new AllianceSettingChangedCommand();
                edit.SetAlliance(alliance);
                edit.SetPlayer(level);

                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage(level.GetClient());
                availableServerCommandMessage.SetCommandId(6);
                availableServerCommandMessage.SetCommand(edit);
                availableServerCommandMessage.Send();

                foreach(AllianceMemberEntry op in alliance.GetAllianceMembers())
                {
                    Level user = ResourcesManager.GetPlayer(op.GetAvatarId());
                    if (ResourcesManager.IsPlayerOnline(user))
                    {
                        AllianceStreamEntryMessage p = new AllianceStreamEntryMessage(user.GetClient());
                        p.SetStreamEntry(eventStreamEntry);
                        p.Send();
                    }
                }

                DatabaseManager.Single().Save(alliance);
            }
        }
    }
}
