﻿using System;
using UCS.Core;
using UCS.Core.Network;
using UCS.Helpers.Binary;
using UCS.Logic;
using UCS.Logic.AvatarStreamEntry;
using UCS.Packets.Messages.Server;

namespace UCS.Packets.Commands.Client
{
    // Packet 537
    internal class SendAllianceMailCommand : Command
    {
        internal string m_vMailContent;

        public SendAllianceMailCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        internal override void Decode()
        {
            this.m_vMailContent = this.Reader.ReadString();
            this.Reader.ReadInt32();
        }

        internal override async void Process()
        {
            try
            {
                var avatar = this.Device.Player.Avatar;
                var allianceId = avatar.AllianceId;
                if (allianceId > 0)
                {
                    var alliance = ObjectManager.GetAlliance(allianceId);
                    if (alliance != null)
                    {
                        var mail = new AllianceMailStreamEntry();
                        mail.SetId((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                        mail.SetAvatar(avatar);
                        mail.SetIsNew(2);
                        mail.SetSenderId(avatar.UserId);
                        mail.SetAllianceId(allianceId);
                        mail.SetAllianceBadgeData(alliance.m_vAllianceBadgeData);
                        mail.SetAllianceName(alliance.m_vAllianceName);
                        mail.SetMessage(m_vMailContent);

                        foreach (var onlinePlayer in ResourcesManager.GetOnlinePlayers())
                        {
                            if (onlinePlayer.Avatar.AllianceId == allianceId)
                            {
                                var p = new AvatarStreamEntryMessage(onlinePlayer.Client);
                                p.SetAvatarStreamEntry(mail);
                                p.Send();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}