using System;
using System.Collections.Generic;
using System.IO;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SubTerra.Editor
{
    // 원화 텍스처를 공유하는 관절 메시에서 프레임을 굽는다. 런타임은 스프라이트만 사용한다.
    public static class SmoothWalkCycleBaker
    {
        private const string Art = "Assets/_Project/Art/Characters/Player/";
        private static GameObject root;
        private static Camera camera;
        private static RenderTexture target;
        private static Texture2D source;
        private static Material material;
        private static Mesh mesh;
        private static readonly List<Vector2> pixels = new();
        private static readonly List<int> regions = new();
        private static readonly List<int> triangles = new();
        private static Vector3[] positions;
        private static int attempts;

        [MenuItem("SubTerra/Bake Smooth Walk Cycle")]
        public static void Bake()
        {
            try
            {
                root = new GameObject("Continuous walk bake", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
                camera = root.GetComponent<Camera>();
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = .5f;
                camera.aspect = 1;
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                source.LoadImage(File.ReadAllBytes(Art + "Frames/Idle/player_idle_01.png"));
                source.filterMode = FilterMode.Bilinear;
                material = new Material(Shader.Find("Hidden/SubTerra/WalkBake"));
                material.SetTexture("_MainTex", source);
                mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                pixels.Clear();
                regions.Clear();
                triangles.Clear();
                // 각 다리의 메시를 분리해, 서로 교차할 때 사잇면이 길게 늘어나지 않게 한다.
                AddRegion(2, new[]{138,168,159,168,156,172,165,185,166,194,164,206,159,215,168,220,174,228,173,242,131,242,131,229,138,221,137,214,137,202,132,190,131,184});
                AddRegion(1, new[]{110,168,138,168,137,182,136,193,132,207,128,217,124,219,133,229,134,242,80,242,80,228,89,220,95,215,95,205,101,191,101,181});
                // 고정 바지 밑단이 움직이는 허벅지를 가로로 자르지 않게 접합선(170)까지만 덮는다.
                // 양손 영역은 기존 윤곽을 유지하고, 접합선 아래 바지는 각 다리 메시가 담당한다.
                AddRegion(0, new[]{0,0,256,0,256,169,174,169,174,180,159,180,159,170,110,170,110,177,94,182,76,178,70,169,0,169});
                positions = new Vector3[pixels.Count];
                var uv = new Vector2[pixels.Count];
                for (int i = 0; i < pixels.Count; i++) uv[i] = new Vector2(pixels[i].x / 256, 1 - pixels[i].y / 256);
                Pose(0);
                mesh.vertices = positions;
                mesh.uv = uv;
                mesh.triangles = triangles.ToArray();
                mesh.RecalculateBounds();
                var visual = new GameObject("Original miner mesh", typeof(MeshFilter), typeof(MeshRenderer))
                { layer = 31, hideFlags = HideFlags.HideAndDontSave };
                visual.transform.SetParent(root.transform, false);
                visual.transform.position = Vector3.zero;
                visual.GetComponent<MeshFilter>().sharedMesh = mesh;
                visual.GetComponent<MeshRenderer>().sharedMaterial = material;
                attempts = 0;
                EditorApplication.update += Tick;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static void AddRegion(int region, int[] outline)
        {
            var polygon = new List<Vector2>();
            for (int i = 0; i < outline.Length; i += 2) polygon.Add(new Vector2(outline[i], outline[i + 1]));
            while (polygon.Count > 2)
            {
                bool found = false;
                for (int i = 0; i < polygon.Count; i++)
                {
                    var a = polygon[(i + polygon.Count - 1) % polygon.Count];
                    var b = polygon[i];
                    var c = polygon[(i + 1) % polygon.Count];
                    if (Cross(b - a, c - b) <= 0) continue;
                    bool inside = false;
                    foreach (var p in polygon)
                    {
                        if (p == a || p == b || p == c) continue;
                        if (Cross(b - a, p - a) >= 0 && Cross(c - b, p - b) >= 0 && Cross(a - c, p - c) >= 0)
                        { inside = true; break; }
                    }
                    if (inside) continue;
                    Subdivide(a, b, c, region, 0);
                    polygon.RemoveAt(i);
                    found = true;
                    break;
                }
                if (!found) throw new InvalidOperationException("Invalid walk outline.");
            }
        }

        private static void Subdivide(Vector2 a, Vector2 b, Vector2 c, int region, int depth)
        {
            if (depth < 7 && Mathf.Max((a - b).sqrMagnitude, (b - c).sqrMagnitude, (c - a).sqrMagnitude) > 16)
            {
                var ab = (a + b) * .5f;
                var bc = (b + c) * .5f;
                var ca = (c + a) * .5f;
                Subdivide(a, ab, ca, region, depth + 1);
                Subdivide(ab, b, bc, region, depth + 1);
                Subdivide(ca, bc, c, region, depth + 1);
                Subdivide(ab, bc, ca, region, depth + 1);
                return;
            }
            int start = pixels.Count;
            pixels.Add(a); pixels.Add(b); pixels.Add(c);
            for (int i = 0; i < 3; i++)
            {
                regions.Add(region);
                triangles.Add(start + i);
            }
        }

        private static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
        }

        private readonly struct LegPose
        {
            private readonly Vector2 originalHip, originalKnee, originalAnkle, hip, knee, ankle;
            private readonly float thighRotation, shinRotation, bootRotation, thighScale, shinScale;
            private readonly float bodyBob;

            public LegPose(bool near, float phase, float bob)
            {
                bodyBob = bob;
                originalHip = near ? new Vector2(119, 82) : new Vector2(144, 82);
                originalKnee = near ? new Vector2(119, 61) : new Vector2(150, 61);
                originalAnkle = near ? new Vector2(108, 36) : new Vector2(150, 36);
                hip = new Vector2(near ? 128 : 136, 82 + bob);
                float t = (phase - .5f) * 2;
                float stride = phase < .5f ? Mathf.Lerp(24, -24, phase * 2) : Mathf.Lerp(-24, 24, t * t * (3 - 2 * t));
                float lift = phase < .5f ? 0 : 9 * Mathf.Sin(t * Mathf.PI);
                ankle = new Vector2(132 + stride, 37 + lift);
                Vector2 delta = ankle - hip;
                float distance = delta.magnitude;
                const float thighLength = 23, shinLength = 28;
                float along = (thighLength * thighLength - shinLength * shinLength + distance * distance) / (2 * distance);
                knee = hip + delta / distance * along + new Vector2(-delta.y, delta.x) / distance
                    * Mathf.Sqrt(Mathf.Max(0, thighLength * thighLength - along * along));
                Vector2 a = originalKnee - originalHip, b = originalAnkle - originalKnee;
                thighRotation = Mathf.Atan2((knee - hip).y, (knee - hip).x) - Mathf.Atan2(a.y, a.x);
                shinRotation = Mathf.Atan2((ankle - knee).y, (ankle - knee).x) - Mathf.Atan2(b.y, b.x);
                thighScale = (knee - hip).magnitude / a.magnitude;
                shinScale = (ankle - knee).magnitude / b.magnitude;
                bootRotation = phase < .5f ? 0 : 8 * Mathf.Deg2Rad * Mathf.Sin(t * Mathf.PI);
            }

            public Vector2 Deform(Vector2 p)
            {
                float down = 256 - p.y;
                Vector2 deformed;
                if (down <= 204)
                {
                    float bend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(186, 204, down));
                    // 회전된 위치의 평균은 무릎 폭을 줄인다. 공통 관절을 중심으로 각도를 보간한다.
                    float angle = BlendAngle(thighRotation, shinRotation, bend);
                    deformed = knee + Rotate(p - originalKnee, angle) * Mathf.Lerp(thighScale, shinScale, bend);
                }
                else
                {
                    float bend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(211, 225, down));
                    float angle = BlendAngle(shinRotation, bootRotation, bend);
                    deformed = ankle + Rotate(p - originalAnkle, angle) * Mathf.Lerp(shinScale, 1f, bend);
                }
                // 바지 윗단은 몸통과 같은 위치를 유지하고, 허벅지 안쪽에서 다리 변형으로 이어 준다.
                float attachment = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(170, 184, down));
                return Vector2.Lerp(p + Vector2.up * bodyBob, deformed, attachment);
            }

            private static float BlendAngle(float from, float to, float weight) =>
                Mathf.LerpAngle(from * Mathf.Rad2Deg, to * Mathf.Rad2Deg, weight) * Mathf.Deg2Rad;
        }
        private static void Pose(float phase)
        {
            float bob = 1.5f - 3.5f * Mathf.Cos(phase * 4 * Mathf.PI);
            var near = new LegPose(true, phase, bob);
            var far = new LegPose(false, Mathf.Repeat(phase + .5f, 1), bob);
            float swing = 12 * Mathf.Deg2Rad * Mathf.Cos(phase * 2 * Mathf.PI);
            for (int i = 0; i < pixels.Count; i++)
            {
                Vector2 pixel = pixels[i], p = new Vector2(pixel.x, 256 - pixel.y);
                Vector2 basePoint = p + new Vector2(0, bob);
                if (regions[i] != 0)
                {
                    basePoint = regions[i] == 1 ? near.Deform(p) : far.Deform(p);
                }
                if (regions[i] == 0 && pixel.y > 108 && pixel.y < 180)
                {
                    float arm = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(108, 125, pixel.y));
                    float nearWeight = arm * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(101, 112, pixel.x)))
                        * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(73, 84, pixel.x));
                    float farWeight = arm * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(158, 166, pixel.x));
                    var shoulder = new Vector2(99, 137);
                    basePoint = Vector2.Lerp(basePoint, shoulder + Rotate(p - shoulder, -swing) + new Vector2(0, bob), nearWeight);
                    shoulder = new Vector2(160, 121);
                    basePoint = Vector2.Lerp(basePoint, shoulder + Rotate(p - shoulder, swing) + new Vector2(0, bob), farWeight);
                }
                positions[i] = (Vector3)(basePoint - Vector2.one * 128) / 256;
            }
            if (mesh != null)
            {
                mesh.vertices = positions;
                mesh.RecalculateBounds();
            }
        }

        private static void Tick()
        {
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (++attempts < 3 || !RenderPipeline.SupportsRenderRequest(camera, request))
                {
                    if (attempts > 100) throw new InvalidOperationException("Walk renderer unavailable");
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }
                for (int i = 0; i < PlayerAnimationController.WalkFrameCount; i++)
                {
                    Pose(i / (float)PlayerAnimationController.WalkFrameCount);
                    RenderPipeline.SubmitRenderRequest(camera, request);
                    var previous = RenderTexture.active;
                    var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                    try
                    {
                        RenderTexture.active = target;
                        texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                        texture.Apply();
                        File.WriteAllBytes(Art + "Frames/Walk/walk_" + (i + 1).ToString("D2") + ".png", texture.EncodeToPNG());
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        Object.DestroyImmediate(texture);
                    }
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                PlayerAnimationAssetBuilder.ApplySmoothWalkCycle();
                PlayerAnimationAssetBuilder.ValidateRuntimeTransition();
                Finish(null);
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static void Finish(Exception error)
        {
            EditorApplication.update -= Tick;
            if (root != null) Object.DestroyImmediate(root);
            if (mesh != null) Object.DestroyImmediate(mesh);
            if (material != null) Object.DestroyImmediate(material);
            if (source != null) Object.DestroyImmediate(source);
            if (target != null)
            {
                target.Release();
                Object.DestroyImmediate(target);
            }
            pixels.Clear();
            regions.Clear();
            triangles.Clear();
            positions = null;
            if (error != null) Debug.LogException(error);
            else Debug.Log("Original-art walk frames exported.");
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        }
    }
}
