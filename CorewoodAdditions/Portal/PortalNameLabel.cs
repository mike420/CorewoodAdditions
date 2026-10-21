using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;

namespace CorewoodAdditions
{
    // Shows the portal's tag as world-space text above the Corewood portal. It is added to our portal prefab only,
    // so vanilla portals are left alone.
    internal sealed class PortalNameLabel : MonoBehaviour
    {
        private const float RefreshInterval = 0.5f;
        private const float ClearanceAboveModel = 0.6f;
        private const float FallbackHeight = 3f;
        private const float CharacterSize = 0.1f;
        private const int FontSize = 64;
        private static readonly Color TextColor = new Color(1f, 0.55f, 0.15f, 1f);
        private static readonly Color OutlineColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Vector3[] OutlineOffsets =
        {
            new Vector3(-0.02f, 0f, 0f),
            new Vector3(0.02f, 0f, 0f),
            new Vector3(0f, -0.02f, 0f),
            new Vector3(0f, 0.02f, 0f)
        };

        private const string TokenShown = "$msg_corewood_portal_names_shown";
        private const string TokenHidden = "$msg_corewood_portal_names_hidden";
        // Aim at roughly the middle of the portal arch for the line-of-sight ray.
        private const float SightTargetHeight = 1.5f;

        private static readonly RaycastHit[] SightHits = new RaycastHit[16];
        private static int _sightMask;
        private static Font _font;
        // Set when only a regular-weight Norse was found, so the text asks Unity for its bold style instead.
        private static bool _useBoldStyle;
        // Asset names in the bundle, and installed font names on Windows, bold first for readability at a distance.
        private static readonly string[] PreferredFonts = { "Norsebold", "Norse" };
        private static readonly string[] PreferredSystemFonts = { "Norse Bold", "Norse" };

        // Flipped by the toggle hotkey; starts shown each session.
        internal static bool HotkeyVisible = true;

        private TeleportWorld _portal;
        private ZNetView _nview;
        private Transform _label;
        private TextMesh[] _meshes;
        private MeshRenderer[] _renderers;
        private string _text = string.Empty;
        private float _nextRefreshTime;
        private bool _visible = true;

        private void Start()
        {
            _portal = GetComponent<TeleportWorld>();
            _nview = GetComponent<ZNetView>();
            // The placement ghost has no valid ZNetView and nothing to read a tag from.
            if (_portal == null || _nview == null || !_nview.IsValid())
            {
                enabled = false;
                return;
            }
            CreateLabel();
        }

        private void LateUpdate()
        {
            if (_label == null)
            {
                return;
            }
            Player player = Player.m_localPlayer;
            if (!MainConfig.portalShowNames.Value || !HotkeyVisible || player == null)
            {
                SetVisible(false);
                return;
            }
            if (Time.time >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.time + RefreshInterval;
                RefreshText();
            }
            float maxDistance = MainConfig.portalNameDistance.Value;
            bool inRange = maxDistance <= 0f || (player.transform.position - transform.position).sqrMagnitude <= maxDistance * maxDistance;
            SetVisible(inRange && _text.Length > 0 && (!MainConfig.portalNamesNeedSight.Value || HasLineOfSight()));
            if (_visible)
            {
                FaceCamera();
            }
        }

        // Called once per frame from the plugin, not per portal.
        internal static void CheckToggleKey()
        {
            KeyboardShortcut key = MainConfig.portalNamesToggleKey.Value;
            if (key.MainKey == KeyCode.None || !key.IsDown() || IsTyping())
            {
                return;
            }
            HotkeyVisible = !HotkeyVisible;
            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, HotkeyVisible ? TokenShown : TokenHidden);
        }

