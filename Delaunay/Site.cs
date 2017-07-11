using System;
using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	public class Site : ICoord {
		private const float EPSILON = 0.005f;

		#region Pool
		private static Queue<Site> pool = new Queue<Site>();

		public static Site Create(Vector2f p, int index, float weigth) {
			if (Site.pool.Count > 0) {
				return Site.pool.Dequeue().Init(p, index, weigth);
			} else {
				return new Site(p, index, weigth);
			}
		}

		public static void SortSites(List<Site> sites) {
			sites.Sort(delegate(Site s0, Site s1) {
				int returnValue = Voronoi.CompareByYThenX(s0,s1);

				int tempIndex;

				if (returnValue == -1) {
					if (s0.SiteIndex > s1.SiteIndex) {
						tempIndex = s0.SiteIndex;
						s0.SiteIndex = s1.SiteIndex;
						s1.SiteIndex = tempIndex;
					}
				} else if (returnValue == 1) {
					if (s1.SiteIndex > s0.SiteIndex) {
						tempIndex = s1.SiteIndex;
						s1.SiteIndex = s0.SiteIndex;
						s0.SiteIndex = tempIndex;
					}
				}

				return returnValue;
			});
		}
		#endregion

		#region Object
		public Vector2f coord;
		// Which end of each edge hooks up with the previous edge in edges.
		public List<LR> edgeOrientations;
		// ordered list of points that define the region clipped to bounds:
		public List<Vector2f> region;

		public int SiteIndex { get; set; }
		// The edges that define this Site's Voronoi region.
		public List<Edge> Edges { get; private set; }
		public float Weigth { get; private set; }

		public Vector2f Coord {
			get {
				return this.coord;
			}
			set {
				this.coord = value;
			}
		}

		public float X {
			get {
				return this.coord.x;
			}
			set {
				this.coord.x = value;
			}
		}

		public float Y {
			get {
				return this.coord.y;
			}
			set {
				this.coord.y = value;
			}
		}

		public Site(Vector2f p, int index, float weigth) {
			this.Init(p, index, weigth);
		}

		public override string ToString() {
			return "Site " + this.SiteIndex + ": " + this.Coord;
		}

		private void Move(Vector2f p) {
			this.Clear();
			this.Coord = p;
		}

		public void Dispose() {
			this.Clear();
			Site.pool.Enqueue(this);
		}

		private void Clear() {
			if (this.Edges != null) {
				this.Edges.Clear();
				this.Edges = null;
			}
			if (edgeOrientations != null) {
				edgeOrientations.Clear();
				edgeOrientations = null;
			}
			if (region != null) {
				region.Clear();
				region = null;
			}
		}

		public void PrepForUpdate() {
			if (this.Edges != null) {
				this.Edges.Clear();
			}
			if (edgeOrientations != null) {
				edgeOrientations.Clear();
				edgeOrientations = null;
			}
			if (region != null) {
				region.Clear();
			}
		}

		public void AddEdge(Edge edge) {
			this.Edges.Add(edge);
		}

		public Edge NearestEdge() {
			this.Edges.Sort(Edge.CompareSitesDistances);
			return this.Edges[0];
		}

		public List<Site> NeighborSites() {
			if (this.Edges == null || this.Edges.Count == 0) {
				return new List<Site>();
			}
			if (edgeOrientations == null) {
				ReorderEdges();
			}
			List<Site> list = new List<Site>();
			foreach (Edge edge in this.Edges) {
				list.Add(NeighborSite(edge));
			}
			return list;
		}

		public Dictionary<Site, Edge> NeighborSiteEdges() {
			if (this.Edges == null || this.Edges.Count == 0) {
				return new Dictionary<Site, Edge>();
			}
			if (edgeOrientations == null) {
				ReorderEdges();
			}
			Dictionary<Site, Edge> list = new Dictionary<Site, Edge>();
			foreach (Edge edge in this.Edges) {
				list.Add(NeighborSite(edge), edge);
			}
			return list;
		}

		public Site NeighborSite(Edge edge) {
			if (this == edge.LeftSite) {
				return edge.RightSite;
			}
			if (this == edge.RightSite) {
				return edge.LeftSite;
			}
			return null;
		}

		public List<Vector2f> Region(Rectf clippingBounds) {
			if (this.Edges == null || this.Edges.Count == 0) {
				return new List<Vector2f>();
			}

			if (this.edgeOrientations == null) {
				this.ReorderEdges();
				this.region = this.ClipToBounds(clippingBounds);
				if ((new Polygon(this.region)).PolyWinding == Winding.CLOCKWISE) {
					this.region.Reverse();
				}
			}

			return this.region;
		}

		public float Dist(ICoord p) {
			return (this.Coord - p.Coord).Magnitude;
		}

		public int Compare(Site s1, Site s2) {
			return s1.CompareTo(s2);
		}

		public int CompareTo(Site s1) {
			int returnValue = Voronoi.CompareByYThenX(this,s1);

			int tempIndex;

			if (returnValue == -1) {
				if (this.SiteIndex > s1.SiteIndex) {
					tempIndex = this.SiteIndex;
					this.SiteIndex = s1.SiteIndex;
					s1.SiteIndex = tempIndex;
				}
			} else if (returnValue == 1) {
				if (s1.SiteIndex > this.SiteIndex) {
					tempIndex = s1.SiteIndex;
					s1.SiteIndex = this.SiteIndex;
					this.SiteIndex = tempIndex;
				}
			}

			return returnValue;
		}

		private static bool CloseEnough(Vector2f p0, Vector2f p1) {
			return (p0 - p1).Magnitude < EPSILON;
		}

		private Site Init(Vector2f p, int index, float weigth) {
			this.Coord = p;
			this.SiteIndex = index;
			this.Weigth = weigth;
			this.Edges = new List<Edge>();
			this.region = null;

			return this;
		}

		private void ReorderEdges() {
			EdgeReorderer reorderer = new EdgeReorderer(this.Edges, typeof(Vertex));
			this.Edges = reorderer.Edges;
			this.edgeOrientations = reorderer.EdgeOrientations;
			reorderer.Dispose();
		}

		private List<Vector2f> ClipToBounds(Rectf bounds) {
			List<Vector2f> points = new List<Vector2f>();
			int n = this.Edges.Count;
			int i = 0;
			Edge edge;

			// Look for the index of the first visible edge.
			while (i < n && !this.Edges[i].Visible) {
				i++;
			}

			// Reached end of edge list without encountering a visible edge.
			if (i == n) {
				// No edges visible
				return new List<Vector2f>();
			}

			edge = this.Edges[i];
			LR orientation = this.edgeOrientations[i];
			points.Add(edge.ClippedVertices[orientation]);
			points.Add(edge.ClippedVertices[LR.Other(orientation)]);

			// Continue to process any additional visible edges.
			for (int j = i + 1; j < n; j++) {
				edge = this.Edges[j];
				if (!edge.Visible) {
					continue;
				}
				this.Connect(ref points, j, bounds);
			}
			// Close up the polygon by adding another corner point of the bounds if needed:
			this.Connect(ref points, i, bounds, true);

			return points;
		}

		private void Connect(ref List<Vector2f> points, int j, Rectf bounds, bool closingUp = false) {
			Vector2f rightPoint = points[points.Count - 1];
			Edge newEdge = this.Edges[j];
			LR newOrientation = this.edgeOrientations[j];

			// The point that must be conected to rightPoint:
			Vector2f newPoint = newEdge.ClippedVertices[newOrientation];

			if (!Site.CloseEnough(rightPoint, newPoint)) {
				// The points do not coincide, so they must have been clipped at the bounds;
				// see if they are on the same border of the bounds:
				if (rightPoint.x != newPoint.x && rightPoint.y != newPoint.y) {
					// They are on different borders of the bounds;
					// insert one or two corners of bounds as needed to hook them up:
					// (NOTE this will not be correct if the region should take up more than
					// half of the bounds rect, for then we will have gone the wrong way
					// around the bounds and included the smaller part rather than the larger)
					int rightCheck = BoundsCheck.Check(rightPoint, bounds);
					int newCheck = BoundsCheck.Check(newPoint, bounds);
					float px, py;
					if ((rightCheck & BoundsCheck.RIGHT) != 0) {
						px = bounds.Right;

						if ((newCheck & BoundsCheck.BOTTOM) != 0) {
							py = bounds.Bottom;
							points.Add(new Vector2f(px, py));

						} else if ((newCheck & BoundsCheck.TOP) != 0) {
							py = bounds.Top;
							points.Add(new Vector2f(px, py));

						} else if ((newCheck & BoundsCheck.LEFT) != 0) {
							if (rightPoint.y - bounds.Y + newPoint.y - bounds.Y < bounds.Height) {
								py = bounds.Top;
							} else {
								py = bounds.Bottom;
							}
							points.Add(new Vector2f(px, py));
							points.Add(new Vector2f(bounds.Left, py));
						}
					} else if ((rightCheck & BoundsCheck.LEFT) != 0) {
						px = bounds.Left;

						if ((newCheck & BoundsCheck.BOTTOM) != 0) {
							py = bounds.Bottom;
							points.Add(new Vector2f(px,py));

						} else if ((newCheck & BoundsCheck.TOP) != 0) {
							py = bounds.Top;
							points.Add(new Vector2f(px,py));

						} else if ((newCheck & BoundsCheck.RIGHT) != 0) {
							if (rightPoint.y - bounds.Y + newPoint.y - bounds.Y < bounds.Height) {
								py = bounds.Top;
							} else {
								py = bounds.Bottom;
							}
							points.Add(new Vector2f(px, py));
							points.Add(new Vector2f(bounds.Right, py));
						}
					} else if ((rightCheck & BoundsCheck.TOP) != 0) {
						py = bounds.Top;

						if ((newCheck & BoundsCheck.RIGHT) != 0) {
							px = bounds.Right;
							points.Add(new Vector2f(px, py));

						} else if ((newCheck & BoundsCheck.LEFT) != 0) {
							px = bounds.Left;
							points.Add(new Vector2f(px, py));

						} else if ((newCheck & BoundsCheck.BOTTOM) != 0) {
							if (rightPoint.x - bounds.X + newPoint.x - bounds.X < bounds.Width) {
								px = bounds.Left;
							} else {
								px = bounds.Right;
							}
							points.Add(new Vector2f(px, py));
							points.Add(new Vector2f(px, bounds.Bottom));
						}
					} else if ((rightCheck & BoundsCheck.BOTTOM) != 0) {
						py = bounds.Bottom;

						if ((newCheck & BoundsCheck.RIGHT) != 0) {
							px = bounds.Right;
							points.Add(new Vector2f(px, py));

						} else if ((newCheck & BoundsCheck.LEFT) != 0) {
							px = bounds.Left;
							points.Add(new Vector2f(px, py));

						} else if ((newCheck & BoundsCheck.TOP) != 0) {
							if (rightPoint.x - bounds.X + newPoint.x - bounds.X < bounds.Width) {
								px = bounds.Left;
							} else {
								px = bounds.Right;
							}
							points.Add(new Vector2f(px, py));
							points.Add(new Vector2f(px, bounds.Top));
						}
					}
				}
				if (closingUp) {
					// newEdge's ends have already been added
					return;
				}
				points.Add(newPoint);
			}
			Vector2f newRightPoint = newEdge.ClippedVertices[LR.Other(newOrientation)];
			if (!Site.CloseEnough(points[0], newRightPoint)) {
				points.Add(newRightPoint);
			}
		}
		#endregion
	}

	public class BoundsCheck {
		public const int TOP = 1;
		public const int BOTTOM = 2;
		public const int LEFT = 4;
		public const int RIGHT = 8;

		/*
		 *
		 * @param point
		 * @param bounds
		 * @return an int with the appropriate bits set if the Point lies on the corresponding bounds lines
		 */
		public static int Check(Vector2f point, Rectf bounds) {
			int value = 0;
			if (point.x == bounds.Left) {
				value |= LEFT;
			}
			if (point.x == bounds.Right) {
				value |= RIGHT;
			}
			if (point.y == bounds.Top) {
				value |= TOP;
			}
			if (point.y == bounds.Bottom) {
				value |= BOTTOM;
			}

			return value;
		}
	}
}