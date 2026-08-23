using Godot;

public partial class SemicircleMesh : MeshInstance3D
{
	[Export] public float Radius = 6.0f;
	[Export] public float Width = 0.12f;
	[Export] public int Segments = 48;
	[Export] public float ArcDegrees = 180.0f;

	public override void _Ready()
	{
		Mesh = GenerateSemicircle();
	}

	private ArrayMesh GenerateSemicircle()
	{
		float arcRad = Mathf.DegToRad(ArcDegrees);
		float halfWidth = Width * 0.5f;
		int vertCount = Segments + 1;
		int totalVerts = vertCount * 2;

		var vertices = new Vector3[totalVerts];
		var normals = new Vector3[totalVerts];
		var uvs = new Vector2[totalVerts];
		var indices = new int[Segments * 6];

		Vector3 up = Vector3.Up;

		for (int i = 0; i <= Segments; i++)
		{
			float t = (float)i / Segments;
			float angle = -arcRad * 0.5f + t * arcRad;
			float cos = Mathf.Cos(angle);
			float sin = Mathf.Sin(angle);

			float outerR = Radius + halfWidth;
			float innerR = Radius - halfWidth;

			vertices[i] = new Vector3(cos * outerR, 0, sin * outerR);
			normals[i] = up;
			uvs[i] = new Vector2(t, 0);

			vertices[vertCount + i] = new Vector3(cos * innerR, 0, sin * innerR);
			normals[vertCount + i] = up;
			uvs[vertCount + i] = new Vector2(t, 1);
		}

		int idx = 0;
		for (int i = 0; i < Segments; i++)
		{
			int o0 = i;
			int o1 = i + 1;
			int i0 = vertCount + i;
			int i1 = vertCount + i + 1;

			indices[idx++] = o0;
			indices[idx++] = i0;
			indices[idx++] = o1;

			indices[idx++] = o1;
			indices[idx++] = i0;
			indices[idx++] = i1;
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}
}
