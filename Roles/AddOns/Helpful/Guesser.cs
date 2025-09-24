using UnityEngine;
using AmongUs.GameOptions;
using System.Linq;
using Il2CppSystem.Collections.Generic;
using System;
using Hazel;

using Object = UnityEngine.Object;

namespace MoreGamemodes
{
    public class Guesser : AddOn
    {
        public override bool CanGuess(PlayerControl target, CustomRoles role)
        {
            if (Player.GetRole().IsImpostor() && target.GetRole().IsImpostor()) return false; 
            if (role == CustomRoles.Crewmate && !CanGuessCrewmateRole.GetBool()) return false;
            return (CustomRolesHelper.IsCrewmate(role) && !Player.GetRole().IsCrewmate()) || (CustomRolesHelper.IsImpostor(role) && !Player.GetRole().IsImpostor()) ||
                (CustomRolesHelper.IsNeutralKilling(role) && CanGuessNeutralKilling.GetBool()) || (CustomRolesHelper.IsNeutralEvil(role) && CanGuessNeutralEvil.GetBool()) ||
                (CustomRolesHelper.IsNeutralBenign(role) && CanGuessNeutralBenign.GetBool());
        }

        public override bool CanGuess(PlayerControl target, AddOns addOn)
        {
            if (target == Player || (Player.GetRole().IsImpostor() && target.GetRole().IsImpostor())) return false;
            if (Player.GetRole().IsImpostor() && AddOnsHelper.IsImpostorOnly(addOn)) return false;
            return CanGuessAddOns.GetBool();
        }
        
        // https://github.com/EnhancedNetwork/TownofHost-Enhanced/blob/main/Modules/GuessManager.cs#L638
        public static void CreateGuessButtons(MeetingHud __instance, bool canGuessNB, bool canGuessNE, bool canGuessNK, bool canGuessAddOns, bool canGuessCrewmate)
        {
            foreach (var pva in __instance.playerStates)
            {
                if (pva.transform.FindChild("GuessButton") != null)
                    Object.Destroy(pva.transform.FindChild("GuessButton").gameObject);
                var player = GameData.Instance.GetPlayerById(pva.TargetPlayerId);
                if (player.IsDead || player.Disconnected || player.ClientId == AmongUsClient.Instance.ClientId || (player.GetRole().IsImpostor() && PlayerControl.LocalPlayer.GetRole().IsImpostor()) || PlayerControl.LocalPlayer.Data.IsDead || !player.GetRole().CanGetGuessed(PlayerControl.LocalPlayer, null)) continue;
                GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
                GameObject targetBox = Object.Instantiate(template, pva.transform);
                targetBox.name = "GuessButton";
                targetBox.transform.localPosition = pva.transform.FindChild("MeetingButton") != null ? new Vector3(-0.35f, 0.03f, -1.31f) : new Vector3(-0.95f, 0.03f, -1.31f);
                SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                renderer.sprite = Utils.LoadSprite("MoreGamemodes.Resources.GuessIcon.png", 115f);
                PassiveButton button = targetBox.GetComponent<PassiveButton>();
                button.OnClick.RemoveAllListeners();
                button.OnClick.AddListener((Action)(() => GuessButtonOnClick(pva.TargetPlayerId, __instance, canGuessNB, canGuessNE, canGuessNK, canGuessAddOns, canGuessCrewmate)));
            }
        }

