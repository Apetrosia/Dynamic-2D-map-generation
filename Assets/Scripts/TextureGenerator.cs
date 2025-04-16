using UnityEngine;

public static class TextureGenerator {

    // Height Map Colors
    private static Color IceColor = new Color(227 / 255f, 255 / 255f, 252 / 255f, 1);
    private static Color TundraColor = new Color(23 / 255f, 38 / 255f, 31 / 255f, 1);
    private static Color ForestColor = new Color(20 / 255f, 82 / 255f, 11 / 255f, 1);
    private static Color FieldColor = new Color(90 / 255f, 179 / 255f, 27 / 255f, 1);
    private static Color DesertColor = new Color(173 / 255f, 173 / 255f, 97 / 255f, 1);


    public static Texture2D GetTexture(int width, int height, MyTile[,] tiles)
	{
		var texture = new Texture2D(width, height);
		var pixels = new Color[width * height];

		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
			{
				switch (tiles[x,y].BiomType)
				{
					case BiomType.Ice:
						pixels[x + y * width] = IceColor;
						break;
					case BiomType.Tundra:
						pixels[x + y * width] = TundraColor;
						break;
					case BiomType.Forest:
                        pixels[x + y * width] = ForestColor;
                        break;
					case BiomType.Field:
						pixels[x + y * width] = FieldColor;
						break;
					case BiomType.Desert:
						pixels[x + y * width] = DesertColor;
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
