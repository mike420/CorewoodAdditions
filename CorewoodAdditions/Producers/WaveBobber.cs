using System.Collections.Generic;
using UnityEngine;

namespace CorewoodAdditions.Producers;

// Moves a water piece up and down with the waves while it stays anchored in place. Vanilla Floating needs a
// Rigidbody and pushes the object around with forces, which would let a built piece drift and desync between players.
internal sealed class WaveBobber : MonoBehaviour
{
    internal const string FloatsChildName = "floats";
    private const float MaxWaveOffset = 3f;

    private static readonly Dictionary<string, float> FloatLineOffsets = new Dictionary<string, float>();

    private ZNetView _nview;
    private float _restY;

    private void Awake()
    {
        _nview = GetComponent<ZNetView>();
        _restY = transform.position.y;
    }

    private void Update()
    {
        // The placement ghost has no valid ZNetView; its height is set by the placement patch instead.
        if (_nview == null || !_nview.IsValid() || ZoneSystem.instance == null)
        {
            return;
        }
        Vector3 position = transform.position;
        float seaLevel = ZoneSystem.instance.m_waterLevel;
        float surface = Floating.GetLiquidLevel(new Vector3(position.x, seaLevel - 0.5f, position.z));
        float offset = surface > -10000f ? Mathf.Clamp(surface - seaLevel, -MaxWaveOffset, MaxWaveOffset) : 0f;
        position.y = _restY + offset;
        transform.position = position;
    }

    // Height of the floats' centre above the piece's pivot, so the pivot can be placed that far below the waterline.
    internal static float GetFloatLineOffset(GameObject root)
    {
        string key = Utils.GetPrefabName(root);
        if (FloatLineOffsets.TryGetValue(key, out float cached))
        {
            return cached;
        }
        float offset = 0f;
        GameObject floats = ProducerVisuals.FindChildRecursive(root.transform, FloatsChildName);
        Renderer[] renderers = floats != null ? floats.GetComponentsInChildren<Renderer>(includeInactive: true) : new Renderer[0];
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            offset = bounds.center.y - root.transform.position.y;
        }
        FloatLineOffsets[key] = offset;
        return offset;
    }
}