        public static void GuessButtonOnClick(byte playerId, MeetingHud __instance, bool canGuessNB, bool canGuessNE, bool canGuessNK, bool canGuessAddOns, bool canGuessCrewmate)
        {
            if (__instance == null) return;
            ShapeshifterRole shapeshifterRole = Object.Instantiate(RoleManager.Instance.AllRoles.ToArray().First((RoleBehaviour r) => r.Role == RoleTypes.Shapeshifter)).Cast<ShapeshifterRole>();
            ShapeshifterMinigame minigame = Object.Instantiate(shapeshifterRole.ShapeshifterMenu);
            Object.Destroy(shapeshifterRole.gameObject);
            minigame.name = "GuessMenu";
            SpriteRenderer[] componentsInChildren = minigame.GetComponentsInChildren<SpriteRenderer>();
			minigame.transform.SetParent(Camera.main.transform, false);
			minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
            Minigame.Instance = minigame;
            minigame.MyTask = null;
		    minigame.MyNormTask = null;
		    minigame.timeOpened = Time.realtimeSinceStartup;
            if (PlayerControl.LocalPlayer)
		    {
			    if (MapBehaviour.Instance)
			    {
				    MapBehaviour.Instance.Close();
			    }
			    PlayerControl.LocalPlayer.MyPhysics.SetNormalizedVelocity(Vector2.zero);
		    }
            minigame.StartCoroutine(minigame.CoAnimateOpen());
            DestroyableSingleton<DebugAnalytics>.Instance.Analytics.MinigameOpened(PlayerControl.LocalPlayer.Data, minigame.TaskType);
            minigame.potentialVictims = new List<ShapeshifterPanel>();
            foreach (var pva in __instance.playerStates)
                pva.transform.localPosition += new Vector3(0f, 100f, 0f);
            CreateTabs(minigame, playerId, __instance, canGuessNB, canGuessNE, canGuessNK, canGuessAddOns, canGuessCrewmate);
        }

        public static void CreateTabs(ShapeshifterMinigame minigame, byte playerId, MeetingHud __instance, bool canGuessNB, bool canGuessNE, bool canGuessNK, bool canGuessAddOns, bool canGuessCrewmate)
        {
            if (__instance == null)
            {
                minigame.Close();
                return;
            }
            foreach (var panel in minigame.potentialVictims)
                Object.Destroy(panel.gameObject);
            minigame.potentialVictims = new List<ShapeshifterPanel>();
            ControllerManager.Instance.CloseOverlayMenu(minigame.name);
            System.Collections.Generic.List<(int, string, Color)> tabs = new();
            tabs.Add((0, "Vanilla roles", Color.yellow));
            if (!PlayerControl.LocalPlayer.GetRole().IsCrewmate())
            {
                tabs.Add((1, "Crewmate investigative", Palette.CrewmateBlue));
                tabs.Add((2, "Crewmate killing", Palette.CrewmateBlue));
                tabs.Add((3, "Crewmate protective", Palette.CrewmateBlue));
                tabs.Add((4, "Crewmate support", Palette.CrewmateBlue));
            }
            if (!PlayerControl.LocalPlayer.GetRole().IsImpostor())
            {
                tabs.Add((5, "Impostor concealing", Palette.ImpostorRed));
                tabs.Add((6, "Impostor killing", Palette.ImpostorRed));
                tabs.Add((7, "Impostor support", Palette.ImpostorRed));
            }
            if (canGuessNB)
                tabs.Add((8, "Neutral benign", Color.gray));
            if (canGuessNE)
                tabs.Add((9, "Neutral evil", Color.gray));
            if (canGuessNK)
                tabs.Add((10, "Neutral killing", Color.gray));
            if (canGuessAddOns)
            {
                tabs.Add((11, "Helpful add ons", Color.yellow));
                tabs.Add((12, "Harmful add ons", Color.yellow));
                if (!PlayerControl.LocalPlayer.GetRole().IsImpostor())
                    tabs.Add((13, "Impostor add ons", Color.yellow));
            }
            
            List<UiElement> list = new();
            for (int i = 0; i < tabs.Count; ++i)
            {
                int num = i % 3;
			    int num2 = i / 3;
                ShapeshifterPanel shapeshifterPanel = Object.Instantiate(minigame.PanelPrefab, minigame.transform);
			    shapeshifterPanel.transform.localPosition = new Vector3(minigame.XStart + num * minigame.XOffset, minigame.YStart + num2 * minigame.YOffset, -1f);
                PassiveButton button = shapeshifterPanel.GetComponent<PassiveButton>();
                int id = tabs[i].Item1;
                shapeshifterPanel.shapeshift = (Action)(() => OpenTab(minigame, playerId, __instance, id, canGuessNB, canGuessNE, canGuessNK, canGuessAddOns, canGuessCrewmate));
                SpriteRenderer[] componentsInChildren = shapeshifterPanel.GetComponentsInChildren<SpriteRenderer>();
		        for (int j = 0; j < componentsInChildren.Length; j++)
		        {
			        componentsInChildren[j].material.SetInt(PlayerMaterial.MaskLayer, i + 2);
                    if (j != 9)
                        componentsInChildren[j].material.color = Palette.ImpostorRed;
		        }
                componentsInChildren[9].material.color = Color.clear;
                shapeshifterPanel.NameText.text = tabs[i].Item2;
                Object.Destroy(shapeshifterPanel.PlayerIcon.gameObject);
                shapeshifterPanel.LevelNumberText.text = "<font=\"VCR SDF\"><size=15>■";
                shapeshifterPanel.LevelNumberText.transform.localPosition += new Vector3(0.2f, -0.02f, -10f); 
                shapeshifterPanel.NameText.transform.localPosition = Vector3.zero;
			    shapeshifterPanel.NameText.color = tabs[i].Item3;
			    minigame.potentialVictims.Add(shapeshifterPanel);
			    list.Add(shapeshifterPanel.Button);
            }
            ControllerManager.Instance.OpenOverlayMenu(minigame.name, minigame.BackButton, minigame.DefaultButtonSelected, list, false);
        }

