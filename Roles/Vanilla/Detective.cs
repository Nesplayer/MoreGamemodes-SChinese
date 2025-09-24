namespace MoreGamemodes
{
    public class Detective : CustomRole
    {
        public Detective(PlayerControl player)
        {
            Role = CustomRoles.Detective;
            BaseRole = BaseRoles.Detective;
            Player = player;
            Utils.SetupRoleInfo(this);
            AbilityUses = -1f;
        }
    }
}