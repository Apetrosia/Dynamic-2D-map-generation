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

public class MyTile
{
	public HeightType HeightType;
	public float HeightValue { get; set; }
	public int X, Y;
		
	public MyTile()
	{
	}
}
