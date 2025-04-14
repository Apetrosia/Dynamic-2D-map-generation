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
public class MyTile
{
	public HeightType HeightType;
    public WarmType WarmType;
    public HumidType HumidType;
	public float HeightValue { get; set; }
	public int X, Y;
		
	public MyTile()
	{
	}
}
