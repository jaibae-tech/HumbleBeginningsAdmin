namespace HumbleBeginnings.Admin.Modules
{
    /// <summary>
    /// Contract for all Admin Tool modules.
    /// AdminApp controls lifecycle via Enter/Exit.
    /// Modules control their own visibility and UI.
    /// </summary>
    public interface IAdminModule
    {
        void Enter();
        void Exit();
    }
}
