using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbCommon
{
    /// <summary>
    ///     The interface for classes implementing directed graphs. A given vvertex is only
    ///     in the graph once, now matter how many times it has been added.
    ///     Membership tests are based on Object.Equals.
    /// </summary>
    public interface IGraph : ICollection
    {
        /// <summary>
        ///     Add a vertex. Note that this differs from IList in not returning
        ///     a value, because a graph has no meaningful index. If the value is already in the
        ///     graph, this method may replace it or do nothing at the discretion of the
        ///     implementation.
        /// </summary>
        /// <param name="value">The value to add</param>
        void Add(object value);

        /// <summary>
        ///     Remove a vertex from the graph. Any edges from this vertex are
        ///     removed as well. If the item is not in the graph, this
        ///     method does nothing.
        /// </summary>
        /// <param name="value">The item to remove</param>
        void Remove(object value);

        /// <summary>
        ///     Determine whether the graph contains the vertex requested.
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>true if the value is in the collection, false otherwise</returns>
        bool Contains(object value);

        /// <summary>
        ///     Clear the set to the empty set.
        /// </summary>
        void Clear();

        /// <summary>
        ///     Add an edge from vertex source to vertex target. If either of
        ///     these vertices are not in the collection, or if the edge already exits,
        ///     this method does nothing.
        /// </summary>
        /// <param name="source">The vertex this edge is traveling from</param>
        /// <param name="target">The vertex this edge is traveling to</param>
        void EdgeAdd(object source, object target);

        /// <summary>
        ///     Removes the edge from vertex source to vertex target. If this edge
        ///     does not exist, this method does nothing.
        ///     <param name="source">The vertex this edge is traveling from</param>
        ///     <param name="target">The vertex this edge is traveling to</param>
        /// </summary>
        void EdgeRemove(object source, object target);

        /// <summary>
        ///     Determine whether the graph contains the edge requested.
        /// </summary>
        /// <param name="source">The vertex this edge is traveling from</param>
        /// <param name="target">The vertex this edge is traveling to</param>
        /// <returns>true if the edge is defined, false otherwise</returns>
        bool EdgeExists(object source, object target);

        /// <summary>
        ///     Retrieves the set of vertices that are targets of edges
        ///     from the given vertex. Returns null if the given vertex is
        ///     not in the collection.
        /// </summary>
        /// <param name="value">The vertex to find neighbors of</param>
        /// <returns>a set of vertices</returns>
        ISet Neighbors(object value);
    }
}