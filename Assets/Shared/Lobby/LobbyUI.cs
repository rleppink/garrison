using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Garrison.Shared.Lobby
{
    public sealed class LobbyUI : MonoBehaviour
    {
        [SerializeField] private LobbyController lobbyController;
        [SerializeField] private Text roleText;
        [SerializeField] private Text configText;
        [SerializeField] private Text playersText;
        [SerializeField] private Button startButton;

        private readonly StringBuilder builder = new();

        private void OnEnable()
        {
            if (lobbyController)
                lobbyController.LobbyChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (lobbyController)
                lobbyController.LobbyChanged -= Refresh;
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!lobbyController)
                return;

            bool isHost = lobbyController.IsLocalHost;

            if (roleText)
                roleText.text = isHost ? "Host" : "Client";

            if (configText)
                configText.text = $"N: {lobbyController.PlayerCountConfig}";

            if (startButton)
            {
                startButton.interactable = false;
                startButton.gameObject.SetActive(isHost);
            }

            if (!playersText)
                return;

            IReadOnlyList<LobbyPlayerView> players = lobbyController.PlayerViews;
            builder.Clear();

            for (int i = 0; i < players.Count; i++)
            {
                LobbyPlayerView player = players[i];
                builder.Append(player.DisplayName);

                if (player.IsHost)
                    builder.Append(" (host)");

                if (i < players.Count - 1)
                    builder.AppendLine();
            }

            playersText.text = builder.Length == 0 ? "Waiting for players" : builder.ToString();
        }
    }
}
