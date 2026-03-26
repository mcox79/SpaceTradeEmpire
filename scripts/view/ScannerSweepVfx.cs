#nullable enable

using Godot;

namespace SpaceTradeEmpire.View;

// GATE.T57.FEEL.SCANNER_SWEEP.001: Ring-expanding sweep VFX on system entry.
// Attach to a Node3D. Plays a torus mesh that scales outward and fades.
// Duration is fixed at 1.5 seconds; auto-frees on completion.
public partial class ScannerSweepVfx : Node3D
{
    private const float SweepDuration = 1.5f; // STRUCTURAL: animation duration
    private const float MaxRadius = 40.0f; // STRUCTURAL: final ring size (matches system scale)
    private const float StartRadius = 2.0f; // STRUCTURAL: initial ring size

    private MeshInstance3D? _ring;
    private StandardMaterial3D? _material;
    private float _elapsed;
    private bool _playing;

    public override void _Ready()
    {
        // Create a TorusMesh procedurally.
        var torus = new TorusMesh();
        torus.InnerRadius = 0.8f;
        torus.OuterRadius = 1.0f;
        torus.Rings = 32;
        torus.RingSegments = 16;

        _material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.3f, 0.7f, 1.0f, 0.6f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };

        _ring = new MeshInstance3D
        {
            Mesh = torus,
            MaterialOverride = _material
        };

        AddChild(_ring);
        Scale = Vector3.One * StartRadius;
        _elapsed = 0f;
        _playing = true;
    }

    public override void _Process(double delta)
    {
        if (!_playing) return;

        _elapsed += (float)delta;
        float t = Mathf.Clamp(_elapsed / SweepDuration, 0f, 1f);

        // Ease-out expansion.
        float eased = 1.0f - (1.0f - t) * (1.0f - t);
        float radius = Mathf.Lerp(StartRadius, MaxRadius, eased);
        Scale = Vector3.One * radius;

        // Fade out alpha.
        if (_material != null)
        {
            float alpha = Mathf.Lerp(0.6f, 0.0f, t);
            _material.AlbedoColor = new Color(0.3f, 0.7f, 1.0f, alpha);
        }

        if (t >= 1.0f)
        {
            _playing = false;
            QueueFree();
        }
    }
}
