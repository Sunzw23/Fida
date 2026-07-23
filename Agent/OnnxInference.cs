using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fida.Agent
{
    /// <summary>
    /// ONNX 推理助手类，用于加载和执行 ONNX 模型。
    /// 这是一个通用的类，可以被 AgentManager 用于多个模型。
    /// </summary>
    public class OnnxInference : IDisposable
    {
        private readonly InferenceSession _session;
        private bool _disposed;

        /// <summary>
        /// 构造函数，加载指定的 ONNX 模型文件。
        /// </summary>
        /// <param name="modelPath">ONNX 模型的路径。</param>
        public OnnxInference(string modelPath)
        {
            // 检查模型文件是否存在
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"ONNX model not found at: {modelPath}");
            }

            // 创建推理会话。可以添加 SessionOptions 来配置执行提供者，例如使用 GPU。
            // 默认情况下，OnnxRuntime 会使用 CPU。
            _session = new InferenceSession(modelPath);
        }

        /// <summary>
        /// 使用模型进行推理，返回动作的索引。
        /// </summary>
        /// <param name="state">游戏的当前状态，一个 float 数组。</param>
        /// <returns>最佳动作的索引。</returns>
        public int InferActionIndex(float[] state)
        {
            // 输入张量的形状为 [1, state.Length]，批次大小为 1
            var inputTensor = new DenseTensor<float>(state, new int[] { 1, state.Length });

            // 创建一个 ONNX 输入列表，"input" 是我们在 Python 导出时指定的输入名称
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            using (var results = _session.Run(inputs))
            {
                // 获取输出张量
                var outputTensor = results.First().AsTensor<float>();

                // 找到 Q 值最大的动作索引
                var maxIndex = 0;
                var maxValue = outputTensor.GetValue(0);

                for (int i = 1; i < outputTensor.Length; i++)
                {
                    if (outputTensor.GetValue(i) > maxValue)
                    {
                        maxValue = outputTensor.GetValue(i);
                        maxIndex = i;
                    }
                }

                return maxIndex;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _session?.Dispose();
        }
    }
}
