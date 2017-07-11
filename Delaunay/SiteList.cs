using System;
using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	public class SiteList {
		private int currentIndex;
		private bool sorted;

		public List<Site> Sites {
			get; set;
		}

		public int Count {
			get {
				return Sites.Count;
			}
		}

		public SiteList() {
			this.Sites = new List<Site>();
			this.sorted = false;
		}

		public void Dispose() {
			this.Sites.Clear();
		}

		public void PrepForUpdate() {
			foreach (Site site in this.Sites) {
				site.PrepForUpdate();
			}
			this.ResetListIndex();
		}

		public int Add(Site site) {
			this.sorted = false;
			this.Sites.Add(site);
			return this.Sites.Count;
		}

		public Site Next() {
			if (!sorted) {
				throw new Exception("SiteList.Next(): sites have not been sorted");
			}
			if (currentIndex < Sites.Count) {
				return Sites[currentIndex++];
			} else {
				return null;
			}
		}

		public List<Vector2f> SiteCoords() {
			List<Vector2f> coords = new List<Vector2f>();
			foreach (Site site in Sites) {
				coords.Add(site.Coord);
			}

			return coords;
		}

		public Rectf GetSitesBounds() {
			if (!sorted) {
				SortList();
				ResetListIndex();
			}
			float xmin, xmax, ymin, ymax;
			if (Sites.Count == 0) {
				return Rectf.ZERO;
			}
			xmin = float.MaxValue;
			xmax = float.MinValue;
			foreach (Site site in Sites) {
				if (site.X < xmin) xmin = site.X;
				if (site.X > xmax) xmax = site.X;
			}
			// here's where we assume that the sites have been sorted on y:
			ymin = Sites[0].Y;
			ymax = Sites[Sites.Count - 1].Y;

			return new Rectf(xmin, ymin, xmax - xmin, ymax - ymin);
		}

		/*
		 *
		 * @return the largest circle centered at each site that fits in its region;
		 * if the region is infinite, return a circle of radius 0.
		 */
		public List<Circle> Circles() {
			List<Circle> circles = new List<Circle>();
			foreach (Site site in Sites) {
				float radius = 0;
				Edge nearestEdge = site.NearestEdge();

				if (!nearestEdge.IsPartOfConvexHull) {
					radius = nearestEdge.SitesDistance * 0.5f;
				}
				circles.Add(new Circle(site.X, site.Y, radius));
			}
			return circles;
		}

		public List<List<Vector2f>> Regions(Rectf plotBounds) {
			List<List<Vector2f>> regions = new List<List<Vector2f>>();
			foreach (Site site in Sites) {
				regions.Add(site.Region(plotBounds));
			}
			return regions;
		}

		public void ResetListIndex() {
			currentIndex = 0;
		}

		public void SortList() {
			Site.SortSites(Sites);
			sorted = true;
		}
	}
}