using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.DemoWorld
{
    public sealed class MineLayerTilemapGenerator : MonoBehaviour, IWorldBaseGenerator
    {
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private MineLayerDistribution distribution;
        [SerializeField] private TileBase rockTile;
        [SerializeField] private TileBase boundaryRockTile;
        [SerializeField] private TileBase copperTile;
        [SerializeField] private TileBase ironTile;
        [SerializeField] private TileBase lithiumTile;
        [SerializeField] private TileBase gasPocketTile;
        [SerializeField] private TileBase lockedSignalTile;
        [SerializeField] private MiningTileResolver tileResolver;
        [SerializeField] private WorldSnapshotSystem snapshotSystem;
        [SerializeField] private StructuralIntegritySystem structuralSystem;
        [SerializeField] private long worldSeed = 20260731;

        public MineLayerLayout CurrentLayout { get; private set; }
        public long WorldSeed => worldSeed;
        public int GeneratorVersion => distribution != null ? distribution.GeneratorVersion : 0;

#if UNITY_EDITOR
        public void EditorConfigure(
            Tilemap map,
            MineLayerDistribution layerDistribution,
            TileBase rock,
            TileBase boundary,
            TileBase copper,
            TileBase iron,
            TileBase lithium,
            TileBase gas,
            TileBase signal,
            MiningTileResolver resolver,
            WorldSnapshotSystem snapshot,
            long seed)
        {
            foregroundTilemap = map;
            distribution = layerDistribution;
            rockTile = rock;
            boundaryRockTile = boundary;
            copperTile = copper;
            ironTile = iron;
            lithiumTile = lithium;
            gasPocketTile = gas;
            lockedSignalTile = signal;
            tileResolver = resolver;
            snapshotSystem = snapshot;
            structuralSystem = snapshot != null
                ? snapshot.GetComponent<StructuralIntegritySystem>()
                : null;
            worldSeed = seed;
        }
#endif

        private void Awake()
        {
            if (!Regenerate(worldSeed, GeneratorVersion))
            {
                Debug.LogError("Mine layer generation failed. Check distribution and tile references.", this);
            }
        }

        public bool Regenerate(long seed, int generatorVersion)
        {
            if (foregroundTilemap == null
                || distribution == null
                || generatorVersion != distribution.GeneratorVersion
                || !HasAllTiles())
            {
                return false;
            }

            MineLayerLayout layout = new MineLayerGenerator().Generate(seed, distribution);
            foregroundTilemap.ClearAllTiles();
            foreach (MineLayerCell cell in layout.EnumerateCells())
            {
                foregroundTilemap.SetTile(
                    new Vector3Int(cell.X, cell.Y, 0),
                    ResolveTile(cell.Kind));
            }

            // 지표면에서도 좌우 경계를 빠져나가지 않도록 같은 비채굴 타일을 연장한다.
            for (int offset = 1; offset <= distribution.SurfaceBoundaryHeight; offset++)
            {
                int y = distribution.TopY + offset;
                foregroundTilemap.SetTile(
                    new Vector3Int(distribution.MinX, y, 0),
                    boundaryRockTile);
                foregroundTilemap.SetTile(
                    new Vector3Int(distribution.MaxX, y, 0),
                    boundaryRockTile);
            }

            tileResolver?.RegisterRuntime(
                boundaryRockTile,
                new MiningTileDto(
                    MineLayerTileIds.BoundaryRock,
                    string.Empty,
                    0,
                    false,
                    1f,
                    0f,
                    0f,
                    false));
            foregroundTilemap.RefreshAllTiles();

            worldSeed = seed;
            CurrentLayout = layout;
            snapshotSystem?.ConfigureBaseWorldIdentity(seed, generatorVersion);
            structuralSystem?.ConfigureWorldSeed(seed);
            return true;
        }

        private bool HasAllTiles()
        {
            return rockTile != null
                && boundaryRockTile != null
                && copperTile != null
                && ironTile != null
                && lithiumTile != null
                && gasPocketTile != null
                && lockedSignalTile != null;
        }

        private TileBase ResolveTile(MineLayerCellKind kind)
        {
            return kind switch
            {
                MineLayerCellKind.BoundaryRock => boundaryRockTile,
                MineLayerCellKind.Copper => copperTile,
                MineLayerCellKind.Iron => ironTile,
                MineLayerCellKind.Lithium => lithiumTile,
                MineLayerCellKind.GasPocket => gasPocketTile,
                MineLayerCellKind.LockedSignal => lockedSignalTile,
                _ => rockTile
            };
        }
    }
}
