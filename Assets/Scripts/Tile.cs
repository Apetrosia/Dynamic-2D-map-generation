using UnityEngine;
using System;
using System.Collections.Generic;

public enum HeightType
{
    Desert = 1,
    Field = 2,
    Forest = 3,
    Snow = 4,
}

public enum HeatType
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

public enum BiomType
{
    Ice = 1,
    Tundra = 2,
    Forest = 3,
    Field = 4,
    Desert = 5
}

public class MyTile
{
	public HeightType HeightType;
    public HeatType HeatType;
    public HumidType HumidType;
    public BiomType BiomType;

	public float HeightValue { get; set; }
    public int X, Y;
		
	public MyTile()
	{
	}
}
