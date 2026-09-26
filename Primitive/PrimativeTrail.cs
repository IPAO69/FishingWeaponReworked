using System;
using System.Collections.Generic;
using System.Linq;
using FishyFishy.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static FishyFishy.Helpers.FishyUtils;

namespace FishyFishy.Primitive
{
	public delegate float TrailWidthFunction(float factorAlongTrail);

	public delegate Color TrailColorFunction(float factorAlongTrail);

	public class PrimitiveTrail : PrimitiveShape
	{
		internal readonly int maxPointCount;

		/* A---B---C
		 * |  /|  /|
		 * D / E / F
		 * |/  |/  |
		 * G---H---I
		 *
		 * Let D, E, F, etc. be the set of n points that define the trail.
		 * Since each point generates 2 vertices, there are 2n vertices, plus the tip's count.
		 *
		 * As for indices - in the region between 2 defining points there are 2 triangles.
		 * The amount of regions in the whole trail are given by n - 1, so there are 2(n - 1) triangles for n points.
		 * Finally, since each triangle is defined by 3 indices, there are 6(n - 1) indices, plus the tip's count.
		 */
		public override int VertexCount => (maxPointCount * 2) + tip.ExtraVertices;
		public override int IndexCount => (6 * (maxPointCount - 1)) + tip.ExtraIndices;

		public override bool InvalidForDrawing => Positions == null;

		internal readonly ITrailTip tip;

		internal readonly TrailWidthFunction trailWidthFunction;

		internal readonly TrailColorFunction trailColorFunction;

		/// <summary>
		/// Array of positions that define the trail. NOTE: Positions[Positions.Length - 1] is assumed to be the start (e.g. Projectile.Center) and Positions[0] is assumed to be the end.
		/// </summary>
		public Vector2[] Positions {
			get => positions;
			set {
				if (value.Length != maxPointCount)
				{
					throw new ArgumentException("Array of positions was a different length than the expected result!");
				}

				positions = value;
			}
		}

		private Vector2[] positions;

		/// <summary>
		/// Used in order to calculate the normal from the frontmost position, because there isn't a point after it in the original list.
		/// This is only necessary if the trail has a tip
		/// </summary>
		public Vector2 NextPosition {
			get;
			set;
		}

		protected const float defaultWidth = 16;

		public PrimitiveTrail(int maxPointCount, TrailWidthFunction trailWidthFunction, TrailColorFunction trailColorFunction, ITrailTip tip = null)
		{
			this.tip = tip ?? new NoTip();

			this.maxPointCount = maxPointCount;

			this.trailWidthFunction = trailWidthFunction;
			this.trailColorFunction = trailColorFunction;

			InitializePrimitives();
		}

		public override void GenerateMesh(out VertexPositionColorTexture[] vertices, out short[] indices)
		{
			vertices = new VertexPositionColorTexture[maxPointCount * 2];
			indices = new short[maxPointCount * 6 - 6];

			// k = 0 indicates starting at the end of the trail (furthest from the origin of it).
			for (int k = 0; k < Positions.Length; k++)
			{
				// 1 at k = Positions.Length - 1 (start) and 0 at k = 0 (end).
				float factorAlongTrail = (float)k / (Positions.Length - 1);

				// Uses the trail width function to decide the width of the trail at this point (if no function, use defaultWidth)
				float width = trailWidthFunction?.Invoke(factorAlongTrail) ?? defaultWidth;

				Vector2 current = Positions[k];
				Vector2 next = (k == Positions.Length - 1 ? Positions[Positions.Length - 1] + (Positions[Positions.Length - 1] - Positions[Positions.Length - 2]) : Positions[k + 1]);

				Vector2 normalToNext = (next - current).SafeNormalize(Vector2.Zero);
				Vector2 normalPerp = normalToNext.RotatedBy(MathHelper.PiOver2);

				/* A
				 * |
				 * B---D
				 * |
				 * C
				 *
				 * Let B be the current point and D be the next one.
				 * A and C are calculated based on the perpendicular vector to the normal from B to D, scaled by the desired width calculated earlier.
				 */

				Vector2 a = current + (normalPerp * width);
				Vector2 c = current - (normalPerp * width);

				/* Texture coordinates are calculated such that the top-left is (0, 0) and the bottom-right is (1, 1).
				 * To achieve this, we consider the Y-coordinate of A to be 0 and that of C to be 1, while the X-coordinate is just the factor along the trail.
				 * This results in the point last in the trail having an X-coordinate of 0, and the first one having a Y-coordinate of 1.
				 */
				Vector2 texCoordA = new Vector2(factorAlongTrail, 0);
				Vector2 texCoordC = new Vector2(factorAlongTrail, 1);

				// Calculates the color for each vertex based on its texture coordinates. This acts like a very simple shader (for more complex effects you can use the actual shader).
				Color color = trailColorFunction?.Invoke(factorAlongTrail) ?? Color.White;

				vertices[k] = new VertexPositionColorTexture(a.Vec3(), color, texCoordA);
				vertices[k + maxPointCount] = new VertexPositionColorTexture(c.Vec3(), color, texCoordC);
			}

			/* Now, we have to loop through the indices to generate triangles.
			 * Looping to maxPointCount - 1 brings us halfway to the end; it covers the top row (excluding the last point on the top row).
			 */
			for (short k = 0; k < maxPointCount - 1; k++)
			{
				/* 0---1
				 * |  /|
				 * A / B
				 * |/  |
				 * 2---3
				 *
				 * This illustration is the most basic set of points (where n = 2).
				 * In this, we want to make triangles (2, 3, 1) and (1, 0, 2).
				 * Generalising this, if we consider A to be k = 0 and B to be k = 1, then the indices we want are going to be (k + n, k + n + 1, k + 1) and (k + 1, k, k + n)
				 */

				indices[k * 6] = (short)(k + maxPointCount);
				indices[k * 6 + 1] = (short)(k + maxPointCount + 1);
				indices[k * 6 + 2] = (short)(k + 1);
				indices[k * 6 + 3] = (short)(k + 1);
				indices[k * 6 + 4] = k;
				indices[k * 6 + 5] = (short)(k + maxPointCount);
			}
		}

