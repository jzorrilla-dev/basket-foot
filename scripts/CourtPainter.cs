using Godot;

public partial class CourtPainter : Node3D
{
	[Export] public float CourtWidth = 20.0f;
	[Export] public float CourtLength = 40.0f;
	[Export] public float LineWidth = 0.12f;
	[Export] public float SurfaceY = 0.012f;
	[Export] public float HoopZ = 20.0f;
	[Export] public float ThreePointRadius = 6.75f;
	[Export] public float CenterCircleRadius = 3.0f;
	[Export] public float PaintWidth = 4.9f;
	[Export] public float PaintLength = 5.8f;
	[Export] public float FreeThrowRadius = 1.8f;

	private StandardMaterial3D _floorMat;
	private StandardMaterial3D _plankDarkMat;
	private StandardMaterial3D _paintMat;
	private StandardMaterial3D _restrictedMat;
	private StandardMaterial3D _lineMat;

	public override void _Ready()
	{
		ClearGeneratedChildren();
		CreateMaterials();
		BuildCourt();
	}

	private void ClearGeneratedChildren()
	{
		foreach (Node child in GetChildren())
		{
			child.QueueFree();
		}
	}

	private void CreateMaterials()
	{
		_floorMat = MakeMaterial(new Color(0.64f, 0.43f, 0.22f), 0.48f);
		_plankDarkMat = MakeMaterial(new Color(0.53f, 0.34f, 0.17f), 0.55f);
		_paintMat = MakeMaterial(new Color(0.12f, 0.36f, 0.58f), 0.52f);
		_restrictedMat = MakeMaterial(new Color(0.82f, 0.26f, 0.12f), 0.58f);
		_lineMat = MakeMaterial(new Color(0.97f, 0.95f, 0.88f), 0.35f);
	}

