using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	public class HalfedgePriorityQueue {
		private Halfedge[] hash;
		private int count;
		private int minBucked;
		private int hashSize;
		private float ymin;
		private float deltaY;

		public bool Empty {
			get {
				return this.count == 0;
			}
		}

		public HalfedgePriorityQueue(float ymin, float deltaY, int sqrtSitesNb) {
			this.ymin = ymin;
			this.deltaY = deltaY;
			this.hashSize = 4 * sqrtSitesNb;
			this.Init();
		}

		public void Dispose() {
			// Get rid of dummies.
			for (int i = 0; i < hashSize; i++) {
				this.hash[i].Dispose();
			}
			this.hash = null;
		}

		public void Insert(Halfedge halfedge) {
			Halfedge previous, next;

			int insertionBucket = Bucket(halfedge);
			if (insertionBucket < minBucked) {
				this.minBucked = insertionBucket;
			}
			previous = this.hash[insertionBucket];
			while ((next = previous.NextInPriorityQueue) != null &&
				(halfedge.Ystar > next.Ystar || (halfedge.Ystar == next.Ystar &&
				halfedge.Vertex.X > next.Vertex.X))) {
				previous = next;
			}
			halfedge.NextInPriorityQueue = previous.NextInPriorityQueue;
			previous.NextInPriorityQueue = halfedge;
			this.count++;
		}

		public void Remove(Halfedge halfedge) {
			Halfedge previous;
			int removalBucket = Bucket(halfedge);

			if (halfedge.Vertex != null) {
				previous = this.hash[removalBucket];
				while (previous.NextInPriorityQueue != halfedge) {
					previous = previous.NextInPriorityQueue;
				}
				previous.NextInPriorityQueue = halfedge.NextInPriorityQueue;
				this.count--;
				halfedge.Vertex = null;
				halfedge.NextInPriorityQueue = null;
				halfedge.Dispose();
			}
		}

		/*
		 * @return coordinates of the Halfedge's vertex in V*, the transformed Voronoi diagram
		 */
		public Vector2f Min() {
			AdjustMinBucket();
			Halfedge answer = this.hash[this.minBucked].NextInPriorityQueue;
			return new Vector2f(answer.Vertex.X, answer.Ystar);
		}

		/*
		 * Remove and return the min Halfedge
		 */
		public Halfedge ExtractMin() {
			Halfedge answer;

			// Get the first real Halfedge in minBucket
			answer = this.hash[this.minBucked].NextInPriorityQueue;

			this.hash[this.minBucked].NextInPriorityQueue = answer.NextInPriorityQueue;
			this.count--;
			answer.NextInPriorityQueue = null;

			return answer;
		}

		private void Init() {
			this.count = 0;
			this.minBucked = 0;
			this.hash = new Halfedge[hashSize];
			// Dummy halfedge at the top of each hash.
			for (int i = 0; i < hashSize; i++) {
				this.hash[i] = Halfedge.CreateDummy();
				this.hash[i].NextInPriorityQueue = null;
			}
		}

		private int Bucket(Halfedge halfedge) {
			int theBucket = (int)((halfedge.Ystar - this.ymin) / this.deltaY * this.hashSize);
			if (theBucket < 0) theBucket = 0;
			if (theBucket >= this.hashSize) theBucket = this.hashSize - 1;
			return theBucket;
		}

		private bool IsBucketEmpty(int bucket) {
			return this.hash[bucket].NextInPriorityQueue == null;
		}

		/*
		 * move minBucket until it contains an actual Halfedge (not just the dummy at the top);
		 */
		private void AdjustMinBucket() {
			while (this.minBucked < this.hashSize - 1 && this.IsBucketEmpty(this.minBucked)) {
				this.minBucked++;
			}
		}
	}
}