		public override void SetupMesh()
		{
			GenerateMesh(out VertexPositionColorTexture[] mainVertices, out short[] mainIndices);

			// The next available index will be the next value after the count of points (starting at 0).
			int nextAvailableIndex = mainVertices.Length;
			Vector2 toNext = (NextPosition - Positions[Positions.Length - 1]).SafeNormalize(Vector2.Zero);
			tip.GenerateMesh(Positions[Positions.Length - 1], toNext, nextAvailableIndex, out VertexPositionColorTexture[] tipVertices, out short[] tipIndices, trailWidthFunction, trailColorFunction);

			primitives.SetVertices(mainVertices.FastUnion(tipVertices));
			primitives.SetIndices(mainIndices.FastUnion(tipIndices));
		}

		/// <summary>
		/// Sets the current positions of the trail to a given array of points
		/// Automatically generates more points if the provided array doesn't contain enough positions, using the provided retrieval function
		/// </summary>
		/// <param name="points">List of points the primitive trail moves across</param>
		/// <param name="retrievalFunction">Retrieval function used to generate more points to fill in the gaps</param>
		public void SetPositions(IEnumerable<Vector2> points, TrailPointRetrievalFunction retrievalFunction = null)
		{
			retrievalFunction ??= RigidPointRetreivalFunction;

			List<Vector2> trailPoints = retrievalFunction(points, maxPointCount);
			if (trailPoints.Count != maxPointCount)
				return;

			Positions = trailPoints.ToArray();
		}

		/// <summary>
		/// Just like SetPositions() but if the list of points has any zeros, it will instead make them use the earliest position thats valid in the list
		/// </summary>
		public void SetPositionsSmart(IEnumerable<Vector2> points, Vector2 fallBack, TrailPointRetrievalFunction retrievalFunction = null)
		{
			if (!points.Contains(Vector2.Zero))
			{
				SetPositions(points, retrievalFunction);
				return;
			}

			Vector2 lastValidPoint = fallBack;
			List<Vector2> pointList = new List<Vector2>();
			for (int i = points.Count() - 1; i >= 0; i--)
			{
				if (points.ElementAt(i) != Vector2.Zero)
					lastValidPoint = points.ElementAt(i);

				pointList.Add(lastValidPoint);
			}
			pointList.Reverse();

			SetPositions(pointList, retrievalFunction);
		}
	}

	public class PrimitiveSliceTrail : PrimitiveShape
	{
		internal readonly int maxPointCount;

		public override int VertexCount => (maxPointCount * 2);
		public override int IndexCount => (6 * (maxPointCount - 1));

		public override bool InvalidForDrawing => Positions == null;
		internal readonly TrailColorFunction trailColorFunction;

		/// <summary>
		/// Array of positions that define the trail. NOTE: Positions[Positions.Length - 1] is assumed to be the start (e.g. Projectile.Center) and Positions[0] is assumed to be the end.
		/// </summary>
		public Vector2[] Positions {
			get => positions;
			set {
				if (value.Length != maxPointCount * 2)
				{
					throw new ArgumentException("Array of positions was a different length than the expected result!");
				}

				positions = value;
			}
		}

		private Vector2[] positions;

		protected const float defaultWidth = 16;