        public static void OpenTab(ShapeshifterMinigame minigame, byte playerId, MeetingHud __instance, int id, bool canGuessNB, bool canGuessNE, bool canGuessNK, bool canGuessAddOns, bool canGuessCrewmate)
        {
            if (__instance == null)
            {
                minigame.Close();
                return;
            }
            foreach (var panel in minigame.potentialVictims)
                Object.Destroy(panel.gameObject);
            minigame.potentialVictims = new List<ShapeshifterPanel>();
            ControllerManager.Instance.CloseOverlayMenu(minigame.name);
            List<UiElement> list = new();
            int i = 0;
            if (id == 0)
            {
                foreach (var role in Enum.GetValues<RoleTypes>())
                {
                    if ((role.IsImpostor() && PlayerControl.LocalPlayer.GetRole().IsImpostor()) || (!role.IsImpostor() && PlayerControl.LocalPlayer.GetRole().IsCrewmate()) ||
                        (role == RoleTypes.Crewmate && !canGuessCrewmate) || role is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost or RoleTypes.GuardianAngel)
                    {
                        continue;
                    }
                    int num = i % 3;
			        int num2 = i / 3;
                    ShapeshifterPanel shapeshifterPanel = Object.Instantiate(minigame.PanelPrefab, minigame.transform);
			        shapeshifterPanel.transform.localPosition = new Vector3(minigame.XStart + num * minigame.XOffset, minigame.YStart + num2 * minigame.YOffset, -1f);
                    PassiveButton button = shapeshifterPanel.GetComponent<PassiveButton>();
                    shapeshifterPanel.shapeshift = (Action)(() => GuessPlayer(playerId, Utils.RoleToString(role).ToLower().Replace(" ", ""), minigame, __instance));
                    SpriteRenderer[] componentsInChildren = shapeshifterPanel.GetComponentsInChildren<SpriteRenderer>();
		            for (int j = 0; j < componentsInChildren.Length; j++)
		            {
			            componentsInChildren[j].material.SetInt(PlayerMaterial.MaskLayer, i + 2);
                        if (j != 9)
                            componentsInChildren[j].material.color = Palette.ImpostorRed;
		            }
                    componentsInChildren[9].sprite = HudManager.Instance.KillButton.graphic.sprite;
                    shapeshifterPanel.NameText.text = Utils.RoleToString(role);
                    Object.Destroy(shapeshifterPanel.PlayerIcon.gameObject);
                    shapeshifterPanel.LevelNumberText.text = "<font=\"VCR SDF\"><size=15>■";
                    shapeshifterPanel.LevelNumberText.transform.localPosition += new Vector3(0.2f, -0.02f, -10f); 
                    shapeshifterPanel.NameText.transform.localPosition = Vector3.zero;
			        shapeshifterPanel.NameText.color = role.IsImpostor() ? Palette.ImpostorRed : Palette.CrewmateBlue;
			        minigame.potentialVictims.Add(shapeshifterPanel);
			        list.Add(shapeshifterPanel.Button);
                    ++i;
                }
            }
            else if (id <= 10)
            {
                foreach (var role in CustomRolesHelper.CommandRoleNames.Keys)
                {
                    CustomRoles roleType = CustomRolesHelper.CommandRoleNames[role];
                    if (CustomRolesHelper.IsVanilla(roleType)) continue;
                    if ((roleType == CustomRoles.Immortal && !Immortal.CanBeGuessed.GetBool()) || (roleType == CustomRoles.SecurityGuard && !SecurityGuard.CanBeGuessed.GetBool()) || 
                        (roleType == CustomRoles.Mortician && !Mortician.CanBeGuessed.GetBool()) || (roleType == CustomRoles.Mayor && !Mayor.CanBeGuessed.GetBool())) continue;
                    if (Options.RolesChance[roleType].Id < id * 100000 || Options.RolesChance[roleType].Id >= id * 100000 + 100000) continue;
                    int num = i % 3;
			        int num2 = i / 3;
                    ShapeshifterPanel shapeshifterPanel = Object.Instantiate(minigame.PanelPrefab, minigame.transform);
			        shapeshifterPanel.transform.localPosition = new Vector3(minigame.XStart + num * minigame.XOffset, minigame.YStart + num2 * minigame.YOffset, -1f);
                    PassiveButton button = shapeshifterPanel.GetComponent<PassiveButton>();
                    shapeshifterPanel.shapeshift = (Action)(() => GuessPlayer(playerId, role, minigame, __instance));
                    SpriteRenderer[] componentsInChildren = shapeshifterPanel.GetComponentsInChildren<SpriteRenderer>();
		            for (int j = 0; j < componentsInChildren.Length; j++)
		            {
			            componentsInChildren[j].material.SetInt(PlayerMaterial.MaskLayer, i + 2);
                        if (j != 9)
                            componentsInChildren[j].material.color = Palette.ImpostorRed;
		            }
                    componentsInChildren[9].sprite = HudManager.Instance.KillButton.graphic.sprite;
                    shapeshifterPanel.NameText.text = CustomRolesHelper.RoleNames[roleType];
                    Object.Destroy(shapeshifterPanel.PlayerIcon.gameObject);
                    shapeshifterPanel.LevelNumberText.text = "<font=\"VCR SDF\"><size=15>■";
                    shapeshifterPanel.LevelNumberText.transform.localPosition += new Vector3(0.2f, -0.02f, -10f); 
                    shapeshifterPanel.NameText.transform.localPosition = Vector3.zero;
			        shapeshifterPanel.NameText.color = CustomRolesHelper.RoleColors[roleType];
			        minigame.potentialVictims.Add(shapeshifterPanel);
			        list.Add(shapeshifterPanel.Button);
                    ++i;
                }
            }
            else
            {
                foreach (var addon in AddOnsHelper.CommandAddOnNames.Keys)
                {
                    AddOns addOn = AddOnsHelper.CommandAddOnNames[addon];
                    if (addOn == AddOns.Bait && !Bait.CanBeGuessed.GetBool()) continue;
                    if (Options.AddOnsChance[addOn].Id < id * 100000 || Options.AddOnsChance[addOn].Id >= id * 100000 + 100000) continue;
                    int num = i % 3;
			        int num2 = i / 3;
                    ShapeshifterPanel shapeshifterPanel = Object.Instantiate(minigame.PanelPrefab, minigame.transform);
			        shapeshifterPanel.transform.localPosition = new Vector3(minigame.XStart + num * minigame.XOffset, minigame.YStart + num2 * minigame.YOffset, -1f);
                    PassiveButton button = shapeshifterPanel.GetComponent<PassiveButton>();
                    shapeshifterPanel.shapeshift = (Action)(() => GuessPlayer(playerId, addon, minigame, __instance));
                    SpriteRenderer[] componentsInChildren = shapeshifterPanel.GetComponentsInChildren<SpriteRenderer>();
		            for (int j = 0; j < componentsInChildren.Length; j++)
		            {
			            componentsInChildren[j].material.SetInt(PlayerMaterial.MaskLayer, i + 2);
                        if (j != 9)
                            componentsInChildren[j].material.color = Palette.ImpostorRed;
		            }
                    componentsInChildren[9].sprite = HudManager.Instance.KillButton.graphic.sprite;
                    shapeshifterPanel.NameText.text = AddOnsHelper.AddOnNames[addOn];
                    Object.Destroy(shapeshifterPanel.PlayerIcon.gameObject);
                    shapeshifterPanel.LevelNumberText.text = "<font=\"VCR SDF\"><size=15>■";
                    shapeshifterPanel.LevelNumberText.transform.localPosition += new Vector3(0.2f, -0.02f, -10f); 
                    shapeshifterPanel.NameText.transform.localPosition = Vector3.zero;
			        shapeshifterPanel.NameText.color = AddOnsHelper.AddOnColors[addOn];
			        minigame.potentialVictims.Add(shapeshifterPanel);
			        list.Add(shapeshifterPanel.Button);
                    ++i;
                }
            }
            i = 14;
            int num3 = i % 3;
			int num4 = i / 3;
            ShapeshifterPanel shapeshifterPanel2 = Object.Instantiate(minigame.PanelPrefab, minigame.transform);
			shapeshifterPanel2.transform.localPosition = new Vector3(minigame.XStart + num3 * minigame.XOffset, minigame.YStart + num4 * minigame.YOffset, -1f);
            shapeshifterPanel2.shapeshift = (Action)(() => CreateTabs(minigame, playerId, __instance, canGuessNB, canGuessNE, canGuessNK, canGuessAddOns, canGuessCrewmate));
            SpriteRenderer[] componentsInChildren2 = shapeshifterPanel2.GetComponentsInChildren<SpriteRenderer>();
	        for (int j = 0; j < componentsInChildren2.Length; j++)
		    {
		        componentsInChildren2[j].material.SetInt(PlayerMaterial.MaskLayer, i + 2);
                if (j != 9)
                    componentsInChildren2[j].material.color = Palette.ImpostorRed;
		    }
            componentsInChildren2[9].material.color = Color.clear;
            shapeshifterPanel2.NameText.text = "Back";
            Object.Destroy(shapeshifterPanel2.PlayerIcon.gameObject);
            shapeshifterPanel2.LevelNumberText.text = "<font=\"VCR SDF\"><size=15>■";
            shapeshifterPanel2.LevelNumberText.transform.localPosition += new Vector3(0.2f, -0.02f, -10f); 
            shapeshifterPanel2.NameText.transform.localPosition = Vector3.zero;
			shapeshifterPanel2.NameText.color = Color.gray;
			minigame.potentialVictims.Add(shapeshifterPanel2);
			list.Add(shapeshifterPanel2.Button);
            ++i;
            ControllerManager.Instance.OpenOverlayMenu(minigame.name, minigame.BackButton, minigame.DefaultButtonSelected, list, false);
        }

