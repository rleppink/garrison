using System.Text;
using Garrison.Shared.Player;
using UnityEngine;
using SharedLifeState = Garrison.Shared.Player.LifeState;

namespace Garrison.Combat
{
    public sealed class CombatHeartsHud : MonoBehaviour, ILocalPlayerViewConsumer
    {
        private const string ArmorFilledSymbol = "\u25C6";
        private const string ArmorEmptySymbol = "\u25C7";

        [SerializeField] private MonoBehaviour localViewSource;
        [SerializeField] private LifeState lifeState;
        [SerializeField] private DefenderArmor defenderArmor;
        [SerializeField] private Vector2 screenOffset = new(16f, 16f);
        [SerializeField] private Color healthyColor = new(0.93f, 0.95f, 0.98f, 1f);
        [SerializeField] private Color downedColor = new(0.98f, 0.8f, 0.42f, 1f);
        [SerializeField] private Color deadColor = new(0.75f, 0.75f, 0.75f, 1f);

        private readonly StringBuilder textBuilder = new();

        private ILocalPlayerView localView;
        private GUIStyle style;

        public void BindLocalView(MonoBehaviour source)
        {
            localViewSource = source;
            localView = localViewSource as ILocalPlayerView;
        }

        private void Awake()
        {
            localView = localViewSource as ILocalPlayerView;
        }

        private void OnGUI()
        {
            if (lifeState == null || localView == null || !localView.IsLocalView)
                return;

            EnsureStyle();

            Color previousColor = GUI.color;
            GUI.color = GetHudColor(lifeState.State);
            GUI.Label(new Rect(screenOffset.x, screenOffset.y, 420f, 32f), BuildHeartsText(), style);
            GUI.color = previousColor;
        }

        private string BuildHeartsText()
        {
            textBuilder.Clear();
            textBuilder.Append("Hearts ");

            int maxHearts = Mathf.Max(1, lifeState.MaxHearts);

            for (int i = 0; i < maxHearts; i++)
                textBuilder.Append(i < lifeState.Hearts ? "\u2665" : "\u2661");

            AppendArmorText(maxHearts);

            if (lifeState.State == SharedLifeState.Downed)
                textBuilder.Append("  DOWNED");
            else if (lifeState.State == SharedLifeState.Dead)
                textBuilder.Append("  DEAD");

            return textBuilder.ToString();
        }

        private void AppendArmorText(int maxHearts)
        {
            if (defenderArmor == null || !defenderArmor.IsEnabled)
                return;

            textBuilder.Append("  Armor ");

            for (int i = 0; i < maxHearts; i++)
            {
                int heartIndex = maxHearts - 1 - i;
                bool heartStillInPlay = heartIndex < lifeState.Hearts;
                bool armorIntact = heartStillInPlay && defenderArmor.HasArmorAtHeartIndex(heartIndex);
                textBuilder.Append(armorIntact ? ArmorFilledSymbol : ArmorEmptySymbol);
            }
        }

        private void EnsureStyle()
        {
            if (style != null)
                return;

            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
        }

        private Color GetHudColor(SharedLifeState state)
        {
            return state switch
            {
                SharedLifeState.Downed => downedColor,
                SharedLifeState.Dead => deadColor,
                _ => healthyColor
            };
        }
    }
}
