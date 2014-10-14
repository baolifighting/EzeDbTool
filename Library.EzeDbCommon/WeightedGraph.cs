using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbCommon
{
    /// <summary>
    ///     A simple graph with weighted edges. Written with sparse graphs in mind,
    ///     makes heavy use of sets rather then an adjacency matrix to keep track of
    ///     edges. Includes an implementation of Dijkstra's algorithm to find
    ///     shortest paths from one vertex to another. No direct access is given to
    ///     the edges, enumeration enumerates vertices.
    /// </summary>
    public class WeightedGraph : IGraph
    {
        #region Fields

        private const int _defaultCapaticy = 16;
        private const int _defaultWeight = 1;
        private readonly Hashtable _edgeSets;
        private readonly HashSet _vertices;
        private Hashtable _graphDistances;

        #endregion Fields

        #region Lifetime

        public WeightedGraph()
            : this(_defaultCapaticy)
        {
        }

        public WeightedGraph(int capacity)
        {
            _vertices = new HashSet(capacity);
            _edgeSets = new Hashtable(capacity);
            _graphDistances = null;
        }

        public WeightedGraph(ICollection collection)
            : this(collection.Count)
        {
            foreach (object obj in collection)
            {
                Add(obj);
            }
        }

        #endregion Lifetime

        #region Methods

        /// <summary>
        ///     Add an edge from vertex source to vertex target. If either of
        ///     these vertices are not in the collection, or if the edge already exits
        ///     with the same weight, this method does nothing. If the edge exists
        ///     with a different weight, the edge will be overwritten with the new weight.
        ///     This method will throw an ArgumentOutOfRangeException if fed a
        ///     negative weight value.
        /// </summary>
        /// <param name="source">The vertex this edge is traveling from</param>
        /// <param name="target">The vertex this edge is traveling to</param>
        /// <param name="weight">The weight/cost of using this edge to travel from source to target</param>
        public void EdgeAdd(object source, object target, int weight)
        {
            if (weight < 0)
            {
                throw new ArgumentOutOfRangeException("weight", weight, "EdgeAdd: negative weight value");
            }

            if (Contains(source) && Contains(target))
            {
                if (!_edgeSets.Contains(source))
                {
                    _edgeSets.Add(source, new ArrayListSet());
                }

                WeightedEdge oldEdge = null;
                foreach (WeightedEdge edge in ((ArrayListSet)_edgeSets[source]))
                {
                    if (edge.TargetVertex.Equals(target))
                    {
                        if (edge.Weight == weight)
                        {
                            return;
                        }
                        oldEdge = edge;
                    }
                }

                ((ArrayListSet)_edgeSets[source]).Remove(oldEdge);
                var newEdge = new WeightedEdge(target, weight);
                ((ArrayListSet)_edgeSets[source]).Add(newEdge);
                _graphDistances = null;
            }
        }

        /// <summary>
        ///     Returns the weight of the edge between the source and target.
        ///     If the edge doesn't exist, if will return -1.
        /// </summary>
        /// <param name="source">The vertex this edge is traveling from</param>
        /// <param name="target">The vertex this edge is traveling to</param>
        /// <returns>the weight of the specified edge</returns>
        public int EdgeWeight(object source, object target)
        {
            int weight = -1;

            if (Contains(source) && _edgeSets.Contains(source))
            {
                foreach (WeightedEdge edge in (ArrayListSet)_edgeSets[source])
                {
                    if (edge.TargetVertex.Equals(target))
                    {
                        weight = edge.Weight;
                        break;
                    }
                }
            }

            return weight;
        }

        /// <summary>
        ///     Return the shortest distance between two vertices, or -1 if no
        ///     such path exists.
        /// </summary>
        /// <param name="source">The vertex to start from</param>
        /// <param name="target">The vertex end at</param>
        /// <returns>the sum of the weights of all the edges that define the shortest path</returns>
        public int ShortestDistance(object source, object target)
        {
            int distance = -1;

            if (Contains(source) && Contains(target))
            {
                GraphDistance graphDistance = GenerateDistance(source);
                distance = (int)graphDistance.Distance[target];
            }

            return distance;
        }

        /// <summary>
        ///     Returns a list of vertices that make up the shortest path between
        ///     two vertices.  The head of the list is the given source vertex,
        ///     and the tail is the given target vertex. Returns null if no such
        ///     path exists.
        /// </summary>
        /// <param name="source">The vertex to start from</param>
        /// <param name="target">The vertex end at</param>
        /// <returns>a list of the vertices that make up the shortest path</returns>
        public IList ShortestPath(object source, object target)
        {
            if (ShortestDistance(source, target) == -1)
            {
                return null;
            }

            var invertedShortestPath = new ArrayList();

            object vertexOnPath = target;
            invertedShortestPath.Add(target);

            GraphDistance graphDistance = GenerateDistance(source);

            while (!vertexOnPath.Equals(source))
            {
                vertexOnPath = graphDistance.Predecessor[vertexOnPath];
                invertedShortestPath.Add(vertexOnPath);
            }

            Array shortestPath = Array.CreateInstance(typeof(object), invertedShortestPath.Count);
            invertedShortestPath.CopyTo(shortestPath);
            Array.Reverse(shortestPath);

            return shortestPath;
        }

        #endregion Methods

        #region Private Implementation

        private GraphDistance GenerateDistance(object startVertex)
        {
            if (_graphDistances == null)
            {
                _graphDistances = new Hashtable(Count);
            }

            if (!_graphDistances.Contains(startVertex))
            {
                var unAllowedVertices = new HashSet(Count);
                var distanceObject = new GraphDistance(Count);
                foreach (object vertex in _vertices)
                {
                    distanceObject.Distance.Add(vertex, -1);
                    distanceObject.Predecessor.Add(vertex, null);
                    unAllowedVertices.Add(vertex);
                }

                distanceObject.Distance[startVertex] = 0;

                for (int allowedSize = 1; allowedSize <= Count; ++allowedSize)
                {
                    object next = null;
                    foreach (object vertex in unAllowedVertices)
                    {
                        if (next == null || (int)distanceObject.Distance[next] == -1 ||
                            ((int)distanceObject.Distance[vertex] != -1 &&
                             (int)distanceObject.Distance[next] > (int)distanceObject.Distance[vertex]))
                        {
                            next = vertex;
                        }
                    }

                    Debug.Assert(next != null, "WeightedGraph.GenerateDistance: next is null");
                    unAllowedVertices.Remove(next);

                    if ((int)distanceObject.Distance[next] != -1)
                    {
                        foreach (object vertex in unAllowedVertices)
                        {
                            if (EdgeExists(next, vertex))
                            {
                                int edgeWeight = EdgeWeight(next, vertex);
                                Debug.Assert(edgeWeight != -1, "WeightedGraph.GenerateDistance: edgeWeight == -1");
                                int sum = (int)distanceObject.Distance[next] + edgeWeight;
                                if ((int)distanceObject.Distance[vertex] == -1 ||
                                    sum < (int)distanceObject.Distance[vertex])
                                {
                                    distanceObject.Distance[vertex] = sum;
                                    distanceObject.Predecessor[vertex] = next;
                                }
                            }
                        }
                    }
                }

                _graphDistances.Add(startVertex, distanceObject);
            }

            return (GraphDistance)_graphDistances[startVertex];
        }

        #endregion Private Implementation

        #region IGraph Members

        public void Add(object value)
        {
            if (!Contains(value))
            {
                _vertices.Add(value);
                _graphDistances = null;
            }
        }

        public void Remove(object value)
        {
            if (_edgeSets.Contains(value))
            {
                _edgeSets.Remove(value);
            }

            //TODO: this sucks.
            foreach (ArrayListSet edgeSet in _edgeSets.Values)
            {
                WeightedEdge edgeToRemove = null;
                foreach (WeightedEdge edge in edgeSet)
                {
                    if (edge.TargetVertex.Equals(value))
                    {
                        edgeToRemove = edge;
                        break;
                    }
                }

                if (edgeToRemove != null)
                {
                    edgeSet.Remove(edgeToRemove);
                }
            }

            _vertices.Remove(value);
            _graphDistances = null;
        }

        public bool Contains(object value)
        {
            return _vertices.Contains(value);
        }

        public void Clear()
        {
            _edgeSets.Clear();
            _vertices.Clear();
            _graphDistances = null;
        }

        public void EdgeAdd(object source, object target)
        {
            EdgeAdd(source, target, _defaultWeight);
        }

        public void EdgeRemove(object source, object target)
        {
            if (_edgeSets.Contains(source))
            {
                WeightedEdge toBeRemoved = null;
                foreach (WeightedEdge edge in ((ArrayListSet)_edgeSets[source]))
                {
                    if (edge.TargetVertex.Equals(target))
                    {
                        toBeRemoved = edge;
                        break;
                    }
                }
                if (toBeRemoved != null)
                {
                    ((ArrayListSet)_edgeSets[source]).Remove(toBeRemoved);
                    _graphDistances = null;
                }
            }
        }

        public bool EdgeExists(object source, object target)
        {
            if (_edgeSets.Contains(source))
            {
                foreach (WeightedEdge edge in ((ArrayListSet)_edgeSets[source]))
                {
                    if (edge.TargetVertex.Equals(target))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public ISet Neighbors(object value)
        {
            if (Contains(value))
            {
                var neighborsSet = new ArrayListSet();

                if (_edgeSets.Contains(value))
                {
                    foreach (WeightedEdge edge in ((ArrayListSet)_edgeSets[value]))
                    {
                        neighborsSet.Add(edge.TargetVertex);
                    }
                }

                return neighborsSet;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region ICollection Members

        public bool IsSynchronized
        {
            get { return false; }
        }

        public void CopyTo(Array array, int index)
        {
            _vertices.CopyTo(array, index);
        }

        public object SyncRoot
        {
            get { return _vertices.SyncRoot; }
        }

        public int Count
        {
            get { return _vertices.Count; }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return _vertices.GetEnumerator();
        }

        #endregion

        #region Supporting Classes

        protected sealed class GraphDistance
        {
            #region Fields

            public Hashtable Distance;
            public Hashtable Predecessor;

            #endregion Fields

            #region Lifetime

            public GraphDistance(int vertexCount)
            {
                Distance = new Hashtable(vertexCount);
                Predecessor = new Hashtable(vertexCount);
            }

            #endregion Lifetime
        }

        protected sealed class WeightedEdge
        {
            #region Fields

            public readonly object TargetVertex;
            public readonly int Weight;

            #endregion Fields

            #region Lifetime

            public WeightedEdge(object target, int weight)
            {
                TargetVertex = target;
                Weight = weight;
            }

            #endregion Lifetime
        }

        #endregion Supporting Classes
    }
}