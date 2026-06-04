using Garrison.Shared.Config;
using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Combat
{
    // Replaces the single CombatAimLine. Draws the player's *spread envelope* as a
    // translucent triangle fanning out from the muzzle, bounded by two edge lines. The
    // wedge's half-angle is the true angular cap a shot can land within right now:
    //
    //     half = weaponBaseSpread + movementSpread + recoilBloom
    //
    // Every spread source in the game samples uniformly within a hard cap (ApplySpread
    // and RecoilState.Kick both use Random.Range over a symmetric range), so the wedge's
    // outer edges are an honest boundary: no shot lands outside them. Centered on the raw
    // cursor aim — there is no separate center line; the whole envelope is the readout.
    //
    // Movement spread is an instant state lookup (idle/run/sprint), so transitions would
    // snap; this eases the movement component toward its target with the recoil settle
    // time so stopping a sprint visibly tightens the wedge over the same window recoil
    // recovers. Bloom is read live (it already decays exponentially in WeaponRecoil), so
    // the wedge never lags the real recoil envelope.
    //
    // Local-presentation only, like the line it replaces: visible solely on the owning
    // client's own body (IsLocalView), injected with the local view by LocalPlayerRegistry.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CombatAimWedge : MonoBehaviour, IConfigConsumer, ILocalPlayerViewConsumer
    {
        private const float DefaultEdgeWidth = 0.025f;
        // Far past the screen so the wedge always reaches the edge at any zoom; the camera
        // frustum clips the overshoot (free) and per-edge collision trims it at blockers.
        private const float DefaultLength = 1000f;
        private const float DefaultBaseSpread = 0f;
        private const float DefaultSettleSeconds = 0.4f;
        private const float DirectionEpsilon = 0.0001f;

        [Header("Seams")]
        [SerializeField] private MonoBehaviour localViewSource;
        [SerializeField] private MonoBehaviour recoilSource;
        [SerializeField] private Accuracy accuracy;
        [SerializeField] private Transform muzzle;

        [Header("Presentation")]
        [SerializeField] private MeshFilter fillFilter;
        [SerializeField] private MeshRenderer fillRenderer;
        [SerializeField] private LineRenderer edgeLeft;
        [SerializeField] private LineRenderer edgeRight;
        [SerializeField] private Color fillColor = new(0.5f, 0.82f, 0.96f, 0.12f);
        [SerializeField] private Color edgeColor = new(0.5f, 0.82f, 0.96f, 0.8f);

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        private IConfig config;
        private ILocalPlayerView localView;
        private Mesh fillMesh;
        private MaterialPropertyBlock fillBlock;
        private float displayedMovementSpread;

        private readonly Vector3[] fillVertices = new Vector3[3];
        // Two triangles (six indices) so the fill shows from both sides regardless of the
        // material's cull mode — the top-down camera only sees one, but cheap insurance.
        private static readonly int[] FillTriangles = { 0, 1, 2, 0, 2, 1 };

        private IAimRecoil Recoil => recoilSource as IAimRecoil;

        public void BindLocalView(MonoBehaviour source)
        {
            if (localView != null)
                localView.LocalViewStatusChanged -= OnLocalViewStatusChanged;

            localViewSource = source;
            localView = localViewSource as ILocalPlayerView;

            if (isActiveAndEnabled && localView != null)
                localView.LocalViewStatusChanged += OnLocalViewStatusChanged;

            UpdateVisibility();
        }

        public void Configure(IConfig source)
        {
            if (config == source)
                return;

            if (config != null)
                config.Changed -= OnConfigChanged;

            config = source;

            if (config != null)
                config.Changed += OnConfigChanged;

            ApplyConfig();
            UpdateVisibility();
        }

        private void Awake()
        {
            localView = localViewSource as ILocalPlayerView;

            fillMesh = new Mesh { name = "CombatAimWedgeFill" };
            fillMesh.MarkDynamic();
            if (fillFilter != null)
                fillFilter.sharedMesh = fillMesh;

            fillBlock = new MaterialPropertyBlock();

            ApplyRendererDefaults();
            ApplyConfig();
        }

        private void OnEnable()
        {
            if (localView != null)
                localView.LocalViewStatusChanged += OnLocalViewStatusChanged;

            if (config != null)
                config.Changed += OnConfigChanged;

            UpdateVisibility();
        }

        private void OnDisable()
        {
            if (localView != null)
                localView.LocalViewStatusChanged -= OnLocalViewStatusChanged;

            if (config != null)
                config.Changed -= OnConfigChanged;
        }

        private void OnDestroy()
        {
            if (fillMesh != null)
                Destroy(fillMesh);
        }

        private void LateUpdate()
        {
            if (!CanRender())
                return;

            Vector3 origin = muzzle.position;
            Vector2 planarAim = localView.Aim != null ? localView.Aim.AimDirection : Vector2.zero;
            Vector3 aim = new(planarAim.x, 0f, planarAim.y);
            if (aim.sqrMagnitude <= DirectionEpsilon)
            {
                Collapse(origin);
                return;
            }

            aim.Normalize();

            float halfAngle = ResolveHalfAngleDegrees();
            float maxLength = config?.GetFloat(ConfigKey.AimLineLength, DefaultLength) ?? DefaultLength;

            Vector3 leftDir = Quaternion.AngleAxis(-halfAngle, Vector3.up) * aim;
            Vector3 rightDir = Quaternion.AngleAxis(halfAngle, Vector3.up) * aim;

            Vector3 leftFar = origin + leftDir * ClampLengthToCollision(origin, leftDir, maxLength);
            Vector3 rightFar = origin + rightDir * ClampLengthToCollision(origin, rightDir, maxLength);

            UpdateFill(origin, leftFar, rightFar);
            UpdateEdge(edgeLeft, origin, leftFar);
            UpdateEdge(edgeRight, origin, rightFar);
        }

        // half = base + (eased) movement + (live) bloom. Bloom already decays in
        // WeaponRecoil so it is read raw; only the instant movement-state step is eased,
        // using the same time constant recoil settles by — a frame-rate-independent
        // exponential approach (identical form to RecoilState.Decay).
        private float ResolveHalfAngleDegrees()
        {
            float baseSpread = config?.GetFloat(ConfigKey.WeaponBaseSpread, DefaultBaseSpread) ?? DefaultBaseSpread;
            float targetMovement = accuracy != null ? accuracy.CurrentMovementSpreadDegrees : 0f;
            float bloom = Recoil?.LocalBloomDegrees ?? 0f;

            float settle = Mathf.Max(0.0001f, config?.GetFloat(ConfigKey.RecoilSettleTime, DefaultSettleSeconds) ?? DefaultSettleSeconds);
            float retained = Mathf.Exp(-Time.deltaTime / settle);
            displayedMovementSpread = targetMovement + (displayedMovementSpread - targetMovement) * retained;
            if (Mathf.Abs(displayedMovementSpread - targetMovement) < 0.01f)
                displayedMovementSpread = targetMovement;

            return Mathf.Max(0f, baseSpread + displayedMovementSpread + bloom);
        }

        private void UpdateFill(Vector3 origin, Vector3 leftFar, Vector3 rightFar)
        {
            if (fillMesh == null)
                return;

            // Mesh renders through this transform, so author vertices in local space.
            fillVertices[0] = transform.InverseTransformPoint(origin);
            fillVertices[1] = transform.InverseTransformPoint(leftFar);
            fillVertices[2] = transform.InverseTransformPoint(rightFar);

            fillMesh.Clear();
            fillMesh.SetVertices(fillVertices);
            fillMesh.SetTriangles(FillTriangles, 0);
            // The far verts reach ~AimLineLength out; without fresh bounds the renderer
            // frustum-culls whenever the muzzle (origin) isn't on screen.
            fillMesh.RecalculateBounds();
        }

        private void UpdateEdge(LineRenderer edge, Vector3 start, Vector3 end)
        {
            if (edge == null)
                return;

            edge.enabled = true;
            edge.positionCount = 2;
            edge.SetPosition(0, start);
            edge.SetPosition(1, end);
        }

        private void Collapse(Vector3 origin)
        {
            if (fillMesh != null)
                fillMesh.Clear();

            UpdateEdge(edgeLeft, origin, origin);
            UpdateEdge(edgeRight, origin, origin);
        }

        private float ClampLengthToCollision(Vector3 origin, Vector3 direction, float maxLength)
        {
            if (maxLength <= 0f)
                return 0f;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxLength, collisionMask, triggerInteraction))
                return hit.distance;

            return maxLength;
        }

        private bool CanRender()
        {
            return muzzle != null
                && localView != null
                && localView.IsLocalView
                && fillRenderer != null
                && fillRenderer.enabled;
        }

        private void ApplyRendererDefaults()
        {
            if (fillRenderer != null)
            {
                fillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fillRenderer.receiveShadows = false;
                fillRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                fillRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                fillRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                ApplyFillColor();
            }

            ConfigureEdge(edgeLeft);
            ConfigureEdge(edgeRight);
        }

        private void ConfigureEdge(LineRenderer edge)
        {
            if (edge == null)
                return;

            edge.positionCount = 2;
            edge.useWorldSpace = true;
            edge.loop = false;
            edge.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            edge.receiveShadows = false;
            edge.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            edge.textureMode = LineTextureMode.Stretch;
            edge.alignment = LineAlignment.View;
            edge.startColor = edgeColor;
            edge.endColor = edgeColor;
        }

        private void ApplyConfig()
        {
            float width = config?.GetFloat(ConfigKey.AimLineWidth, DefaultEdgeWidth) ?? DefaultEdgeWidth;
            SetEdgeWidth(edgeLeft, width);
            SetEdgeWidth(edgeRight, width);
        }

        private static void SetEdgeWidth(LineRenderer edge, float width)
        {
            if (edge == null)
                return;

            edge.widthMultiplier = 1f;
            edge.startWidth = width;
            edge.endWidth = width;
        }

        private void ApplyFillColor()
        {
            if (fillRenderer == null || fillBlock == null)
                return;

            fillRenderer.GetPropertyBlock(fillBlock);
            fillBlock.SetColor(ShaderProps.BaseColor, fillColor);
            fillBlock.SetColor(ShaderProps.Color, fillColor);
            fillRenderer.SetPropertyBlock(fillBlock);
        }

        private void UpdateVisibility()
        {
            bool visible = muzzle != null && localView != null && localView.IsLocalView;

            if (fillRenderer != null)
                fillRenderer.enabled = visible;
            if (edgeLeft != null)
                edgeLeft.enabled = visible;
            if (edgeRight != null)
                edgeRight.enabled = visible;

            if (!visible)
            {
                displayedMovementSpread = 0f;
                if (fillMesh != null)
                    fillMesh.Clear();
            }
        }

        private void OnConfigChanged()
        {
            ApplyConfig();
        }

        private void OnLocalViewStatusChanged()
        {
            UpdateVisibility();
        }

        // Cached shader property IDs: _BaseColor (URP/SRP) with a _Color fallback so the
        // tint lands whichever the assigned unlit transparent shader exposes.
        private static class ShaderProps
        {
            public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
            public static readonly int Color = Shader.PropertyToID("_Color");
        }
    }
}
