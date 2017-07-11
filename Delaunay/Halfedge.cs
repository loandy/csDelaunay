using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	public class Halfedge {
		#region Pool
		private static Queue<Halfedge> pool = new Queue<Halfedge>();

		public static Halfedge Create(Edge edge, LR lr) {
			if (pool.Count > 0) {
				return pool.Dequeue().Init(edge, lr);
			} else {
				return new Halfedge(edge, lr);
			}
		}
		public static Halfedge CreateDummy() {
			return Create(null, null);
		}
		#endregion

		#region Object
		public Halfedge EdgeListLeftNeighbor { get; set; }
		public Halfedge EdgeListRightNeighbor { get; set; }
		public Halfedge NextInPriorityQueue { get; set; }
		public Edge Edge { get; set; }
		public LR LeftRight { get; set; }
		public Vertex Vertex { get; set; }
		// The vertex's y-coordinate in the transformed Voronoi space V.
		public float Ystar { get; set; }

		public Halfedge(Edge edge, LR lr) {
			this.Init(edge, lr);
		}

		private Halfedge Init(Edge edge, LR lr) {
			this.Edge = edge;
			this.LeftRight = lr;
			this.NextInPriorityQueue = null;
			this.Vertex = null;

			return this;
		}

		public void Dispose() {
			if (this.EdgeListLeftNeighbor != null || this.EdgeListRightNeighbor != null) {
				// Still in EdgeList.
				return;
			}
			if (NextInPriorityQueue != null) {
				// Still in PriorityQueue.
				return;
			}
			this.Edge = null;
			this.LeftRight = null;
			this.Vertex = null;
			Halfedge.pool.Enqueue(this);
		}

		public void ReallyDispose() {
			this.EdgeListLeftNeighbor = null;
			this.EdgeListRightNeighbor = null;
			this.NextInPriorityQueue = null;
			this.Edge = null;
			this.LeftRight = null;
			this.Vertex = null;
			Halfedge.pool.Enqueue(this);
		}

		public bool IsLeftOf(Vector2f p) {
			Site topSite;
			bool rightOfSite, above, fast;
			float dxp, dyp, dxs, t1, t2, t3, y1;

			topSite = this.Edge.RightSite;
			rightOfSite = p.x > topSite.X;
			if (rightOfSite && this.LeftRight == LR.LEFT) {
				return true;
			}
			if (!rightOfSite && this.LeftRight == LR.RIGHT) {
				return false;
			}

			if (this.Edge.a == 1) {
				dyp = p.y - topSite.Y;
				dxp = p.x - topSite.X;
				fast = false;
				if ((!rightOfSite && this.Edge.b < 0) || (rightOfSite && this.Edge.b >= 0)) {
					above = dyp >= this.Edge.b * dxp;
					fast = above;
				} else {
					above = p.x + p.y * this.Edge.b > this.Edge.c;
					if (this.Edge.b < 0) {
						above = !above;
					}
					if (!above) {
						fast = true;
					}
				}
				if (!fast) {
					dxs = topSite.X - this.Edge.LeftSite.X;
					above = this.Edge.b * (dxp * dxp - dyp * dyp) < dxs * dyp * (1+2 * dxp/dxs +
						this.Edge.b * this.Edge.b);
					if (this.Edge.b < 0) {
						above = !above;
					}
				}
			} else {
				y1 = this.Edge.c - this.Edge.a * p.x;
				t1 = p.y - y1;
				t2 = p.x - topSite.X;
				t3 = y1 - topSite.Y;
				above = t1 * t1 > t2 * t2 + t3 * t3;
			}
			return this.LeftRight == LR.LEFT ? above : !above;
		}

		public override string ToString() {
			return "Halfedge (LeftRight: " + this.LeftRight + "; vertex: " + this.Vertex + ")";
		}
		#endregion
	}
}
