using Godot;

[GlobalClass]
[Tool]
public partial class CategoryProbabilityResource : Resource
{
    [Export]
    public string Category { get; set; } = "default";

    [Export(PropertyHint.Range, "0,1,,or_greater")]
    public float Probability
    {
        get => probability;
        set => probability = Mathf.Max(0, value);
    }

    private float probability = 1.0f;
}
