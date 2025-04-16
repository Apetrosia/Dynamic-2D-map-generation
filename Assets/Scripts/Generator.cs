using UnityEngine;
using AccidentalNoise;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Profiling.Memory.Experimental;
using JetBrains.Annotations;

public class Generator : MonoBehaviour
{
	// Adjustable variables for Unity Inspector
	[SerializeField]
	int Side = 512;
	[SerializeField]
	int TerrainOctaves = 6;
	[SerializeField]
	double TerrainFrequency = 1.25;
	[SerializeField]
	float[] borders;
	int seedHeight;
    int seedHeat;
    int seedHumid;
    Dictionary<(int, int), Chunk> chunks;
	[SerializeField]
	GameObject chunkPrefab;

    // private variables
    ImplicitFractal HeatMap;
    ImplicitFractal HumidMap;
    ImplicitFractal HeightMap;
	MapData HeightData;

	Dictionary<(int, int), MyTile[,]> Tiles;

	// Our texture output gameobject
	Dictionary<(int, int), MeshRenderer> HeightMapRenderer;
	bool canGenerate = true;

	// For player monitoring
	const float chunkSize = 15f;
	//float newChuckBorder = 2f;
	(int, int) currentChunk = (0, 0);
	//int currentOffsetX = 0;
    //int currentOffsetY = 0;
    Transform playerPosition;

    private readonly object _tilesLock = new object();
    private readonly object _mapsLock = new object();
    private readonly object _texturesLock = new object();

    void Start()
	{
        playerPosition = GameObject.FindWithTag("Player").transform;
        HeightMapRenderer = new Dictionary<(int, int), MeshRenderer>();
        SetChunks();
		GenerateMap();
	}

	private void Update()
	{
        currentChunk = ((int)(playerPosition.position.x + 7.5f) / 15 - (playerPosition.position.x < -7.5f ? 1 : 0),
            (int)(playerPosition.position.y + 7.5f) / 15 - (playerPosition.position.y < -7.5f ? 1 : 0));

        CheckPlayerPosition();
		if (Input.GetKeyDown(KeyCode.Space) && canGenerate)
			RegenerateMap();
    }

	private async void CheckPlayerPosition()
	{
        var tasks = new List<Task>();
        List<(int, int)> chunksToAdd = new List<(int, int)>();

        for (int i = -1; i <= 1; i++)
			for (int j = -1; j <= 1; j++)
				if (CheckChunk((currentChunk.Item1 + i, currentChunk.Item2 + j)))
                    chunksToAdd.Add((currentChunk.Item1 + i, currentChunk.Item2 + j));

		if (chunksToAdd.Count == 0 || !canGenerate)
			return;

		canGenerate = false;

		Initialize(false);

		await Task.Run(() => GenerateMap(chunksToAdd));

        foreach ((int, int) coords in chunksToAdd)
					HeightMapRenderer[coords].materials[0].mainTexture =
						TextureGenerator.GetTexture(Side, Side, Tiles[coords]);
		chunksToAdd.Clear();
		canGenerate = true;
    }

	private bool CheckChunk((int, int) coords)
	{
		if (chunks.Keys.Contains(coords))
		{
			if (chunks[coords].isGenerated)
				return false;
			else
				return true;
		}

		GameObject chunkObj = Instantiate(chunkPrefab, new Vector3(coords.Item1 * chunkSize, coords.Item2 * chunkSize, 0), Quaternion.identity);
		chunkObj.transform.SetParent(transform);

		lock (_texturesLock)
		{
			chunks[coords] = chunkObj.GetComponent<Chunk>();
			chunks[coords].offsetX = coords.Item1;
			chunks[coords].offsetY = coords.Item2;

			HeightMapRenderer[coords] = chunkObj.GetComponent<MeshRenderer>();
			HeightMapRenderer[coords].material.SetFloat("_Glossiness", 0);
		}

		return true;
    }

    private async void RegenerateMap()
    {
		canGenerate = false;

        seedHeight = Random.Range(0, int.MaxValue);
        seedHeat = Random.Range(0, int.MaxValue);
        seedHumid = Random.Range(0, int.MaxValue);

        await Task.Run(() => GenerateMap(true));

        ApplyMap();

		canGenerate = true;
    }

    private void ApplyMap()
    {
		foreach ((int, int) coords in chunks.Keys)
			if (HeightMapRenderer.ContainsKey(coords))
				HeightMapRenderer[coords].materials[0].mainTexture =
					TextureGenerator.GetTexture(Side, Side, Tiles[coords]);
    }

    private void SetChunks()
	{
		chunks = new Dictionary<(int, int), Chunk> ();

		for (int i = 0; i < transform.childCount; i++)
		{
			int x = transform.GetChild(i).GetComponent<Chunk>().offsetX;
			int y = transform.GetChild(i).GetComponent<Chunk>().offsetY;

			chunks[(x, y)] = transform.GetChild(i).GetComponent<Chunk>();
            HeightMapRenderer[(x, y)] = transform.GetChild(i).GetComponent<MeshRenderer>();
        }
    }

