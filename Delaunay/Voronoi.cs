using System;
using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	public class Voronoi {
		private SiteList sitelist;
		private List<Edge> edges;
		private List<Triangle> triangles;
		private System.Random weigthDistributor;

		// TODO generalize this so it doesn't have to be a rectangle;
		// then we can make the fractal voronois-within-voronois
		public Rectf PlotBounds { get; set; }
		public Dictionary<Vector2f, Site> SitesIndexedByLocation { get; private set; }

		public List<Site> Sites {
			get {
				return this.sitelist.Sites;
			}
		}

		public static int CompareByYThenX(Site s1, Site s2) {
			if (s1.Y < s2.Y) return -1;
			if (s1.Y > s2.Y) return 1;
			if (s1.X < s2.X) return -1;
			if (s1.X > s2.X) return 1;
			return 0;
		}

		public static int CompareByYThenX(Site s1, Vector2f s2) {
			if (s1.Y < s2.y) return -1;
			if (s1.Y > s2.y) return 1;
			if (s1.X < s2.x) return -1;
			if (s1.X > s2.x) return 1;
			return 0;
		}

		public Voronoi(List<Vector2f> points, Rectf plotBounds) {
			this.weigthDistributor = new System.Random();
			this.Init(points, plotBounds);
		}

		public Voronoi(List<Vector2f> points, Rectf plotBounds, int lloydIterations) {
			this.weigthDistributor = new System.Random();
			this.Init(points, plotBounds);
			this.LloydRelaxation(lloydIterations);
		}

		public void Dispose() {
			this.sitelist.Dispose();
			this.sitelist = null;

			foreach (Triangle t in this.triangles) {
				t.Dispose();
			}
			this.triangles.Clear();

			foreach (Edge e in this.edges) {
				e.Dispose();
			}
			this.edges.Clear();

			this.PlotBounds = Rectf.ZERO;
			this.SitesIndexedByLocation.Clear();
			this.SitesIndexedByLocation = null;
		}

		public List<Vector2f> Region(Vector2f p) {
			Site site;
			if (this.SitesIndexedByLocation.TryGetValue(p, out site)) {
				return site.Region(this.PlotBounds);
			} else {
				return new List<Vector2f>();
			}
		}

		public List<Vector2f> NeighborSitesForSite(Vector2f coord) {
			List<Vector2f> points = new List<Vector2f>();
			Site site;
			if (this.SitesIndexedByLocation.TryGetValue(coord, out site)) {
				List<Site> sites = site.NeighborSites();
				foreach (Site neighbor in sites) {
					points.Add(neighbor.Coord);
				}
			}

			return points;
		}

		public List<LineSegment> VoronoiBoundaryForSite(Vector2f coord) {
			return LineSegment.VisibleLineSegments(Edge.SelectEdgesForSitePoint(coord, edges));
		}

		/*
		public List<LineSegment> DelaunayLinesForSite(Vector2f coord) {
			return DelaunayLinesForEdges(Edge.SelectEdgesForSitePoint(coord, edges));
		}*/

		public List<LineSegment> VoronoiDiagram() {
			return LineSegment.VisibleLineSegments(edges);
		}

		/*
		public List<LineSegment> Hull() {
			return DelaunayLinesForEdges(HullEdges());
		}*/

		public List<Edge> HullEdges() {
			return this.edges.FindAll(edge => edge.IsPartOfConvexHull);
		}

		public List<Vector2f> HullPointsInOrder() {
			List<Edge> hullEdges = HullEdges();

			List<Vector2f> points = new List<Vector2f>();
			if (hullEdges.Count == 0) {
				return points;
			}

			EdgeReorderer reorderer = new EdgeReorderer(hullEdges, typeof(Site));
			hullEdges = reorderer.Edges;
			List<LR> orientations = reorderer.EdgeOrientations;
			reorderer.Dispose();

			LR orientation;
			for (int i = 0; i < hullEdges.Count; i++) {
				Edge edge = hullEdges[i];
				orientation = orientations[i];
				points.Add(edge.Site(orientation).Coord);
			}
			return points;
		}

		public List<List<Vector2f>> Regions() {
			return this.sitelist.Regions(this.PlotBounds);
		}

		public List<Circle> Circles() {
			return this.sitelist.Circles();
		}

		public List<Vector2f> SiteCoords() {
			return this.sitelist.SiteCoords();
		}

		public void Update() {
			this.PrepForUpdate();
			this.FortunesAlgorithm();
		}

		private void FortunesAlgorithm() {
			Site newSite, bottomSite, topSite, tempSite;
			Vertex v, vertex;
			Vector2f newIntStar = Vector2f.ZERO;
			LR leftRight;
			Halfedge lbnd, rbnd, llbnd, rrbnd, bisector;
			Edge edge;

			Rectf dataBounds = this.sitelist.GetSitesBounds();

			int sqrtSitesNb = (int)Math.Sqrt(sitelist.Count + 4);
			HalfedgePriorityQueue heap = new HalfedgePriorityQueue(dataBounds.Y, dataBounds.Height, sqrtSitesNb);
			EdgeList edgeList = new EdgeList(dataBounds.X, dataBounds.Width, sqrtSitesNb);
			List<Halfedge> halfEdges = new List<Halfedge>();
			List<Vertex> vertices = new List<Vertex>();

			Site bottomMostSite = this.sitelist.Next();
			newSite = this.sitelist.Next();

			while (true) {
				if (!heap.Empty) {
					newIntStar = heap.Min();
				}

				if (newSite != null && (heap.Empty || CompareByYThenX(newSite, newIntStar) < 0)) {
					// Step 8:
					lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coord); // The halfedge just to the left of newSite.
					rbnd = lbnd.EdgeListRightNeighbor; // The halfedge just to the right.
					bottomSite = RightRegion(lbnd, bottomMostSite); // This is the same as leftRegion(rbnd).

					// Step 9:
					edge = Edge.CreateBisectingEdge(bottomSite, newSite);
					edges.Add(edge);

					bisector = Halfedge.Create(edge, LR.LEFT);
					halfEdges.Add(bisector);
					// Inserting two halfedges into edgelist constitutes Step 10:
					// Insert bisector to the right of lbnd.
					edgeList.Insert(lbnd, bisector);

					// First half of Step 11:
					if ((vertex = Vertex.Intersect(lbnd, bisector)) != null) {
						vertices.Add(vertex);
						heap.Remove(lbnd);
						lbnd.Vertex = vertex;
						lbnd.Ystar = vertex.Y + newSite.Dist(vertex);
						heap.Insert(lbnd);
					}

					lbnd = bisector;
					bisector = Halfedge.Create(edge, LR.RIGHT);
					halfEdges.Add(bisector);
					// Second halfedge for Step 10:
					// Insert bisector to the right of lbnd.
					edgeList.Insert(lbnd, bisector);

					// Second half of Step 11:
					if ((vertex = Vertex.Intersect(bisector, rbnd)) != null) {
						vertices.Add(vertex);
						bisector.Vertex = vertex;
						bisector.Ystar = vertex.Y + newSite.Dist(vertex);
						heap.Insert(bisector);
					}

					newSite = sitelist.Next();
				} else if (!heap.Empty) {
					// Intersection is smallest.
					lbnd = heap.ExtractMin();
					llbnd = lbnd.EdgeListLeftNeighbor;
					rbnd = lbnd.EdgeListRightNeighbor;
					rrbnd = rbnd.EdgeListRightNeighbor;
					bottomSite = LeftRegion(lbnd, bottomMostSite);
					topSite = RightRegion(rbnd, bottomMostSite);
					// These three sites define a Delaunay triangle
					// (not actually using these for anything...)
					// triangles.Add(new Triangle(bottomSite, topSite, RightRegion(lbnd, bottomMostSite)));

					v = lbnd.Vertex;
					v.SetIndex();
					lbnd.Edge.SetVertex(lbnd.LeftRight, v);
					rbnd.Edge.SetVertex(rbnd.LeftRight, v);
					edgeList.Remove(lbnd);
					heap.Remove(rbnd);
					edgeList.Remove(rbnd);
					leftRight = LR.LEFT;
					if (bottomSite.Y > topSite.Y) {
						tempSite = bottomSite;
						bottomSite = topSite;
						topSite = tempSite;
						leftRight = LR.RIGHT;
					}
					edge = Edge.CreateBisectingEdge(bottomSite, topSite);
					edges.Add(edge);
					bisector = Halfedge.Create(edge, leftRight);
					halfEdges.Add(bisector);
					edgeList.Insert(llbnd, bisector);
					edge.SetVertex(LR.Other(leftRight), v);
					if ((vertex = Vertex.Intersect(llbnd, bisector)) != null) {
						vertices.Add(vertex);
						heap.Remove(llbnd);
						llbnd.Vertex = vertex;
						llbnd.Ystar = vertex.Y + bottomSite.Dist(vertex);
						heap.Insert(llbnd);
					}
					if ((vertex = Vertex.Intersect(bisector, rrbnd)) != null) {
						vertices.Add(vertex);
						bisector.Vertex = vertex;
						bisector.Ystar = vertex.Y + bottomSite.Dist(vertex);
						heap.Insert(bisector);
					}
				} else {
					break;
				}
			}

			// Heap should be empty now.
			heap.Dispose();
			edgeList.Dispose();

			foreach (Halfedge halfedge in halfEdges) {
				halfedge.ReallyDispose();
			}
			halfEdges.Clear();

			// We need the vertices to clip the edges.
			foreach (Edge e in edges) {
				e.ClipVertices(this.PlotBounds);
			}
		}

		public void LloydRelaxation(int nbIterations) {
			// Reapeat the whole process for the number of iterations asked.
			for (int i = 0; i < nbIterations; i++) {
				List<Vector2f> newPoints = new List<Vector2f>();
				// Go thourgh all sites
				this.sitelist.ResetListIndex();
				Site site = this.sitelist.Next();

				while (site != null) {
					// Loop all corners of the site to calculate the centroid.
					List<Vector2f> region = site.Region(this.PlotBounds);
					if (region.Count < 1) {
						site = sitelist.Next();
						continue;
					}

					Vector2f centroid = Vector2f.ZERO;
					float signedArea = 0;
					float x0 = 0;
					float y0 = 0;
					float x1 = 0;
					float y1 = 0;
					float a = 0;
					// For all vertices except last.
					for (int j = 0; j < region.Count-1; j++) {
						x0 = region[j].x;
						y0 = region[j].y;
						x1 = region[j+1].x;
						y1 = region[j+1].y;
						a = x0*y1 - x1*y0;
						signedArea += a;
						centroid.x += (x0 + x1)*a;
						centroid.y += (y0 + y1)*a;
					}
					// Do last vertex.
					x0 = region[region.Count-1].x;
					y0 = region[region.Count-1].y;
					x1 = region[0].x;
					y1 = region[0].y;
					a = x0*y1 - x1*y0;
					signedArea += a;
					centroid.x += (x0 + x1)*a;
					centroid.y += (y0 + y1)*a;

					signedArea *= 0.5f;
					centroid.x /= (6*signedArea);
					centroid.y /= (6*signedArea);
					// Move site to the centroid of its Voronoi cell.
					newPoints.Add(centroid);
					site = this.sitelist.Next();
				}

				// Recompute the Voronoi diagram.
				Rectf origPlotBounds = this.PlotBounds;
				this.Dispose();
				this.Init(newPoints, origPlotBounds);
			}
		}

		private void Init(List<Vector2f> points, Rectf plotBounds) {
			this.sitelist = new SiteList();
			this.SitesIndexedByLocation = new Dictionary<Vector2f, Site>();
			this.AddSites(points);
			this.PlotBounds = plotBounds;
			this.triangles = new List<Triangle>();
			this.edges = new List<Edge>();

			FortunesAlgorithm();
		}

		private void PrepForUpdate() {
			this.sitelist.PrepForUpdate();
			this.sitelist.SortList();
			this.SitesIndexedByLocation.Clear();
			this.IndexSites(this.sitelist.Sites);
			this.triangles.Clear();
			this.edges.Clear();
		}

		private void AddSites(List<Vector2f> points) {
			for (int i = 0; i < points.Count; i++) {
				AddSite(points[i], i);
			}
		}

		private void AddSite(Vector2f p, int index) {
			float weigth = (float)weigthDistributor.NextDouble() * 100;
			Site site = Site.Create(p, index, weigth);
			this.sitelist.Add(site);
			this.SitesIndexedByLocation[p] = site;
		}

		private void IndexSites(List<Site> sites) {
			foreach (Site site in sites) {
				this.SitesIndexedByLocation[site.Coord] = site;
			}
		}

		private Site LeftRegion(Halfedge he, Site bottomMostSite) {
			Edge edge = he.Edge;
			if (edge == null) {
				return bottomMostSite;
			}
			return edge.Site(he.LeftRight);
		}

		private Site RightRegion(Halfedge he, Site bottomMostSite) {
			Edge edge = he.Edge;
			if (edge == null) {
				return bottomMostSite;
			}
			return edge.Site(LR.Other(he.LeftRight));
		}
	}
}
