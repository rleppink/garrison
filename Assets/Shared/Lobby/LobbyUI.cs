using System.Collections.Generic;
using System.Text;
using Garrison.Shared.Round;
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

            if (startButton)
                startButton.onClick.AddListener(SubmitHostRoundAction);

            Refresh();
        }

        private void OnDisable()
        {
            if (lobbyController)
                lobbyController.LobbyChanged -= Refresh;

            if (startButton)
                startButton.onClick.RemoveListener(SubmitHostRoundAction);
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
                configText.text = $"N: {lobbyController.PlayerCountConfig} | {lobbyController.RoundState}";

            if (startButton)
            {
                startButton.interactable = isHost;
                startButton.gameObject.SetActive(isHost);

                Text buttonLabel = startButton.GetComponentInChildren<Text>();
                if (buttonLabel)
                    buttonLabel.text = lobbyController.RoundState == RoundState.Lobby ? "Start" : "Reset";
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

        private void SubmitHostRoundAction()
        {
            if (!lobbyController || !lobbyController.IsLocalHost)
                return;

            if (lobbyController.RoundState == RoundState.Lobby)
                lobbyController.StartRound();
            else
                lobbyController.ResetRound();
        }
    }
}
