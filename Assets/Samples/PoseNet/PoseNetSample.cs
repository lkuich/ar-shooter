using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TensorFlowLite;
using System.Linq;

public class PoseNetSample : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] string fileName = "posenet_mobilenet_v1_100_257x257_multi_kpt_stripped.tflite";
    [SerializeField] RawImage cameraView = null;
    [SerializeField, Range(0f, 1f)] float threshold = 0.5f;
    [SerializeField, Range(0f, 1f)] float lineThickness = 0.5f;

    WebCamTexture webcamTexture;
    PoseNet poseNet;
    Vector3[] corners = new Vector3[4];
    PrimitiveDraw draw;

    public PoseNet.Result[] results;

    public InputController inputController;
    public RawImage hitmarker;

    public bool showDebug = false;
    public void ToggleDebug()
    {
        this.showDebug = !this.showDebug;
    }

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        poseNet = new PoseNet(path);

        // Init camera
        string cameraName = WebCamUtil.FindName();
        webcamTexture = new WebCamTexture(cameraName, 640, 480, 30);
        webcamTexture.Play();
        cameraView.texture = webcamTexture;

        draw = new PrimitiveDraw()
        {
            color = Color.green,
        };
    }

    void OnDestroy()
    {
        webcamTexture?.Stop();
        poseNet?.Dispose();
        draw?.Dispose();
    }

    void Update()
    {
        poseNet.Invoke(webcamTexture);
        results = poseNet.GetResults();

        cameraView.material = poseNet.transformMat;
        // cameraView.texture = poseNet.inputTex;

        // DrawResult();
        DrawFrame();
        // DrawFrame(ref results);
    }

    void DrawResult()
    {
        var rect = cameraView.GetComponent<RectTransform>();
        rect.GetWorldCorners(corners);
        Vector3 min = corners[0];
        Vector3 max = corners[2];

        var connections = PoseNet.Connections;
        int len = connections.GetLength(0);
        for (int i = 0; i < len; i++)
        {
            var a = results[(int)connections[i, 0]];
            var b = results[(int)connections[i, 1]];
            if (a.confidence >= threshold && b.confidence >= threshold)
            {
                draw.Line3D(
                    MathTF.Lerp(min, max, new Vector3(a.x, 1f - a.y, 0)),
                    MathTF.Lerp(min, max, new Vector3(b.x, 1f - b.y, 0)),
                    lineThickness
                );
            }
        }

        draw.Apply();
    }

    void DrawArea(in Matrix4x4 matrix)
    {
        var rect = cameraView.GetComponent<RectTransform>();
        rect.GetWorldCorners(corners);
        Vector3 min = corners[0];
        Vector3 max = corners[2];

        draw.color = Color.red;

        var mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, false, webcamTexture.videoVerticallyMirrored)
            * matrix.inverse;
        Vector3 a = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 0, 0)));
        Vector3 b = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 0, 0)));
        Vector3 c = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 1, 0)));
        Vector3 d = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 1, 0)));

        draw.Quad(a, b, c, d, 0.02f);
        draw.Apply();
    }

    void DrawFrame()
    {
        var rect = cameraView.GetComponent<RectTransform>();
        rect.GetWorldCorners(corners);
        Vector3 min = corners[0];
        Vector3 max = corners[2];

        var pad = 1;

        var w = results.Where(r => r.confidence >= threshold);
        var leftX = w.Min(r => r.x);
        var rightX = w.Max(r => r.x);

        var topY = w.Min(r => r.y);
        var bottomY = w.Max(r => r.y);

        Vector2[] coords = {
            new Vector2(leftX, topY),
            new Vector2(rightX, bottomY)
        };

        Rect rectt = RectExtension.GetBoundingBox(coords);
        Rect finalRect = MathTF.Lerp(min, max, rectt, true);

        if (Debug.isDebugBuild || showDebug)
        {
            draw.Rect(finalRect, lineThickness, min.z);
            draw.Apply();
        }

        inputController.Overlap(finalRect.Overlaps(hitmarker.uvRect));
    }
}
