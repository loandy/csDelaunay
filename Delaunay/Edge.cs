using System;
using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	/*
	 * The line segment connecting the two Sites is part of the Delaunay triangulation.
	 * The line segment connecting the two Vertices is part of the Voronoi diagram.
	 */
	public class Edge {
		public static readonly Edge DELETED = new Edge();

		#region Pool
		private static Queue<Edge> pool = new Queue<Edge>();
		private static int nEdges = 0;

		/*
		 * This is the only way to create a new Edge
		 * @param site0
		 * @param site1
		 * @return
		 */
		public static Edge CreateBisectingEdge(Site s0, Site s1) {
			float dx, dy;
			float absdx, absdy;
			float a, b, c;

			dx = s1.X - s0.X;
			dy = s1.Y - s0.Y;
			absdx = dx > 0 ? dx : -dx;
			absdy = dy > 0 ? dy : -dy;
			c = s0.X * dx + s0.Y * dy + (dx*dx + dy*dy) * 0.5f;

			if (absdx > absdy) {
				a = 1;
				b = dy/dx;
				c /= dx;
			} else {
				b = 1;
				a = dx/dy;
				c/= dy;
			}

			Edge edge = Edge.Create();

			edge.LeftSite = s0;
			edge.RightSite = s1;
			s0.AddEdge(edge);
			s1.AddEdge(edge);

			edge.a = a;
			edge.b = b;
			edge.c = c;

			return edge;
		}

		private static Edge Create() {
			Edge edge;
			if (Edge.pool.Count > 0) {
				edge = Edge.pool.Dequeue().Init();
			} else {
				edge = new Edge();
			}

			return edge;
		}
		#endregion

		#region Object
		// Attributes.
		// The equation of the edge: ax + by = c
		public float a, b, c;
		private Dictionary<LR, Site> sites;

		// Properties.
		public int EdgeIndex { get; private set; }
		// The two Voronoi vertices that the edge connects (if one of them is null, the edge extends to infinity)
		public Vertex LeftVertex { get; private set; }
		public Vertex RightVertex { get; private set; }
		// Once ClipVertices() is called, this Dictionary will hold two Points
		// representing the clipped coordinates of the left and the right ends...
		public Dictionary<LR, Vector2f> ClippedVertices { get; private set; }

		// The two input Sites for which this Edge is a bisector.
		public Site LeftSite {
			get {
				return this.sites[LR.LEFT];
			} set {
				this.sites[LR.LEFT] = value;
			}
		}

		public Site RightSite {
			get {
				return this.sites[LR.RIGHT];
			} set {
				this.sites[LR.RIGHT] = value;
			}
		}

		public bool IsPartOfConvexHull {
			get {
				return this.LeftVertex == null || this.RightVertex == null;
			}
		}

		// Unless the entire Edge is outside the bounds.
		// In that case visible will be false:
		public bool Visible {
			get {
				return this.ClippedVertices != null;
			}
		}

		public float SitesDistance {
			get {
				return (this.LeftSite.Coord - this.RightSite.Coord).Magnitude;
			}
		}

		// Methods.
		public static List<Edge> SelectEdgesForSitePoint(Vector2f coord, List<Edge> edgesToTest) {
			return edgesToTest.FindAll(
			delegate(Edge e) {
				if (e.LeftSite != null) {
					if (e.LeftSite.Coord == coord) {
						return true;
					}
				}
				if (e.RightSite != null) {
					if (e.RightSite.Coord == coord) {
						return true;
					}
				}
				return false;
			});
		}

		public static int CompareSitesDistances_MAX(Edge edge0, Edge edge1) {
			float length0 = edge0.SitesDistance;
			float length1 = edge1.SitesDistance;
			if (length0 < length1) {
				return 1;
			}
			if (length0 > length1) {
				return -1;
			}
			return 0;
		}

		public static int CompareSitesDistances(Edge edge0, Edge edge1) {
			return - CompareSitesDistances_MAX(edge0, edge1);
		}

		public Edge() {
			this.EdgeIndex = Edge.nEdges++;
			this.Init();
		}

		public void Dispose() {
			this.LeftVertex = null;
			this.RightVertex = null;
			if (this.ClippedVertices != null) {
				this.ClippedVertices.Clear();
				this.ClippedVertices = null;
			}
			this.sites.Clear();
			this.sites = null;

			Edge.pool.Enqueue(this);
		}

		public Vertex Vertex(LR leftRight) {
			return leftRight == LR.LEFT ? this.LeftVertex : this.RightVertex;
		}

		public void SetVertex(LR leftRight, Vertex v) {
			if (leftRight == LR.LEFT) {
				this.LeftVertex = v;
			} else {
				this.RightVertex = v;
			}
		}

		public Site Site(LR leftRight) {
			return this.sites[leftRight];
		}

		public LineSegment DelaunayLine() {
			return new LineSegment(this.LeftSite.Coord, this.RightSite.Coord);
		}

		public LineSegment VoronoiEdge() {
			return new LineSegment(this.ClippedVertices[LR.LEFT], this.ClippedVertices[LR.RIGHT]);
		}

		/*
		 * Set clipped vertices to contain the two ends of the portion of the Voronoi edge that is
		 * visible within the bounds. If no part of the edge falls within the bounds, leave clipped
		 * vertices null.
		 * @param bounds
		 */
		public void ClipVertices(Rectf bounds) {
			float xmin = bounds.X;
			float ymin = bounds.Y;
			float xmax = bounds.Right;
			float ymax = bounds.Bottom;

			Vertex vertex0, vertex1;
			float x0, x1, y0, y1;

			if (a == 1 && b >= 0) {
				vertex0 = this.RightVertex;
				vertex1 = this.LeftVertex;
			} else {
				vertex0 = this.LeftVertex;
				vertex1 = this.RightVertex;
			}

			if (a == 1) {
				y0 = ymin;
				if (vertex0 != null && vertex0.Y > ymin) {
					y0 = vertex0.Y;
				}
				if (y0 > ymax) {
					return;
				}
				x0 = c - b * y0;

				y1 = ymax;
				if (vertex1 != null && vertex1.Y < ymax) {
					y1 = vertex1.Y;
				}
				if (y1 < ymin) {
					return;
				}
				x1 = c - b * y1;

				if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin)) {
					return;
				}

				if (x0 > xmax) {
					x0 = xmax;
					y0 = (c - x0)/b;
				} else if (x0 < xmin) {
					x0 = xmin;
					y0 = (c - x0)/b;
				}

				if (x1 > xmax) {
					x1 = xmax;
					y1 = (c - x1)/b;
				} else if (x1 < xmin) {
					x1 = xmin;
					y1 = (c - x1)/b;
				}
			} else {
				x0 = xmin;
				if (vertex0 != null && vertex0.X > xmin) {
					x0 = vertex0.X;
				}
				if (x0 > xmax) {
					return;
				}
				y0 = c - a * x0;

				x1 = xmax;
				if (vertex1 != null && vertex1.X < xmax) {
					x1 = vertex1.X;
				}
				if (x1 < xmin) {
					return;
				}
				y1 = c - a * x1;

				if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin)) {
					return;
				}

				if (y0 > ymax) {
					y0 = ymax;
					x0 = (c - y0)/a;
				} else if (y0 < ymin) {
					y0 = ymin;
					x0 = (c - y0)/a;
				}

				if (y1 > ymax) {
					y1 = ymax;
					x1 = (c - y1)/a;
				} else if (y1 < ymin) {
					y1 = ymin;
					x1 = (c - y1)/a;
				}
			}

			this.ClippedVertices = new Dictionary<LR, Vector2f>();
			if (vertex0 == this.LeftVertex) {
				this.ClippedVertices[LR.LEFT]  = new Vector2f(x0, y0);
				this.ClippedVertices[LR.RIGHT] = new Vector2f(x1, y1);
			} else {
				this.ClippedVertices[LR.RIGHT] = new Vector2f(x0, y0);
				this.ClippedVertices[LR.LEFT]  = new Vector2f(x1, y1);
			}
		}

		public override string ToString() {
			return "Edge " + this.EdgeIndex + "; sites " + this.sites[LR.LEFT] + ", " +
				this.sites[LR.RIGHT] + "; endVertices " + (this.LeftVertex != null ?
				this.LeftVertex.VertexIndex.ToString() : "null") + ", " +
				(this.RightVertex != null ? this.RightVertex.VertexIndex.ToString() : "null") +
				"::";
		}

		private Edge Init() {
			this.sites = new Dictionary<LR, Site>();
			return this;
		}
		#endregion
	}
}
