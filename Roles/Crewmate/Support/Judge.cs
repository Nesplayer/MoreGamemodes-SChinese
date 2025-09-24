using System;
using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

using Object = UnityEngine.Object;

namespace MoreGamemodes
{
    public class Judge : CustomRole
    {
        public override void OnExile(NetworkedPlayerInfo exiled)
        {
            if (DieAfterUsingAbility.GetBool() && AbilityUsed && !Player.Data.IsDead && !Player.Data.Disconnected)
            {
                Player.RpcSetDeathReason(DeathReasons.Suicide);
                Player.RpcExileV2();
            }
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

        public override void OnMeeting()
        {
            if (!Player.AmOwner && !Main.IsModded[Player.PlayerId] && !Player.Data.IsDead)
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

        public override void OnFixedUpdate()
        {
            if (!Player.AmOwner && !Main.IsModded[Player.PlayerId] && MeetingHud.Instance && MeetingHud.Instance.state is MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted && !AbilityUsed && !Player.Data.IsDead)
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
        }

        public override void OnCheckShapeshiftMeeting(PlayerControl target)
        {
            if (target == Player || target.Data.IsDead || AbilityUsed || MeetingHud.Instance.state is MeetingHud.VoteStates.Discussion) return;
            MeetingHud.Instance.RpcVotingComplete(new MeetingHud.VoterState[]{ new ()
            {
                VoterId = Player.PlayerId,
                VotedForId = target.PlayerId
            }}, target.Data, false);
            SendRPC();
        }

        

        public override int GetPlayerCount()
        {
            if (!ShouldContinueTheGame.GetBool()) return 1;
            if (AbilityUsed || DieAfterUsingAbility.GetBool()) return 1;
            return 2;
        }

        // https://github.com/EnhancedNetwork/TownofHost-Enhanced/blob/main/Modules/GuessManager.cs#L638
        public override void CreateMeetingButtons(MeetingHud __instance)
        {
            foreach (var pva in __instance.playerStates)
            {
                if (pva.transform.FindChild("MeetingButton") != null)
                    Object.Destroy(pva.transform.FindChild("MeetingButton").gameObject);
                var player = GameData.Instance.GetPlayerById(pva.TargetPlayerId);
                var judgeRole = PlayerControl.LocalPlayer.GetRole() as Judge;
                if (player.IsDead || player.Disconnected || player.ClientId == AmongUsClient.Instance.ClientId || Player.Data.IsDead || judgeRole.AbilityUsed || __instance.state is MeetingHud.VoteStates.Animating or MeetingHud.VoteStates.Discussion) continue;
                GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
                GameObject targetBox = Object.Instantiate(template, pva.transform);
                targetBox.name = "MeetingButton";
                targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f);
                SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                renderer.sprite = Utils.LoadSprite("MoreGamemodes.Resources.JudgeIcon.png", 115f);
                PassiveButton button = targetBox.GetComponent<PassiveButton>();
                button.OnClick.RemoveAllListeners();
                button.OnClick.AddListener((Action)(() => PlayerControl.LocalPlayer.CmdCheckShapeshift(player.Object, true)));
            }
        }

        public void SendRPC()
        {
            AbilityUsed = true;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(Player.NetId, (byte)CustomRPC.SyncCustomRole, SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public override void ReceiveRPC(MessageReader reader)
        {
            AbilityUsed = true;
        }

        public Judge(PlayerControl player)
        {
            Role = CustomRoles.Judge;
            BaseRole = BaseRoles.Crewmate;
            Player = player;
            Utils.SetupRoleInfo(this);
            AbilityUses = -1f;
            AbilityUsed = false;
        }

        public bool AbilityUsed;

        public static OptionItem Chance;
        public static OptionItem Count;
        public static OptionItem DieAfterUsingAbility;
        public static OptionItem ShouldContinueTheGame;
        public static void SetupOptionItem()
        {
            Chance = RoleOptionItem.Create(400300, CustomRoles.Judge, TabGroup.CrewmateRoles, false);
            Count = IntegerOptionItem.Create(400301, "Max", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false)
                .SetParent(Chance);
            DieAfterUsingAbility = BooleanOptionItem.Create(400302, "Die after using ability", false, TabGroup.CrewmateRoles, false)
                .SetParent(Chance);
            ShouldContinueTheGame = BooleanOptionItem.Create(400303, "Should continue the game", false, TabGroup.CrewmateRoles, false)
                .SetParent(Chance);
            Options.RolesChance[CustomRoles.Judge] = Chance;
            Options.RolesCount[CustomRoles.Judge] = Count;
        }
    }
}