using UnityEngine;
using AccidentalNoise;
using System.Collections.Generic;

public class Generator : MonoBehaviour {

	// Adjustable variables for Unity Inspector
	[SerializeField]
	int Side = 512;
	[SerializeField]
	int TerrainOctaves = 6;
	[SerializeField]
	double TerrainFrequency = 1.25;
	[SerializeField]
	float DeepWater = 0.2f;
	[SerializeField]
	float ShallowWater = 0.4f;	
	[SerializeField]
	float Sand = 0.5f;
	[SerializeField]
	float Grass = 0.7f;
	[SerializeField]
	float Forest = 0.8f;
	[SerializeField]
	float Rock = 0.9f;

	// private variables
	ImplicitFractal HeightMap;
	MapData HeightData;
	List<Tile[,]> Tiles;

	// Our texture output gameobject
	List<MeshRenderer> HeightMapRenderer;

	void Start()
	{
		GenerateMap();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
            GenerateMap();
    }

    private void GenerateMap()
	{
        Initialize();

        for (int i = 0; i < transform.childCount; i++)
        {
            HeightMapRenderer.Add(transform.GetChild(i).GetComponent<MeshRenderer>());

            GetData(HeightMap, ref HeightData, Side * i, 0);
            LoadTiles(Side * i, 0);
            HeightMapRenderer[i].materials[0].mainTexture = TextureGenerator.GetTexture(Side, Side, Tiles[i]);
        }
        Debug.Log("Generated");
    }

	private void Initialize()
	{
		// Initialize the HeightMap Generator
		HeightMap = new ImplicitFractal (FractalType.MULTI, 
		                               BasisType.SIMPLEX, 
		                               InterpolationType.QUINTIC, 
		                               TerrainOctaves, 
		                               TerrainFrequency, 
		                               Random.Range (0, int.MaxValue));
        HeightMapRenderer = new List<MeshRenderer>();
        HeightData = new MapData (Side * 2, Side);
		Tiles = new List<Tile[,]> ();
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

				mapData.Data[x,y] = value;
			}
		}	
	}

	// Build a Tile array from our data
	private void LoadTiles(int offsetX = 0, int offsetY = 0)
	{
		Tiles.Add(new Tile[Side, Side]);
		int index = Tiles.Count - 1;
		
		for (var x = offsetX; x < Side + offsetX; x++)
		{
			for (var y = offsetY; y < Side + offsetY; y++)
			{
				Tile t = new Tile();
				t.X = x;
				t.Y = y;
				
				float value = HeightData.Data[x, y];
				value = (value - HeightData.Min) / (HeightData.Max - HeightData.Min);
				
				t.HeightValue = value;
				
				//HeightMap Analyze
				if (value < DeepWater)  {
					t.HeightType = HeightType.DeepWater;
				}
				else if (value < ShallowWater)  {
					t.HeightType = HeightType.ShallowWater;
				}
				else if (value < Sand) {
					t.HeightType = HeightType.Sand;
				}
				else if (value < Grass) {
					t.HeightType = HeightType.Grass;
				}
				else if (value < Forest) {
					t.HeightType = HeightType.Forest;
				}
				else if (value < Rock) {
					t.HeightType = HeightType.Rock;
				}
				else  {
					t.HeightType = HeightType.Snow;
				}
				
				Tiles[index][x - offsetX,y - offsetY] = t;
			}
		}
	}


}
