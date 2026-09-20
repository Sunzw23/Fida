namespace Fida.Agent;

/// <summary>
/// Mobile fallback for the optional desktop ONNX policy. Campaign-specific
/// scripted controllers and the deterministic AgentManager fallback remain
/// available without shipping a native ONNX runtime in the Android package.
/// </summary>
public sealed class OnnxInference
{
    public OnnxInference(string modelPath) { }

    public int InferActionIndex(float[] state) => 0;
}
