namespace Oxide.Plugins
{
    [Info("No Give Notices", "Wulf", "0.3.0")]
    [Description("Prevents F1 item giving notices from showing in the chat")]
    class NoGiveNotices : RustPlugin
    {
        private object OnServerMessage(string message, string name)
        {
            if (message.Contains("gave") && name == "SERVER")
            {
                return true;
            }

            return null;
        }
    }
}