	private void GenerateMap(bool parallel = false)
	{
		if (!parallel)
		{
            seedHeight = Random.Range(0, int.MaxValue);
            seedHeat = Random.Range(0, int.MaxValue);
            seedHumid = Random.Range(0, int.MaxValue);
        }
        Initialize();
        foreach ((int, int) coords in chunks.Keys)
            GenerateNewChunk(coords, parallel);
        Debug.Log("Map has generated");
    }

    private void GenerateMap(List<(int, int)> chunksToAdd)
    {
        foreach ((int, int) coords in chunksToAdd)
            GenerateNewChunk(coords, true);
        Debug.Log("New chunks have generated");
    }

    private void GenerateNewChunk((int, int) coords, bool parallel = false)
	{
		GetData(ref HeightData, Side * chunks[coords].offsetX, Side * chunks[coords].offsetY);
		LoadTiles(Side * chunks[coords].offsetX, Side * chunks[coords].offsetY);
		if (!parallel)
				HeightMapRenderer[coords].materials[0].mainTexture = TextureGenerator.GetTexture(Side, Side, Tiles[coords]);
    }


    private void Initialize(bool newSeed = true)
	{
		if (newSeed)
		{
			// Initialize the HeightMap Generator
			HeightMap = new ImplicitFractal(FractalType.MULTI,
										   BasisType.SIMPLEX,
										   InterpolationType.QUINTIC,
										   TerrainOctaves,
										   TerrainFrequency,
										   seedHeight);
			HeatMap = new ImplicitFractal(FractalType.MULTI,
										   BasisType.SIMPLEX,
										   InterpolationType.QUINTIC,
										   TerrainOctaves,
										   TerrainFrequency,
										   seedHeat);
			HumidMap = new ImplicitFractal(FractalType.MULTI,
										   BasisType.SIMPLEX,
										   InterpolationType.QUINTIC,
										   TerrainOctaves,
										   TerrainFrequency,
										   seedHumid);
		}

        HeightData = new MapData ();
		Tiles = new Dictionary<(int, int), MyTile[,]> ();
    }
	
	// Extract data from a noise module
	private void GetData(ref MapData mapData, int offsetX = 0, int offsetY = 0)
	{
		// loop through each x,y point - get height value
		for (var x = offsetX; x < Side + offsetX; x++)
		{
			for (var y = offsetY; y < Side + offsetY; y++)
			{
				//Sample the noise at smaller intervals
				float x1 = x / (float)Side;
				float y1 = y / (float)Side;

				float value = (float)HeightMap.Get (x1, y1);

				//keep track of the max and min values found
				if (value > mapData.Max) mapData.Max = value;
				if (value < mapData.Min) mapData.Min = value;

				lock (mapData)
				{
					mapData.Data[(x, y)] = value;
				}
			}
		}	
	}

	// Build a Tile array from our data
	private void LoadTiles(int offsetX = 0, int offsetY = 0)
	{
		lock (_tilesLock)
		{
			Tiles[(offsetX / Side, offsetY / Side)] = new MyTile[Side, Side];
		}
		
		for (var x = offsetX; x < Side + offsetX; x++)
		{
			for (var y = offsetY; y < Side + offsetY; y++)
			{
                MyTile t = new MyTile();
				t.X = x;
				t.Y = y;
				
				float value = HeightData.Data[(x, y)];
				value = (value - HeightData.Min) / (HeightData.Max - HeightData.Min);
				
				t.HeightValue = value;
				
				//HeightMap Analyze
				if (value < borders[0])
					t.HeightType = HeightType.Desert;
				else if (value < borders[1])
					t.HeightType = HeightType.Field;
				else if (value < borders[2])
					t.HeightType = HeightType.Forest;
				else
					t.HeightType = HeightType.Snow;

                // TODO add heatmap
                float term = (float)HeatMap.Get(x, y);
                if (t.HeightType == HeightType.Desert)
                    HeightData.Data[(x, y)] = term - 0.1f * t.HeightValue;
                else if (t.HeightType == HeightType.Field)
                    HeightData.Data[(x, y)] = term - 0.2f * t.HeightValue;
                else if (t.HeightType == HeightType.Forest)
                    HeightData.Data[(x, y)] = term - 0.3f * t.HeightValue;
                else if (t.HeightType == HeightType.Snow)
                    HeightData.Data[(x, y)] = term - 0.4f * t.HeightValue;

                switch ((float)HumidMap.Get(x, y))
				{
					case 1:
						break;
				}

				lock (_tilesLock)
				{
					Tiles[(offsetX / Side, offsetY / Side)][x - offsetX, y - offsetY] = t;
				}
			}
		}
	}
}
