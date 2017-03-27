﻿using System;
using System.IO;
using UCS.Core;
using UCS.Core.Network;
using UCS.Helpers;
using UCS.Helpers.Binary;
using UCS.Logic;
using UCS.Logic.StreamEntry;
using UCS.Packets.Commands.Server;
using UCS.Packets.Messages.Server;

namespace UCS.Packets.Messages.Client
{
    // Packet 14321
    internal class TakeDecisionJoinRequestMessage : Message
    {
        public TakeDecisionJoinRequestMessage(Device device, Reader reader) : base(device, reader)
        {
        }

        public long MessageID { get; set; }

        public byte Choice { get; set; }

        internal override void Decode()
        {
            this.MessageID = this.Reader.ReadInt64();
            this.Choice    = this.Reader.ReadByte();
        }

        internal async void Process()
        {
            try
            {
                Alliance a = ObjectManager.GetAlliance(this.Device.Player.Avatar.AllianceId);
                StreamEntry message = a.m_vChatMessages.Find(c => c.GetId() == MessageID);
                Level requester = await ResourcesManager.GetPlayer(message.GetSenderId());
                if (Choice == 1)
                {
                    if (!a.IsAllianceFull())
                    {
                        requester.Avatar.SetAllianceId(a.m_vAllianceId);

                        AllianceMemberEntry member = new AllianceMemberEntry(requester.Avatar.UserId);
                        member.SetRole(1);
                        a.AddAllianceMember(member);

                        StreamEntry e = a.m_vChatMessages.Find(c => c.GetId() == MessageID);
                        e.SetJudgeName(this.Device.Player.Avatar.AvatarName);
                        e.SetState(2);

                        AllianceEventStreamEntry eventStreamEntry = new AllianceEventStreamEntry();
                        eventStreamEntry.SetId(a.m_vChatMessages.Count + 1);
                        eventStreamEntry.SetSender(requester.Avatar);
                        eventStreamEntry.SetEventType(2);

                        a.AddChatMessage(eventStreamEntry);

                        foreach (AllianceMemberEntry op in a.GetAllianceMembers())
                        {
                            Level player = await ResourcesManager.GetPlayer(op.AvatarId);
                            if (player.Client != null)
                            {
                                AllianceStreamEntryMessage c = new AllianceStreamEntryMessage(player.Client);
                                AllianceStreamEntryMessage p = new AllianceStreamEntryMessage(player.Client);
                                p.SetStreamEntry(eventStreamEntry);
                                c.SetStreamEntry(e);

                                p.Send();
                                c.Send();
                            }
                        }
                        if (ResourcesManager.IsPlayerOnline(requester))
                        {
                            JoinedAllianceCommand joinAllianceCommand = new JoinedAllianceCommand(requester.Client);
                            joinAllianceCommand.SetAlliance(a);

                            new AvailableServerCommandMessage(requester.Client, joinAllianceCommand.Handle()).Send();

                            AllianceRoleUpdateCommand d = new AllianceRoleUpdateCommand(requester.Client);
                            d.SetAlliance(a);
                            d.SetRole(4);
                            d.Tick(requester);

                            new AvailableServerCommandMessage(requester.Client, d.Handle()).Send();

                            new AllianceStreamMessage(requester.Client, a).Send();
                        }
                    }
                }
                else
                {
                    StreamEntry e = a.m_vChatMessages.Find(c => c.GetId() == MessageID);
                    e.SetJudgeName(this.Device.Player.Avatar.AvatarName);
                    e.SetState(3);

                    foreach (AllianceMemberEntry op in a.GetAllianceMembers())
                    {
                        Level player = await ResourcesManager.GetPlayer(op.AvatarId);
                        if (player.Client != null)
                        {
                            AllianceStreamEntryMessage c = new AllianceStreamEntryMessage(player.Client);
                            c.SetStreamEntry(e);
                            c.Send();
                        }
                    }
                }
            } catch (Exception) { }
        }

    }
}