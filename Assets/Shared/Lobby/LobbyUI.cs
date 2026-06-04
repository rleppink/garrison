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
        private const string TooltipVisibleClass = "option-tooltip--visible";

        // Gap between the hovered row and the floating tooltip, and the screen edges.
        // Matches --space-2 so it sits on the theme's spacing rhythm.
        private const float TooltipGap = 12f;

        private readonly StringBuilder builder = new();
        private readonly List<OptionControl> optionControls = new();

        private VisualElement lobbyRoot;
        private VisualElement optionGroups;
        private Label roleLabel;
        private Label configLabel;
        private Label playersLabel;
        private Button roundButton;

        // A single floating tooltip reused across all rows. It always lives in the tree
        // at opacity 0 (so the fade can run both ways) and only repositions/reveals on
        // hover. picking-mode is ignored so it never steals hover from the rows beneath.
        private VisualElement tooltipCard;
        private Label tooltipText;
        private VisualElement currentTooltipAnchor;
        private bool hideQueued;

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
            BuildTooltip(root);
        }

        private void BuildTooltip(VisualElement root)
        {
            tooltipCard = new VisualElement { name = "OptionTooltip", pickingMode = PickingMode.Ignore };
            tooltipCard.AddToClassList("option-tooltip");

            tooltipText = new Label { name = "OptionTooltipText", pickingMode = PickingMode.Ignore };
            tooltipText.AddToClassList("option-tooltip__text");
            tooltipCard.Add(tooltipText);

            // The card's size isn't known until it has laid out its (wrapped) text, so
            // reposition whenever its geometry resolves — the first show lands correctly
            // even though we set the text and ask to position in the same frame.
            tooltipCard.RegisterCallback<GeometryChangedEvent>(_ => PositionTooltip());

            // Added last so it draws over the dossiers, and to the panel root so panel
            // overflow can't clip it.
            root.Add(tooltipCard);
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

            // MouseEnter/Leave follow :hover semantics — they treat the row's children
            // (label, field, toggle) as part of the row, so moving the cursor anywhere
            // within the row keeps the same tooltip up without re-firing.
            if (!string.IsNullOrEmpty(definition.Tooltip))
            {
                string tooltip = definition.Tooltip;
                container.RegisterCallback<MouseEnterEvent>(_ => ShowTooltip(container, tooltip));
                container.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip(container));
            }

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

        private void ShowTooltip(VisualElement anchor, string text)
        {
            if (tooltipCard == null)
                return;

            // Cancel any pending hide so moving directly between two rows swaps the text
            // and position in place instead of fading out and back in.
            hideQueued = false;
            currentTooltipAnchor = anchor;
            tooltipText.text = text;
            PositionTooltip();
        }

        private void HideTooltip(VisualElement anchor)
        {
            if (tooltipCard == null || currentTooltipAnchor != anchor)
                return;

            // Defer the hide one frame: if the cursor is moving onto an adjacent row, its
            // MouseEnter (and ShowTooltip) runs next and clears this, avoiding a flicker.
            hideQueued = true;
            tooltipCard.schedule.Execute(ApplyPendingHide);
        }

        private void ApplyPendingHide()
        {
            if (!hideQueued)
                return;

            hideQueued = false;
            currentTooltipAnchor = null;
            tooltipCard.RemoveFromClassList(TooltipVisibleClass);
        }

        private void PositionTooltip()
        {
            if (tooltipCard == null || currentTooltipAnchor == null)
                return;

            float width = tooltipCard.resolvedStyle.width;
            float height = tooltipCard.resolvedStyle.height;

            // Not laid out yet (text just changed) — the GeometryChangedEvent will call
            // back once the card has resolved its wrapped size.
            if (float.IsNaN(width) || width <= 0f || float.IsNaN(height) || height <= 0f)
                return;

            Rect anchor = currentTooltipAnchor.worldBound;
            Rect panel = tooltipCard.parent.worldBound;

            // Prefer the left of the row (the config panel sits on the right); flip to the
            // right side if there isn't room, then clamp inside the panel either way.
            float x = anchor.xMin - width - TooltipGap;
            if (x < TooltipGap)
                x = anchor.xMax + TooltipGap;

            x = Mathf.Clamp(x, TooltipGap, Mathf.Max(TooltipGap, panel.width - width - TooltipGap));

            // Vertically centered on the row, clamped so a long tooltip stays on screen.
            float y = anchor.center.y - height * 0.5f;
            y = Mathf.Clamp(y, TooltipGap, Mathf.Max(TooltipGap, panel.height - height - TooltipGap));

            // Only write when it actually moves, so the GeometryChanged we trigger here
            // doesn't loop. Reveal only once placed, so it never flashes at the origin.
            if (Mathf.Abs(tooltipCard.resolvedStyle.left - x) > 0.5f)
                tooltipCard.style.left = x;
            if (Mathf.Abs(tooltipCard.resolvedStyle.top - y) > 0.5f)
                tooltipCard.style.top = y;

            tooltipCard.AddToClassList(TooltipVisibleClass);
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
