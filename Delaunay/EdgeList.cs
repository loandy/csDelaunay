using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	public class EdgeList {
		// Attributes.
		private float deltaX;
		private float xmin;
		private int hashSize;
		private Halfedge[] hash;

		// Properties.
		public Halfedge LeftEnd { get; private set; }
		public Halfedge RightEnd { get; private set; }

		// Methods.
		public EdgeList(float xmin, float deltaX, int sqrtSitesNb) {
			this.xmin = xmin;
			this.deltaX = deltaX;
			this.hashSize = 2 * sqrtSitesNb;

			this.hash = new Halfedge[this.hashSize];

			// Two dummy Halfedges:
			this.LeftEnd = Halfedge.CreateDummy();
			this.RightEnd = Halfedge.CreateDummy();
			this.LeftEnd.EdgeListLeftNeighbor = null;
			this.LeftEnd.EdgeListRightNeighbor = this.RightEnd;
			this.RightEnd.EdgeListLeftNeighbor = this.LeftEnd;
			this.RightEnd.EdgeListRightNeighbor = null;
			this.hash[0] = this.LeftEnd;
			this.hash[hashSize - 1] = this.RightEnd;
		}

		/*
		 * Insert newHalfedge to the right of lb
		 * @param lb
		 * @param newHalfedge
		 */
		public void Insert(Halfedge lb, Halfedge newHalfedge) {
			newHalfedge.EdgeListLeftNeighbor = lb;
			newHalfedge.EdgeListRightNeighbor = lb.EdgeListRightNeighbor;
			lb.EdgeListRightNeighbor.EdgeListLeftNeighbor = newHalfedge;
			lb.EdgeListRightNeighbor = newHalfedge;
		}

		/*
		 * This function only removes the Halfedge from the left-right list.
		 * We cannot dispose it yet because we are still using it.
		 * @param halfEdge
		 */
		public void Remove(Halfedge halfedge) {
			halfedge.EdgeListLeftNeighbor.EdgeListRightNeighbor = halfedge.EdgeListRightNeighbor;
			halfedge.EdgeListRightNeighbor.EdgeListLeftNeighbor = halfedge.EdgeListLeftNeighbor;
			halfedge.Edge = Edge.DELETED;
			halfedge.EdgeListLeftNeighbor = halfedge.EdgeListRightNeighbor = null;
		}

		public void Dispose() {
			Halfedge halfedge = this.LeftEnd;
			Halfedge prevHe;
			while (halfedge != this.RightEnd) {
				prevHe = halfedge;
				halfedge = halfedge.EdgeListRightNeighbor;
				prevHe.Dispose();
			}
			this.LeftEnd = null;
			this.RightEnd.Dispose();
			this.RightEnd = null;

			this.hash = null;
		}

		/*
		 * Find the rightmost Halfedge that is still elft of p
		 * @param p
		 * @return
		 */
		public Halfedge EdgeListLeftNeighbor(Vector2f p) {
			int bucket;
			Halfedge halfedge;

			// Use hash table to get close to desired halfedge
			bucket = (int)((p.x - xmin) / deltaX * hashSize);
			if (bucket < 0) {
				bucket = 0;
			}
			if (bucket >= this.hashSize) {
				bucket = this.hashSize - 1;
			}
			halfedge = this.GetHash(bucket);
			if (halfedge == null) {
				for (int i = 0; true; i++) {
					if ((halfedge = this.GetHash(bucket - i)) != null) {
						break;
					}
					if ((halfedge = this.GetHash(bucket + i)) != null) {
						break;
					}
				}
			}
			// Now search linear list of haledges for the correct one
			if (halfedge == this.LeftEnd || (halfedge != this.RightEnd && halfedge.IsLeftOf(p))) {
				do {
					halfedge = halfedge.EdgeListRightNeighbor;
				} while (halfedge != this.RightEnd && halfedge.IsLeftOf(p));
				halfedge = halfedge.EdgeListLeftNeighbor;
			} else {
				do {
					halfedge = halfedge.EdgeListLeftNeighbor;
				} while (halfedge != this.LeftEnd && !halfedge.IsLeftOf(p));
			}

			// Update hash table and reference counts
			if (bucket > 0 && bucket < this.hashSize - 1) {
				this.hash[bucket] = halfedge;
			}

			return halfedge;
		}

		// Get entry from the has table, pruning any deleted nodes
		private Halfedge GetHash(int b) {
			Halfedge halfedge;

			if (b < 0 || b >= this.hashSize) {
				return null;
			}

			halfedge = this.hash[b];
			if (halfedge != null && halfedge.Edge == Edge.DELETED) {
				// Hash table points to deleted halfedge. Patch as necessary.
				this.hash[b] = null;
				// Still can't dispose halfedge yet!
				return null;
			} else {
				return halfedge;
			}
		}
	}
}
