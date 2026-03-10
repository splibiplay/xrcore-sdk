// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Meta.XR;
using Meta.XR.Samples;
using Unity.Collections;
using Unity.InferenceEngine;
using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class SentisInferenceRunManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private DetectionUiMenuManager m_uiMenuManager;
        [SerializeField] private DetectionManager m_detectionManager;

        [Header("Sentis Model config")]
        [SerializeField] private BackendType m_backend = BackendType.CPU;
        [SerializeField] private bool m_forceCpuBackend = true;
        [SerializeField] private ModelAsset m_sentisModel;
        [SerializeField] private TextAsset m_labelsAsset;
        [SerializeField, Range(0, 1)] private float m_iouThreshold = 0.6f;
        [SerializeField, Range(0, 1)] private float m_scoreThreshold = 0.23f;
        [SerializeField] private bool m_forceSingleOutputParser;
        [Header("Performance")]
        [SerializeField, Min(0f)] private float m_inferenceIntervalSeconds = 0.3f;
        [SerializeField] private bool m_logInferenceTiming;
        [SerializeField] private bool m_logModelInfo = true;
        [SerializeField] private bool m_enableBackendMetrics = false;

        [Header("UI display references")]
        [SerializeField] private SentisInferenceUiManager m_uiInference;

        [Header("[Editor Only] Convert to Sentis")]
        public ModelAsset OnnxModel;
        [Space(40)]

        private Model m_loadedModel;
        private Worker m_engine;
        private Vector2Int m_inputSize;
        private int m_modelOutputCount;
        private bool m_useSingleOutputParser;
        private Tensor<float> m_inputTensor;
        private TextureTransform m_textureTransform;
        private int m_textureWidth = -1;
        private int m_textureHeight = -1;
        private readonly List<(int classId, Vector4 boundingBox)> m_detections = new List<(int classId, Vector4 boundingBox)>();

        private int m_inferenceCount = 0;
        private float m_inferenceAccumMs = 0f;
        private float m_inferenceMinMs = float.MaxValue;
        private float m_inferenceMaxMs = 0f;
        private float m_sessionStartTime;
        private BackendType m_requestedBackend;

        [Serializable]
        private struct BackendSessionMetrics
        {
            public string backend;
            public string requestedBackend;
            public int inferenceCount;
            public float avgInferenceMs;
            public float minInferenceMs;
            public float maxInferenceMs;
            public float sessionDurationSec;
            public string timestampIsoUtc;
        }

        private void Awake()
        {
            if (m_sentisModel == null)
            {
                Debug.LogError("[SentisInferenceRunManager] Sentis model is null.");
                enabled = false;
                return;
            }

            m_loadedModel = ModelLoader.Load(m_sentisModel);
            var inputShape = m_loadedModel.inputs[0].shape;
            m_inputSize = new Vector2Int(inputShape.Get(2), inputShape.Get(3));
            m_modelOutputCount = m_loadedModel.outputs.Count;
            m_useSingleOutputParser = m_forceSingleOutputParser || m_modelOutputCount == 1;
            m_inputTensor = new Tensor<float>(new TensorShape(1, 3, m_inputSize.x, m_inputSize.y));
            m_textureTransform = new TextureTransform();
            m_sessionStartTime = Time.realtimeSinceStartup;
            m_requestedBackend = m_backend;
            // Priorizamos estabilidad en Quest: ejecutamos en CPU salvo que se desactive explícitamente.
            if (m_forceCpuBackend && m_backend != BackendType.CPU)
            {
                Debug.LogWarning($"[SentisInferenceRunManager] Forcing backend from {m_backend} to CPU for runtime stability (ForceCpuBackend enabled).");
                m_backend = BackendType.CPU;
            }
            CreateWorker(m_backend);
            if (m_logModelInfo)
            {
                Debug.Log($"[SentisInferenceRunManager] backend={m_backend} modelOutputs={m_modelOutputCount} singleOutputParser={m_useSingleOutputParser}");
            }
        }

        private IEnumerator Start()
        {
            m_uiInference.SetLabels(m_labelsAsset);

            while (true)
            {
                while (m_uiMenuManager.IsPaused)
                {
                    yield return null;
                }

                float cycleStart = Time.realtimeSinceStartup;
                yield return RunInference();

                float elapsed = Time.realtimeSinceStartup - cycleStart;
                float wait = m_inferenceIntervalSeconds - elapsed;
                if (wait > 0f)
                {
                    yield return new WaitForSeconds(wait);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void OnDestroy()
        {
            if (m_enableBackendMetrics && m_inferenceCount > 0)
            {
                TrySaveBackendMetrics();
            }

            if (m_engine != null)
            {
                for (int i = 0; i < m_modelOutputCount; i++)
                {
                    m_engine.PeekOutput(i)?.CompleteAllPendingOperations();
                }
                m_engine.Dispose();
            }

            m_inputTensor?.Dispose();
        }

        internal static void PreloadModel(ModelAsset modelAsset)
        {
            // Load model
            var model = ModelLoader.Load(modelAsset);
            var inputShape = model.inputs[0].shape;
            int outputCount = model.outputs.Count;

            // Create engine to run model
            using var worker = new Worker(model, BackendType.CPU);

            // Run inference with an empty image to load the model in the memory. The first inference blocks the main thread for a long time, so we're doing it on the app launch
            Texture tempTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var textureTransform = new TextureTransform().SetDimensions(tempTexture.width, tempTexture.height, 3);
            using var input = new Tensor<float>(new TensorShape(1, 3, inputShape.Get(2), inputShape.Get(3)));
            TextureConverter.ToTensor(tempTexture, input, textureTransform);
            worker.Schedule(input);

            // Complete the inference immediately and destroy the temporary texture
            for (int i = 0; i < outputCount; i++)
            {
                worker.PeekOutput(i)?.CompleteAllPendingOperations();
            }
            Destroy(tempTexture);
        }

        private IEnumerator RunInference()
        {
            if (!m_cameraAccess.IsPlaying || m_engine == null)
            {
                yield break;
            }

            [DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
            static extern OVRPlugin.Result ovrp_GetNodePoseStateAtTime(double time, OVRPlugin.Node nodeId, out OVRPlugin.PoseStatef nodePoseState);
            if (!ovrp_GetNodePoseStateAtTime(OVRPlugin.GetTimeInSeconds(), OVRPlugin.Node.Head, out _).IsSuccess())
            {
                Debug.Log("ovrp_GetNodePoseStateAtTime failed, which means 'm_cameraAccess.GetCameraPose()' is not reliable, skipping.");
                yield break;
            }

            var cachedCameraPose = m_cameraAccess.GetCameraPose();

            // Update Capture data
            Texture targetTexture = m_cameraAccess.GetTexture();

            // Convert the texture to a Tensor and schedule the inference
            EnsureTextureTransform(targetTexture.width, targetTexture.height);
            TextureConverter.ToTensor(targetTexture, m_inputTensor, m_textureTransform);

            // Medimos tiempo total de inferencia (modelo YOLO en Sentis)
            float t0 = Time.realtimeSinceStartup;

            // Schedule all model layers
            m_engine.Schedule(m_inputTensor);

            if (m_useSingleOutputParser)
            {
                var outputAwaiter = (m_engine.PeekOutput(0) as Tensor<float>).ReadbackAndCloneAsync().GetAwaiter();
                while (!outputAwaiter.IsCompleted)
                {
                    yield return null;
                }

                using var outputTensor = outputAwaiter.GetResult();
                ParseSingleOutputDetections(m_detections, outputTensor, m_scoreThreshold);
            }
            else
            {
                // Get the results. ReadbackAndCloneAsync waits for all layers to complete before returning the result
                var boxesAwaiter = (m_engine.PeekOutput(0) as Tensor<float>).ReadbackAndCloneAsync().GetAwaiter();
                while (!boxesAwaiter.IsCompleted)
                {
                    yield return null;
                }
                using var boxes = boxesAwaiter.GetResult();
                if (boxes.shape[0] == 0)
                {
                    yield break;
                }

                var classIDsAwaiter = (m_engine.PeekOutput(1) as Tensor<int>).ReadbackAndCloneAsync().GetAwaiter();
                while (!classIDsAwaiter.IsCompleted)
                {
                    yield return null;
                }
                using var classIDs = classIDsAwaiter.GetResult();
                if (classIDs.shape[0] == 0)
                {
                    Debug.LogError("classIDs.shape[0] == 0");
                    yield break;
                }

                var scoresAwaiter = (m_engine.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync().GetAwaiter();
                while (!scoresAwaiter.IsCompleted)
                {
                    yield return null;
                }
                using var scores = scoresAwaiter.GetResult();
                if (scores.shape[0] == 0)
                {
                    Debug.LogError("scores.shape[0] == 0");
                    yield break;
                }

                NonMaxSuppression(m_detections, boxes, classIDs, scores, m_iouThreshold, m_scoreThreshold);
            }

            // En este punto ya hemos recibido todas las salidas del modelo
            float dt = (Time.realtimeSinceStartup - t0) * 1000f;
            if (m_logInferenceTiming)
            {
                Debug.Log($"[SentisInferenceRunManager] YOLO inference time: {dt:0.0} ms");
            }

            if (m_enableBackendMetrics)
            {
                m_inferenceCount++;
                m_inferenceAccumMs += dt;
                if (dt < m_inferenceMinMs) m_inferenceMinMs = dt;
                if (dt > m_inferenceMaxMs) m_inferenceMaxMs = dt;
            }

            // Checking if spatial anchor is tracked ensures bounding boxes are placed at correct world space positIons.
            if (!m_cameraAccess.IsPlaying || m_detectionManager.m_spatialAnchor == null || !m_detectionManager.m_spatialAnchor.IsTracked)
            {
                yield break;
            }

            // Update UI.
            m_uiInference.DrawUIBoxes(m_detections, m_inputSize, cachedCameraPose);
        }

        private void EnsureTextureTransform(int width, int height)
        {
            if (width == m_textureWidth && height == m_textureHeight)
            {
                return;
            }

            m_textureWidth = width;
            m_textureHeight = height;
            m_textureTransform = new TextureTransform().SetDimensions(width, height, 3);
        }

        private void CreateWorker(BackendType backend)
        {
            m_engine?.Dispose();
            m_engine = new Worker(m_loadedModel, backend);
            m_backend = backend;
        }

        private static void ParseSingleOutputDetections(List<(int classId, Vector4 boundingBox)> outDetections, Tensor<float> output, float scoreThreshold)
        {
            outDetections.Clear();

            NativeArray<float>.ReadOnly data = output.AsReadOnlyNativeArray();
            const int stride = 6; // x1, y1, x2, y2, score, classId
            int count = data.Length / stride;
            for (int i = 0; i < count; i++)
            {
                int offset = i * stride;
                float v4 = data[offset + 4];
                float v5 = data[offset + 5];

                // Algunos exports ONNX invierten score/class: [x1,y1,x2,y2,class,score].
                float score = v4;
                float classRaw = v5;
                if (v4 > 1f && v5 >= 0f && v5 <= 1f)
                {
                    score = v5;
                    classRaw = v4;
                }

                if (score < scoreThreshold)
                {
                    continue;
                }

                var box = new Vector4(
                    data[offset + 0],
                    data[offset + 1],
                    data[offset + 2],
                    data[offset + 3]
                );

                if (box.z <= box.x || box.w <= box.y)
                {
                    continue;
                }

                int classId = Mathf.Max(0, Mathf.FloorToInt(classRaw + 0.0001f));
                outDetections.Add((classId, box));
            }
        }

        private static void NonMaxSuppression(List<(int classId, Vector4 boundingBox)> outDetections, Tensor<float> boxes, Tensor<int> classIDs, Tensor<float> scores, float iouThreshold, float scoreThreshold)
        {
            outDetections.Clear();

            // Filter by score threshold first
            List<int> filteredIndices = new List<int>();
            NativeArray<float>.ReadOnly scoresArray = scores.AsReadOnlyNativeArray();
            for (int i = 0; i < scoresArray.Length; i++)
            {
                if (scoresArray[i] >= scoreThreshold)
                {
                    filteredIndices.Add(i);
                }
            }

            if (filteredIndices.Count == 0)
            {
                return;
            }

            // Sort filtered indices by scores in descending order
            filteredIndices.Sort((a, b) => scoresArray[b].CompareTo(scoresArray[a]));

            // Apply NMS algorithm
            bool[] suppressed = new bool[filteredIndices.Count];
            for (int i = 0; i < filteredIndices.Count; i++)
            {
                if (suppressed[i])
                    continue;

                int idx = filteredIndices[i];

                // Add this detection to results
                outDetections.Add((classIDs[idx], GetBox(idx)));

                // Suppress overlapping boxes regardless of class
                for (int j = i + 1; j < filteredIndices.Count; j++)
                {
                    if (suppressed[j])
                        continue;

                    int jdx = filteredIndices[j];

                    float iou = CalculateIoU(GetBox(idx), GetBox(jdx));
                    if (iou > iouThreshold)
                    {
                        suppressed[j] = true;
                    }
                }
            }

            Vector4 GetBox(int i) => new Vector4(boxes[i, 0], boxes[i, 1], boxes[i, 2], boxes[i, 3]);
        }

        internal static float CalculateIoU(Vector4 boxA, Vector4 boxB)
        {
            // Boxes are in format (topLeftX, topLeftY, bottomRightX, bottomRightY)
            // Calculate intersection coordinates
            float x1 = Mathf.Max(boxA.x, boxB.x);
            float y1 = Mathf.Max(boxA.y, boxB.y);
            float x2 = Mathf.Min(boxA.z, boxB.z);
            float y2 = Mathf.Min(boxA.w, boxB.w);

            // Calculate intersection area
            float intersectionWidth = Mathf.Max(0, x2 - x1);
            float intersectionHeight = Mathf.Max(0, y2 - y1);
            float intersectionArea = intersectionWidth * intersectionHeight;

            // Calculate individual box areas
            float boxAArea = (boxA.z - boxA.x) * (boxA.w - boxA.y);
            float boxBArea = (boxB.z - boxB.x) * (boxB.w - boxB.y);

            // Calculate union area
            float unionArea = boxAArea + boxBArea - intersectionArea;

            // Return IoU (Intersection over Union)
            if (unionArea == 0)
                return 0;

            return intersectionArea / unionArea;
        }

        private void TrySaveBackendMetrics()
        {
            try
            {
                float sessionDurationSec = Time.realtimeSinceStartup - m_sessionStartTime;
                var metrics = new BackendSessionMetrics
                {
                    backend = m_backend.ToString(),
                    requestedBackend = m_requestedBackend.ToString(),
                    inferenceCount = m_inferenceCount,
                    avgInferenceMs = m_inferenceCount > 0 ? m_inferenceAccumMs / m_inferenceCount : 0f,
                    minInferenceMs = m_inferenceCount > 0 && m_inferenceMinMs < float.MaxValue ? m_inferenceMinMs : 0f,
                    maxInferenceMs = m_inferenceMaxMs,
                    sessionDurationSec = sessionDurationSec,
                    timestampIsoUtc = DateTime.UtcNow.ToString("o")
                };

                string root = Application.persistentDataPath;
                string dir = Path.Combine(root, "DomusViBackendMetrics");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                DateTime ts;
                if (!DateTime.TryParse(metrics.timestampIsoUtc, out ts))
                {
                    ts = DateTime.UtcNow;
                }
                string stamp = ts.ToString("yyyyMMdd_HHmmss");
                string safeBackend = string.IsNullOrEmpty(metrics.backend) ? "UnknownBackend" : metrics.backend;

                // JSON
                string jsonName = $"{safeBackend}_{stamp}.json";
                string jsonPath = Path.Combine(dir, jsonName);
                string json = JsonUtility.ToJson(metrics, true);
                File.WriteAllText(jsonPath, json);

                // CSV (separador ;)
                string csvName = $"{safeBackend}_{stamp}.csv";
                string csvPath = Path.Combine(dir, csvName);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("backend;requestedBackend;timestampUtc;sessionDurationSec;inferenceCount;avgInferenceMs;minInferenceMs;maxInferenceMs;approxFps");
                float approxFps = metrics.avgInferenceMs > 0f ? 1000f / metrics.avgInferenceMs : 0f;
                sb.Append(metrics.backend).Append(";");
                sb.Append(metrics.requestedBackend).Append(";");
                sb.Append(metrics.timestampIsoUtc).Append(";");
                sb.Append(metrics.sessionDurationSec.ToString("0.000")).Append(";");
                sb.Append(metrics.inferenceCount).Append(";");
                sb.Append(metrics.avgInferenceMs.ToString("0.000")).Append(";");
                sb.Append(metrics.minInferenceMs.ToString("0.000")).Append(";");
                sb.Append(metrics.maxInferenceMs.ToString("0.000")).Append(";");
                sb.Append(approxFps.ToString("0.0")).AppendLine();
                File.WriteAllText(csvPath, sb.ToString());

                if (m_logModelInfo)
                {
                    Debug.Log($"[SentisInferenceRunManager][BackendMetrics] Guardado JSON en: {jsonPath}");
                    Debug.Log($"[SentisInferenceRunManager][BackendMetrics] Guardado CSV en: {csvPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SentisInferenceRunManager][BackendMetrics] Error al guardar métricas: {ex.Message}");
            }
        }
    }
}
