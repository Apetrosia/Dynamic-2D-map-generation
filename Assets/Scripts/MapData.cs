using System.Collections.Generic;

public class MapData {

	public float[,] Data;
	public float Min { get; set; }
	public float Max { get; set; }

	public MapData(int Side)
	{
		Data = new float[Side, Side];
		Min = float.MaxValue;
		Max = float.MinValue;
	}
}
