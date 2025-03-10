using UnityEngine;
using AccidentalNoise;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;

public class Generator : MonoBehaviour {

	// Debug information
	int chunkCount = 0;

	// Adjustable variables for Unity Inspector
	[SerializeField]
	int Side = 512;
	[SerializeField]
	int TerrainOctaves = 6;
	[SerializeField]
	double TerrainFrequency = 1.25;
	[SerializeField]
	float[] borders;
	Dictionary<(int, int), Chunk> chunks;

    // private variables
    ImplicitFractal HeightMap;
	MapData HeightData;
	Dictionary<(int, int), Tile[,]> Tiles;

	// Our texture output gameobject
	Dictionary<(int, int), MeshRenderer> HeightMapRenderer;
	bool canGenerate = true;

	void Start()
	{
        HeightMapRenderer = new Dictionary<(int, int), MeshRenderer>();
        SetChunks();
		GenerateMap(Random.Range(0, int.MaxValue));
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space) && canGenerate)
			RegenerateMap();
    }

    private async void RegenerateMap()
    {
		canGenerate = false;

        // Генерация сидов в основном потоке (иначе Unity выдаст ошибку)
        int seed = Random.Range(0, int.MaxValue);

        // Запускаем генерацию карты в фоновом потоке
        await Task.Run(() => GenerateMap(seed, true));

        // После завершения фоновой задачи обновляем объекты в основном потоке
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

	private void GenerateMap(int seed, bool parallel = false)
	{
        Initialize(seed);
        foreach ((int, int) coords in chunks.Keys)
            GenerateNewChunk(coords, parallel);
        Debug.Log("Map has generated");
    }

    private void GenerateNewChunk((int, int) coords, bool parallel = false)
	{
		GetData(HeightMap, ref HeightData, Side * chunks[coords].offsetX, Side * chunks[coords].offsetY);
		LoadTiles(Side * chunks[coords].offsetX, Side * chunks[coords].offsetY);
		if (!parallel)
			HeightMapRenderer[coords].materials[0].mainTexture = TextureGenerator.GetTexture(Side, Side, Tiles[coords]);

		chunkCount++;
    }

	private void Initialize(int seed)
	{
		// Initialize the HeightMap Generator
		HeightMap = new ImplicitFractal (FractalType.MULTI, 
		                               BasisType.SIMPLEX, 
		                               InterpolationType.QUINTIC, 
		                               TerrainOctaves, 
		                               TerrainFrequency,
                                       seed);
        HeightData = new MapData ();
		Tiles = new Dictionary<(int, int), Tile[,]> ();
    }
	
	// Extract data from a noise module
	private void GetData(ImplicitModuleBase module, ref MapData mapData, int offsetX = 0, int offsetY = 0)
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

				mapData.Data[(x,y)] = value;
			}
		}	
	}

	// Build a Tile array from our data
	private void LoadTiles(int offsetX = 0, int offsetY = 0)
	{
		Tiles[(offsetX / Side, offsetY / Side)] = new Tile[Side, Side];
		
		for (var x = offsetX; x < Side + offsetX; x++)
		{
			for (var y = offsetY; y < Side + offsetY; y++)
			{
				Tile t = new Tile();
				t.X = x;
				t.Y = y;
				
				float value = HeightData.Data[(x, y)];
				value = (value - HeightData.Min) / (HeightData.Max - HeightData.Min);
				
				t.HeightValue = value;
				
				//HeightMap Analyze
				if (value < borders[0])
					t.HeightType = HeightType.Dirt;
				else if (value < borders[1])
					t.HeightType = HeightType.DryGrass;
				else if (value < borders[2])
					t.HeightType = HeightType.LightGrass;
				else
					t.HeightType = HeightType.DarkGrass;
				
				Tiles[(offsetX / Side, offsetY / Side)][x - offsetX,y - offsetY] = t;
			}
		}
	}


}
