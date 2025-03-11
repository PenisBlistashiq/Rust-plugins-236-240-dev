using Oxide.Core.Plugins;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("HTeamWarning", "https://topplugin.ru/", "1.0.0")]
    public class HTeamWarning : RustPlugin
    {
        private const int TeamLimit = 3; // Максимальное количество игроков в команде
        private const string PanelName = "TeamLimitWarningPanel"; 

        void OnTeamInvite(BasePlayer sender, BasePlayer receiver)
        {
            if (sender.currentTeam != 0 && RelationshipManager.ServerInstance.teams[sender.currentTeam].members.Count >= TeamLimit)
            {
                sender.ChatMessage("Ваша команда достигла лимита, если вы примите еще одного игрока - вы будете забанены!");
                return;
            }

            ShowWarningCUI(sender);
            ShowWarningCUI(receiver);
        }

        void OnTeamAccept(BasePlayer receiver, ulong teamID)
        {
            var team = RelationshipManager.ServerInstance.FindTeam(teamID);

            if (team != null && team.members.Count >= TeamLimit)
            {
                receiver.ChatMessage("Невозможно вступить в команду, достигнут максимальный размер команды (3 игрока).");
                return;
            }

            ShowWarningCUI(receiver);
        }

        private void ShowWarningCUI(BasePlayer player)
        {
            DestroyWarningCUI(player);

            CuiElementContainer elements = new CuiElementContainer();
            elements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.75" },
                RectTransform = { AnchorMin = "0.25 0.8", AnchorMax = "0.75 0.9" },
                CursorEnabled = false
            }, "Overlay", PanelName);

            elements.Add(new CuiLabel
            {
                Text = { 
                    Text = "<color=red>ВНИМАНИЕ!</color> Лимит игроков в команде - <color=orange>3 человека</color>.\nВы несёте ответственность за всех своих тиммейтов (если один из них - <color=red>читер</color>, вы будете заблокированы).", 
                    FontSize = 15, 
                    Color = "1 1 1 1"
                },
                RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = "0.95 0.9" }
            }, PanelName);

            CuiHelper.AddUi(player, elements);
            Puts($"Отправлено CUI предупреждение для {player.displayName}");

            timer.Once(10f, () => DestroyWarningCUI(player));
        }

        private void DestroyWarningCUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelName);
        }
    }
}