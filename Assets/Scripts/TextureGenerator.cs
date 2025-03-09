using UnityEngine;

public static class TextureGenerator {		

	// Height Map Colors
	private static Color DirtColor = new Color(235/225f, 178/225f, 45/225f, 1);
	private static Color DryGrassColor = new Color(1, 242/255f, 184/255f, 1);
	private static Color LightGrass = new Color(95/255f, 161/255f, 63/255f, 1);
	private static Color DarkGrassColor = new Color(34/255f, 115/255f, 20/ 255f, 1);

	public static Texture2D GetTexture(int width, int height, Tile[,] tiles)
	{
		var texture = new Texture2D(width, height);
		var pixels = new Color[width * height];

		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
			{
				switch (tiles[x,y].HeightType)
				{
				case HeightType.Dirt:
					pixels[x + y * width] = DirtColor;
					break;
				case HeightType.DryGrass:
					pixels[x + y * width] = DryGrassColor;
					break;
				case HeightType.LightGrass:
					pixels[x + y * width] = LightGrass;
					break;
				case HeightType.DarkGrass:
					pixels[x + y * width] = DarkGrassColor;
					break;
				}
			}
		}
		
		texture.SetPixels(pixels);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.Apply();
		return texture;
	}
	
}
