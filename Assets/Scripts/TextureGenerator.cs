using UnityEngine;

public static class TextureGenerator {		

	// Height Map Colors
	private static Color DesertColor = new Color(255 / 255f, 255 / 255f, 143 / 255f, 1); //new Color(235/255f, 178/255f, 45/255f, 1);
    private static Color FieldColor = new Color(180 / 255f, 240 / 255f, 158 / 255f, 1); //new Color(1, 242/255f, 184/255f, 1);
    private static Color ForestColor = new Color(29 / 255f, 84 / 255f, 33 / 255f, 1); //new Color(95/255f, 161/255f, 63/255f, 1);
    private static Color SnowColor = new Color(174 / 255f, 230 / 255f, 226 / 255f, 1); //new Color(34/255f, 115/255f, 20/ 255f, 1);


    public static Texture2D GetTexture(int width, int height, MyTile[,] tiles)
	{
		var texture = new Texture2D(width, height);
		var pixels = new Color[width * height];

		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
			{
				switch (tiles[x,y].HeightType)
				{
				case HeightType.Desert:
					pixels[x + y * width] = DesertColor;
					break;
				case HeightType.Field:
					pixels[x + y * width] = FieldColor;
					break;
				case HeightType.Forest:
					pixels[x + y * width] = ForestColor;
					break;
				case HeightType.Snow:
					pixels[x + y * width] = SnowColor;
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
