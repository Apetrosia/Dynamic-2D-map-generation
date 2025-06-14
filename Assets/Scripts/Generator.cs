using AccidentalNoise;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Generator : MonoBehaviour
{

    // Adjustable variables for Unity Inspector
    [SerializeField]
	int Side = 256;
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
    ImplicitFractal HeightMap;
    ImplicitFractal HeatMap;
    ImplicitFractal HumidMap;
    float[] HeightData;
    float[] HeatData;
    float[] HumidData;

    Dictionary<(int, int), MyTile[]> Tiles;
	BiomType[] biomTable;

	// Our texture output gameobject
	Dictionary<(int, int), MeshRenderer> MapRenderer;
	bool canGenerate = true;

	// For player monitoring
	const float chunkSize = 15f;
	//float newChuckBorder = 2f;
	(int, int) currentChunk = (0, 0);
	//int currentOffsetX = 0;
    //int currentOffsetY = 0;
    Transform playerPosition;
	int addBorder = 2;
	int deleteBorder = 4;

	[SerializeField]
	GameObject[] iceObjects;
    [SerializeField]
    GameObject[] tundraObjects;
    [SerializeField]
    GameObject[] forestObjects;
    [SerializeField]
    GameObject[] fieldObjects;
    [SerializeField]
    GameObject[] desertObjects;

    void Awake()
    {
        playerPosition = GameObject.FindWithTag("Player").transform;
        MapRenderer = new Dictionary<(int, int), MeshRenderer>();
        biomTable = new BiomType[6] { BiomType.Ice, BiomType.Field, BiomType.Desert, BiomType.Tundra, BiomType.Forest, BiomType.Field};
        HeightData = new float[Side * Side];
        HeatData = new float[Side * Side];
        HumidData = new float[Side * Side];
		TerrainOctaves = Mathf.Max(TerrainOctaves, 2);
    }

    void Start()
	{
		canGenerate = false;
        SetChunks();
		GenerateMap();
		foreach ((int, int) coords in chunks.Keys)
			GenerateObjects(coords);
		canGenerate = true;
    }

	private void Update()
	{
        currentChunk = ((int)(playerPosition.position.x + 7.5f) / (int)chunkSize - (playerPosition.position.x < -7.5f ? 1 : 0),
            (int)(playerPosition.position.y + 7.5f) / (int)chunkSize - (playerPosition.position.y < -7.5f ? 1 : 0));

        CheckPlayerPosition();
		if (Input.GetKeyDown(KeyCode.Space) && canGenerate)
			RegenerateMap();
    }

	private async void CheckPlayerPosition()
	{
		for (int i = -deleteBorder; i <= deleteBorder; i++)
			for (int j = -deleteBorder; j <= deleteBorder; j++)
			{
				if (Mathf.Abs(i) != deleteBorder && Mathf.Abs(j) != deleteBorder)
					continue;
				if (chunks.Keys.Contains((currentChunk.Item1 + i, currentChunk.Item2 + j)))
				{
					Destroy(chunks[(currentChunk.Item1 + i, currentChunk.Item2 + j)].gameObject);
					chunks.Remove((currentChunk.Item1 + i, currentChunk.Item2 + j));
				}	
			}

		List<(int, int)> chunksToAdd = new List<(int, int)>();

        for (int i = -addBorder; i <= addBorder; i++)
			for (int j = -addBorder; j <= addBorder; j++)
				if (CheckChunk((currentChunk.Item1 + i, currentChunk.Item2 + j)))
                    chunksToAdd.Add((currentChunk.Item1 + i, currentChunk.Item2 + j));

		if (chunksToAdd.Count == 0 || !canGenerate)
			return;

		canGenerate = false;

		Initialize(false);

		await Task.Run(() => GenerateMap(chunksToAdd));

		foreach ((int, int) coords in chunksToAdd)
		{
			MapRenderer[coords].materials[0].mainTexture =
				TextureGenerator.GetTexture(Side, Tiles[coords]);
			GenerateObjects(coords);
		}

        canGenerate = true;
    }

	private bool CheckChunk((int, int) coords)
	{
		if (chunks.Keys.Contains(coords))
		{
			if (chunks[coords].isGenerated)
				return false;
			return true;
		}

		GameObject chunkObj = Instantiate(chunkPrefab,
			new Vector3(coords.Item1 * chunkSize, coords.Item2 * chunkSize, 0),
			Quaternion.identity);
		chunkObj.transform.SetParent(transform);

		chunks[coords] = chunkObj.GetComponent<Chunk>();
		chunks[coords].offsetX = coords.Item1;
		chunks[coords].offsetY = coords.Item2;

		MapRenderer[coords] = chunkObj.GetComponent<MeshRenderer>();
		MapRenderer[coords].material.SetFloat("_Glossiness", 0);

		return true;
    }

    private async void RegenerateMap()
    {
		canGenerate = false;

        seedHeight = Random.Range(0, int.MaxValue);
        seedHeat = Random.Range(0, int.MaxValue);
        seedHumid = Random.Range(0, int.MaxValue);

        await Task.Run(() => GenerateMap(true));

        foreach ((int, int) coords in chunks.Keys)
			GenerateObjects(coords);

		ApplyMap();

        canGenerate = true;
    }

    private void ApplyMap()
    {
		foreach ((int, int) coords in chunks.Keys)
			if (MapRenderer.ContainsKey(coords))
				MapRenderer[coords].materials[0].mainTexture =
					TextureGenerator.GetTexture(Side, Tiles[coords]);
    }

    private void SetChunks()
	{
		chunks = new Dictionary<(int, int), Chunk> ();

		for (int i = 0; i < transform.childCount; i++)
		{
			int x = transform.GetChild(i).GetComponent<Chunk>().offsetX;
			int y = transform.GetChild(i).GetComponent<Chunk>().offsetY;

			chunks[(x, y)] = transform.GetChild(i).GetComponent<Chunk>();
            MapRenderer[(x, y)] = transform.GetChild(i).GetComponent<MeshRenderer>();
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
    }

    private void GenerateMap(List<(int, int)> chunksToAdd)
    {
        foreach ((int, int) coords in chunksToAdd)
            GenerateNewChunk(coords, true);
    }

    private void GenerateNewChunk((int, int) coords, bool parallel = false)
	{
        chunks[coords].isGenerated = true;
        GetData(Side * chunks[coords].offsetX, Side * chunks[coords].offsetY);
		LoadTiles(Side * chunks[coords].offsetX, Side * chunks[coords].offsetY);
		if (!parallel)
				MapRenderer[coords].materials[0].mainTexture = TextureGenerator.GetTexture(Side, Tiles[coords]);
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
										   TerrainOctaves - 1,
										   TerrainFrequency - 0.1,
										   seedHeat);
			HumidMap = new ImplicitFractal(FractalType.MULTI,
										   BasisType.SIMPLEX,
										   InterpolationType.QUINTIC,
										   TerrainOctaves - 1,
										   TerrainFrequency - 0.1,
										   seedHumid);
		}

        Tiles = new Dictionary<(int, int), MyTile[]> ();
    }

    private void GenerateObjects((int, int) coords)
    {
        canGenerate = false;

		System.Random rand = new System.Random((int)((coords.Item1 + coords.Item2) *
			(chunks[coords].transform.position.x + chunks[coords].transform.position.y) * seedHeight) % 100);

		int count = rand.Next(5, 10);

		for (int i = 0; i < count; i++)
		{
			(int, int) position = (rand.Next(0, Side), rand.Next(0, Side));
			if (Tiles[coords][position.Item1 + position.Item2 * Side].haveObject)
				continue;
			switch (Tiles[coords][position.Item1 + position.Item2 * Side].BiomType)
			{
				//new Vector3(position.Item1 * chunkSize, position.Item2 * chunkSize, 0),
				case BiomType.Ice:
					Instantiate(iceObjects[rand.Next(0, iceObjects.Length)],
						new Vector3((position.Item1 + chunks[coords].offsetX * Side) * chunkSize / Side - 7.5f,
						(position.Item2 + chunks[coords].offsetY * Side) * chunkSize / Side - 7.5f, 0),
						Quaternion.identity).transform.SetParent(chunks[coords].transform, true);
                    Tiles[coords][position.Item1 + position.Item2 * Side].haveObject = true;
					break;
				case BiomType.Tundra:
                    Instantiate(tundraObjects[rand.Next(0, tundraObjects.Length)],
                        new Vector3((position.Item1 + chunks[coords].offsetX * Side) * chunkSize / Side - 7.5f,
                        (position.Item2 + chunks[coords].offsetY * Side) * chunkSize / Side - 7.5f, 0),
                        Quaternion.identity).transform.SetParent(chunks[coords].transform, true);
                    Tiles[coords][position.Item1 + position.Item2 * Side].haveObject = true;
                    break;
				case BiomType.Forest:
                    Instantiate(forestObjects[rand.Next(0, forestObjects.Length)],
                        new Vector3((position.Item1 + chunks[coords].offsetX * Side) * chunkSize / Side - 7.5f,
                        (position.Item2 + chunks[coords].offsetY * Side) * chunkSize / Side - 7.5f, 0),
                        Quaternion.identity).transform.SetParent(chunks[coords].transform, true);
                    Tiles[coords][position.Item1 + position.Item2 * Side].haveObject = true;
                    break;
				case BiomType.Field:
                    Instantiate(fieldObjects[rand.Next(0, fieldObjects.Length)],
                        new Vector3((position.Item1 + chunks[coords].offsetX * Side) * chunkSize / Side - 7.5f,
                        (position.Item2 + chunks[coords].offsetY * Side) * chunkSize / Side - 7.5f, 0),
                        Quaternion.identity).transform.SetParent(chunks[coords].transform, true);
                    Tiles[coords][position.Item1 + position.Item2 * Side].haveObject = true;
                    break;
				case BiomType.Desert:
                    Instantiate(desertObjects[rand.Next(0, desertObjects.Length)],
                        new Vector3((position.Item1 + chunks[coords].offsetX * Side) * chunkSize / Side - 7.5f,
                        (position.Item2 + chunks[coords].offsetY * Side) * chunkSize / Side - 7.5f, 0),
                        Quaternion.identity).transform.SetParent(chunks[coords].transform, true);
                    Tiles[coords][position.Item1 + position.Item2 * Side].haveObject = true;
                    break;
			}
		}

        canGenerate = true;
    }

    // Extract data from a noise module
    private void GetData(int offsetX = 0, int offsetY = 0)
	{
		// loop through each x,y point - get height value
		for (var x = offsetX; x < Side + offsetX; x++)
		{
			for (var y = offsetY; y < Side + offsetY; y++)
			{
				//Sample the noise at smaller intervals
				float x1 = x / (float)Side;
				float y1 = y / (float)Side;

				HeightData[x - offsetX + (y - offsetY) * Side] = ((float)HeightMap.Get(x1, y1) + 2f) / 4f;
				HeatData[x - offsetX + (y - offsetY) * Side] = ((float)HeatMap.Get(x1, y1) + 2f) / 4f;
				HumidData[x - offsetX + (y - offsetY) * Side] = ((float)HumidMap.Get(x1, y1) + 2f) / 4f;
			}
		}	
	}

	// Build a Tile array from our data
	private void LoadTiles(int offsetX = 0, int offsetY = 0)
	{
		Tiles[(offsetX / Side, offsetY / Side)] = new MyTile[Side * Side];
		
		for (var x = offsetX; x < Side + offsetX; x++)
		{
			for (var y = offsetY; y < Side + offsetY; y++)
			{
                MyTile t = new MyTile();
				t.haveObject = false;
				t.X = x;
				t.Y = y;
				
				float height = HeightData[x - offsetX + (y - offsetY) * Side];
				
				t.HeightValue = height;
				
				if (height < borders[0])
					t.HeightType = HeightType.Desert;
				else if (height < borders[1])
					t.HeightType = HeightType.Field;
				else if (height < borders[2])
					t.HeightType = HeightType.Forest;
				else
					t.HeightType = HeightType.Snow;

                float temp = HeatData[x - offsetX + (y - offsetY) * Side];

                if (t.HeightType == HeightType.Desert)
                    temp -= 0.01f * t.HeightValue;
                else if (t.HeightType == HeightType.Field)
                    temp -= 0.02f * t.HeightValue;
                else if (t.HeightType == HeightType.Forest)
                    temp -= 0.03f * t.HeightValue;
                else if (t.HeightType == HeightType.Snow)
                    temp -= 0.04f * t.HeightValue;

				if (temp < 0.3)
					t.HeatType = HeatType.Low;
				else if (temp < 0.4)
					t.HeatType = HeatType.Medium;
				else
					t.HeatType = HeatType.Hight;


				float h = HumidData[x - offsetX + (y - offsetY) * Side];

                if (h < 0.5f)
					t.HumidType = HumidType.Low;
				else
					t.HumidType = HumidType.Hight;

				t.BiomType = biomTable[((int)t.HumidType - 1) * 3 + (int)t.HeatType - 1];

				Tiles[(offsetX / Side, offsetY / Side)][x - offsetX + (y - offsetY) * Side] = t;
			}
		}
	}
}