		/// <summary>
		/// A primitive trail optimized for all sorts of slice trails
		/// Instead of calculating normals at each point, it takes positions two per two, one for the top and another for the bottom of the "blade"
		/// </summary>
		/// <param name="maxPointCount">This is the amount of samples it can take. Gets multiplied by two automatically</param>
		/// <param name="trailColorFunction"></param>
		public PrimitiveSliceTrail(int maxPointCount, TrailColorFunction trailColorFunction)
		{
			this.maxPointCount = maxPointCount;
			this.trailColorFunction = trailColorFunction;
			InitializePrimitives();
		}

		public override void GenerateMesh(out VertexPositionColorTexture[] vertices, out short[] indices)
		{
			vertices = new VertexPositionColorTexture[maxPointCount * 2];
			indices = new short[maxPointCount * 6 - 6];

			// k = 0 indicates starting at the end of the trail (furthest from the origin of it).
			for (int k = 0; k < Positions.Length; k += 2)
			{
				// 1 at k = Positions.Length - 1 (start) and 0 at k = 0 (end).
				float factorAlongTrail = (float)k / (Positions.Length - 1);

				Vector2 a = Positions[k];
				Vector2 c = Positions[k + 1];

				Vector2 texCoordA = new Vector2(factorAlongTrail, 0);
				Vector2 texCoordC = new Vector2(factorAlongTrail, 1);
				Color color = trailColorFunction.Invoke(factorAlongTrail);

				vertices[k / 2] = new VertexPositionColorTexture(a.Vec3(), color, texCoordA);
				vertices[k / 2 + maxPointCount] = new VertexPositionColorTexture(c.Vec3(), color, texCoordC);
			}

			/* Now, we have to loop through the indices to generate triangles.
			 * Looping to maxPointCount - 1 brings us halfway to the end; it covers the top row (excluding the last point on the top row).
			 */
			for (short k = 0; k < maxPointCount - 1; k++)
			{
				indices[k * 6] = (short)(k + maxPointCount);
				indices[k * 6 + 1] = (short)(k + maxPointCount + 1);
				indices[k * 6 + 2] = (short)(k + 1);
				indices[k * 6 + 3] = (short)(k + 1);
				indices[k * 6 + 4] = k;
				indices[k * 6 + 5] = (short)(k + maxPointCount);
			}
		}

		/// <summary>
		/// Sets the current positions of the trail to a given array of points
		/// Automatically generates more points if the provided array doesn't contain enough positions, using the provided retrieval function
		/// </summary>
		/// <param name="edgePoints">List of points that are at the edge of the slice</param>
		/// <param name="hiltPoints">List of points that are at the hilt of the slice</param>
		/// <param name="retrievalFunction">Retrieval function used to generate more points to fill in the gaps</param>
		public void SetPositions(IEnumerable<Vector2> edgePoints, IEnumerable<Vector2> hiltPoints, TrailPointRetrievalFunction retrievalFunction = null)
		{
			if (edgePoints.Count() != hiltPoints.Count())
				return;

			retrievalFunction ??= RigidPointRetreivalFunction;

			List<Vector2> trailPointsEdge = retrievalFunction(edgePoints, maxPointCount);
			List<Vector2> trailPointsHilt = retrievalFunction(hiltPoints, maxPointCount);

			if (trailPointsEdge.Count != maxPointCount)
				return;

			//Fill in a complete list that alternates between hilt and edge
			Vector2[] trailPoints = new Vector2[maxPointCount * 2];
			for (int i = 0; i < maxPointCount * 2; i++)
			{
				trailPoints[i] = i % 2 == 0 ? trailPointsEdge[i / 2] : trailPointsHilt[(i - 1) / 2];
			}

			Positions = trailPoints;
		}
	}

	#region Trail tips
	public interface ITrailTip
	{
		int ExtraVertices { get; }

		int ExtraIndices { get; }

		void GenerateMesh(Vector2 trailTipPosition, Vector2 trailTipNormal, int startFromIndex, out VertexPositionColorTexture[] vertices, out short[] indices, TrailWidthFunction trailWidthFunction, TrailColorFunction trailColorFunction);
	}

	public class NoTip : ITrailTip
	{
		public int ExtraVertices => 0;

		public int ExtraIndices => 0;

		public void GenerateMesh(Vector2 trailTipPosition, Vector2 trailTipNormal, int startFromIndex, out VertexPositionColorTexture[] vertices, out short[] indices, TrailWidthFunction trailWidthFunction, TrailColorFunction trailColorFunction)
		{
			vertices = new VertexPositionColorTexture[0];
			indices = new short[0];
		}
	}

	public class TriangularTip : ITrailTip
	{
		public int ExtraVertices => 3;

		public int ExtraIndices => 3;

		private readonly float length;

		public TriangularTip(float length)
		{
			this.length = length;
		}

