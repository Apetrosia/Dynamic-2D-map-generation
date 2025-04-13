using UnityEngine;
using AccidentalNoise;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;
using Unity.VisualScripting;

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
	Dictionary<(int, int), Chunk> chunks;
	[SerializeField]
	GameObject chunkPrefab;

    // private variables
    ImplicitFractal HeightMap;
	MapData HeightData;
	Dictionary<(int, int), MyTile[,]> Tiles;

	// Our texture output gameobject
	Dictionary<(int, int), MeshRenderer> HeightMapRenderer;
	bool canGenerate = true;

	// For player monitoring
	const float chunkSize = 15f;
	float newChuckBorder = 2f;
	(int, int) currentChunk = (0, 0);
	int currentOffsetX = 0;
    int currentOffsetY = 0;
    Transform playerPosition;

	void Start()
	{
		playerPosition = GameObject.FindWithTag("Player").transform;
        HeightMapRenderer = new Dictionary<(int, int), MeshRenderer>();
        SetChunks();
		GenerateMap(Random.Range(0, int.MaxValue));
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
		/*
        List<Task> chunkTasks = new List<Task>();

		for (int i = -1; i <= 1; i++)
			for (int j = -1; j <= 1; j++)
				if (i != j || i != 0)
					chunkTasks.Add(ChechChunk((currentChunk.Item1 + i, currentChunk.Item2 + j)));

        ChechChunk(currentChunk);

		for (int i = -1; i <= 1; i++)
			for (int j = -1; j <= 1; j++)
				if (i != j || i != 0)
					HeightMapRenderer[(currentChunk.Item1 + i, currentChunk.Item2 + j)].materials[0].mainTexture =
						TextureGenerator.GetTexture(Side, Side, Tiles[(currentChunk.Item1 + i, currentChunk.Item2 + j)]);
		*/
    }

	private void ChechChunk((int, int) coords)
	{
		if (chunks.Keys.Contains(coords))
			return;

		GameObject chunkObj = Instantiate(chunkPrefab, new Vector3(coords.Item1 * chunkSize, coords.Item2 * chunkSize, 0), Quaternion.identity);
		chunkObj.transform.SetParent(transform);

		chunks[coords] = chunkObj.GetComponent<Chunk>();
		chunks[coords].offsetX = coords.Item1;
		chunks[coords].offsetY = coords.Item2;

		HeightMapRenderer[coords] = chunkObj.GetComponent<MeshRenderer>();
        HeightMapRenderer[coords].material.SetFloat("_Glossiness", 0);
        AddNewChunk(coords);
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
    }

    private void AddNewChunk((int, int) coords)
	{
		//await Task.Run(() => GenerateNewChunk(coords, true));
		GenerateNewChunk(coords, true);

        //HeightMapRenderer[coords].materials[0].mainTexture = TextureGenerator.GetTexture(Side, Side, Tiles[coords]);
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
		Tiles = new Dictionary<(int, int), MyTile[,]> ();
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
		Tiles[(offsetX / Side, offsetY / Side)] = new MyTile[Side, Side];
		
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
