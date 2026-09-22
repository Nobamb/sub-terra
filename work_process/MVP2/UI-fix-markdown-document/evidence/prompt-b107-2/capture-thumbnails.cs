// Unity MCP RunCommand에서 실행. Bootstrap을 거친 Play Mode, 선택된 저장 슬롯 0이 전제다.
// 실제 Tilemap을 촬영용으로 잠시 배치하고 원복한다. Scene/Prefab/세이브는 저장하지 않는다.
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;

internal class CommandScript : IRunCommand
{
    const string Art = "Assets/_Project/Art/";
    const string Output = Art + "UI/Gameplay/Quest/Thumbnails/";
    const float Floor = -11f;
    GameObject shot;
    Material material;

    public void Execute(ExecutionResult result)
    {
        if (!EditorApplication.isPlaying || SaveRuntimeController.Instance == null
            || SaveRuntimeController.Instance.ActiveSlot != 0)
            throw new InvalidOperationException("Bootstrap capture session with no active save slot required.");
        Tilemap map = null;
        foreach (var candidate in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (candidate.name == "ForegroundTilemap") map = candidate;
        if (map == null) throw new InvalidOperationException("ForegroundTilemap missing.");
        material = map.GetComponent<TilemapRenderer>().sharedMaterial;
        var hiddenLines = new List<LineRenderer>();
        foreach (var line in UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
            if (line.enabled) { hiddenLines.Add(line); line.enabled = false; }
        var originals = new Dictionary<Vector3Int, TileBase>();
        for (int x = -9; x <= 9; x++)
            for (int y = -14; y <= -6; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                originals.Add(cell, map.GetTile(cell));
            }
        var tiles = new List<Tile>();
        var oldActive = RenderTexture.active;
        var target = new RenderTexture(1448, 472, 24);
        var cameraObject = new GameObject("QuestShotCamera", typeof(Camera));
        result.RegisterObjectCreation(cameraObject);
        var camera = cameraObject.GetComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.transform.position = new Vector3(0f, Floor + 1.45f, -10f);
        camera.orthographicSize = 2.25f;
        camera.aspect = 1448f / 472f;
        camera.targetTexture = target;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.04f, 0.055f);
        Directory.CreateDirectory(Output);
        try
        {
            for (int i = 0; i < DemoObjectiveIds.Ordered.Length; i++)
            {
                shot = new GameObject("QuestShot_" + i);
                result.RegisterObjectCreation(shot);
                var deep = i >= 13;
                camera.orthographicSize = i == 0 || i == 1 || i == 2 || i == 5 || i == 14 ? 1.55f : 2.05f;
                camera.transform.position = new Vector3(0.4f, Floor + camera.orthographicSize - 0.55f, -10f);
                var ground = TileFor("Tiles/Ground/ground_" + (deep ? "deep" : "normal") + "_01.png", tiles);
                foreach (var pair in originals)
                {
                    var p = pair.Key;
                    map.SetTile(p, p.y < -11 || p.y >= -7 || p.x < -6 || p.x > 5 ? ground : null);
                }
                // 바닥과 천장 사이에 실제 채굴 통로를 구성한다. 캐릭터/시설 크기는 블록 단위다.
                switch (i)
                {
                    case 0: case 1: case 2: case 5: case 14:
                        var path = i == 1 ? "Tiles/Ore/ore_copper_01.png"
                            : i == 5 ? "Tiles/Ore/ore_iron_01.png"
                            : i == 14 ? "Tiles/Ore/ore_lithium_01.png"
                            : "Tiles/Ground/ground_normal_01.png";
                        var ore = TileFor(path, tiles);
                        for (int x = 1; x <= 5; x++)
                            for (int y = -11; y <= -8; y++)
                                map.SetTile(new Vector3Int(x, y, 0), (x + y) % 3 == 0 ? ground : ore);
                        Person("Mining/mining_06.png", 0.52f, 1.15f);
                        Sparks(0.98f, Floor + 0.37f, new Color(1f, 0.74f, 0.28f));
                        if (i == 2) Facility("settlement_console_cartoon_v3.png", -1.7f, 1.65f);
                        break;
                    case 3:
                        Facility("elevator_station_mine.png", 0.65f, 2.8f);
                        Person("Idle/player_idle_01.png", 0.65f, 1.1f);
                        break;
                    case 4:
                        Facility("elevator_station_mine.png", -2.4f, 2.8f);
                        Person("Walk/walk_04.png", 0.55f, 1.1f);
                        break;
                    case 6:
                        Facility("support_pillar_mine.png", 0.65f, 3.6f);
                        SpriteAt("Tiles/Crack_Overlay/crack_orange_overlay.png", 0.6f, Floor + 3.85f, 1.35f, 1.1f, 2);
                        Person("Mining/mining_04.png", -0.55f, 1.1f);
                        Sparks(0.15f, Floor + 0.65f, Color.cyan);
                        break;
                    case 7:
                        for (int y = 0; y < 4; y++) Facility("ladder_segment_mine.png", 0.7f, 1f, y);
                        Person("Mining/mining_04.png", -0.25f, 1.1f);
                        Sparks(0.4f, Floor + 0.6f, Color.cyan);
                        break;
                    case 8:
                        Facility("light_basic_cartoon_v3.png", 0.7f, 2.1f);
                        Person("Mining/mining_04.png", -0.65f, 1.1f);
                        Sparks(0.1f, Floor + 0.7f, Color.cyan);
                        break;
                    case 9:
                        Facility("storage_basic_cartoon_v2.png", 0.65f, 1.65f);
                        Person("Idle/player_idle_01.png", -0.9f, 1.1f);
                        SpriteAt("Icons/icon_copper.png", -0.25f, Floor + 0.68f, 0.35f, 0.35f, 12);
                        break;
                    case 10:
                        Facility("outpost_core_cartoon_v3.png", 0.8f, 2.45f);
                        Person("Mining/mining_04.png", -0.85f, 1.1f);
                        Sparks(-0.2f, Floor + 0.65f, Color.cyan);
                        break;
                    case 11: case 12: case 16:
                        Facility("outpost_core_cartoon_v3.png", -2.8f, 2.45f);
                        Facility(i == 11 ? "charger_basic_cartoon_v2.png"
                            : i == 12 ? "clinic_basic_cartoon_v3.png" : "settlement_console_cartoon_v3.png", 0.85f, 2f);
                        Person("Idle/player_idle_01.png", -0.6f, 1.1f);
                        Sparks(-0.25f, Floor + 0.65f, i == 12 ? Color.green : Color.cyan);
                        break;
                    case 13:
                        for (int y = -11; y <= -8; y++) map.SetTile(new Vector3Int(3, y, 0), ground);
                        SpriteAt("Tiles/SealedGlyph/sealed_glyph_stone_01.png", 3.5f, Floor + 1.5f, 1f, 1f, 1);
                        Person("Walk/walk_04.png", 0.25f, 1.1f);
                        break;
                    case 15:
                        Facility("outpost_core_cartoon_v3.png", -1.8f, 2.45f);
                        Person("Mining/mining_04.png", 0.5f, 1.1f);
                        var gas = TileFor("Tiles/Ground/ground_gas_01.png", tiles);
                        map.SetTile(new Vector3Int(1, -11, 0), gas);
                        map.SetTile(new Vector3Int(2, -11, 0), gas);
                        PrefabVisual("Hazards/GasZone.prefab", 1.8f, 1.8f);
                        Sparks(1.1f, Floor + 0.65f, new Color(0.5f, 1f, 0.3f));
                        break;
                    case 17:
                        Facility("elevator_station_mine.png", 2.7f, 2.8f);
                        Facility("outpost_core_cartoon_v3.png", -2.8f, 2.45f);
                        PrefabVisual("Buildings/EmergencyEscapePortal.prefab", 0f, 2.4f);
                        Person("Walk/walk_04.png", 0f, 1.1f);
                        Sparks(0f, Floor + 0.55f, Color.cyan);
                        break;
                }
                map.RefreshAllTiles();
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1448, 472, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1448, 472), 0, 0);
                image.Apply();
                var name = DemoObjectiveIds.Ordered[i].Replace("demo.quest.", "");
                File.WriteAllBytes(Output + name + ".png", image.EncodeToPNG());
                result.DestroyObject(image);
                shot.SetActive(false);
                result.DestroyObject(shot);
                shot = null;
            }
            result.Log("Captured 18 staged gameplay thumbnails.");
        }
        finally
        {
            foreach (var pair in originals) map.SetTile(pair.Key, pair.Value);
            foreach (var line in hiddenLines) if (line != null) line.enabled = true;
            if (shot != null) result.DestroyObject(shot);
            camera.targetTexture = null;
            RenderTexture.active = oldActive;
            result.DestroyObject(target);
            result.DestroyObject(cameraObject);
            foreach (var tile in tiles) result.DestroyObject(tile);
        }
    }

    Tile TileFor(string path, List<Tile> tiles)
    {
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path);
        if (tile.sprite == null) throw new InvalidOperationException(path);
        tiles.Add(tile);
        return tile;
    }

    void Person(string frame, float x, float height)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Characters/Player/Frames/" + frame);
        SpriteAt("Characters/Player/Frames/" + frame, x, Floor - 0.06f + height / 2f,
            height * sprite.bounds.size.x / sprite.bounds.size.y, height, 10);
    }

    void Facility(string name, float x, float height, float bottom = 0f)
    {
        var path = "Facilities/MVP/" + name;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path);
        if (sprite == null) throw new InvalidOperationException(path);
        SpriteAt(path, x, Floor - 0.17f + bottom + height / 2f,
            height * sprite.bounds.size.x / sprite.bounds.size.y, height, 4);
    }

    void PrefabVisual(string path, float x, float height)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/" + path);
        var group = new GameObject("ExistingPrefabVisual");
        group.transform.SetParent(shot.transform);
        var bounds = new Bounds();
        bool first = true;
        foreach (var source in prefab.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (source.sprite == null || source.color.a <= 0f) continue;
            var child = new GameObject(source.name, typeof(SpriteRenderer));
            child.transform.SetParent(group.transform);
            child.transform.localPosition = source.transform.position - prefab.transform.position;
            child.transform.localRotation = source.transform.rotation;
            child.transform.localScale = source.transform.lossyScale;
            var sr = child.GetComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.color = source.color;
            sr.sharedMaterial = material;
            sr.sortingOrder = source.sortingOrder + 5;
            if (first) { bounds = sr.bounds; first = false; }
            else bounds.Encapsulate(sr.bounds);
        }
        float scale = height / bounds.size.y;
        group.transform.localScale = Vector3.one * scale;
        group.transform.position = new Vector3(x - bounds.center.x * scale, Floor - bounds.min.y * scale, 0f);
    }

    void SpriteAt(string path, float x, float y, float width, float height, int order)
    {
        var go = new GameObject(Path.GetFileNameWithoutExtension(path), typeof(SpriteRenderer));
        go.transform.SetParent(shot.transform);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path);
        if (sr.sprite == null) throw new InvalidOperationException(path);
        sr.sharedMaterial = material;
        sr.sortingOrder = order;
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(width / sr.sprite.bounds.size.x, height / sr.sprite.bounds.size.y, 1f);
    }

    void Sparks(float x, float y, Color color)
    {
        for (int i = 0; i < 9; i++)
        {
            var go = new GameObject("ContactSpark", typeof(LineRenderer));
            go.transform.SetParent(shot.transform);
            var line = go.GetComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.sortingOrder = 15;
            line.startColor = line.endColor = color;
            line.startWidth = 0.025f;
            line.endWidth = 0.006f;
            float angle = i * 2.39996f;
            var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var point = new Vector3(x, y, -0.1f);
            line.SetPosition(0, point + direction * 0.06f);
            line.SetPosition(1, point + direction * (0.16f + i * 0.025f));
        }
    }
}
