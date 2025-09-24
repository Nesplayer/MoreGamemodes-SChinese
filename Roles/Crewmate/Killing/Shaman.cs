using System;
using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

using Object = UnityEngine.Object;
using Epic.OnlineServices.Stats;

namespace MoreGamemodes
{
    public class Shaman : CustomRole
    {
        public override void OnExile(NetworkedPlayerInfo exiled)
        {
            if (Target == byte.MaxValue) return;
            var player = Utils.GetPlayerById(Target);
            if (player == null || player.Data.IsDead || !(player.GetRole().IsImpostor() || player.GetRole().IsNeutralKilling() || player.GetRole().Role == CustomRoles.Sheriff))
            {
                Target = byte.MaxValue;
                SendRPC();
            }
            else
                player.Notify(Utils.ColorString(Color, "You are cursed by shaman! Kill someone to remove curse or you will die when meeting is called!"));
        }

        public override void OnVotingComplete(MeetingHud __instance, MeetingHud.VoterState[] states, NetworkedPlayerInfo exiled, bool tie)
        {
            if (BaseRole == BaseRoles.DesyncShapeshifter)
            {
                BaseRole = BaseRoles.Crewmate;
                Player.RpcSetDesyncRole(RoleTypes.Crewmate, Player);
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.GetRole().BaseRole is BaseRoles.Impostor && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Impostor, Player);
                    else if (pc.GetRole().BaseRole is BaseRoles.Shapeshifter && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Shapeshifter, Player);
                    else if (pc.GetRole().BaseRole is BaseRoles.Phantom && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Phantom, Player);
                    else if (pc.GetRole().BaseRole is BaseRoles.Viper && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Viper, Player);
                }
                Player.SyncPlayerSettings();
                Main.NameColors[(Player.PlayerId, Player.PlayerId)] = Color.clear;
            }
        }

        public override void OnGlobalMurderPlayer(PlayerControl killer, PlayerControl target)
        {
            if (Target != byte.MaxValue && killer.PlayerId == Target)
            {
                Target = byte.MaxValue;
                SendRPC();
            }
        }

        public override void OnCheckShapeshiftMeeting(PlayerControl target)
        {
            if (target == Player || target.Data.IsDead || AbilityUses < 1f || Target != byte.MaxValue) return;
            Target = target.PlayerId;
            SendRPC();
            Player.RpcSendMessage("You cursed " + Main.StandardNames[Target] + "!", "Shaman");
            Player.RpcSetAbilityUses(AbilityUses - 1f);
            if (BaseRole == BaseRoles.DesyncShapeshifter)
            {
                BaseRole = BaseRoles.Crewmate;
                Player.RpcSetDesyncRole(RoleTypes.Crewmate, Player);
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.GetRole().BaseRole is BaseRoles.Impostor && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Impostor, Player);
                    else if (pc.GetRole().BaseRole is BaseRoles.Shapeshifter && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Shapeshifter, Player);
                    else if (pc.GetRole().BaseRole is BaseRoles.Phantom && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Phantom, Player);
                    else if (pc.GetRole().BaseRole is BaseRoles.Viper && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Viper, Player);
                }
                Player.SyncPlayerSettings();
                Main.NameColors[(Player.PlayerId, Player.PlayerId)] = Color.clear;
            }
            return;
        }

        public override void OnMeeting()
        {
            if (!Player.AmOwner && !Main.IsModded[Player.PlayerId] && AbilityUses >= 1f && !Player.Data.IsDead)
            {
                BaseRole = BaseRoles.DesyncShapeshifter;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.GetRole().BaseRole is BaseRoles.Impostor or BaseRoles.Shapeshifter or BaseRoles.Phantom or BaseRoles.Viper && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Crewmate, Player);
                }
                Player.RpcSetDesyncRole(RoleTypes.Shapeshifter, Player);
                Player.SyncPlayerSettings();
                Main.NameColors[(Player.PlayerId, Player.PlayerId)] = Color.white;
            }
        }

        public override bool OnReportDeadBody(NetworkedPlayerInfo target)
        {
            return target != null;
        }

        public void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
        {
            if (Player.Data.IsDead || Target == byte.MaxValue) return;
            var player = Utils.GetPlayerById(Target);
            if (player != null && !player.Data.IsDead && (player.GetRole().IsImpostor() || player.GetRole().IsNeutralKilling() || player.GetRole().Role == CustomRoles.Sheriff))
            {
                player.RpcSetDeathReason(DeathReasons.Cursed);
                player.RpcMurderPlayer(player, true);
                ++Main.PlayerKills[Player.PlayerId];
                ClassicGamemode.instance.PlayerKiller[player.PlayerId] = Player.PlayerId;
            }
            Target = byte.MaxValue;
            SendRPC();
        }

        public override void OnFixedUpdate()
        {
            if (!Player.AmOwner && !Main.IsModded[Player.PlayerId] && MeetingHud.Instance && MeetingHud.Instance.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted && AbilityUses >= 1f && Target == byte.MaxValue && !Player.Data.IsDead)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(Player.NetId, (byte)RpcCalls.SetRole, SendOption.None, Player.GetClientId());
                writer.Write((ushort)RoleTypes.Shapeshifter);
                writer.Write(true);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            if (BaseRole == BaseRoles.DesyncShapeshifter && Player.Data.IsDead)
            {
                BaseRole = BaseRoles.Crewmate;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.GetRole().BaseRole is BaseRoles.Impostor && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Impostor, Player);
                    else if (pc.GetRole().BaseRole is BaseRoles.Shapeshifter && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Shapeshifter, Player);
                    else if (pc.GetRole().BaseRole is BaseRoles.Phantom && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Phantom, Player);
                    else if (pc.GetRole().BaseRole is BaseRoles.Viper && !pc.Data.IsDead)
                        pc.RpcSetDesyncRole(RoleTypes.Viper, Player);
                }
                Player.SyncPlayerSettings();
                Main.NameColors[(Player.PlayerId, Player.PlayerId)] = Color.clear;
            }
            if (Target == byte.MaxValue) return;
            var player = Utils.GetPlayerById(Target);
            if (player == null || player.Data.IsDead) return;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == player || pc.Data.IsDead)
                    ClassicGamemode.instance.NameSymbols[(Target, pc.PlayerId)][CustomRoles.Shaman] = ("乂", Color);
            }
        }

        public override void OnCompleteTask()
        {
            if (AbilityUseGainWithEachTaskCompleted.GetFloat() <= 0f) return;
            Player.RpcSetAbilityUses(AbilityUses + AbilityUseGainWithEachTaskCompleted.GetFloat());
        }

        public override bool ShouldContinueGame()
        {
            return ShouldContinueTheGame.GetBool();
        }

        // https://github.com/EnhancedNetwork/TownofHost-Enhanced/blob/main/Modules/GuessManager.cs#L638
        public override void CreateMeetingButtons(MeetingHud __instance)
        {
            foreach (var pva in __instance.playerStates)
            {
                if (pva.transform.FindChild("MeetingButton") != null)
                    Object.Destroy(pva.transform.FindChild("MeetingButton").gameObject);
                var player = GameData.Instance.GetPlayerById(pva.TargetPlayerId);
                Shaman shamanRole = PlayerControl.LocalPlayer.GetRole() as Shaman;
                if (player.IsDead || player.Disconnected || player.ClientId == AmongUsClient.Instance.ClientId || Player.Data.IsDead || shamanRole.AbilityUses < 1f || shamanRole.Target != byte.MaxValue) continue;
                GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
                GameObject targetBox = Object.Instantiate(template, pva.transform);
                targetBox.name = "MeetingButton";
                targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f);
                SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                renderer.sprite = Utils.LoadSprite("MoreGamemodes.Resources.ShamanIcon.png", 115f);
                PassiveButton button = targetBox.GetComponent<PassiveButton>();
                button.OnClick.RemoveAllListeners();
                button.OnClick.AddListener((Action)(() => PlayerControl.LocalPlayer.CmdCheckShapeshift(player.Object, true)));
            }
        }

        public void SendRPC()
        {
            if (Player.AmOwner && MeetingHud.Instance != null)
                CreateMeetingButtons(MeetingHud.Instance);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(Player.NetId, (byte)CustomRPC.SyncCustomRole, SendOption.Reliable, -1);
            writer.Write(Target);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public override void ReceiveRPC(MessageReader reader)
        {
            Target = reader.ReadByte();
            if (Player.AmOwner && MeetingHud.Instance != null)
                CreateMeetingButtons(MeetingHud.Instance);
        }

        public static void OnGlobalReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.GetRole().Role == CustomRoles.Shaman)
                {
                    Shaman shamanRole = pc.GetRole() as Shaman;
                    if (shamanRole == null) continue;
                    shamanRole.OnReportDeadBody(reporter, target);
                }
            }
        }

        public Shaman(PlayerControl player)
        {
            Role = CustomRoles.Shaman;
            BaseRole = BaseRoles.Crewmate;
            Player = player;
            Utils.SetupRoleInfo(this);
            AbilityUses = InitialAbilityUseLimit.GetFloat();
            Target = byte.MaxValue;
        }

        public byte Target;

        public static OptionItem Chance;
        public static OptionItem Count;
        public static OptionItem DieAfterUsingAbility;
        public static OptionItem InitialAbilityUseLimit;
        public static OptionItem AbilityUseGainWithEachTaskCompleted;
        public static OptionItem ShouldContinueTheGame;
        public static void SetupOptionItem()
        {
            Chance = RoleOptionItem.Create(200300, CustomRoles.Shaman, TabGroup.CrewmateRoles, false);
            Count = IntegerOptionItem.Create(200301, "Max", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false)
                .SetParent(Chance);
            InitialAbilityUseLimit = FloatOptionItem.Create(200302, "Initial ability use limit", new(0f, 15f, 1f), 1f, TabGroup.CrewmateRoles, false)
                .SetParent(Chance);
            AbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(200303, "Ability use gain with each task completed", new(0f, 2f, 0.1f), 0.4f, TabGroup.CrewmateRoles, false)
                .SetParent(Chance);
            ShouldContinueTheGame = BooleanOptionItem.Create(200304, "Should continue the game", true, TabGroup.CrewmateRoles, false)
                .SetParent(Chance);
            Options.RolesChance[CustomRoles.Shaman] = Chance;
            Options.RolesCount[CustomRoles.Shaman] = Count;
        }
    }
}