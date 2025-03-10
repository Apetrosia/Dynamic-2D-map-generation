using System.Collections.Generic;

public class MapData {

	public Dictionary<(float, float), float> Data;
	public float Min { get; set; }
	public float Max { get; set; }

	public MapData()
	{
		Data = new Dictionary<(float, float), float>();
		Min = float.MaxValue;
		Max = float.MinValue;
	}
}
