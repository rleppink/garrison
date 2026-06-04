using System.Collections.Generic;
using System.Text;
using Garrison.Shared.Config;
using Garrison.Shared.Round;
using UnityEngine;
using UnityEngine.UIElements;

namespace Garrison.Shared.Lobby
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class LobbyUI : MonoBehaviour
    {
        [SerializeField] private LobbyController lobbyController;
        [SerializeField] private UIDocument document;
        [SerializeField] private VisualTreeAsset layoutTemplate;
        [SerializeField] private VisualTreeAsset optionGroupTemplate;
        [SerializeField] private VisualTreeAsset textOptionRowTemplate;
        [SerializeField] private VisualTreeAsset toggleOptionRowTemplate;
        [SerializeField] private StyleSheet styleSheet;

        private const string HiddenClass = "is-hidden";

        private readonly StringBuilder builder = new();
        private readonly List<OptionControl> optionControls = new();

        private VisualElement lobbyRoot;
        private VisualElement optionGroups;
        private Label roleLabel;
        private Label configLabel;
        private Label playersLabel;
        private Button roundButton;

        private sealed class OptionControl
        {
            public ConfigOptionDefinition Definition;
            public TextField TextField;
            public Toggle Toggle;
        }

        private void OnEnable()
        {
            if (!document || document.rootVisualElement.childCount == 0)
                BuildVisualTree();

            if (lobbyController)
                lobbyController.LobbyChanged += Refresh;

            if (roundButton != null)
                roundButton.clicked += SubmitHostRoundAction;

            Refresh();
        }

        private void OnDisable()
        {
            if (lobbyController)
                lobbyController.LobbyChanged -= Refresh;

            if (roundButton != null)
                roundButton.clicked -= SubmitHostRoundAction;
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

            if (roleLabel != null)
                roleLabel.text = isHost ? "Host" : "Client";

            if (configLabel != null)
                configLabel.text = $"N: {lobbyController.PlayerCountConfig} | {lobbyController.RoundState}";

            if (lobbyRoot != null)
                lobbyRoot.EnableInClassList(HiddenClass, lobbyController.RoundState != RoundState.Lobby);

            RefreshOptionControls(isHost);

            if (roundButton != null)
            {
                roundButton.SetEnabled(isHost);
                roundButton.EnableInClassList(HiddenClass, !isHost);
                roundButton.text = lobbyController.RoundState == RoundState.Lobby ? "Start" : "Reset";
            }

            if (playersLabel == null)
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

            playersLabel.text = builder.Length == 0 ? "Waiting for players" : builder.ToString();
        }

        private void BuildVisualTree()
        {
            if (!document || !layoutTemplate)
                return;

            optionControls.Clear();

            VisualElement root = document.rootVisualElement;
            root.Clear();

            if (styleSheet && !root.styleSheets.Contains(styleSheet))
                root.styleSheets.Add(styleSheet);

            layoutTemplate.CloneTree(root);

            lobbyRoot = root.Q<VisualElement>("LobbyRoot");
            optionGroups = root.Q<VisualElement>("OptionGroups");
            roleLabel = root.Q<Label>("RoleLabel");
            configLabel = root.Q<Label>("ConfigLabel");
            playersLabel = root.Q<Label>("PlayersLabel");
            roundButton = root.Q<Button>("RoundButton");

            BuildOptions();
        }

        private void BuildOptions()
        {
            if (optionGroups == null || !optionGroupTemplate || !textOptionRowTemplate || !toggleOptionRowTemplate)
                return;

            optionGroups.Clear();

            VisualElement currentRows = null;
            string currentGroup = null;
            IReadOnlyList<ConfigOptionDefinition> definitions = ConfigOptionDefinitions.All;
            for (int i = 0; i < definitions.Count; i++)
            {
                ConfigOptionDefinition definition = definitions[i];
                if (definition.Group != currentGroup)
                {
                    currentGroup = definition.Group;
                    currentRows = AddOptionGroup(currentGroup);
                }

                AddOptionRow(currentRows, definition);
            }
        }

        private VisualElement AddOptionGroup(string groupName)
        {
            TemplateContainer container = optionGroupTemplate.CloneTree();
            container.AddToClassList("option-group-host");
            Label header = container.Q<Label>("OptionGroupHeader");
            if (header != null)
                header.text = groupName;

            optionGroups.Add(container);
            return container.Q<VisualElement>("OptionRows");
        }

        private void AddOptionRow(VisualElement parent, ConfigOptionDefinition definition)
        {
            if (parent == null)
                return;

            TemplateContainer container = definition.Type == ConfigValueType.Bool
                ? toggleOptionRowTemplate.CloneTree()
                : textOptionRowTemplate.CloneTree();
            container.AddToClassList("option-row-host");

            Label label = container.Q<Label>("OptionLabel");
            if (label != null)
                label.text = definition.Label;

            OptionControl control = new() { Definition = definition };

            if (definition.Type == ConfigValueType.Bool)
            {
                Toggle toggle = container.Q<Toggle>("OptionToggle");
                if (toggle != null)
                {
                    toggle.RegisterValueChangedCallback(change => lobbyController.SetConfigBool(definition.Key, change.newValue));
                    control.Toggle = toggle;
                }
            }
            else
            {
                TextField textField = container.Q<TextField>("OptionInput");
                if (textField != null)
                {
                    textField.RegisterCallback<FocusOutEvent>(_ => lobbyController.SetConfigFromText(definition, textField.value));
                    textField.RegisterCallback<KeyDownEvent>(evt =>
                    {
                        if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                            return;

                        lobbyController.SetConfigFromText(definition, textField.value);
                        textField.Blur();
                    });
                    control.TextField = textField;
                }
            }

            optionControls.Add(control);
            parent.Add(container);
        }

        private void RefreshOptionControls(bool isHost)
        {
            for (int i = 0; i < optionControls.Count; i++)
            {
                OptionControl control = optionControls[i];
                if (control.TextField != null)
                {
                    control.TextField.SetEnabled(isHost);
                    if (control.TextField.focusController?.focusedElement != control.TextField)
                        control.TextField.SetValueWithoutNotify(lobbyController.GetConfigDisplayValue(control.Definition));
                }

                if (control.Toggle != null)
                {
                    control.Toggle.SetEnabled(isHost);
                    control.Toggle.SetValueWithoutNotify(lobbyController.GetConfigBool(control.Definition.Key));
                }
            }
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
