using System;
using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	public class Vertex : ICoord {
		public static readonly Vertex VERTEX_AT_INFINITY = new Vertex(float.NaN, float.NaN);

		#region Pool
		private static Queue<Vertex> pool = new Queue<Vertex>();
		private static int nVertices = 0;

		/*
		 * This is the only way to make a Vertex.
		 *
		 * @param halfedge0
		 * @param halfedge1
		 * @return
		 *
		 */
		public static Vertex Intersect(Halfedge halfedge0, Halfedge halfedge1) {
			Edge edge, edge0, edge1;
			Halfedge halfedge;
			float determinant, intersectionX, intersectionY;
			bool rightOfSite;

			edge0 = halfedge0.Edge;
			edge1 = halfedge1.Edge;
			if (edge0 == null || edge1 == null) {
				return null;
			}
			if (edge0.RightSite == edge1.RightSite) {
				return null;
			}

			determinant = edge0.a * edge1.b - edge0.b * edge1.a;
			if (Math.Abs(determinant) < 1E-10) {
				// The edges are parallel
				return null;
			}

			intersectionX = (edge0.c * edge1.b - edge1.c * edge0.b) / determinant;
			intersectionY = (edge1.c * edge0.a - edge0.c * edge1.a) / determinant;

			if (Voronoi.CompareByYThenX(edge0.RightSite, edge1.RightSite) < 0) {
				halfedge = halfedge0;
				edge = edge0;
			} else {
				halfedge = halfedge1;
				edge = edge1;
			}
			rightOfSite = intersectionX >= edge.RightSite.X;
			if ((rightOfSite && halfedge.LeftRight == LR.LEFT) ||
				(!rightOfSite && halfedge.LeftRight == LR.RIGHT)) {
				return null;
			}

			return Vertex.Create(intersectionX, intersectionY);
		}

		private static Vertex Create(float x, float y) {
			if (float.IsNaN(x) || float.IsNaN(y)) {
				return VERTEX_AT_INFINITY;
			}
			if (pool.Count > 0) {
				return pool.Dequeue().Init(x,y);
			} else {
				return new Vertex(x,y);
			}
		}
		#endregion

		#region Object
		public int VertexIndex { get; private set; }
		public Vector2f Coord { get; set; }

		public float X {
			get {
				return this.Coord.x;
			}
		}

		public float Y {
			get{
				return this.Coord.y;
			}
		}

		public Vertex(float x, float y) {
			this.Init(x, y);
		}

		private Vertex Init(float x, float y) {
			this.Coord = new Vector2f(x, y);
			return this;
		}

		public void Dispose() {
			this.Coord = Vector2f.ZERO;
			Vertex.pool.Enqueue(this);
		}

		public void SetIndex() {
			this.VertexIndex = nVertices++;
		}

		public override string ToString() {
			return "Vertex (" + this.VertexIndex + ")";
		}
		#endregion
	}
}
