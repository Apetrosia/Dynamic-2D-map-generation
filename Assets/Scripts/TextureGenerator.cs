using UnityEngine;

public static class TextureGenerator {

    // Height Map Colors
    private static Color IceColor = new Color(227 / 255f, 255 / 255f, 252 / 255f, 1);
    private static Color TundraColor = new Color(23 / 255f, 38 / 255f, 31 / 255f, 1);
    private static Color ForestColor = new Color(20 / 255f, 82 / 255f, 11 / 255f, 1);
    private static Color FieldColor = new Color(90 / 255f, 179 / 255f, 27 / 255f, 1);
    private static Color DesertColor = new Color(173 / 255f, 173 / 255f, 97 / 255f, 1);


    public static Texture2D GetTexture(int side, MyTile[] tiles)
	{
		var texture = new Texture2D(side, side);
		var pixels = new Color[side * side];

		for (var x = 0; x < side; x++)
		{
			for (var y = 0; y < side; y++)
			{
				switch (tiles[x + y * side].BiomType)
				{
					case BiomType.Ice:
						pixels[x + y * side] = IceColor;
						break;
					case BiomType.Tundra:
						pixels[x + y * side] = TundraColor;
						break;
					case BiomType.Forest:
                        pixels[x + y * side] = ForestColor;
                        break;
					case BiomType.Field:
						pixels[x + y * side] = FieldColor;
						break;
					case BiomType.Desert:
						pixels[x + y * side] = DesertColor;
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