        private static bool IsTyping()
        {
            return Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()) || TextInput.IsVisible();
        }

        // Visible when nothing solid sits between the camera and the portal, other than the portal itself.
        private bool HasLineOfSight()
        {
            Camera camera = Utils.GetMainCamera();
            if (camera == null)
            {
                return false;
            }
            if (_sightMask == 0)
            {
                _sightMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle", "viewblock");
            }
            Vector3 origin = camera.transform.position;
            Vector3 toPortal = transform.position + Vector3.up * SightTargetHeight - origin;
            float distance = toPortal.magnitude;
            if (distance < 0.05f)
            {
                return true;
            }
            int count = Physics.RaycastNonAlloc(origin, toPortal / distance, SightHits, distance, _sightMask, QueryTriggerInteraction.Ignore);
            Collider nearest = null;
            float nearestDistance = float.MaxValue;
            Transform player = Player.m_localPlayer != null ? Player.m_localPlayer.transform : null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = SightHits[i];
                if (hit.collider == null || (player != null && hit.collider.transform.IsChildOf(player)))
                {
                    continue;
                }
                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearest = hit.collider;
                }
            }
            return nearest == null || nearest.transform.IsChildOf(transform);
        }

        private void CreateLabel()
        {
            _label = new GameObject("PortalNameLabel").transform;
            _label.SetParent(transform, worldPositionStays: false);
            _label.localPosition = Vector3.up * GetLabelHeight();

            // Outline copies first, then the coloured text slightly in front of them.
            _meshes = new TextMesh[OutlineOffsets.Length + 1];
            _renderers = new MeshRenderer[_meshes.Length];
            for (int i = 0; i < _meshes.Length; i++)
            {
                bool front = i == OutlineOffsets.Length;
                GameObject part = new GameObject(front ? "Text" : "Outline");
                part.transform.SetParent(_label, worldPositionStays: false);
                part.transform.localPosition = front ? new Vector3(0f, 0f, -0.01f) : OutlineOffsets[i];
                _meshes[i] = CreateTextMesh(part, front ? TextColor : OutlineColor);
                _renderers[i] = part.GetComponent<MeshRenderer>();
            }
            RefreshText();
        }

        private static TextMesh CreateTextMesh(GameObject go, Color color)
        {
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = CharacterSize;
            mesh.fontSize = FontSize;
            mesh.color = color;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            Font font = GetFont();
            if (font != null)
            {
                mesh.font = font;
                mesh.fontStyle = _useBoldStyle ? FontStyle.Bold : FontStyle.Normal;
                renderer.sharedMaterial = font.material;
            }
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return mesh;
        }

        // Norse from the asset bundle (so every player gets it), then Norse if installed on this PC, then
        // Valheim's own serif font. Unity 6 no longer ships the old built-in Arial.ttf.
        private static Font GetFont()
        {
            if (_font != null)
            {
                return _font;
            }
            _font = LoadBundleFont() ?? LoadSystemFont();
            if (_font != null)
            {
                return _font;
            }
            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font != null && font.name.StartsWith("AveriaSerifLibre-Bold"))
                {
                    _font = font;
                    return _font;
                }
            }
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }

        private static Font LoadBundleFont()
        {
            AssetBundle bundle = CorewoodAdditionsMod.cwadditions;
            if (bundle == null)
            {
                return null;
            }
            // Font asset names follow the file name, which may be upper case (NORSEBOLD.OTF), so match ignoring case.
            Font[] fonts = bundle.LoadAllAssets<Font>();
            foreach (string name in PreferredFonts)
            {
                foreach (Font font in fonts)
                {
                    if (font != null && string.Equals(font.name, name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        _useBoldStyle = string.Equals(name, "Norse", System.StringComparison.OrdinalIgnoreCase);
                        return font;
                    }
                }
            }
            return null;
        }

        // Only use an OS font that is really installed; otherwise Unity silently substitutes a default one.
        private static Font LoadSystemFont()
        {
            string[] installed = Font.GetOSInstalledFontNames();
            foreach (string preferred in PreferredSystemFonts)
            {
                foreach (string name in installed)
                {
                    if (string.Equals(name, preferred, System.StringComparison.OrdinalIgnoreCase))
                    {
                        _useBoldStyle = !name.EndsWith("Bold", System.StringComparison.OrdinalIgnoreCase);
                        return Font.CreateDynamicFontFromOSFont(name, FontSize);
                    }
                }
            }
            return null;
        }

        // Just above the top of the portal's meshes (particles excluded, as their bounds are unreliable).
        private float GetLabelHeight()
        {
            float top = float.MinValue;
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(includeInactive: false))
            {
                Bounds bounds = renderer.bounds;
                top = Mathf.Max(top, transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z)).y);
            }
            return top > 0.5f ? top + ClearanceAboveModel : FallbackHeight;
        }

        private void RefreshText()
        {
            string text = (_portal.GetText() ?? string.Empty).RemoveRichTextTags().Trim();
            if (text == _text)
            {
                return;
            }
            _text = text;
            foreach (TextMesh mesh in _meshes)
            {
                mesh.text = text;
            }
        }

        private void FaceCamera()
        {
            Camera camera = Utils.GetMainCamera();
            if (camera == null)
            {
                return;
            }
            Vector3 direction = _label.position - camera.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                _label.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_visible == visible)
            {
                return;
            }
            _visible = visible;
            foreach (MeshRenderer renderer in _renderers)
            {
                renderer.enabled = visible;
            }
        }
    }
}