        public static void GuessPlayer(byte playerId, string roleName, Minigame minigame, MeetingHud __instance)
        {
            if (__instance == null)
            {
                minigame.Close();
                return;
            }
            string message = "/guess " + playerId + " " + roleName;
            if (AmongUsClient.Instance.AmHost)
            {
                string text = HudManager.Instance.Chat.freeChatField.Text;
                HudManager.Instance.Chat.freeChatField.textArea.text = message;
                HudManager.Instance.Chat.SendChat();
                HudManager.Instance.Chat.freeChatField.textArea.text = text;
            }
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, AmongUsClient.Instance.HostId);
                writer.Write(message);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            minigame.Close();
        }

        public Guesser(PlayerControl player)
        {
            Type = AddOns.Guesser;
            Player = player;
            Utils.SetupAddOnInfo(this);
        }

        public static OptionItem Chance;
        public static OptionItem Count;
        public static OptionItem CanGuessNeutralKilling;
        public static OptionItem CanGuessNeutralEvil;
        public static OptionItem CanGuessNeutralBenign;
        public static OptionItem CanGuessCrewmateRole;
        public static OptionItem CanGuessAddOns;
        public static OptionItem CrewmatesCanBecomeGuesser;
        public static OptionItem BenignNeutralsCanBecomeGuesser;
        public static OptionItem EvilNeutralsCanBecomeGuesser;
        public static OptionItem KillingNeutralsCanBecomeGuesser;
        public static OptionItem ImpostorsCanBecomeGuesser;
        public static void SetupOptionItem()
        {
            Chance = AddOnOptionItem.Create(1100400, AddOns.Guesser, TabGroup.AddOns, false);
            Count = IntegerOptionItem.Create(1100401, "Max", new(1, 15, 1), 1, TabGroup.AddOns, false)
                .SetParent(Chance);
            CanGuessNeutralKilling = BooleanOptionItem.Create(1100402, "Can guess neutral killing", true, TabGroup.AddOns, false)
                .SetParent(Chance);
            CanGuessNeutralEvil = BooleanOptionItem.Create(1100403, "Can guess neutral evil", true, TabGroup.AddOns, false)
                .SetParent(Chance);
            CanGuessNeutralBenign = BooleanOptionItem.Create(1100404, "Can guess neutral benign", true, TabGroup.AddOns, false)
                .SetParent(Chance);
            CanGuessCrewmateRole = BooleanOptionItem.Create(1100405, "Can guess \"Crewmate\" role", true, TabGroup.AddOns, false)
                .SetParent(Chance);
            CanGuessAddOns = BooleanOptionItem.Create(1100406, "Can guess add ons", false, TabGroup.AddOns, false)
                .SetParent(Chance);
            CrewmatesCanBecomeGuesser = BooleanOptionItem.Create(1100407, "Crewmates can become guesser", true, TabGroup.AddOns, false)
                .SetParent(Chance);
            BenignNeutralsCanBecomeGuesser = BooleanOptionItem.Create(1100408, "Benign neutrals can become guesser", true, TabGroup.AddOns, false)
                .SetParent(Chance);
            EvilNeutralsCanBecomeGuesser = BooleanOptionItem.Create(1100409, "Evil neutrals can become guesser", true, TabGroup.AddOns, false)
                .SetParent(Chance);
            KillingNeutralsCanBecomeGuesser = BooleanOptionItem.Create(1100410, "Killing neutrals can become guesser", true, TabGroup.AddOns, false)
                .SetParent(Chance);
            ImpostorsCanBecomeGuesser = BooleanOptionItem.Create(1100411, "Impostors can become guesser", true, TabGroup.AddOns, false)
                .SetParent(Chance);
            Options.AddOnsChance[AddOns.Guesser] = Chance;
            Options.AddOnsCount[AddOns.Guesser] = Count;
        }
    }
}