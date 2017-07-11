using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	public class Triangle {
		public List<Site> Sites { get; private set; }

		public Triangle(Site a, Site b, Site c) {
			this.Sites = new List<Site>();
			this.Sites.Add(a);
			this.Sites.Add(b);
			this.Sites.Add(c);
		}

		public void Dispose() {
			this.Sites.Clear();
		}
	}
}