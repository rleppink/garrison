using System;
using UnityEngine;
using SharedLifeState = Garrison.Shared.Player.LifeState;

namespace Garrison.Combat
{
    public sealed class CombatLifePresentation : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private LifeState lifeState;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Vector3 downedLocalPosition = new(0f, 0.5f, 0f);
        [SerializeField] private Vector3 downedLocalEuler = new(0f, 0f, 90f);
        [SerializeField] private Color healthyTint = Color.white;
        [SerializeField] private Color downedTint = new(0.9f, 0.8f, 0.58f, 1f);
        [SerializeField] private Color deadTint = new(0.45f, 0.45f, 0.45f, 1f);

        private MaterialPropertyBlock propertyBlock;
        private Color[] baseColors;
        private Vector3 healthyLocalPosition;
        private Quaternion healthyLocalRotation;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();

            if (visualRoot != null)
            {
                healthyLocalPosition = visualRoot.localPosition;
                healthyLocalRotation = visualRoot.localRotation;

                if (renderers == null || renderers.Length == 0)
                    renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            }

            CacheBaseColors();
        }

        private void OnEnable()
        {
            if (lifeState != null)
                lifeState.StateChanged += OnStateChanged;

            ApplyVisuals();
        }

        private void OnDisable()
        {
            if (lifeState != null)
                lifeState.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(SharedLifeState _)
        {
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (lifeState == null)
                return;

            ApplyPose();
            ApplyTint();
        }

        private void ApplyPose()
        {
            if (visualRoot == null)
                return;

            if (lifeState.State is SharedLifeState.Downed or SharedLifeState.Dead)
            {
                visualRoot.localPosition = downedLocalPosition;
                visualRoot.localRotation = Quaternion.Euler(downedLocalEuler);
                return;
            }

            visualRoot.localPosition = healthyLocalPosition;
            visualRoot.localRotation = healthyLocalRotation;
        }

        private void ApplyTint()
        {
            if (renderers == null || baseColors == null)
                return;

            Color tint = lifeState.State switch
            {
                SharedLifeState.Downed => downedTint,
                SharedLifeState.Dead => deadTint,
                _ => healthyTint
            };

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(propertyBlock);

                Color color = i < baseColors.Length ? baseColors[i] * tint : tint;
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void CacheBaseColors()
        {
            if (renderers == null)
            {
                baseColors = Array.Empty<Color>();
                return;
            }

            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    baseColors[i] = Color.white;
                    continue;
                }

                Material sharedMaterial = renderer.sharedMaterial;
                if (sharedMaterial.HasProperty(BaseColorId))
                    baseColors[i] = sharedMaterial.GetColor(BaseColorId);
                else if (sharedMaterial.HasProperty(ColorId))
                    baseColors[i] = sharedMaterial.GetColor(ColorId);
                else
                    baseColors[i] = Color.white;
            }
        }
    }
}