		public void GenerateMesh(Vector2 trailTipPosition, Vector2 trailTipNormal, int startFromIndex, out VertexPositionColorTexture[] vertices, out short[] indices, TrailWidthFunction trailWidthFunction, TrailColorFunction trailColorFunction)
		{
			Vector2 normalPerp = trailTipNormal.RotatedBy(MathHelper.PiOver2);

			float width = trailWidthFunction?.Invoke(1) ?? 1;
			Vector2 a = trailTipPosition + (normalPerp * width);
			Vector2 b = trailTipPosition - (normalPerp * width);
			Vector2 c = trailTipPosition + (trailTipNormal * length);

			Vector2 texCoordA = Vector2.UnitX;
			Vector2 texCoordB = Vector2.One;
			Vector2 texCoordC = new Vector2(1, 0.5f);

			Color colorA = trailColorFunction?.Invoke(1) ?? Color.White;
			Color colorB = trailColorFunction?.Invoke(1) ?? Color.White;
			Color colorC = trailColorFunction?.Invoke(1) ?? Color.White;

			vertices = new VertexPositionColorTexture[]
			{
				new VertexPositionColorTexture(a.Vec3(), colorA, texCoordA),
				new VertexPositionColorTexture(b.Vec3(), colorB, texCoordB),
				new VertexPositionColorTexture(c.Vec3(), colorC, texCoordC)
			};

			indices = new short[]
			{
				(short)startFromIndex,
				(short)(startFromIndex + 1),
				(short)(startFromIndex + 2)
			};
		}
	}

	// Note: Every vertex in this tip is drawn twice, but the performance impact from this would be very little
	public class RoundedTip : ITrailTip
	{
		public int ExtraVertices => (triCount * 2) + 3;

		public int ExtraIndices => ((triCount * 2) + 1) * 3;

		private readonly int triCount;

		public RoundedTip(int triCount = 2)
		{
			this.triCount = triCount;

			if (triCount < 2)
			{
				throw new ArgumentException($"Parameter {nameof(triCount)} cannot be less than 2.");
			}
		}

		public void GenerateMesh(Vector2 trailTipPosition, Vector2 trailTipNormal, int startFromIndex, out VertexPositionColorTexture[] vertices, out short[] indices, TrailWidthFunction trailWidthFunction, TrailColorFunction trailColorFunction)
		{
			vertices = new VertexPositionColorTexture[ExtraVertices];

			Vector2 fanCenterTexCoord = new Vector2(1, 0.5f);

			vertices[0] = new VertexPositionColorTexture(trailTipPosition.Vec3(), (trailColorFunction?.Invoke(1f) ?? Color.White) * 0.75f, fanCenterTexCoord);

			List<short> indicesTemp = new List<short>();

			for (int k = 0; k <= triCount; k++)
			{
				float rotationFactor = k / (float)(triCount);

				float angle = MathHelper.PiOver2 - (rotationFactor * MathHelper.Pi);

				Vector2 circlePoint = trailTipPosition + (trailTipNormal.RotatedBy(angle) * (trailWidthFunction?.Invoke(1) ?? 1));

				Vector2 circleTexCoord = new Vector2(rotationFactor, 1);

				Color circlePointColor = (trailColorFunction?.Invoke(1f) ?? Color.White) * rotationFactor * 0.85f;

				vertices[k + 1] = new VertexPositionColorTexture(circlePoint.Vec3(), circlePointColor, circleTexCoord);

				short[] tri = new short[]
				{
					(short)startFromIndex,
					(short)(startFromIndex + k + 2),
					(short)(startFromIndex + k + 1)
				};

				indicesTemp.AddRange(tri);
			}

			for (int k = triCount + 1; k <= triCount * 2 + 1; k++)
			{
				float rotationFactor = ((k - 1) / (float)(triCount)) - 1;

				float angle = MathHelper.PiOver2 - (rotationFactor * MathHelper.Pi);

				Vector2 circlePoint = trailTipPosition + (trailTipNormal.RotatedBy(-angle) * (trailWidthFunction?.Invoke(1) ?? 1));

				Vector2 circleTexCoord = new Vector2(rotationFactor, 0);

				Color circlePointColor = ((trailColorFunction?.Invoke(1f) ?? Color.White) * rotationFactor * 0.75f);

				vertices[k + 1] = new VertexPositionColorTexture(circlePoint.Vec3(), circlePointColor, circleTexCoord);

				if (k == triCount * 2 + 1)
				{
					continue;
				}

				short[] tri = new short[]
				{
					(short)startFromIndex,
					(short)(startFromIndex + k + 1),
					(short)(startFromIndex + k + 2)
				};

				indicesTemp.AddRange(tri);
			}

			indices = indicesTemp.ToArray();
		}
	}
	#endregion
}
