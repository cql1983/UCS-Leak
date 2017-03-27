using UCS.Core;
using UCS.Core.Network;
using UCS.Logic;
using UCS.Packets.Messages.Server;

namespace UCS.Packets.GameOpCommands
{
    internal class BecomeLeaderGameOpCommand : GameOpCommand
    {
        readonly string[] m_vArgs;

        public BecomeLeaderGameOpCommand(string[] args)
        {
            m_vArgs = args;
            SetRequiredAccountPrivileges(5);
        }

        public override async void Execute(Level level)
        {
            if (level.Avatar.AccountPrivileges >= GetRequiredAccountPrivileges())
            {
                var clanid = level.Avatar.AllianceId;
                if (clanid != 0)
                {
                    Alliance _Alliance = ObjectManager.GetAlliance(level.Avatar.AllianceId);

                    foreach (var pl in _Alliance.GetAllianceMembers())
                    {
                        if (pl.Role == 2)
                        {
                            pl.SetRole(4);
                            break;
                        }
                    }
                    level.Avatar.SetAllianceRole(2);
                }
            }
            else
            {
                var p = new GlobalChatLineMessage(level.Client)
                {
                    Message = "GameOp command failed. Access to Admin GameOP is prohibited.",
                    HomeId = 0,
                    CurrentHomeId = 0,
                    LeagueId = 22,
                    PlayerName = "UCS Bot"
                };
                Processor.Send(p);
            }
        }
    }
}
