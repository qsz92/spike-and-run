using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelGenerator : MonoBehaviourPunCallbacks
{
    [Header("Tilemap")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private TileBase groundTile;
    [SerializeField] private TileBase[] groundTiles;

    [Header("Prefabs")]
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject platformPrefab;

    [Header("Размер уровня")]
    [SerializeField] private int levelWidthTiles  = 1000;
    [SerializeField] private int groundTileY      = -4;
    [SerializeField] private int groundDepthTiles = 3;
    [SerializeField] private int boundaryWallInsetTiles = 20;

    [Header("Зона спавна игроков (мировые единицы)")]
    [SerializeField] private float safeZoneHalfWidth = 4f;

    [Header("Количество объектов")]
    [SerializeField] private int platformCount = 8;
    [SerializeField] private int spikeCount    = 7;
    [SerializeField] private int enemyCount    = 4;
    [SerializeField] private int coinCount     = 16;

    [Header("Платформы")]
    [SerializeField] private float platformMinWidth = 2.5f;
    [SerializeField] private float platformMaxWidth = 5f;

    public static List<Vector3> PlatformPositions = new();

    private readonly List<GameObject>             spawnedObjects = new();
    private readonly List<(Vector3 pos, float w)> platformData   = new();
    private readonly List<float>                  spikeXList     = new();
    private readonly Dictionary<int, int>         columnHeights  = new();

    private float WorldGroundTopY =>
        groundTilemap.GetCellCenterWorld(new Vector3Int(0, groundTileY, 0)).y
        + groundTilemap.cellSize.y * 0.5f;

    private float HalfWorldWidth => (levelWidthTiles / 2f) * groundTilemap.cellSize.x;
    private float BoundaryWallInsetWorld => boundaryWallInsetTiles * groundTilemap.cellSize.x;
    private float PlayableHalfWorldWidth => HalfWorldWidth - BoundaryWallInsetWorld;

    private void Start()
    {
        ClearSceneManualObjects();
        if (PhotonNetwork.IsMasterClient)
        {
            int seed = Random.Range(0, 999999);
            photonView.RPC(nameof(RPC_GenerateLevel), RpcTarget.AllBuffered, seed);
        }
    }

    private void ClearSceneManualObjects()
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Enemy")) Destroy(go);
        foreach (var go in GameObject.FindGameObjectsWithTag("Coin"))  Destroy(go);
        string[] names = { "platform(1)", "platform(2)", "platform(3)",
                            "platform(4)", "platform(5)", "platform(6)" };
        foreach (string n in names) { var go = GameObject.Find(n); if (go) Destroy(go); }
    }

    [PunRPC]
    private void RPC_GenerateLevel(int seed)
    {
        ClearGenerated();
        Random.InitState(seed);
        platformData.Clear();
        spikeXList.Clear();
        PlatformPositions.Clear();

        float half = PlayableHalfWorldWidth;

        GenerateGround(seed);
        GenerateBoundaryWalls();
        GenerateSpikes(half);
        GeneratePlatforms(half);
        GenerateEnemies(half);
        GenerateCoins(half);

        Debug.Log($"[LevelGenerator] seed={seed} platforms={PlatformPositions.Count}");
    }

    private void GenerateGround(int seed)
    {
        columnHeights.Clear();
        int halfTiles = levelWidthTiles / 2;

        for (int x = -halfTiles; x < halfTiles; x++)
        {
            float noise = Mathf.PerlinNoise(x * 0.05f, seed * 0.01f);
            int heightOffset = Mathf.RoundToInt(noise * 4f);
            columnHeights[x] = groundTileY + heightOffset;
        }

        for (int x = -halfTiles + 1; x < halfTiles; x++)
        {
            int previousTopY = columnHeights[x - 1];
            int topTileY = columnHeights[x];
            if (topTileY > previousTopY + 1)
                columnHeights[x] = previousTopY + 1;
            else if (topTileY < previousTopY - 1)
                columnHeights[x] = previousTopY - 1;
        }

        for (int x = -halfTiles; x < halfTiles; x++)
        {
            int topTileY = columnHeights[x];
            for (int y = groundTileY - groundDepthTiles; y <= topTileY; y++)
                groundTilemap.SetTile(new Vector3Int(x, y, 0), GetGroundTile(x, y, topTileY));
        }
    }

    private TileBase GetGroundTile(int x, int y, int topTileY)
    {
        EnsureGroundTilesLoaded();
        if (groundTiles == null || groundTiles.Length == 0)
            return groundTile;

        bool hasLeft = columnHeights.TryGetValue(x - 1, out int leftTopY) && y <= leftTopY;
        bool hasRight = columnHeights.TryGetValue(x + 1, out int rightTopY) && y <= rightTopY;
        int index;

        if (y == topTileY)
        {
            if (!hasLeft && hasRight)
                index = 0;
            else if (hasLeft && !hasRight)
                index = 2;
            else
                index = 1;
        }
        else if (y == topTileY - 1 && hasLeft && leftTopY < topTileY)
        {
            index = 9;
        }
        else if (y == topTileY - 1 && hasRight && rightTopY < topTileY)
        {
            index = 10;
        }
        else if (!hasLeft)
        {
            index = 3;
        }
        else if (!hasRight)
        {
            index = 5;
        }
        else
        {
            index = 4;
        }

        index = Mathf.Min(index, groundTiles.Length - 1);
        return groundTiles[index] ? groundTiles[index] : groundTile;
    }

    private void EnsureGroundTilesLoaded()
    {
        if (groundTiles != null && groundTiles.Length > 0) return;

#if UNITY_EDITOR
        groundTiles = new TileBase[11];
        for (int i = 0; i < groundTiles.Length; i++)
            groundTiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/2D Platformer/Assets/TilePalette/ground({i + 1}).asset");
#endif
    }

    private float GetWorldTopY(float worldX)
    {
        int cellX = groundTilemap.WorldToCell(new Vector3(worldX, 0f, 0f)).x;
        if (!columnHeights.TryGetValue(cellX, out int topTileY))
            topTileY = groundTileY;

        return groundTilemap.GetCellCenterWorld(new Vector3Int(cellX, topTileY, 0)).y
            + groundTilemap.cellSize.y * 0.5f;
    }

    private void GeneratePlatforms(float half)
    {
        int currentHeight = 1;
        float stepX = (half * 2f) / (platformCount + 1);
        float startX = -half + stepX;
        int totalAttempts = 0;

        for (int i = 0; i < platformCount; i++)
        {
            if (totalAttempts++ > platformCount * 30) break;

            float w = Random.Range(platformMinWidth, platformMaxWidth);
            float x = startX + i * stepX;

            if (Mathf.Abs(x) < safeZoneHalfWidth)
                x = x > 0 ? safeZoneHalfWidth + w : -safeZoneHalfWidth - w;

            float y = GetWorldTopY(x) + currentHeight;

            bool nearSpike = false;
            foreach (float sx in spikeXList)
                if (Mathf.Abs(sx - x) < 1.5f) { nearSpike = true; break; }
            if (nearSpike) { i--; continue; }

            if (OverlapsPlatform(x, y, w)) { i--; continue; }

            var p = Instantiate(platformPrefab, new Vector3(x, y, 0), Quaternion.identity);
            spawnedObjects.Add(p);
            platformData.Add((new Vector3(x, y, 0), w));
            if (Mathf.Abs(x) < half - 10f)
                PlatformPositions.Add(new Vector3(x, y + 1f, 0));

            int delta = Random.Range(-1, 2);
            currentHeight = Mathf.Clamp(currentHeight + delta, 1, 4);
        }
    }

    private void GenerateBoundaryWalls()
    {
        float half = PlayableHalfWorldWidth;
        float wallWidth = groundTilemap.cellSize.x;
        float wallHeight = 200f;
        float centerY = groundTilemap.GetCellCenterWorld(new Vector3Int(0, groundTileY, 0)).y + wallHeight * 0.5f;

        CreateBoundaryWall(-half, centerY, wallWidth, wallHeight);
        CreateBoundaryWall(half, centerY, wallWidth, wallHeight);
    }

    private void CreateBoundaryWall(float x, float y, float width, float height)
    {
        var wall = new GameObject("Invisible Boundary Wall");
        wall.transform.position = new Vector3(x, y, 0f);
        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, height);
        spawnedObjects.Add(wall);
    }

    private void GenerateSpikes(float half)
    {
        const float spikeHalfH = 0.127f;
        var spawnedSpikeX = new List<float>();
        int attempts = 0, spawned = 0;

        while (spawned < spikeCount && attempts < spikeCount * 20)
        {
            attempts++;
            float x = RandomXOutsideSafeZone(half, 0.5f);
            if (!IsFlatGroundAt(x, 1)) continue;

            bool tooClose = false;
            foreach (float sx in spawnedSpikeX)
                if (Mathf.Abs(sx - x) < 2f) { tooClose = true; break; }
            if (tooClose) continue;

            spikeXList.Add(x);
            spawnedSpikeX.Add(x);
            spawnedObjects.Add(Instantiate(spikePrefab, new Vector3(x, GetWorldTopY(x) + spikeHalfH, 0), Quaternion.identity));
            spawned++;
        }
    }

    private bool IsFlatGroundAt(float worldX, int radiusTiles)
    {
        int cellX = groundTilemap.WorldToCell(new Vector3(worldX, 0f, 0f)).x;
        if (!columnHeights.TryGetValue(cellX, out int centerTopY))
            return false;

        for (int x = cellX - radiusTiles; x <= cellX + radiusTiles; x++)
            if (!columnHeights.TryGetValue(x, out int topY) || topY != centerTopY)
                return false;

        return true;
    }

    private void GenerateEnemies(float half)
    {
        int attempts = 0, spawned = 0;
        var enemyXList = new List<float>();

        while (spawned < enemyCount && attempts < enemyCount * 30)
        {
            attempts++;
            float x = RandomXOutsideSafeZone(half, 1f);

            bool nearSpike = false;
            foreach (float sx in spikeXList)
                if (Mathf.Abs(sx - x) < 2f) { nearSpike = true; break; }
            if (nearSpike) continue;

            bool nearEnemy = false;
            foreach (float ex in enemyXList)
                if (Mathf.Abs(ex - x) < 5f) { nearEnemy = true; break; }
            if (nearEnemy) continue;

            spawnedObjects.Add(Instantiate(enemyPrefab, new Vector3(x, GetWorldTopY(x) + GetGroundSpawnOffsetY(enemyPrefab, 0.5f), 0), Quaternion.identity));
            enemyXList.Add(x);
            spawned++;
        }
    }

    private float GetGroundSpawnOffsetY(GameObject prefab, float fallback)
    {
        if (!prefab) return fallback;
        if (prefab.TryGetComponent(out CapsuleCollider2D capsule))
            return (capsule.size.y * 0.5f - capsule.offset.y) * Mathf.Abs(prefab.transform.localScale.y);
        if (prefab.TryGetComponent(out BoxCollider2D box))
            return (box.size.y * 0.5f - box.offset.y) * Mathf.Abs(prefab.transform.localScale.y);
        return fallback;
    }

    private void GenerateCoins(float half)
    {
        var coinPositions = new List<Vector3>();

        for (int i = 0; i < coinCount; i++)
        {
            Vector3 pos;
            int attempts = 0;
            do
            {
                if (platformData.Count > 0 && Random.value < 0.5f)
                {
                    var (pPos, pW) = platformData[Random.Range(0, platformData.Count)];
                    pos = new Vector3(pPos.x + Random.Range(0f, pW), pPos.y + Random.Range(0.8f, 2f), 0);
                }
                else
                {
                    float x = Random.Range(-half + 1f, half - 1f);
                    pos = new Vector3(x, GetWorldTopY(x) + Random.Range(0.8f, 4f), 0);
                }
                attempts++;
            }
            while (attempts < 20 && coinPositions.Exists(c => Vector3.Distance(c, pos) < 1.5f));

            var coin = Instantiate(coinPrefab, pos, Quaternion.identity);
            var sync = coin.AddComponent<CoinSync>();
            sync.coinIndex = i;
            spawnedObjects.Add(coin);
            coinPositions.Add(pos);
        }
    }

    private float RandomXOutsideSafeZone(float half, float objHalfW)
    {
        return Random.value < 0.5f
            ? Random.Range(-half + objHalfW, -safeZoneHalfWidth - objHalfW)
            : Random.Range(safeZoneHalfWidth + objHalfW, half - objHalfW);
    }

    private bool OverlapsPlatform(float x, float y, float w)
    {
        foreach (var (ePos, eW) in platformData)
            if (Mathf.Abs(x - ePos.x) < (w + eW) * 0.5f + 0.5f &&
                Mathf.Abs(y - ePos.y) < 1.8f) return true;
        return false;
    }

    private void ClearGenerated()
    {
        groundTilemap.ClearAllTiles();
        foreach (var obj in spawnedObjects) if (obj) Destroy(obj);
        spawnedObjects.Clear();
    }

    public void RegenerateLevel()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int seed = Random.Range(0, 999999);
        photonView.RPC(nameof(RPC_GenerateLevel), RpcTarget.AllBuffered, seed);
    }
}
