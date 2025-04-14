using System.Collections.Generic;

public enum WarmType
{
    Low = 1,
    Medium = 2,
    Hight = 3,
}

public enum HumidType
{
    Low = 1,
    Hight = 2,
}

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
