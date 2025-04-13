using UnityEngine;
using System;
using System.Collections.Generic;

public enum HeightType
{
    Dirt = 1,
    DryGrass = 2,
    LightGrass = 3,
    DarkGrass = 4,
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
