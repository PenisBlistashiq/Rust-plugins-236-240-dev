using Network;

namespace Oxide.Plugins
{
    [Info("HKSHOST AutoInsecure", "playermodel", "1.0.0")]
    class NoSteamDevRust : RustPlugin
    {		
        void OnUserApprove(Connection connection)
        {
			Server.Command("secure 0");
			Server.Command("encryption 0");
		}
	}
}