	private static StandardMaterial3D MakeMaterial(Color color, float roughness)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			Roughness = roughness,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};
	}

	private void BuildCourt()
	{
		float halfW = CourtWidth * 0.5f;
		float halfL = CourtLength * 0.5f;

		AddRect("Floor", Vector3.Zero, CourtWidth, CourtLength, _floorMat, SurfaceY - 0.004f);
		AddPlanks(halfW, halfL);

		AddHalfCourtPaint(-1, halfW, halfL);
		AddHalfCourtPaint(1, halfW, halfL);

		AddLine("SidelineLeft", new Vector3(-halfW, 0, 0), LineWidth, CourtLength, _lineMat, SurfaceY + 0.004f);
		AddLine("SidelineRight", new Vector3(halfW, 0, 0), LineWidth, CourtLength, _lineMat, SurfaceY + 0.004f);
		AddLine("BaselineSouth", new Vector3(0, 0, -halfL), CourtWidth, LineWidth, _lineMat, SurfaceY + 0.004f);
		AddLine("BaselineNorth", new Vector3(0, 0, halfL), CourtWidth, LineWidth, _lineMat, SurfaceY + 0.004f);
		AddLine("HalfCourt", Vector3.Zero, CourtWidth, LineWidth, _lineMat, SurfaceY + 0.006f);
		AddRing("CenterCircle", Vector3.Zero, CenterCircleRadius, LineWidth, 96, _lineMat, SurfaceY + 0.008f);

		AddHoopLines(-1, halfL);
		AddHoopLines(1, halfL);
	}

	private void AddPlanks(float halfW, float halfL)
	{
		const float plankWidth = 1.0f;
		int count = Mathf.CeilToInt(CourtWidth / plankWidth);
		for (int i = 0; i < count; i++)
		{
			if (i % 2 == 0)
				continue;

			float x = -halfW + plankWidth * (i + 0.5f);
			AddRect($"Plank{i:00}", new Vector3(x, 0, 0), plankWidth * 0.92f, halfL * 2.0f, _plankDarkMat, SurfaceY - 0.002f);
		}
	}

	private void AddHalfCourtPaint(int side, float halfW, float halfL)
	{
		float baselineZ = side * halfL;
		float paintCenterZ = baselineZ - side * PaintLength * 0.5f;
		float freeThrowZ = baselineZ - side * PaintLength;

		AddRect($"Paint{SideName(side)}", new Vector3(0, 0, paintCenterZ), PaintWidth, PaintLength, _paintMat, SurfaceY);
		AddArcFill($"Restricted{SideName(side)}", new Vector3(0, 0, side * HoopZ), 1.25f, side, _restrictedMat, SurfaceY + 0.001f);
		AddLine($"PaintLeft{SideName(side)}", new Vector3(-PaintWidth * 0.5f, 0, paintCenterZ), LineWidth, PaintLength, _lineMat, SurfaceY + 0.01f);
		AddLine($"PaintRight{SideName(side)}", new Vector3(PaintWidth * 0.5f, 0, paintCenterZ), LineWidth, PaintLength, _lineMat, SurfaceY + 0.01f);
		AddLine($"FreeThrowLine{SideName(side)}", new Vector3(0, 0, freeThrowZ), PaintWidth, LineWidth, _lineMat, SurfaceY + 0.01f);
	}

	private void AddHoopLines(int side, float halfL)
	{
		Vector3 hoop = new(0, 0, side * HoopZ);
		float freeThrowZ = side * (halfL - PaintLength);

		AddArc($"ThreePoint{SideName(side)}", hoop, ThreePointRadius, LineWidth, 96, side, _lineMat, SurfaceY + 0.012f);
		AddArc($"FreeThrowArc{SideName(side)}", new Vector3(0, 0, freeThrowZ), FreeThrowRadius, LineWidth, 64, -side, _lineMat, SurfaceY + 0.012f);
		AddRing($"RimMarker{SideName(side)}", hoop, 0.72f, LineWidth * 0.65f, 48, _lineMat, SurfaceY + 0.014f);
	}

	private static string SideName(int side)
	{
		return side < 0 ? "South" : "North";
	}

	private void AddRect(string name, Vector3 center, float widthX, float lengthZ, Material material, float y)
	{
		float hx = widthX * 0.5f;
		float hz = lengthZ * 0.5f;
		Vector3[] vertices =
		{
			new(center.X - hx, y, center.Z - hz),
			new(center.X + hx, y, center.Z - hz),
			new(center.X + hx, y, center.Z + hz),
			new(center.X - hx, y, center.Z + hz)
		};
		AddMesh(name, vertices, new[] { 0, 1, 2, 0, 2, 3 }, material);
	}

	private void AddLine(string name, Vector3 center, float widthX, float lengthZ, Material material, float y)
	{
		AddRect(name, center, widthX, lengthZ, material, y);
	}

	private void AddRing(string name, Vector3 center, float radius, float width, int segments, Material material, float y)
	{
		AddArcMesh(name, center, radius, width, segments, 0.0f, Mathf.Tau, material, y);
	}

	private void AddArc(string name, Vector3 center, float radius, float width, int segments, int side, Material material, float y)
	{
		float start = side < 0 ? 0.0f : Mathf.Pi;
		float end = side < 0 ? Mathf.Pi : Mathf.Tau;
		AddArcMesh(name, center, radius, width, segments, start, end, material, y);
	}

	private void AddArcFill(string name, Vector3 center, float radius, int side, Material material, float y)
	{
		float start = side < 0 ? 0.0f : Mathf.Pi;
		float end = side < 0 ? Mathf.Pi : Mathf.Tau;
		int segments = 40;
		Vector3[] vertices = new Vector3[segments + 2];
		int[] indices = new int[segments * 3];

		vertices[0] = new Vector3(center.X, y, center.Z);
		for (int i = 0; i <= segments; i++)
		{
			float angle = Mathf.Lerp(start, end, (float)i / segments);
			vertices[i + 1] = new Vector3(center.X + Mathf.Cos(angle) * radius, y, center.Z + Mathf.Sin(angle) * radius);
		}

		int idx = 0;
		for (int i = 0; i < segments; i++)
		{
			indices[idx++] = 0;
			indices[idx++] = i + 1;
			indices[idx++] = i + 2;
		}

		AddMesh(name, vertices, indices, material);
	}

	private void AddArcMesh(string name, Vector3 center, float radius, float width, int segments, float start, float end, Material material, float y)
	{
		float halfWidth = width * 0.5f;
		int vertCount = segments + 1;
		Vector3[] vertices = new Vector3[vertCount * 2];
		int[] indices = new int[segments * 6];

		for (int i = 0; i <= segments; i++)
		{
			float angle = Mathf.Lerp(start, end, (float)i / segments);
			float cos = Mathf.Cos(angle);
			float sin = Mathf.Sin(angle);

			vertices[i] = new Vector3(center.X + cos * (radius + halfWidth), y, center.Z + sin * (radius + halfWidth));
			vertices[vertCount + i] = new Vector3(center.X + cos * (radius - halfWidth), y, center.Z + sin * (radius - halfWidth));
		}

		int idx = 0;
		for (int i = 0; i < segments; i++)
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

		AddMesh(name, vertices, indices, material);
	}

	private void AddMesh(string name, Vector3[] vertices, int[] indices, Material material)
	{
		Vector3[] normals = new Vector3[vertices.Length];
		Vector2[] uvs = new Vector2[vertices.Length];
		for (int i = 0; i < vertices.Length; i++)
		{
			normals[i] = Vector3.Up;
			uvs[i] = new Vector2(vertices[i].X, vertices[i].Z);
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		var instance = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			MaterialOverride = material
		};
		AddChild(instance);
	}
}
