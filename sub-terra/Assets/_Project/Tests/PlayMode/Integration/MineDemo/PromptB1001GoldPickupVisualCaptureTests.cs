#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Integration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    /// <summary>
    /// 골드 팝업 연출을 정해진 시각마다 실제 카메라로 렌더링해 Temp/GoldPopupFrames 아래 PNG 시트로 저장한다.
    /// 판정은 사람이 시트를 눈으로 확인한다. 평소 실행 목록에서는 빠지고 이름을 직접 지정할 때만 돈다.
    /// </summary>
    [Explicit("visual capture")]
    public sealed class PromptB1001GoldPickupVisualCaptureTests
    {
        private const string FontPath = "Assets/_Project/Fonts/SeoulAlrimTTF-Heavy_SDF.asset";
        private const string CoinPath = "Assets/_Project/Art/FX/gold_coin_01.png";
        private const int CellWidth = 560;
        private const int CellHeight = 320;
        private const float Step = 1f / 240f;

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    Object.Destroy(created[index]);
                }
            }

            created.Clear();
        }

        [UnityTest]
        public IEnumerator Capture_BonusPopupSequenceSheets()
        {
            GoldPickupVfx vfx = NewVfx();
            Camera camera = NewCamera();
            yield return null;

            string dir = OutputDirectory();
            var rig = new Rig { Vfx = vfx, Camera = camera };

            // 기본 20 + 추가 15. 시트마다 구간을 나눠 한 번의 재생으로 모두 담는다.
            float[] appear = { 0.03f, 0.08f, 0.13f, 0.18f, 0.24f, 0.30f, 0.35f, 0.41f, 0.46f, 0.50f, 0.56f, 0.64f };
            float[] fall = { 0.52f, 0.56f, 0.60f, 0.64f, 0.68f, 0.71f, 0.74f, 0.78f, 0.82f, 0.86f, 0.89f, 0.95f };
            float[] exit = { 1.05f, 1.24f, 1.30f, 1.36f, 1.42f, 1.48f, 1.54f, 1.60f, 1.66f, 1.72f, 1.78f, 1.82f };
            float[] leverTimes = { 0.30f, 0.40f, 0.45f, 0.50f, 0.56f, 0.62f, 0.68f, 0.74f };
            var appearSheet = new Sheet(CellWidth, CellHeight, 3);
            var fallSheet = new Sheet(CellWidth, CellHeight, 3);
            var exitSheet = new Sheet(CellWidth, CellHeight, 3);
            var leverSheet = new Sheet(360, 360, 4);

            var times = new SortedDictionary<float, List<System.Action>>();
            Schedule(times, appear, t => appearSheet.Add(rig.Shoot(CellWidth, CellHeight, new Vector3(0.15f, 0.55f, -10f), 1.2f)));
            Schedule(times, fall, t => fallSheet.Add(rig.Shoot(CellWidth, CellHeight, new Vector3(0.15f, 0.55f, -10f), 1.2f)));
            Schedule(times, exit, t => exitSheet.Add(rig.Shoot(CellWidth, CellHeight, new Vector3(0.15f, 0.55f, -10f), 1.2f)));
            Schedule(times, leverTimes, t => leverSheet.Add(rig.Shoot(360, 360, new Vector3(1.12f, 0.46f, -10f), 0.36f)));

            vfx.Play(35, new Vector3(30f, 0f, 0f), Vector3.zero, 15);
            Play(vfx, times);

            appearSheet.Save(Path.Combine(dir, "sheet_1_appear_lever.png"));
            fallSheet.Save(Path.Combine(dir, "sheet_2_fall_coins.png"));
            exitSheet.Save(Path.Combine(dir, "sheet_3_exit.png"));
            leverSheet.Save(Path.Combine(dir, "sheet_4_lever_closeup.png"));
            Assert.That(File.Exists(Path.Combine(dir, "sheet_1_appear_lever.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(dir, "sheet_3_exit.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator Capture_BaseOnlyPopupSheet()
        {
            GoldPickupVfx vfx = NewVfx();
            Camera camera = NewCamera();
            yield return null;

            string dir = OutputDirectory();
            var rig = new Rig { Vfx = vfx, Camera = camera };
            var sheet = new Sheet(CellWidth, CellHeight, 3);
            vfx.Play(1234, new Vector3(30f, 0f, 0f), Vector3.zero, 0);
            float[] times = { 0.20f, 0.52f, 0.60f, 0.66f, 0.72f, 0.80f, 1.10f, 1.25f, 1.35f, 1.45f, 1.55f, 1.63f };
            var schedule = new SortedDictionary<float, List<System.Action>>();
            Schedule(schedule, times, t => sheet.Add(rig.Shoot(CellWidth, CellHeight, new Vector3(0.15f, 0.55f, -10f), 1.2f)));
            Play(vfx, schedule);
            sheet.Save(Path.Combine(dir, "sheet_5_base_only.png"));
            Assert.That(File.Exists(Path.Combine(dir, "sheet_5_base_only.png")), Is.True);
        }

        private static void Schedule(SortedDictionary<float, List<System.Action>> map, float[] times, System.Action<float> shoot)
        {
            foreach (float time in times)
            {
                float key = time;
                // 같은 시각 키가 겹치면 미세하게 밀어 둘 다 담는다.
                while (map.ContainsKey(key))
                {
                    key += 0.0001f;
                }

                map[key] = new List<System.Action> { () => shoot(time) };
            }
        }

        private static void Play(GoldPickupVfx vfx, SortedDictionary<float, List<System.Action>> schedule)
        {
            float elapsed = 0f;
            foreach (KeyValuePair<float, List<System.Action>> entry in schedule)
            {
                while (elapsed + Step <= entry.Key + 0.00001f)
                {
                    vfx.Tick(Step);
                    elapsed += Step;
                }

                foreach (System.Action shoot in entry.Value)
                {
                    shoot();
                }
            }
        }

        private static string OutputDirectory()
        {
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Temp", "GoldPopupFrames");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private GoldPickupVfx NewVfx()
        {
            var host = new GameObject("GoldPickupHost");
            created.Add(host);
            var vfx = host.AddComponent<GoldPickupVfx>();
            var coin = AssetDatabase.LoadAssetAtPath<Sprite>(CoinPath);
            Assume.That(coin, Is.Not.Null, "gold_coin_01 스프라이트");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Assume.That(font, Is.Not.Null, "골드 전용 폰트");
            SetPrivate(vfx, "coinSprite", coin);
            SetPrivate(vfx, "pickupFont", font);
            return vfx;
        }

        private Camera NewCamera()
        {
            var go = new GameObject("CaptureCamera", typeof(Camera));
            created.Add(go);
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.15f, 0.17f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.enabled = false;
            return camera;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private sealed class Rig
        {
            public GoldPickupVfx Vfx;
            public Camera Camera;

            public Texture2D Shoot(int width, int height, Vector3 position, float orthoSize)
            {
                Camera.transform.position = position;
                Camera.orthographicSize = orthoSize;
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                Camera.targetTexture = rt;
                Canvas.ForceUpdateCanvases();
                Camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
                Camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
                return texture;
            }
        }

        private sealed class Sheet
        {
            private readonly int cellWidth;
            private readonly int cellHeight;
            private readonly int columns;
            private readonly List<Texture2D> cells = new List<Texture2D>();

            public Sheet(int cellWidth, int cellHeight, int columns)
            {
                this.cellWidth = cellWidth;
                this.cellHeight = cellHeight;
                this.columns = columns;
            }

            public void Add(Texture2D cell)
            {
                cells.Add(cell);
            }

            public void Save(string path)
            {
                if (cells.Count == 0)
                {
                    return;
                }

                int rows = (cells.Count + columns - 1) / columns;
                var sheet = new Texture2D(cellWidth * columns, cellHeight * rows, TextureFormat.RGBA32, false);
                var clear = new Color32[sheet.width * sheet.height];
                sheet.SetPixels32(clear);
                for (var index = 0; index < cells.Count; index++)
                {
                    int column = index % columns;
                    int row = rows - 1 - index / columns;
                    sheet.SetPixels(column * cellWidth, row * cellHeight, cellWidth, cellHeight, cells[index].GetPixels());
                    Object.Destroy(cells[index]);
                }

                sheet.Apply();
                File.WriteAllBytes(path, sheet.EncodeToPNG());
                Object.Destroy(sheet);
                cells.Clear();
            }
        }
    }
}
#endif
