using AmongUs.GameOptions;
using Hazel;
using InnerNet;

namespace MoreGamemodes
{
    public class Viper : CustomRole
    {
        public override bool OnCheckMurderLate(PlayerControl target)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var role = Main.DesyncRoles.ContainsKey((Player.PlayerId, pc.PlayerId)) ? Main.DesyncRoles[(Player.PlayerId, pc.PlayerId)] : Main.StandardRoles[Player.PlayerId];
                if (role == RoleTypes.Viper)
                {
                    if (pc.AmOwner)
                        Player.MurderPlayer(target, MurderResultFlags.Succeeded);
                    else
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(Player.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, pc.GetClientId());
                        writer.WriteNetObject(target);
                        writer.Write((int)MurderResultFlags.Succeeded);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                }
                else
                {
                    if (pc.AmOwner)
                    {
                        Player.StartCoroutine(Player.CoSetRole(RoleTypes.Viper, true));
                        Player.MurderPlayer(target, MurderResultFlags.Succeeded);
                        Player.StartCoroutine(Player.CoSetRole(role, true));
                    }
                    else
                    {
                        CustomRpcSender sender = CustomRpcSender.Create(SendOption.Reliable);
                        sender.StartMessage(pc.GetClientId());
                        sender.StartRpc(Player.NetId, (byte)RpcCalls.SetRole)
                            .Write((ushort)RoleTypes.Viper)
                            .Write(true)
                            .EndRpc();
                        sender.StartRpc(Player.NetId, (byte)RpcCalls.MurderPlayer)
                            .WriteNetObject(target)
                            .Write((int)MurderResultFlags.Succeeded)
                            .EndRpc();
                        sender.StartRpc(Player.NetId, (byte)RpcCalls.SetRole)
                            .Write((ushort)role)
                            .Write(true)
                            .EndRpc();
                        sender.EndMessage();
                        sender.SendMessage();
                    }
                }
            }
            return false;
        }

        public Viper(PlayerControl player)
        {
            Role = CustomRoles.Viper;
            BaseRole = BaseRoles.Viper;
            Player = player;
            Utils.SetupRoleInfo(this);
            AbilityUses = -1f;
        }
    }
}